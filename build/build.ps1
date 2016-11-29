$ErrorActionPreference = "Stop"

function testExitCode(){
    If($LASTEXITCODE -ne 0) {
        write-host -f green "lastexitcode: $LASTEXITCODE"
        exit $LASTEXITCODE
    }
}

if ($env:IS_PULLREQUEST -eq "true") { 
    write-host -f green "in a pull request"
    
    #download MSBuild
    $url = "https://github.com/SonarSource-VisualStudio/sonar-msbuild-runner/releases/download/2.0/MSBuild.SonarQube.Runner-2.0.zip"
    $output = ".\MSBuild.SonarQube.Runner.zip"    
    Invoke-WebRequest -Uri $url -OutFile $output
    unzip -o .\MSBuild.SonarQube.Runner.zip
    testExitCode

    .\MSBuild.SonarQube.Runner begin /k:sonarlint-visualstudio /n:"SonarLint for Visual Studio" /v:latest `
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

    #build with VS2017
    & $env:MSBUILD_PATH_2017 .\src\SonarLint.VisualStudio.Integration.2017.sln /t:rebuild /p:Configuration=Release /p:DeployExtension=false
    testExitCode


} else {
    if (($env:GITHUB_BRANCH -eq "master") -or ($env:GITHUB_BRANCH -eq "refs/heads/master")) {
        write-host -f green "Building master branch"

        #generate build version from the build number
        $buildversion="$env:BUILD_NUMBER"

        $branchName = "$env:GITHUB_BRANCH"
        $sha1 = "$env:GIT_SHA1"

        #Append build number to the versions
        (Get-Content .\build\Version.props) -replace '<AssemblyFileVersion>\$\(MainVersion\)\.0</AssemblyFileVersion>', "<AssemblyFileVersion>`$(MainVersion).$buildversion</AssemblyFileVersion>" | Set-Content .\build\Version.props
        (Get-Content .\build\Version.props) -replace '<AssemblyInformationalVersion>Version:\$\(AssemblyFileVersion\) Branch:not-set Sha1:not-set</AssemblyInformationalVersion>', "<AssemblyInformationalVersion>Version:`$(AssemblyFileVersion) Branch:$branchName Sha1:$sha1</AssemblyInformationalVersion>" | Set-Content .\build\Version.props
        & $env:MSBUILD_PATH  .\build\ChangeVersion.proj
        testExitCode

        #build
        & $env:NUGET_PATH restore .\src\SonarLint.VisualStudio.Integration.sln
        testExitCode
        & $env:MSBUILD_PATH .\src\SonarLint.VisualStudio.Integration.sln /p:configuration=Release /p:DeployExtension=false /p:ZipPackageCompressionLevel=normal /v:m /p:defineConstants=SignAssembly /p:SignAssembly=true /p:AssemblyOriginatorKeyFile=$env:CERT_PATH
        testExitCode

        #build with VS2017
        & $env:MSBUILD_PATH_2017 .\src\SonarLint.VisualStudio.Integration.2017.sln /t:rebuild /p:configuration=Release /p:DeployExtension=false /p:ZipPackageCompressionLevel=normal /v:m /p:defineConstants=SignAssembly /p:SignAssembly=true /p:AssemblyOriginatorKeyFile=$env:CERT_PATH
        testExitCode

        #get version number
        [xml]$versionProps = Get-Content .\build\Version.props
        $version  = $versionProps.Project.PropertyGroup.MainVersion+".$buildversion"
        $file     = Get-Item .\binaries\SonarLint.2015.vsix
        $artifact = "SonarLint.2015.VSIX"
        $filePath = $file.fullname
        
        & "$env:WINDOWS_MVN_HOME\bin\mvn.bat" deploy:deploy-file -DgroupId="org.sonarsource.dotnet" -DartifactId="$artifact" -Dversion="$version" -Dpackaging="vsix" -Dfile="$filePath" -DrepositoryId="sonarsource-public-qa" -Durl="https://repox.sonarsource.com/sonarsource-public-qa"
        testExitCode
        
        $file     = Get-Item .\binaries\SonarLint.2017.vsix
        $artifact = "SonarLint.2017.VSIX"
        $filePath = $file.fullname
        
        #deploy VS2017 build
        & "$env:WINDOWS_MVN_HOME\bin\mvn.bat" deploy:deploy-file -DgroupId="org.sonarsource.dotnet" -DartifactId="$artifact" -Dversion="$version" -Dpackaging="vsix" -Dfile="$filePath" -DrepositoryId="sonarsource-public-qa" -Durl="https://repox.sonarsource.com/sonarsource-public-qa"
        testExitCode
        
        #create empty file to trigger qa
        new-item -path . -name qa.properties -type "file"
    } else {
        write-host -f green "not on master"
    }

}



