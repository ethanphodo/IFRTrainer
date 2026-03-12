#include "flight_model.h"
#include <cmath>
#include <algorithm>

FlightModel::FlightModel() {
    state_ = AircraftState();
}

void FlightModel::initialize(double lat, double lon, double altFeet,
                             double headingDeg, double speedKnots) {
    state_.position = GeoPosition(lat, lon, altFeet);
    state_.heading = normalizeHeading(headingDeg);
    state_.speed = speedKnots;
    state_.verticalSpeed = 0;
    state_.bankAngle = 0;
    state_.targetHeading = state_.heading;
    state_.targetAltitude = altFeet;
}

void FlightModel::update(double dt) {
    updateHeading(dt);
    updateAltitude(dt);
    updatePosition(dt);
}

void FlightModel::setTargetHeading(double heading) {
    state_.targetHeading = normalizeHeading(heading);
}

void FlightModel::setTargetAltitude(double altitude) {
    state_.targetAltitude = altitude;
}

double FlightModel::normalizeHeading(double heading) {
    while (heading < 0) heading += 360.0;
    while (heading >= 360.0) heading -= 360.0;
    return heading;
}

double FlightModel::shortestTurnDirection(double current, double target) {
    // Returns positive for right turn, negative for left turn
    double diff = target - current;

    // Normalize to [-180, 180]
    while (diff > 180.0) diff -= 360.0;
    while (diff < -180.0) diff += 360.0;

    return diff;
}

void FlightModel::updateHeading(double dt) {
    double headingError = shortestTurnDirection(state_.heading, state_.targetHeading);

    // Calculate desired bank angle based on heading error
    double desiredBank = 0;
    if (std::abs(headingError) > 1.0) {
        // Bank proportional to error, capped at standard rate
        desiredBank = std::clamp(headingError * 0.5, -MAX_BANK_ANGLE, MAX_BANK_ANGLE);
    }

    // Smoothly transition bank angle
    double bankError = desiredBank - state_.bankAngle;
    double bankChange = std::clamp(bankError, -BANK_RATE * dt, BANK_RATE * dt);
    state_.bankAngle += bankChange;

    // Turn rate based on bank angle (simplified formula)
    // Standard rate turn (3 deg/sec) at ~25 degrees bank at 150 knots
    double turnRate = state_.bankAngle * (STANDARD_RATE_TURN / MAX_BANK_ANGLE);

    // Update heading
    state_.heading += turnRate * dt;
    state_.heading = normalizeHeading(state_.heading);

    // Snap to target if very close and not banking
    if (std::abs(headingError) < 0.5 && std::abs(state_.bankAngle) < 1.0) {
        state_.heading = state_.targetHeading;
        state_.bankAngle = 0;
    }
}

void FlightModel::updateAltitude(double dt) {
    double altError = state_.targetAltitude - state_.position.altitude;

    // Calculate desired vertical speed
    double desiredVS = 0;
    if (std::abs(altError) > 50) {
        if (altError > 0) {
            desiredVS = CLIMB_RATE;
        } else {
            desiredVS = -DESCENT_RATE;
        }
    }

    // Smoothly transition vertical speed
    double vsError = desiredVS - state_.verticalSpeed;
    double vsChange = std::clamp(vsError, -VS_RATE * dt, VS_RATE * dt);
    state_.verticalSpeed += vsChange;

    // Update altitude (convert fpm to feet per second)
    state_.position.altitude += state_.verticalSpeed * dt / 60.0;

    // Snap to target if very close
    if (std::abs(altError) < 20 && std::abs(state_.verticalSpeed) < 50) {
        state_.position.altitude = state_.targetAltitude;
        state_.verticalSpeed = 0;
    }
}

void FlightModel::updatePosition(double dt) {
    // Convert speed from knots to meters per second
    double speedMps = state_.speed * KNOTS_TO_MPS;

    // Calculate velocity components (heading 0 = north)
    double headingRad = state_.heading * DEG_TO_RAD;
    double vNorth = speedMps * std::cos(headingRad);  // meters per second north
    double vEast = speedMps * std::sin(headingRad);   // meters per second east

    // Calculate position change in meters
    double dNorth = vNorth * dt;
    double dEast = vEast * dt;

    // Convert to lat/lon changes (flat earth approximation)
    // At equator: 1 degree latitude = 111km, longitude varies with cos(lat)
    double latRad = state_.position.latitude * DEG_TO_RAD;
    double metersPerDegreeLat = 111320.0;  // approximate
    double metersPerDegreeLon = 111320.0 * std::cos(latRad);

    state_.position.latitude += dNorth / metersPerDegreeLat;
    state_.position.longitude += dEast / metersPerDegreeLon;
}
