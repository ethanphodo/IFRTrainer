#include "types.h"
#include "flight_model.h"
#include "vor_navigation.h"
#include <memory>
#include <cstring>

// Global simulation state
static std::unique_ptr<FlightModel> g_flightModel;
static std::unique_ptr<VORNavigation> g_vorNav;
static bool g_initialized = false;

// Export macros for cross-platform compatibility
#ifdef _WIN32
    #define EXPORT extern "C" __declspec(dllexport)
#else
    #define EXPORT extern "C" __attribute__((visibility("default")))
#endif

// ============================================================================
// Lifecycle functions
// ============================================================================

EXPORT int ifr_sim_init(double lat, double lon, double alt, double heading, double speed) {
    if (g_initialized) {
        return 0;  // Already initialized
    }

    g_flightModel = std::make_unique<FlightModel>();
    g_vorNav = std::make_unique<VORNavigation>();

    g_flightModel->initialize(lat, lon, alt, heading, speed);
    g_initialized = true;

    return 1;  // Success
}

EXPORT void ifr_sim_update(float dt) {
    if (!g_initialized || !g_flightModel || !g_vorNav) {
        return;
    }

    g_flightModel->update(dt);

    // Update tracking if active
    if (g_vorNav->getTrackingData().isTracking) {
        VORIndication indication = g_vorNav->calculateIndication(g_flightModel->getPosition());
        g_vorNav->updateTracking(indication, dt);
    }
}

EXPORT void ifr_sim_shutdown() {
    g_flightModel.reset();
    g_vorNav.reset();
    g_initialized = false;
}

EXPORT int ifr_sim_is_initialized() {
    return g_initialized ? 1 : 0;
}

// ============================================================================
// Aircraft Control
// ============================================================================

EXPORT void ifr_set_target_heading(float heading) {
    if (g_flightModel) {
        g_flightModel->setTargetHeading(heading);
    }
}

EXPORT void ifr_set_target_altitude(float altitude) {
    if (g_flightModel) {
        g_flightModel->setTargetAltitude(altitude);
    }
}

// ============================================================================
// Aircraft State
// ============================================================================

EXPORT void ifr_get_position(double* lat, double* lon, float* alt) {
    if (g_flightModel && lat && lon && alt) {
        GeoPosition pos = g_flightModel->getPosition();
        *lat = pos.latitude;
        *lon = pos.longitude;
        *alt = static_cast<float>(pos.altitude);
    }
}

EXPORT void ifr_get_dynamics(float* hdg, float* spd, float* vspd, float* bank) {
    if (g_flightModel) {
        if (hdg) *hdg = static_cast<float>(g_flightModel->getHeading());
        if (spd) *spd = static_cast<float>(g_flightModel->getSpeed());
        if (vspd) *vspd = static_cast<float>(g_flightModel->getVerticalSpeed());
        if (bank) *bank = static_cast<float>(g_flightModel->getBankAngle());
    }
}

EXPORT double ifr_get_heading() {
    if (g_flightModel) {
        return g_flightModel->getHeading();
    }
    return 0;
}

EXPORT double ifr_get_speed() {
    if (g_flightModel) {
        return g_flightModel->getSpeed();
    }
    return 0;
}

EXPORT double ifr_get_altitude() {
    if (g_flightModel) {
        return g_flightModel->getPosition().altitude;
    }
    return 0;
}

// ============================================================================
// VOR Navigation
// ============================================================================

EXPORT int ifr_add_vor(const char* id, double lat, double lon, float freq) {
    if (g_vorNav && id) {
        return g_vorNav->addStation(std::string(id), lat, lon, freq);
    }
    return -1;
}

EXPORT void ifr_clear_vors() {
    if (g_vorNav) {
        g_vorNav->clearStations();
    }
}

EXPORT void ifr_tune_nav(float frequency) {
    if (g_vorNav) {
        g_vorNav->tuneFrequency(frequency);
    }
}

EXPORT void ifr_set_obs(float radial) {
    if (g_vorNav) {
        g_vorNav->setOBS(radial);
    }
}

EXPORT float ifr_get_obs() {
    if (g_vorNav) {
        return static_cast<float>(g_vorNav->getSelectedRadial());
    }
    return 0;
}

EXPORT float ifr_get_tuned_frequency() {
    if (g_vorNav) {
        return static_cast<float>(g_vorNav->getTunedFrequency());
    }
    return 0;
}

EXPORT void ifr_get_vor_indication(float* cdi, int* toFrom, float* bearing, float* dist) {
    if (!g_vorNav || !g_flightModel) {
        if (cdi) *cdi = 0;
        if (toFrom) *toFrom = FLAG_OFF;
        if (bearing) *bearing = 0;
        if (dist) *dist = 0;
        return;
    }

    VORIndication indication = g_vorNav->calculateIndication(g_flightModel->getPosition());

    if (cdi) *cdi = static_cast<float>(indication.cdiDeflection);
    if (toFrom) *toFrom = indication.toFromFlag;
    if (bearing) *bearing = static_cast<float>(indication.bearingToStation);
    if (dist) *dist = static_cast<float>(indication.distanceNM);
}

EXPORT int ifr_is_vor_valid() {
    if (!g_vorNav || !g_flightModel) {
        return 0;
    }
    VORIndication indication = g_vorNav->calculateIndication(g_flightModel->getPosition());
    return indication.valid ? 1 : 0;
}

// ============================================================================
// Scoring / Tracking
// ============================================================================

EXPORT void ifr_start_tracking() {
    if (g_vorNav) {
        g_vorNav->startTracking();
    }
}

EXPORT void ifr_stop_tracking(float* avgDev, float* maxDev, float* timeOnRadial) {
    if (!g_vorNav) {
        if (avgDev) *avgDev = 0;
        if (maxDev) *maxDev = 0;
        if (timeOnRadial) *timeOnRadial = 0;
        return;
    }

    g_vorNav->stopTracking();
    const TrackingData& data = g_vorNav->getTrackingData();

    if (avgDev) *avgDev = static_cast<float>(data.averageDeviation());
    if (maxDev) *maxDev = static_cast<float>(data.maxDeviation);
    if (timeOnRadial) *timeOnRadial = static_cast<float>(data.timeOnRadial);
}

EXPORT void ifr_get_tracking_stats(float* avgDev, float* maxDev, float* timeOnRadial, float* totalTime) {
    if (!g_vorNav) {
        if (avgDev) *avgDev = 0;
        if (maxDev) *maxDev = 0;
        if (timeOnRadial) *timeOnRadial = 0;
        if (totalTime) *totalTime = 0;
        return;
    }

    const TrackingData& data = g_vorNav->getTrackingData();

    if (avgDev) *avgDev = static_cast<float>(data.averageDeviation());
    if (maxDev) *maxDev = static_cast<float>(data.maxDeviation);
    if (timeOnRadial) *timeOnRadial = static_cast<float>(data.timeOnRadial);
    if (totalTime) *totalTime = static_cast<float>(data.totalTime);
}

EXPORT int ifr_is_tracking() {
    if (g_vorNav) {
        return g_vorNav->getTrackingData().isTracking ? 1 : 0;
    }
    return 0;
}

// ============================================================================
// Utility functions for testing
// ============================================================================

EXPORT double ifr_calc_bearing(double lat1, double lon1, double lat2, double lon2) {
    return VORNavigation::calculateBearing(lat1, lon1, lat2, lon2);
}

EXPORT double ifr_calc_distance(double lat1, double lon1, double lat2, double lon2) {
    return VORNavigation::calculateDistance(lat1, lon1, lat2, lon2);
}
