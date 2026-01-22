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
    
    # Create a temporary output folder
    TEMP_OUTPUT="$PUBLISH_DIR/temp_$RID"
    # The folder name we want inside the archive
    INTERNAL_DIR="tcptui"
    FINAL_OUTPUT="$TEMP_OUTPUT/$INTERNAL_DIR"
    
    # Publish command
    dotnet publish -c Release -r $RID --self-contained true \
        -o "$FINAL_OUTPUT"

    # Compress the output
    cd "$TEMP_OUTPUT"
    if [[ $RID == win* ]]; then
        zip -r "../../$PUBLISH_DIR/${APP_NAME}-${RID}-v${VERSION}.zip" "$INTERNAL_DIR"
    else
        tar -czf "../../$PUBLISH_DIR/${APP_NAME}-${RID}-v${VERSION}.tar.gz" "$INTERNAL_DIR"
    fi
    cd ../..

    echo "Done: $RID"
done

# Clean up the temporary folders
echo "Cleaning up temporary build directories..."
rm -rf "$PUBLISH_DIR"/temp_*

echo "---------------------------------------"
echo "Build complete! Artifacts are in the $PUBLISH_DIR directory."
