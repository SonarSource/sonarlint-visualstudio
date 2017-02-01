$ErrorActionPreference = "Stop"

function testExitCode(){
    If($LASTEXITCODE -ne 0) {
        write-host -f green "lastexitcode: $LASTEXITCODE"
        exit $LASTEXITCODE
    }
}

if ($env:IS_PULLREQUEST -eq "true") { 
    write-host -f green "in a pull request"

    & $env:NUGET_PATH restore .\src\SonarLint.VisualStudio.Integration.2017.sln
    testExitCode
    Start-Process "build/vs2017.bat" -NoNewWindow -Wait

} else {
    if (($env:GITHUB_BRANCH -eq "multiVM") -or ($env:GITHUB_BRANCH -eq "refs/heads/master")) {
        write-host -f green "Building multiVM branch"

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
        & $env:NUGET_PATH restore .\src\SonarLint.VisualStudio.Integration.2017.sln
        testExitCode

        #build with VS2017
        Start-Process "build/vs2017.bat" -NoNewWindow -Wait

    } else {
        write-host -f green "not on master"
    }

}



