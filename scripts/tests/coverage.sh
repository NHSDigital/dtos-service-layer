#!/bin/bash
set -euo pipefail

: "${SONAR_TOKEN:?SONAR_TOKEN is required}"
: "${SONAR_PROJECT_KEY:?SONAR_PROJECT_KEY is required}"
: "${SONAR_ORGANISATION_KEY:?SONAR_ORGANISATION_KEY is required}"

SONAR_SCANNER="./.sonar/scanner/dotnet-sonarscanner"
COVERAGE_FILES=$(find coverage -name '*.coverage.xml' | paste -sd "," -)

$SONAR_SCANNER begin /k:"$SONAR_PROJECT_KEY" /o:"$SONAR_ORGANISATION_KEY" /d:sonar.token="$SONAR_TOKEN" /d:sonar.host.url="https://sonarcloud.io" /d:sonar.cs.vscoveragexml.reportsPaths="$COVERAGE_FILES"

dotnet build src/ServiceLayer.sln

make test-unit

$SONAR_SCANNER end /d:sonar.token="$SONAR_TOKEN"
