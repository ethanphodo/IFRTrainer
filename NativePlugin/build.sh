#!/bin/bash

# Build script for libifr_sim native plugin
# Supports macOS, Linux, and Windows (via MinGW/MSYS2)

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BUILD_DIR="$SCRIPT_DIR/build"
OUTPUT_DIR="$SCRIPT_DIR/../Assets/Plugins"

# Detect platform
case "$(uname -s)" in
    Darwin*)
        PLATFORM="macOS"
        LIB_EXT="dylib"
        ;;
    Linux*)
        PLATFORM="Linux"
        LIB_EXT="so"
        ;;
    MINGW*|MSYS*|CYGWIN*)
        PLATFORM="Windows"
        LIB_EXT="dll"
        ;;
    *)
        echo "Unknown platform: $(uname -s)"
        exit 1
        ;;
esac

echo "Building libifr_sim for $PLATFORM..."

# Create build directory
mkdir -p "$BUILD_DIR"
cd "$BUILD_DIR"

# Configure with CMake
cmake .. -DCMAKE_BUILD_TYPE=Release

# Build
cmake --build . --config Release

# Copy to Unity Plugins folder
mkdir -p "$OUTPUT_DIR"

if [ "$PLATFORM" = "Windows" ]; then
    cp "$BUILD_DIR/lib/ifr_sim.$LIB_EXT" "$OUTPUT_DIR/"
else
    cp "$BUILD_DIR/lib/libifr_sim.$LIB_EXT" "$OUTPUT_DIR/"
fi

echo ""
echo "Build complete!"
echo "Library copied to: $OUTPUT_DIR"
echo ""
ls -la "$OUTPUT_DIR"
