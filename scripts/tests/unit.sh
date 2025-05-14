#!/bin/bash
set -euo pipefail

COVERAGE_DIR="coverage"
TEST_PROJECTS=$(find tests -name '*.csproj')

rm -rf "$COVERAGE_DIR"
mkdir -p "$COVERAGE_DIR"

for proj in $TEST_PROJECTS; do
  proj_name=$(basename "$proj" .csproj)
  out_file="$COVERAGE_DIR/$proj_name.coverage.xml"
  echo "🧪 Running coverage for $proj -> $out_file"
  dotnet-coverage collect -f xml -o "$out_file" dotnet test "$proj"
done
