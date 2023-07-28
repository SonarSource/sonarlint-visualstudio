@REM this script cleans all of the artifact locations and rebuilds the project
call cd ..
(for %%a in (".\build\ProcessJarFiles\tmp" ".\src\Rules\Embedded" ".\src\Integration.Vsix\lib" "%LOCALAPPDATA%\SLVS_Build_DownloadedJars" "%LOCALAPPDATA%\SLVS_CFamily_Build" "%LOCALAPPDATA%\SLVS_TypeScript_Build") do rd /s /q "%%~a")
call cd ".\build\ProcessJarFiles"
call msbuild.exe -t:Rebuild
call cd "..\..\"
call msbuild.exe "SonarLint.VisualStudio.Integration.sln" -t:Rebuild
call cd "..\tools"