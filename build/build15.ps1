$ErrorActionPreference = "Stop"

function testExitCode(){
    If($LASTEXITCODE -ne 0) {
        write-host -f green "lastexitcode: $LASTEXITCODE"
        exit $LASTEXITCODE
    }
}

function downloadMSBuildSonarQubeScanner() {
    #download MSBuild
    $url = "https://github.com/SonarSource-VisualStudio/sonar-msbuild-runner/releases/download/2.0/MSBuild.SonarQube.Runner-2.0.zip"
    $output = ".\MSBuild.SonarQube.Runner.zip"    
    Invoke-WebRequest -Uri $url -OutFile $output
    unzip -o .\MSBuild.SonarQube.Runner.zip
    testExitCode
}

if ($env:IS_PULLREQUEST -eq "true") { 
    write-host -f green "in a pull request"

    downloadMSBuildSonarQubeScanner

    .\MSBuild.SonarQube.Runner begin /k:sonarlint-visualstudio /n:"SonarLint for Visual Studio" /v:master `
        /d:sonar.host.url=$env:SONAR_HOST_URL `
        /d:sonar.login=$env:SONAR_TOKEN `
        /d:sonar.github.pullRequest=$env:PULL_REQUEST `
        /d:sonar.github.repository=$env:GITHUB_REPO `
        /d:sonar.github.oauth=$env:GITHUB_TOKEN `
        /d:sonar.analysis.mode=issues `
        /d:sonar.scanAllFiles=true
    testExitCode

    & $env:NUGET_PATH restore .\src\SonarLint.VisualStudio.Integration.sln
    testExitCode
    & $env:MSBUILD_PATH .\src\SonarLint.VisualStudio.Integration.sln /t:rebuild /p:Configuration=Release /p:DeployExtension=false
    testExitCode

    .\MSBuild.SonarQube.Runner end /d:sonar.login=$env:SONAR_TOKEN
    testExitCode

} else {
    #generate build version from the build number
    $buildversion="$env:BUILD_NUMBER"
    $branchName = "$env:GITHUB_BRANCH"
    $sha1 = "$env:GIT_SHA1"

    write-host -f green "Building ${branchName} branch"

    $isMaster = ($env:GITHUB_BRANCH -eq "master") -or ($env:GITHUB_BRANCH -eq "refs/heads/master");

    #Append build number to the versions
    (Get-Content .\build\Version.props) -replace '<AssemblyFileVersion>\$\(MainVersion\)\.0</AssemblyFileVersion>', "<AssemblyFileVersion>`$(MainVersion).$buildversion</AssemblyFileVersion>" | Set-Content .\build\Version.props
    (Get-Content .\build\Version.props) -replace '<AssemblyInformationalVersion>Version:\$\(AssemblyFileVersion\) Branch:not-set Sha1:not-set</AssemblyInformationalVersion>', "<AssemblyInformationalVersion>Version:`$(AssemblyFileVersion) Branch:$branchName Sha1:$sha1</AssemblyInformationalVersion>" | Set-Content .\build\Version.props
    & $env:MSBUILD_PATH  .\build\ChangeVersion.proj
    testExitCode

    if($isMaster)
    {
        #start analysis
        downloadMSBuildSonarQubeScanner
        .\MSBuild.SonarQube.Runner begin /k:sonarlint-visualstudio /n:"SonarLint for Visual Studio" /v:master `
            /d:sonar.host.url=$env:SONAR_HOST_URL `
            /d:sonar.login=$env:SONAR_TOKEN 
        testExitCode
    }

    #build
    & $env:NUGET_PATH restore .\src\SonarLint.VisualStudio.Integration.sln
    testExitCode
    & $env:MSBUILD_PATH .\src\SonarLint.VisualStudio.Integration.sln /p:configuration=Release /p:DeployExtension=false /p:ZipPackageCompressionLevel=normal /v:m /p:defineConstants=SignAssembly /p:SignAssembly=true /p:AssemblyOriginatorKeyFile=$env:CERT_PATH
    testExitCode

    if ($isMaster)
    {
        #end analysis
        .\MSBuild.SonarQube.Runner end /d:sonar.login=$env:SONAR_TOKEN
        testExitCode
    }
}



