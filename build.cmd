@echo off
setlocal ENABLEDELAYEDEXPANSION

ECHO ======= Starting to wait
SLEEP 30

IF NOT "%NODE_LABELS%"=="%NODE_LABELS:vs2015=%" (
	PowerShell -NonInteractive -NoProfile -ExecutionPolicy Unrestricted -Command ".\build\build.ps1 -analyze -test -coverage -githubRepo $env:GITHUB_REPO -githubToken $env:GITHUB_TOKEN -githubPullRequest $env:PULL_REQUEST -isPullRequest $env:IS_PULLREQUEST -sonarQubeUrl $env:SONAR_HOST_URL -sonarQubeToken $env:SONAR_TOKEN -solutionName "SonarLint.VisualStudio.Integration.sln" -snkCertificatePath $env:CERT_PATH -pfxCertificatePath $env:PFX_PATH -pfxPassword $env:PFX_PASSWORD"
	echo From Cmd.exe: build.ps1 exited with exit code !errorlevel!
	exit !errorlevel!
) ELSE IF NOT "%NODE_LABELS%"=="%NODE_LABELS:vs2017=%" (
	PowerShell -NonInteractive -NoProfile -ExecutionPolicy Unrestricted -Command ".\build\build.ps1 -test -githubRepo $env:GITHUB_REPO -githubToken $env:GITHUB_TOKEN -githubPullRequest $env:PULL_REQUEST -isPullRequest $env:IS_PULLREQUEST -sonarQubeUrl $env:SONAR_HOST_URL -sonarQubeToken $env:SONAR_TOKEN -solutionName "SonarLint.VisualStudio.Integration.2017.sln" -snkCertificatePath $env:CERT_PATH -pfxCertificatePath $env:PFX_PATH -pfxPassword $env:PFX_PASSWORD"
	echo From Cmd.exe: build.ps1 exited with exit code !errorlevel!
	exit !errorlevel!
) ELSE (
    echo ERROR: NODE_LABELS contains neither vs2015 nor vs2017
    exit 1
)
