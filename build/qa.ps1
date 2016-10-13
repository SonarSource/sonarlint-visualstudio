$ErrorActionPreference = "Stop"

function testExitCode(){
    If($LASTEXITCODE -ne 0) {
        write-host -f green "lastexitcode: $LASTEXITCODE"
        exit $LASTEXITCODE
    }
}

#cleanup
$importBeforeTargetFileName="%USERPROFILE%\AppData\Local\Microsoft\MSBuild\14.0\Microsoft.Common.targets\ImportBefore\SonarAnalyzer.Testing.ImportBefore.targets" 
If (Test-Path $importBeforeTargetFileName){
  Remove-Item $importBeforeTargetFileName
}

#nuget restore
& $env:NUGET_PATH restore .\src\SonarLint.VisualStudio.Integration.sln
testExitCode

#build tests
& $env:MSBUILD_PATH .\src\SonarLint.VisualStudio.Integration.sln /p:configuration=Release /p:DeployExtension=false /p:ZipPackageCompressionLevel=normal /v:m /p:defineConstants=SignAssembly /p:SignAssembly=true /p:AssemblyOriginatorKeyFile=$env:CERT_PATH
testExitCode

#download VSIX
$ARTIFACTORY_SRC_REPO="sonarsource-public-qa/org/sonarsource/dotnet/SonarLint.VSIX"
[xml]$versionProps = Get-Content .\build\Version.props
$version  = $versionProps.Project.PropertyGroup.MainVersion+".$env:CI_BUILD_NUMBER"
$fileName = "SonarLint.VSIX-$version.vsix"
$url = "$env:ARTIFACTORY_URL/$ARTIFACTORY_SRC_REPO/$version/$fileName"
Write-Host "Downloading $url"
$pair = "$($env:REPOX_QAPUBLICADMIN_USERNAME):$($env:REPOX_QAPUBLICADMIN_PASSWORD)"
$encodedCreds = [System.Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes($pair))
$basicAuthValue = "Basic $encodedCreds"
$Headers = @{Authorization = $basicAuthValue}
Invoke-WebRequest -UseBasicParsing -Uri "$url" -Headers $Headers -OutFile $fileName

#unzip VSIX package
$zipName="SonarLint.VSIX-$version.zip"
Move-Item $fileName $zipName -force
$shell_app=new-object -com shell.application
$baseDir=(Get-Item -Path ".\" -Verbose).FullName
$destination = $shell_app.NameSpace($baseDir)
$zip_file = $shell_app.NameSpace("$baseDir\$zipName")
Write-Host "Unzipping $baseDir\$zipName"
$destination.CopyHere($zip_file.Items(), 0x14) 

#move dlls to correct locations
Write-Host "Copying DLLs"
Copy-Item *.dll .src\Integration.UnitTests\bin\Release\ -force
Copy-Item *.dll .src\Integration.Vsix.UnitTests\bin\Release\ -force
Copy-Item *.dll .src\Progress.UnitTests\bin\Release\ -force

#run tests
Write-Host "Start tests"
& $env:VSTEST_PATH .src\Integration.UnitTests\bin\Release\SonarLint.VisualStudio.Integration.UnitTests.dll
testExitCode
& $env:VSTEST_PATH .src\Integration.Vsix.UnitTests\bin\Release\SonarLint.VisualStudio.Integration.Vsix.UnitTests.dll
testExitCode
& $env:VSTEST_PATH .src\Progress.UnitTests\bin\Release\SonarLint.VisualStudio.Progress.UnitTests.dll
testExitCode
