#include "vor_navigation.h"
#include <cmath>
#include <algorithm>

VORNavigation::VORNavigation()
    : tunedFrequency_(0), selectedRadial_(0) {
}

int VORNavigation::addStation(const std::string& id, double lat, double lon, double freqMHz) {
    stations_.emplace_back(id, lat, lon, freqMHz);
    return static_cast<int>(stations_.size() - 1);
}

void VORNavigation::clearStations() {
    stations_.clear();
}

const VORStation* VORNavigation::getStationByFrequency(double freq) const {
    for (const auto& station : stations_) {
        // Match within 0.05 MHz tolerance
        if (std::abs(station.frequency - freq) < 0.05) {
            return &station;
        }
    }
    return nullptr;
}

const VORStation* VORNavigation::getStationById(const std::string& id) const {
    for (const auto& station : stations_) {
        if (station.identifier == id) {
            return &station;
        }
    }
    return nullptr;
}

void VORNavigation::tuneFrequency(double freqMHz) {
    tunedFrequency_ = freqMHz;
}

void VORNavigation::setOBS(double radial) {
    selectedRadial_ = normalizeAngle(radial);
}

double VORNavigation::normalizeAngle(double angle) {
    while (angle < 0) angle += 360.0;
    while (angle >= 360.0) angle -= 360.0;
    return angle;
}

double VORNavigation::angleDifference(double angle1, double angle2) {
    double diff = angle1 - angle2;
    while (diff > 180.0) diff -= 360.0;
    while (diff < -180.0) diff += 360.0;
    return diff;
}

double VORNavigation::calculateBearing(double lat1, double lon1, double lat2, double lon2) {
    // Calculate bearing from point 1 to point 2
    double lat1Rad = lat1 * DEG_TO_RAD;
    double lat2Rad = lat2 * DEG_TO_RAD;
    double dLonRad = (lon2 - lon1) * DEG_TO_RAD;

    double y = std::sin(dLonRad) * std::cos(lat2Rad);
    double x = std::cos(lat1Rad) * std::sin(lat2Rad) -
               std::sin(lat1Rad) * std::cos(lat2Rad) * std::cos(dLonRad);

    double bearing = std::atan2(y, x) * RAD_TO_DEG;
    return normalizeAngle(bearing);
}

double VORNavigation::calculateDistance(double lat1, double lon1, double lat2, double lon2) {
    // Haversine formula for great circle distance
    double lat1Rad = lat1 * DEG_TO_RAD;
    double lat2Rad = lat2 * DEG_TO_RAD;
    double dLatRad = (lat2 - lat1) * DEG_TO_RAD;
    double dLonRad = (lon2 - lon1) * DEG_TO_RAD;

    double a = std::sin(dLatRad / 2) * std::sin(dLatRad / 2) +
               std::cos(lat1Rad) * std::cos(lat2Rad) *
               std::sin(dLonRad / 2) * std::sin(dLonRad / 2);
    double c = 2 * std::atan2(std::sqrt(a), std::sqrt(1 - a));

    double distanceMeters = EARTH_RADIUS_M * c;
    return distanceMeters * METERS_TO_NM;
}

VORIndication VORNavigation::calculateIndication(const GeoPosition& aircraftPos) const {
    VORIndication indication;
    indication.valid = false;
    indication.cdiDeflection = 0;
    indication.toFromFlag = FLAG_OFF;
    indication.bearingToStation = 0;
    indication.distanceNM = 0;

    // Find tuned station
    const VORStation* station = getStationByFrequency(tunedFrequency_);
    if (!station) {
        return indication;
    }

    indication.valid = true;

    // Calculate bearing from aircraft to station
    double bearingToStation = calculateBearing(
        aircraftPos.latitude, aircraftPos.longitude,
        station->latitude, station->longitude
    );
    indication.bearingToStation = bearingToStation;

    // Calculate distance to station
    indication.distanceNM = calculateDistance(
        aircraftPos.latitude, aircraftPos.longitude,
        station->latitude, station->longitude
    );

    // Calculate current radial (FROM the station)
    // Radial is the bearing FROM the station TO the aircraft
    double currentRadial = normalizeAngle(bearingToStation + 180.0);

    // Calculate CDI deflection
    // CDI shows deviation from selected radial
    // Positive deflection = fly right to intercept
    // Negative deflection = fly left to intercept
    double radialDiff = angleDifference(selectedRadial_, currentRadial);

    // Determine TO/FROM flag
    // The TO/FROM flag indicates what happens if you fly the selected OBS course:
    // - TO: Flying the OBS course (selected radial) takes you TO the station
    // - FROM: Flying the OBS course (selected radial) takes you away FROM the station
    //
    // Logic: Compare bearing TO station with the selected radial
    // If bearing to station is within 90° of the OBS setting → TO
    // If bearing to station is more than 90° from the OBS setting → FROM
    //
    // Example: Aircraft east of VOR, OBS=090
    // - Bearing to station = 270° (westward)
    // - OBS = 090° (eastward)
    // - Difference = 180° → FROM (flying 090 takes you away from station)
    //
    // Example: Aircraft east of VOR, OBS=270
    // - Bearing to station = 270°
    // - OBS = 270°
    // - Difference = 0° → TO (flying 270 takes you to station)

    double diffToBearing = std::abs(angleDifference(bearingToStation, selectedRadial_));

    if (diffToBearing <= 90.0) {
        // Flying the OBS course takes you toward the station
        indication.toFromFlag = FLAG_TO;
    } else {
        // Flying the OBS course takes you away from the station
        indication.toFromFlag = FLAG_FROM;
    }

    // CDI deflection calculation
    // Full scale deflection at CDI_FULL_SCALE_DEG degrees off course
    // For TO indication: positive diff means radial is to the right
    // For FROM indication: we're tracking outbound, same sense
    double cdi = radialDiff / CDI_FULL_SCALE_DEG;
    indication.cdiDeflection = std::clamp(cdi, -1.0, 1.0);

    return indication;
}

void VORNavigation::startTracking() {
    trackingData_.reset();
    trackingData_.isTracking = true;
}

void VORNavigation::stopTracking() {
    trackingData_.isTracking = false;
}

void VORNavigation::updateTracking(const VORIndication& indication, double dt) {
    if (!trackingData_.isTracking || !indication.valid) {
        return;
    }

    // Calculate deviation in degrees from CDI deflection
    double deviationDeg = std::abs(indication.cdiDeflection) * CDI_FULL_SCALE_DEG;

    trackingData_.totalDeviation += deviationDeg;
    trackingData_.maxDeviation = std::max(trackingData_.maxDeviation, deviationDeg);
    trackingData_.totalTime += dt;
    trackingData_.samples++;

    if (deviationDeg <= RADIAL_TOLERANCE_DEG) {
        trackingData_.timeOnRadial += dt;
    }
}
