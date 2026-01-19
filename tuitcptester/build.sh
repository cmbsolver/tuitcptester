#!/bin/bash

# Configuration
APP_NAME="tuitcptester"
PUBLISH_DIR="publish"

# Prompt for version number
echo -n "Enter version number (e.g., 1.0.0): "
read VERSION

if [ -z "$VERSION" ]; then
    echo "Version cannot be empty. Exiting."
    exit 1
fi

# Target RIDs (Runtime Identifiers)
TARGETS=("win-x64" "linux-x64" "osx-x64" "osx-arm64")

# 1. Clear out the publish directory immediately
echo "Cleaning up $PUBLISH_DIR directory..."
rm -rf "$PUBLISH_DIR"
mkdir -p "$PUBLISH_DIR"

# 2. Prompt for version number
echo "Starting build process for version $VERSION..."

for RID in "${TARGETS[@]}"
do
    echo "---------------------------------------"
    echo "Building for $RID..."
    
    OUTPUT_FOLDER="$PUBLISH_DIR/$RID"
    
    # Publish command:
    # -c Release: Optimized build
    # -r $RID: Target runtime
    # --self-contained true: Includes .NET runtime
    dotnet publish -c Release -r $RID --self-contained true \
        -o "$OUTPUT_FOLDER"

    # Compress the output
    cd $PUBLISH_DIR
    if [[ $RID == win* ]]; then
        zip -j "${APP_NAME}-${RID}-v${VERSION}.zip" "$RID/"*
    else
        tar -czf "${APP_NAME}-${RID}-v${VERSION}.tar.gz" -C "$RID" .
    fi
    cd ..

    echo "Done: $RID"
done

# Clean up the subdirectories, leaving only the compressed artifacts
echo "Cleaning up temporary build directories..."
find "$PUBLISH_DIR" -mindepth 1 -maxdepth 1 -type d -exec rm -rf {} +

echo "---------------------------------------"
echo "Build complete! Artifacts are in the $PUBLISH_DIR directory."
