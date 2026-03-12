cmake_minimum_required(VERSION 3.15)
project(ifr_sim VERSION 1.0.0 LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_POSITION_INDEPENDENT_CODE ON)

# Source files
set(SOURCES
    src/ifr_sim.cpp
    src/flight_model.cpp
    src/vor_navigation.cpp
)

# Create shared library
add_library(ifr_sim SHARED ${SOURCES})

# Include directories
target_include_directories(ifr_sim PRIVATE ${CMAKE_CURRENT_SOURCE_DIR}/src)

# Platform-specific settings
if(APPLE)
    set_target_properties(ifr_sim PROPERTIES
        SUFFIX ".dylib"
        INSTALL_RPATH "@loader_path"
    )
elseif(WIN32)
    set_target_properties(ifr_sim PROPERTIES
        SUFFIX ".dll"
        WINDOWS_EXPORT_ALL_SYMBOLS ON
    )
else()
    set_target_properties(ifr_sim PROPERTIES
        SUFFIX ".so"
    )
endif()

# Output directory
set_target_properties(ifr_sim PROPERTIES
    LIBRARY_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib
    RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib
)

# Install target
install(TARGETS ifr_sim
    LIBRARY DESTINATION lib
    RUNTIME DESTINATION lib
)
