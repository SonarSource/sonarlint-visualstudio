@echo off
setlocal ENABLEDELAYEDEXPANSION
IF NOT "%NODE_LABELS%"=="%NODE_LABELS:vs2015=%" (
	PowerShell -NonInteractive -NoProfile -ExecutionPolicy Unrestricted -Command ".\build\ci-build.ps1 -vsTargetVersion vs2015"
	echo From Cmd.exe: ci-build.ps1 exited with exit code !errorlevel!
	exit !errorlevel!
) ELSE IF NOT "%NODE_LABELS%"=="%NODE_LABELS:vs2017=%" (
	PowerShell -NonInteractive -NoProfile -ExecutionPolicy Unrestricted -Command ".\build\ci-build.ps1 -vsTargetVersion vs2017"
	echo From Cmd.exe: ci-build.ps1 exited with exit code !errorlevel!
	exit !errorlevel!
) ELSE (
    echo ERROR: NODE_LABELS contains neither vs2015 nor vs2017
    exit 1
)