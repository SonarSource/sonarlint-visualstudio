#!/usr/bin/env bash

set -xeo pipefail

version=$(grep -oP '<MainVersion>\K[^<]+' ./build/Version.props)

SONAR_PARAMS=(
  -v:"${version}"
  -k:"SonarSource_sonarlint-visualstudio"
  -o:"sonarsource"
  -d:sonar.host.url="${SONAR_US_URL}"
  -d:sonar.token="${SONAR_US_TOKEN}"
  -d:sonar.analysis.buildNumber="${CI_BUILD_NUMBER}"
  -d:sonar.analysis.pipeline="$CIRRUS_BUILD_ID"
  -d:sonar.analysis.sha1="${CIRRUS_CHANGE_IN_REPO}"
  -d:sonar.cs.vscoveragexml.reportsPaths="${COVERAGE_FILE}"
  -d:sonar.scanner.scanAll=false
  -d:sonar.c.file.suffixes=-
  -d:sonar.cpp.file.suffixes=-
  -d:sonar.objc.file.suffixes=-
)

echo '======= Analyze master branch'
dotnet sonarscanner begin "${SONAR_PARAMS[@]}" -d:sonar.branch.name="master"