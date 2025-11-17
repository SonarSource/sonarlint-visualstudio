#!/usr/bin/env bash

set -xeo pipefail

# Determine if this is a pull request
if [ "${GITHUB_EVENT_NAME}" == "pull_request" ]; then
    export PULL_REQUEST="${GITHUB_REF##refs/pull/}"
    export PULL_REQUEST="${PULL_REQUEST%/merge}"
    export GITHUB_BRANCH="${GITHUB_HEAD_REF}"
    export GITHUB_BASE_BRANCH="${GITHUB_BASE_REF}"
else
    export PULL_REQUEST="false"
    export GITHUB_BRANCH="${GITHUB_REF_NAME}"
    export GITHUB_BASE_BRANCH="${GITHUB_BASE_REF:-}"
fi

echo "Environment variables set:"
echo "BUILD_NUMBER: $BUILD_NUMBER"
echo "GITHUB_SHA: $GITHUB_SHA"
echo "GITHUB_REPOSITORY: $GITHUB_REPOSITORY"
echo "PULL_REQUEST: $PULL_REQUEST"
echo "GITHUB_BRANCH: $GITHUB_BRANCH"
echo "GITHUB_BASE_BRANCH: $GITHUB_BASE_BRANCH"
echo "GITHUB_BASE_REF: $GITHUB_BASE_REF"
echo "PROJECT_VERSION_WITHOUT_BUILD_NUMBER: $PROJECT_VERSION_WITHOUT_BUILD_NUMBER"

SONAR_PARAMS=(
  -v:"${PROJECT_VERSION_WITHOUT_BUILD_NUMBER}"
  -k:"${SONAR_PROJECT_KEY}"
  -o:"sonarsource"
  -d:sonar.host.url="${SONAR_URL}"
  -d:sonar.token="${SONAR_TOKEN}"
  -d:sonar.analysis.buildNumber="${BUILD_NUMBER}"
  -d:sonar.analysis.pipeline="${GITHUB_RUN_ID}"
  -d:sonar.analysis.sha1="${GITHUB_SHA}"
  -d:sonar.cs.vscoveragexml.reportsPaths="${COVERAGE_FILE}"
  -d:sonar.scanner.scanAll=false
  -d:sonar.c.file.suffixes=-
  -d:sonar.cpp.file.suffixes=-
  -d:sonar.objc.file.suffixes=-
)

if [ "$GITHUB_BRANCH" == "master" ] && [ "$PULL_REQUEST" == "false" ]; then
  echo '======= Analyze master branch'
  dotnet sonarscanner begin "${SONAR_PARAMS[@]}"

elif [ "$PULL_REQUEST" != "false" ]; then
  echo '======= Analyze pull request'
  dotnet sonarscanner begin "${SONAR_PARAMS[@]}" \
    -d:sonar.pullrequest.key="${PULL_REQUEST}" \
    -d:sonar.pullrequest.branch="${GITHUB_BRANCH}" \
    -d:sonar.pullrequest.base="${GITHUB_BASE_BRANCH}"

else
    echo '======= Analyze branch'
    dotnet sonarscanner begin "${SONAR_PARAMS[@]}" -d:sonar.branch.name="${GITHUB_BRANCH}"
fi
