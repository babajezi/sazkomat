#!/bin/bash
# Continuous test execution (watch mode)

set -e

FILTER="${1:-Category=Fast}"

echo -e "\033[36m👀 Watch Mode - Continuous Test Execution\033[0m"
echo -e "\033[90mFilter: $FILTER\033[0m"
echo -e "\033[90mPress Ctrl+C to stop\033[0m"
echo ""

TEST_PROJECT="tests/Sazkomat.Tests/Sazkomat.Tests.csproj"

dotnet watch test "$TEST_PROJECT" \
    --filter "$FILTER" \
    --verbosity minimal \
    --nologo \
    --configuration Debug
