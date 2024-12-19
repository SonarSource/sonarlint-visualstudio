@REM this script cleans all of the artifact locations and rebuilds the project

@REM %~dp0 gives the directory containing this batch file

(for %%a in ("%LOCALAPPDATA%\SLVS_Build_DownloadedJars" "%LOCALAPPDATA%\SLVS_Build_SLOOP" "%LOCALAPPDATA%\SLVS_Build_Dotnet") do rd /s /q "%%~a")

call msbuild.exe %~dp0..\build\DownloadDependencies -t:Rebuild
call msbuild.exe "%~dp0..\SonarLint.VisualStudio.Integration.sln" -t:Rebuild
