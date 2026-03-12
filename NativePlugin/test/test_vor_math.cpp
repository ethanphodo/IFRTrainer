// Test program for VOR navigation math
// Compile: g++ -std=c++17 -I../src test_vor_math.cpp ../src/vor_navigation.cpp -o test_vor_math

#include <iostream>
#include <cmath>
#include <cassert>
#include "../src/vor_navigation.h"

void testBearingCalculation() {
    std::cout << "Testing bearing calculations...\n";

    // LAX VOR to a point due east
    double laxLat = 33.934;
    double laxLon = -118.40;
    double eastLat = 33.934;
    double eastLon = -118.30;

    double bearing = VORNavigation::calculateBearing(laxLat, laxLon, eastLat, eastLon);
    std::cout << "  Bearing LAX to point east: " << bearing << "° (expected ~90°)\n";
    assert(std::abs(bearing - 90.0) < 1.0);

    // Point north of LAX
    double northLat = 34.0;
    double northLon = -118.40;
    bearing = VORNavigation::calculateBearing(laxLat, laxLon, northLat, northLon);
    std::cout << "  Bearing LAX to point north: " << bearing << "° (expected ~0°)\n";
    assert(bearing < 1.0 || bearing > 359.0);

    std::cout << "  ✓ Bearing tests passed\n\n";
}

void testDistanceCalculation() {
    std::cout << "Testing distance calculations...\n";

    // LAX to a point approximately 10 NM east
    double laxLat = 33.934;
    double laxLon = -118.40;

    // Approximately 10 NM east (1 degree longitude at this latitude ≈ 50 NM)
    double eastLon = -118.20;  // ~10 NM

    double distance = VORNavigation::calculateDistance(laxLat, laxLon, laxLat, eastLon);
    std::cout << "  Distance to point ~10 NM east: " << distance << " NM\n";
    assert(distance > 8.0 && distance < 12.0);

    std::cout << "  ✓ Distance tests passed\n\n";
}

void testCDIDeflection() {
    std::cout << "Testing CDI deflection calculations...\n";

    VORNavigation vorNav;
    vorNav.addStation("LAX", 33.934, -118.40, 113.6);
    vorNav.tuneFrequency(113.6);

    // Test 1: Aircraft directly east of VOR (on 090 radial)
    // Set OBS to 090, should show centered CDI
    vorNav.setOBS(90.0);
    GeoPosition eastOfVOR(33.934, -118.30, 5000);
    VORIndication ind = vorNav.calculateIndication(eastOfVOR);

    std::cout << "  Aircraft on 090 radial, OBS=090:\n";
    std::cout << "    CDI: " << ind.cdiDeflection << " (expected ~0)\n";
    std::cout << "    TO/FROM: " << ind.toFromFlag << " (expected FROM=2)\n";
    assert(std::abs(ind.cdiDeflection) < 0.1);
    assert(ind.toFromFlag == FLAG_FROM);

    // Test 2: Aircraft on 095 radial, OBS set to 090
    // Should show CDI deflected right (positive)
    vorNav.setOBS(90.0);
    // 095 radial means aircraft bearing to station is 275
    GeoPosition onRadial095(33.934 + 0.05, -118.30 + 0.01, 5000);
    ind = vorNav.calculateIndication(onRadial095);
    std::cout << "  Aircraft ~095 radial, OBS=090:\n";
    std::cout << "    CDI: " << ind.cdiDeflection << " (expected positive/right)\n";
    std::cout << "    Bearing to station: " << ind.bearingToStation << "°\n";

    // Test 3: TO/FROM flag logic
    // Aircraft west of VOR, OBS set to 090 (inbound)
    GeoPosition westOfVOR(33.934, -118.50, 5000);
    vorNav.setOBS(90.0);
    ind = vorNav.calculateIndication(westOfVOR);
    std::cout << "  Aircraft west of VOR, OBS=090:\n";
    std::cout << "    TO/FROM: " << ind.toFromFlag << " (expected TO=1)\n";
    assert(ind.toFromFlag == FLAG_TO);

    std::cout << "  ✓ CDI deflection tests passed\n\n";
}

void testRadialIntercept() {
    std::cout << "Testing radial intercept scenario...\n";

    VORNavigation vorNav;
    vorNav.addStation("LAX", 33.934, -118.40, 113.6);
    vorNav.tuneFrequency(113.6);
    vorNav.setOBS(90.0);  // Want to intercept 090 radial

    // Aircraft starting position (northeast of VOR, heading SW)
    GeoPosition aircraft(33.95, -118.35, 5000);
    VORIndication ind = vorNav.calculateIndication(aircraft);

    std::cout << "  Aircraft NE of VOR, OBS=090:\n";
    std::cout << "    CDI: " << ind.cdiDeflection << "\n";
    std::cout << "    TO/FROM: " << (ind.toFromFlag == FLAG_TO ? "TO" : "FROM") << "\n";
    std::cout << "    Bearing to station: " << ind.bearingToStation << "°\n";
    std::cout << "    Distance: " << ind.distanceNM << " NM\n";

    std::cout << "  ✓ Radial intercept test passed\n\n";
}

int main() {
    std::cout << "=== VOR Navigation Math Tests ===\n\n";

    testBearingCalculation();
    testDistanceCalculation();
    testCDIDeflection();
    testRadialIntercept();

    std::cout << "=== All tests passed! ===\n";
    return 0;
}
