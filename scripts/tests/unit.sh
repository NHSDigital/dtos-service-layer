#!/bin/bash
set -euo pipefail

COVERAGE_DIR="coverage"
TEST_PROJECTS=$(find tests -name '*.csproj')

for proj in $TEST_PROJECTS; do
  echo "🧪 Running tests for $proj..."
  dotnet test "$proj" \
    --collect:"XPlat Code Coverage" \
    --results-directory "$COVERAGE_DIR/$(basename "$proj" .csproj)"
done

echo "✅ All tests completed. Coverage reports in $COVERAGE_DIR/"
