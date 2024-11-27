#!/usr/bin/env bash

set -xeuo pipefail

if [ "$CIRRUS_BRANCH" == "master" ] && [ "$CIRRUS_PR" == "false" ]; then
  echo '======= Analyze master branch'

  dotnet sonarscanner begin \
    -k:"${CIRRUS_REPO_NAME}" \
    -o:"sonarsource" \
    -d:sonar.host.url="${SONAR_URL}" \
    -d:sonar.token=${SONAR_TOKEN} \
    -d:sonar.analysis.buildNumber="${CI_BUILD_NUMBER}" \
    -d:sonar.analysis.pipeline="$CIRRUS_BUILD_ID" \
    -d:sonar.cs.vscoveragexml.reportsPaths="${COVERAGE_FILE}" \
    -d:sonar.scanner.scanAll=false

elif [[ "$CIRRUS_BRANCH" == "branch-"* || "$CIRRUS_BRANCH" == "feature/"* ]] && [ "$CIRRUS_PR" == "false" ]; then
  echo '======= Analyze long lived branch'

  dotnet sonarscanner begin \
    -k:"${CIRRUS_REPO_NAME}" \
    -o:"sonarsource" \
    -d:sonar.host.url="${SONAR_URL}" \
    -d:sonar.token=${SONAR_TOKEN} \
    -d:sonar.analysis.buildNumber="${CI_BUILD_NUMBER}" \
    -d:sonar.analysis.pipeline="$CIRRUS_BUILD_ID" \
    -d:sonar.branch.name="${CIRRUS_BRANCH}" \
    -d:sonar.cs.vscoveragexml.reportsPaths="${COVERAGE_FILE}" \
    -d:sonar.scanner.scanAll=false

elif [ "$CIRRUS_PR" != "false" ]; then
  echo '======= Analyze pull request'

  dotnet sonarscanner begin \
    -k:"${CIRRUS_REPO_NAME}" \
    -o:"sonarsource" \
    -d:sonar.host.url="${SONAR_URL}" \
    -d:sonar.token=${SONAR_TOKEN} \
    -d:sonar.analysis.buildNumber="${CI_BUILD_NUMBER}" \
    -d:sonar.analysis.pipeline="$CIRRUS_BUILD_ID" \
    -d:sonar.pullrequest.key="${CIRRUS_PR}" \
    -d:sonar.pullrequest.branch="${CIRRUS_BRANCH}" \
    -d:sonar.pullrequest.base="${CIRRUS_BASE_BRANCH}" \
    -d:sonar.cs.vscoveragexml.reportsPaths="${COVERAGE_FILE}" \
    -d:sonar.scanner.scanAll=false

else
  echo '======= No analysis'
fi
