#ifndef IFR_SIM_FLIGHT_MODEL_H
#define IFR_SIM_FLIGHT_MODEL_H

#include "types.h"

class FlightModel {
public:
    FlightModel();

    // Initialize aircraft state
    void initialize(double lat, double lon, double altFeet,
                   double headingDeg, double speedKnots);

    // Update simulation by dt seconds
    void update(double dt);

    // Control inputs
    void setTargetHeading(double heading);
    void setTargetAltitude(double altitude);

    // State accessors
    const AircraftState& getState() const { return state_; }
    GeoPosition getPosition() const { return state_.position; }
    double getHeading() const { return state_.heading; }
    double getSpeed() const { return state_.speed; }
    double getVerticalSpeed() const { return state_.verticalSpeed; }
    double getBankAngle() const { return state_.bankAngle; }

private:
    AircraftState state_;

    // Flight model parameters
    static constexpr double STANDARD_RATE_TURN = 3.0;  // degrees per second
    static constexpr double MAX_BANK_ANGLE = 25.0;     // degrees
    static constexpr double BANK_RATE = 5.0;           // degrees per second
    static constexpr double CLIMB_RATE = 500.0;        // feet per minute
    static constexpr double DESCENT_RATE = 500.0;      // feet per minute
    static constexpr double VS_RATE = 200.0;           // fpm per second change rate

    // Helper functions
    double normalizeHeading(double heading);
    double shortestTurnDirection(double current, double target);
    void updatePosition(double dt);
    void updateHeading(double dt);
    void updateAltitude(double dt);
};

#endif // IFR_SIM_FLIGHT_MODEL_H
