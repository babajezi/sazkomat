#!/bin/bash
# Runs only fast unit tests (< 10s target)

set -e

echo -e "\033[36m🚀 Running Fast Tests (Unit + Repository)\033[0m"
echo -e "\033[90mExpected runtime: < 10 seconds\033[0m"
echo ""

TEST_PROJECT="tests/Sazkomat.Tests/Sazkomat.Tests.csproj"
FILTER="Category=Fast"
VERBOSITY="${1:-minimal}"

echo -e "\033[90mFilter: $FILTER\033[0m"
echo ""

# Measure execution time
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
    echo -e "\033[32m✅ Fast tests passed in ${ELAPSED}s\033[0m"

    if [ $ELAPSED -gt 10 ]; then
        echo -e "\033[33m⚠️  Warning: Tests took longer than expected (> 10s)\033[0m"
    fi
else
    echo -e "\033[31m❌ Fast tests failed in ${ELAPSED}s\033[0m"
fi

exit $EXIT_CODE
