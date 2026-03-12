#ifndef IFR_SIM_TYPES_H
#define IFR_SIM_TYPES_H

#include <cmath>
#include <string>

// Unit conversion constants (from SkyGuard)
constexpr double FEET_TO_METERS = 0.3048;
constexpr double METERS_TO_FEET = 3.28084;
constexpr double KNOTS_TO_MPS = 0.514444;
constexpr double MPS_TO_KNOTS = 1.94384;
constexpr double NM_TO_METERS = 1852.0;
constexpr double METERS_TO_NM = 0.000539957;
constexpr double DEG_TO_RAD = M_PI / 180.0;
constexpr double RAD_TO_DEG = 180.0 / M_PI;

// Earth radius in meters (for flat-earth approximation)
constexpr double EARTH_RADIUS_M = 6371000.0;

// Vec3 structure (from SkyGuard types.hpp)
struct Vec3 {
    double x, y, z;

    Vec3() : x(0), y(0), z(0) {}
    Vec3(double x_, double y_, double z_) : x(x_), y(y_), z(z_) {}

    Vec3 operator+(const Vec3& other) const {
        return Vec3(x + other.x, y + other.y, z + other.z);
    }

    Vec3 operator-(const Vec3& other) const {
        return Vec3(x - other.x, y - other.y, z - other.z);
    }

    Vec3 operator*(double scalar) const {
        return Vec3(x * scalar, y * scalar, z * scalar);
    }

    double magnitude() const {
        return std::sqrt(x * x + y * y + z * z);
    }

    Vec3 normalized() const {
        double mag = magnitude();
        if (mag > 0) {
            return Vec3(x / mag, y / mag, z / mag);
        }
        return Vec3();
    }
};

// Geographic position
struct GeoPosition {
    double latitude;   // degrees
    double longitude;  // degrees
    double altitude;   // feet

    GeoPosition() : latitude(0), longitude(0), altitude(0) {}
    GeoPosition(double lat, double lon, double alt)
        : latitude(lat), longitude(lon), altitude(alt) {}
};

// Aircraft state
struct AircraftState {
    GeoPosition position;
    double heading;        // degrees (0-360)
    double speed;          // knots
    double verticalSpeed;  // feet per minute
    double bankAngle;      // degrees
    double targetHeading;  // degrees
    double targetAltitude; // feet

    AircraftState()
        : heading(0), speed(0), verticalSpeed(0),
          bankAngle(0), targetHeading(0), targetAltitude(0) {}
};

// VOR station
struct VORStation {
    std::string identifier;
    double latitude;    // degrees
    double longitude;   // degrees
    double frequency;   // MHz

    VORStation() : latitude(0), longitude(0), frequency(0) {}
    VORStation(const std::string& id, double lat, double lon, double freq)
        : identifier(id), latitude(lat), longitude(lon), frequency(freq) {}
};

// VOR indication (what instruments show)
struct VORIndication {
    double cdiDeflection;  // -1.0 to +1.0 (full scale)
    int toFromFlag;        // 0 = OFF, 1 = TO, 2 = FROM
    double bearingToStation;  // degrees
    double distanceNM;     // nautical miles
    bool valid;            // true if tuned to valid VOR
};

// Scoring data
struct TrackingData {
    bool isTracking;
    double totalDeviation;
    double maxDeviation;
    double timeOnRadial;  // seconds within tolerance
    double totalTime;     // total tracking time
    int samples;

    TrackingData()
        : isTracking(false), totalDeviation(0), maxDeviation(0),
          timeOnRadial(0), totalTime(0), samples(0) {}

    void reset() {
        isTracking = false;
        totalDeviation = 0;
        maxDeviation = 0;
        timeOnRadial = 0;
        totalTime = 0;
        samples = 0;
    }

    double averageDeviation() const {
        return samples > 0 ? totalDeviation / samples : 0;
    }
};

// TO/FROM flag values
enum ToFromFlag {
    FLAG_OFF = 0,
    FLAG_TO = 1,
    FLAG_FROM = 2
};

#endif // IFR_SIM_TYPES_H
