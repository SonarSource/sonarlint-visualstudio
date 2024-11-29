#!/usr/bin/env bash

set -xeuo pipefail

# Fetch all commit history so that SonarCloud has exact blame information
# for issue auto-assignment
# This command can fail with "fatal: --unshallow on a complete repository does not make sense"
# if there are not enough commits in the Git repository
# For this reason errors are ignored with "|| true"
git fetch --unshallow || true

# fetch references from github for PR analysis
if [ -n "${CIRRUS_BASE_BRANCH}" ]; then
	git fetch origin "${CIRRUS_BASE_BRANCH}"
fi

if [ "$CIRRUS_BRANCH" == "master" ] && [ "$CIRRUS_PR" == "false" ]; then
  echo '======= Analyze master branch'

  dotnet sonarscanner begin \
    -k:"${CIRRUS_REPO_NAME}" \
    -o:"sonarsource" \
    -d:sonar.host.url="${SONAR_URL}" \
    -d:sonar.token=${SONAR_TOKEN} \
    -d:sonar.analysis.buildNumber="${CI_BUILD_NUMBER}" \
    -d:sonar.analysis.pipeline="$CIRRUS_BUILD_ID" \
    -d:sonar.analysis.sha1="${CIRRUS_CHANGE_IN_REPO}" \
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
    -d:sonar.analysis.sha1="${CIRRUS_CHANGE_IN_REPO}" \
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
    -d:sonar.analysis.sha1="${CIRRUS_CHANGE_IN_REPO}" \
    -d:sonar.pullrequest.key="${CIRRUS_PR}" \
    -d:sonar.pullrequest.branch="${CIRRUS_BRANCH}" \
    -d:sonar.pullrequest.base="${CIRRUS_BASE_BRANCH}" \
    -d:sonar.cs.vscoveragexml.reportsPaths="${COVERAGE_FILE}" \
    -d:sonar.scanner.scanAll=false \
    -d:sonar.verbose=true

else
  echo '======= No analysis'
fi
