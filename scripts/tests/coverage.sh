#!/bin/bash
set -euo pipefail

: "${SONAR_TOKEN:?SONAR_TOKEN is required}"
: "${SONAR_PROJECT_KEY:?SONAR_PROJECT_KEY is required}"
: "${SONAR_ORGANISATION_KEY:?SONAR_ORGANISATION_KEY is required}"

SONAR_SCANNER="./.sonar/scanner/dotnet-sonarscanner"
REPORT_GENERATOR="./.sonar/scanner/reportgenerator"



$SONAR_SCANNER begin /k:"$SONAR_PROJECT_KEY" /o:"$SONAR_ORGANISATION_KEY" /d:sonar.token="$SONAR_TOKEN" /d:sonar.host.url="https://sonarcloud.io" /d:sonar.coverageReportPaths="coverage/converted/SonarQube.xml"

dotnet build src/ServiceLayer.sln

make test-unit ARGS="--no-build"

$REPORT_GENERATOR \
  -reports:coverage/**/coverage.cobertura.xml \
  -targetdir:coverage/converted \
  -reporttypes:SonarQube

$SONAR_SCANNER end /d:sonar.token="$SONAR_TOKEN"
