#!/bin/bash
# Fast Docker build with BuildKit

set -e

SERVICE="${1:-api}"
NO_CACHE="${2}"

echo -e "\033[36m🚀 Fast Docker Build - $SERVICE\033[0m"
echo ""

# Enable BuildKit
export DOCKER_BUILDKIT=1
export COMPOSE_DOCKER_CLI_BUILD=1

echo -e "\033[32mBuildKit: ENABLED\033[0m"
echo -e "\033[90mService: $SERVICE\033[0m"
echo ""

BUILD_ARGS="build $SERVICE"

if [ "$NO_CACHE" = "--no-cache" ]; then
    echo -e "\033[33mCache: DISABLED (clean build)\033[0m"
    BUILD_ARGS="$BUILD_ARGS --no-cache"
else
    echo -e "\033[32mCache: ENABLED (incremental build)\033[0m"
fi

echo ""
echo -e "\033[90mStarting build...\033[0m"
echo ""

START_TIME=$(date +%s)

if docker-compose $BUILD_ARGS; then
    EXIT_CODE=0
else
    EXIT_CODE=$?
fi

END_TIME=$(date +%s)
ELAPSED=$((END_TIME - START_TIME))

echo ""
if [ $EXIT_CODE -eq 0 ]; then
    echo -e "\033[32m✅ Build completed in ${ELAPSED}s\033[0m"
else
    echo -e "\033[31m❌ Build failed in ${ELAPSED}s\033[0m"
fi

exit $EXIT_CODE
