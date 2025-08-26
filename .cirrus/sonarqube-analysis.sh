#!/usr/bin/env bash

set -xeo pipefail

version=$(grep -oP '<MainVersion>\K[^<]+' ./build/Version.props)

SONAR_PARAMS=(
  -v:"${version}"
  -k:"${SONAR_PROJECT_KEY}"
  -o:"sonarsource"
  -d:sonar.host.url="${SONAR_URL}"
  -d:sonar.token="${SONAR_TOKEN}"
  -d:sonar.analysis.buildNumber="${CI_BUILD_NUMBER}"
  -d:sonar.analysis.pipeline="$CIRRUS_BUILD_ID"
  -d:sonar.analysis.sha1="${CIRRUS_CHANGE_IN_REPO}"
  -d:sonar.cs.vscoveragexml.reportsPaths="${COVERAGE_FILE}"
  -d:sonar.scanner.scanAll=false
  -d:sonar.c.file.suffixes=-
  -d:sonar.cpp.file.suffixes=-
  -d:sonar.objc.file.suffixes=-
)

if [ "$CIRRUS_BRANCH" == "master" ] && [ -z "$CIRRUS_PR" ]; then
  echo '======= Analyze master branch'
  dotnet sonarscanner begin "${SONAR_PARAMS[@]}"

elif [ -n "$CIRRUS_PR" ]; then
  echo '======= Analyze pull request'
  dotnet sonarscanner begin "${SONAR_PARAMS[@]}" \
    -d:sonar.pullrequest.key="${CIRRUS_PR}" \
    -d:sonar.pullrequest.branch="${CIRRUS_BRANCH}" \
    -d:sonar.pullrequest.base="${CIRRUS_BASE_BRANCH}"

else
    echo '======= Analyze branch'
    dotnet sonarscanner begin "${SONAR_PARAMS[@]}" -d:sonar.branch.name="${CIRRUS_BRANCH}"
fi
