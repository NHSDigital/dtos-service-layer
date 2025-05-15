#!/bin/bash
set -euo pipefail

BUILD_ARGS=""
if [[ "${1:-}" == "--no-build" ]]; then
  BUILD_ARGS="--no-build"
fi

COVERAGE_DIR="coverage"
TEST_PROJECTS=$(find tests -name '*.csproj')

rm -rf "$COVERAGE_DIR"
mkdir -p "$COVERAGE_DIR"

for proj in $TEST_PROJECTS; do
  proj_name=$(basename "$proj" .csproj)
  out_file="$COVERAGE_DIR/$proj_name.coverage.xml"
  echo "🧪 Running coverage for $proj -> $out_file"
  dotnet test "$proj" \
   $BUILD_ARGS \
  --collect:"XPlat Code Coverage" \
  --results-directory coverage
done
