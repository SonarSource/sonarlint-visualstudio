@echo off
setlocal ENABLEDELAYEDEXPANSION


REM We're now using Azure DevOps for the PR analysis.
REM However, there is a GitHub trigger at organization level that fires off a cix build for
REM pull requests as a required check. As it's a required check we need it to send a success
REM code to GitHub otherwise PR completion is blocked. However, we don't want the cix PR 
REM build to do any work so we'll early out as soon as possible.

echo IsPullRequest environment variable: %IS_PULLREQUEST%
IF "%IS_PULLREQUEST%"=="True" (
    @echo Skipping build step on CIX for pull requests. The PR analysis is now handled by the Azure DevOps build job:
    @echo   https://sonarsource.visualstudio.com/DotNetTeam%20Project/_build?definitionId=47
pause
    exit 0
)


IF NOT "%NODE_LABELS%"=="%NODE_LABELS:vs2015=%" (
	PowerShell -NonInteractive -NoProfile -ExecutionPolicy Unrestricted -Command ".\build\ci-build.ps1 -vsTargetVersion VS2015"
	echo From Cmd.exe: ci-build.ps1 exited with exit code !errorlevel!
	exit !errorlevel!
) ELSE IF NOT "%NODE_LABELS%"=="%NODE_LABELS:vs2017=%" (
	PowerShell -NonInteractive -NoProfile -ExecutionPolicy Unrestricted -Command ".\build\ci-build.ps1 -vsTargetVersion VS2017"
	echo From Cmd.exe: ci-build.ps1 exited with exit code !errorlevel!
    PowerShell -NonInteractive -NoProfile -ExecutionPolicy Unrestricted -Command ".\build\ci-build.ps1 -vsTargetVersion VS2019"
	echo From Cmd.exe: ci-build.ps1 exited with exit code !errorlevel!
	exit !errorlevel!
) ELSE (
    echo ERROR: NODE_LABELS contains neither vs2015 nor vs2017
    exit 1
)