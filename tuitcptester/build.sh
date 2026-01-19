#!/bin/bash

# Configuration
APP_NAME="tuitcptester"
PUBLISH_DIR="publish"
VERSION="1.0.0"

# Target RIDs (Runtime Identifiers)
TARGETS=("win-x64" "linux-x64" "osx-x64" "osx-arm64")

# Clean up previous builds
rm -rf $PUBLISH_DIR
mkdir -p $PUBLISH_DIR

echo "Starting build process..."

for RID in "${TARGETS[@]}"
do
    echo "---------------------------------------"
    echo "Building for $RID..."
    
    OUTPUT_FOLDER="$PUBLISH_DIR/$RID"
    
    # Publish command:
    # -c Release: Optimized build
    # -r $RID: Target runtime
    # --self-contained true: Includes .NET runtime
    # -p:PublishSingleFile=true: Packs everything into one executable
    # -p:PublishTrimmed=true: Removes unused code to reduce size (optional but recommended for self-contained)
    dotnet publish -c Release -r $RID --self-contained true \
        -p:PublishSingleFile=true \
        -p:PublishTrimmed=true \
        -o $OUTPUT_FOLDER

    # Compress the output
    cd $PUBLISH_DIR
    if [[ $RID == win* ]]; then
        zip -j "${APP_NAME}-${RID}-${VERSION}.zip" "$RID/"*
    else
        tar -czf "${APP_NAME}-${RID}-${VERSION}.tar.gz" -C "$RID" .
    fi
    cd ..

    echo "Done: $RID"
done

echo "---------------------------------------"
echo "Build complete! Artifacts are in the $PUBLISH_DIR directory."
