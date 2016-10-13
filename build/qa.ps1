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
$fileName = $env:FILENAME
$version  = $fileName.Substring(0, $fileName.IndexOf('-'))
$url = "$env:ARTIFACTORY_URL/$ARTIFACTORY_SRC_REPO/$version/$fileName"
Write-Host "Downloading $url"
$pair = "$($env:REPOX_QAPUBLICADMIN_USERNAME):$($env:REPOX_QAPUBLICADMIN_PASSWORD)"
$encodedCreds = [System.Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes($pair))
$basicAuthValue = "Basic $encodedCreds"
$Headers = @{Authorization = $basicAuthValue}
Invoke-WebRequest -UseBasicParsing -Uri "$url" -Headers $Headers -OutFile $env:FILENAME

#unzip VSIX package
$zipName=$env:FILENAME.Substring(0, $env:FILENAME.LastIndexOf('.'))+".zip"
Move-Item $env:FILENAME $zipName -force
$shell_app=new-object -com shell.application
$baseDir=(Get-Item -Path ".\" -Verbose).FullName
$destination = $shell_app.NameSpace($baseDir)
$zip_file = $shell_app.NameSpace("$baseDir\$zipName")
Write-Host "Unzipping $baseDir\$zipName"
$destination.CopyHere($zip_file.Items(), 0x14) 

$fileInfo = ls .\SonarLint.dll | % { $_.versioninfo.productversion }

#find the sha1 
$sha1=$fileInfo.Substring($fileInfo.LastIndexOf('Sha1:')+5)
Write-Host "Checking out $sha1"
$s="SHA1=$sha1"
$s | out-file -encoding utf8 ".\sha1.properties"

#find the branch
$GITHUB_BRANCH=$fileInfo.split("{ }")[1].Substring(7)
Write-Host "GITHUB_BRANCH $GITHUB_BRANCH"
if ($GITHUB_BRANCH.StartsWith("refs/heads/")) {
    $GITHUB_BRANCH=$GITHUB_BRANCH.Substring(11)
}
$s="GITHUB_BRANCH=$GITHUB_BRANCH"
Write-Host "$s"
$s | out-file -encoding utf8 -append ".\sha1.properties"
#convert sha1 property file to unix for jenkins compatiblity
Get-ChildItem .\sha1.properties | ForEach-Object {
  $contents = [IO.File]::ReadAllText($_) -replace "`r`n?", "`n"
  $utf8 = New-Object System.Text.UTF8Encoding $false
  [IO.File]::WriteAllText($_, $contents, $utf8)
}

#checkout commit
git pull origin $GITHUB_BRANCH
testExitCode
git checkout -f $sha1
testExitCode

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
