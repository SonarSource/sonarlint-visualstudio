@echo off
setlocal ENABLEDELAYEDEXPANSION

PowerShell -NonInteractive -NoProfile -ExecutionPolicy Unrestricted -Command ".\build\ci-qa.ps1" 
echo From Cmd.exe: qa.ps1 exited with exit code !errorlevel!
exit !errorlevel!