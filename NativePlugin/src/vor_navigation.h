#ifndef IFR_SIM_VOR_NAVIGATION_H
#define IFR_SIM_VOR_NAVIGATION_H

#include "types.h"
#include <vector>
#include <string>

class VORNavigation {
public:
    VORNavigation();

    // VOR station management
    int addStation(const std::string& id, double lat, double lon, double freqMHz);
    void clearStations();
    const VORStation* getStationByFrequency(double freq) const;
    const VORStation* getStationById(const std::string& id) const;

    // NAV receiver
    void tuneFrequency(double freqMHz);
    void setOBS(double radial);
    double getTunedFrequency() const { return tunedFrequency_; }
    double getSelectedRadial() const { return selectedRadial_; }

    // Calculate VOR indication for given aircraft position
    VORIndication calculateIndication(const GeoPosition& aircraftPos) const;

    // Scoring/tracking
    void startTracking();
    void stopTracking();
    void updateTracking(const VORIndication& indication, double dt);
    const TrackingData& getTrackingData() const { return trackingData_; }

    // Static helper functions (exposed for testing)
    static double calculateBearing(double lat1, double lon1, double lat2, double lon2);
    static double calculateDistance(double lat1, double lon1, double lat2, double lon2);
    static double normalizeAngle(double angle);
    static double angleDifference(double angle1, double angle2);

private:
    std::vector<VORStation> stations_;
    double tunedFrequency_;
    double selectedRadial_;  // OBS setting
    TrackingData trackingData_;

    static constexpr double CDI_FULL_SCALE_DEG = 10.0;  // 10 degrees = full scale deflection
    static constexpr double RADIAL_TOLERANCE_DEG = 2.0; // degrees for "on radial" tracking
};

#endif // IFR_SIM_VOR_NAVIGATION_H
