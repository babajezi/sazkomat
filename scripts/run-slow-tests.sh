#!/bin/bash
# Runs slow and integration tests

set -e

echo -e "\033[36m🐌 Running Slow + Integration Tests\033[0m"
echo -e "\033[90mExpected runtime: < 60 seconds\033[0m"
echo ""

TEST_PROJECT="tests/Sazkomat.Tests/Sazkomat.Tests.csproj"
FILTER="Category=Slow|Category=Integration"
VERBOSITY="${1:-minimal}"

echo -e "\033[90mFilter: $FILTER\033[0m"
echo ""

START_TIME=$(date +%s)

if dotnet test "$TEST_PROJECT" \
    --filter "$FILTER" \
    --verbosity "$VERBOSITY" \
    --nologo \
    --no-build \
    --configuration Debug; then
    EXIT_CODE=0
else
    EXIT_CODE=$?
fi

END_TIME=$(date +%s)
ELAPSED=$((END_TIME - START_TIME))

echo ""
if [ $EXIT_CODE -eq 0 ]; then
    echo -e "\033[32m✅ Slow tests passed in ${ELAPSED}s\033[0m"

    if [ $ELAPSED -gt 60 ]; then
        echo -e "\033[33m⚠️  Warning: Tests took longer than expected (> 60s)\033[0m"
    fi
else
    echo -e "\033[31m❌ Slow tests failed in ${ELAPSED}s\033[0m"
fi

exit $EXIT_CODE
