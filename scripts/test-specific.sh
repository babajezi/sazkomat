#!/bin/bash
# Run tests from a specific test class

set -e

if [ -z "$1" ]; then
    echo "Usage: ./test-specific.sh <ClassName> [verbosity]"
    echo "Example: ./test-specific.sh LeagueRepositoryTests"
    exit 1
fi

CLASS_NAME="$1"
VERBOSITY="${2:-minimal}"

echo -e "\033[36m🎯 Running tests from: $CLASS_NAME\033[0m"
echo ""

TEST_PROJECT="tests/Sazkomat.Tests/Sazkomat.Tests.csproj"
FILTER="FullyQualifiedName~$CLASS_NAME"

echo -e "\033[90mFilter: $FILTER\033[0m"
echo ""

dotnet test "$TEST_PROJECT" \
    --filter "$FILTER" \
    --verbosity "$VERBOSITY" \
    --nologo \
    --configuration Debug
