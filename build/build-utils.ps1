Add-Type -AssemblyName "System.IO.Compression.FileSystem"

# Resolves the given relative to the repository path to absolute.
function Resolve-RepoPath([string]$relativePath) {
    return (Join-Path (Resolve-Path (Join-Path $PSScriptRoot "..")) $relativePath)
}

function Test-Debug() {
    return $DebugPreference -ne "SilentlyContinue"
}

# Original: http://jameskovacs.com/2010/02/25/the-exec-problem
function Exec ([scriptblock]$command, [string]$errorMessage = "Error executing command: " + $command) {
    $output = & $command
    if ((-not $?) -or ($lastexitcode -ne 0)) {
        Write-Host $output
        throw $errorMessage
    }
    return $output
}

function Test-ExitCode([string]$errorMessage = "Error executing command.") {
    if ((-not $?) -or ($lastexitcode -ne 0)) {
        throw $errorMessage
    }
}

# Sets the current folder and executes the given script.
# When the script finishes sets the original current folder.
function Exec-InLocation([string]$path, [scriptblock]$command) {
    try {
        Push-Location $path
        & $command
    }
    finally {
        Pop-Location
    }
}

function Write-Header([string]$text) {
    Write-Host "================================================"
    Write-Host $text
    Write-Host "================================================"
}

## Build ############################################################

function Get-BuildNumber([string]$default = "0") {
    if ($env:BUILD_NUMBER) {
        return $env:BUILD_NUMBER
    }
    return $default
}

function Get-BranchName {
    if ($env:GITHUB_BRANCH) {
        if ($env:GITHUB_BRANCH.StartsWith("refs/heads/")) {
            return $env:GITHUB_BRANCH.Substring(11)
        }
        return $env:GITHUB_BRANCH
    }
    return Exec { & git rev-parse --abbrev-ref HEAD }
}

function Get-Sha1 {
    if ($env:GIT_SHA1) {
        return $env:GIT_SHA1
    }
    return Exec { & git rev-parse HEAD }
}

function Get-ExecutablePath([string]$name, [string]$directory, [string]$envVar) {
    $path = [environment]::GetEnvironmentVariable($envVar, "Process")

    if (!$path) {
        if (!$directory) {
            $path = Exec { & where.exe $name } `
                | Select-Object -First 1
        } else {
            $path = Exec { & where.exe /R $directory $name } `
                | Select-Object -First 1
        }
    }

    if (Test-Path $path) {
        Write-Host "Found ${name} at ${path}"
        [environment]::SetEnvironmentVariable($envVar, $path)
        return $path
    }

    Write-Error "Cannot find ${name} in ${path}."
    exit 1
}

function Get-NuGetPath {
    return Get-ExecutablePath -name "nuget.exe" -envVar "NUGET_PATH"
}

function Get-MsBuildPath([ValidateSet("14.0", "15.0", "16.0")][string]$msbuildVersion) {
    if ($msbuildVersion -eq "14.0") {
        return Get-ExecutablePath -name "msbuild.exe" -envVar "MSBUILD_PATH"
    }
    elseif ($msbuildVersion -eq "15.0") {
        Write-Host "Trying to find 'msbuild.exe 15' using 'MSBUILD_PATH' environment variable"
        $msbuild15Env = "MSBUILD_PATH"
        $msbuild15Path = [environment]::GetEnvironmentVariable($msbuild15Env, "Process")

        if (!$msbuild15Path) {
            Write-Host "Environment variable not found"
            Write-Host "Trying to find path using 'vswhere.exe'"

            # Sets the path to MSBuild 15 into an the MSBUILD_PATH environment variable
            # All subsequent builds after this command will use MSBuild 15!
            # Test if vswhere.exe is in your path. Download from: https://github.com/Microsoft/vswhere/releases
            $path = Exec { & (Get-VsWherePath) -version "[15.0, 16.0)" -products * -requires Microsoft.Component.MSBuild `
                -property installationPath } | Select-Object -First 1
            if ($path) {
                $msbuild15Path = Join-Path $path "MSBuild\15.0\Bin\MSBuild.exe"
                [environment]::SetEnvironmentVariable($msbuild15Env, $msbuild15Path)
            }
        }

        if (Test-Path $msbuild15Path) {
            Write-Debug "Found 'msbuild.exe 15' at '${msbuild15Path}'"
            return $msbuild15Path
        }

        throw "'msbuild.exe 15' located at '${msbuild15Path}' doesn't exist"
    }
    else {
        Write-Host "Trying to find 'msbuild.exe 16' using 'MSBUILD_PATH' environment variable"
        $msbuild16Env = "MSBUILD_PATH"
        $msbuild16Path = [environment]::GetEnvironmentVariable($msbuild16Env, "Process")

        if (!$msbuild16Path) {
            Write-Host "Environment variable not found"
            Write-Host "Trying to find path using 'vswhere.exe'"

            # Sets the path to MSBuild 16 into an the MSBUILD_PATH environment variable
            # All subsequent builds after this command will use MSBuild 15!
            # Test if vswhere.exe is in your path. Download from: https://github.com/Microsoft/vswhere/releases
            $path = Exec { & (Get-VsWherePath) -version "[16.0, 17.0)" -products * -requires Microsoft.Component.MSBuild `
                -property installationPath } | Select-Object -First 1
            if ($path) {
                $msbuild16Path = Join-Path $path "MSBuild\16.0\Bin\MSBuild.exe"
                [environment]::SetEnvironmentVariable($msbuild16Env, $msbuild16Path)
            }
        }

        if (Test-Path $msbuild16Path) {
            Write-Debug "Found 'msbuild.exe 16' at '${msbuild16Path}'"
            return $msbuild16Path
        }

        throw "'msbuild.exe 16' located at '${msbuild16Path}' doesn't exist"
    }
}

function Get-VsTestPath {
    return Get-ExecutablePath -name "VSTest.Console.exe" -envVar "VSTEST_PATH"
}

function Get-CodeCoveragePath {
    $vstest_exe = Get-VsTestPath
    $codeCoverageDirectory = Join-Path (Get-ChildItem $vstest_exe).Directory "..\..\..\..\.."
    return Get-ExecutablePath -name "CodeCoverage.exe" -directory $codeCoverageDirectory -envVar "CODE_COVERAGE_PATH"
}

function Expand-ZIPFile($source, $destination) {
    Write-Host "Expanding ZIP file ${source}"
    $application = New-Object -Com Shell.Application
    $zip = $application.NameSpace($source)
    foreach ($item in $zip.items()) {
        $application.NameSpace($destination).CopyHere($item, 0x14)
    }
}

function Get-ScannerMsBuildPath() {
    $currentDir = (Resolve-Path .\).Path
    $scannerMsbuild = Join-Path $currentDir "SonarScanner.MSBuild.exe"

    if (-Not (Test-Path $scannerMsbuild)) {
        Write-Host "Scanner for MSBuild not found, downloading it"

        if ($env:ARTIFACTORY_URL)
        {
            Write-Host "Environment variable ARTIFACTORY_URL = $env:ARTIFACTORY_URL"
        }
        else
        {
            # We want this to be a terminating error so we throw
            Throw "Environment variable ARTIFACTORY_URL is not set"
        }

        # This links always redirect to the latest released scanner
        $downloadLink = "$env:ARTIFACTORY_URL/sonarsource-public-releases/org/sonarsource/scanner/msbuild/" +
            "sonar-scanner-msbuild/%5BRELEASE%5D/sonar-scanner-msbuild-%5BRELEASE%5D-net46.zip"
        $scannerMsbuildZip = Join-Path $currentDir "\SonarScanner.MSBuild.zip"

        Write-Host "Scanner for MSBuild not found, downloading it" "Downloading scanner from '${downloadLink}' at '${currentDir}'"

        # NB: the WebClient class defaults to TLS v1, which is no longer supported by GitHub/Artifactory online
        [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12
        (New-Object System.Net.WebClient).DownloadFile($downloadLink, $scannerMsbuildZip)

        # perhaps we could use other folder, not the repository root
        Expand-ZIPFile $scannerMsbuildZip $currentDir

        Write-Host "Deleting downloaded zip"
        Remove-Item $scannerMsbuildZip -Force
    }

    Write-Host "Scanner for MSBuild found at '$scannerMsbuild'"
    return $scannerMsbuild
}

function Set-Version {
    Write-Header "Updating version in all files..."

    $buildNumber = Get-BuildNumber
    $branchName = Get-BranchName
    $sha1 = Get-Sha1

    Write-Host "Setting build number ${buildNumber}, sha1 ${sha1} and branch ${branchName}"

    $versionPropsPath = (Resolve-RepoPath "build\Version.props")

    (Get-Content $versionPropsPath) `
 -Replace '<Sha1>.*</Sha1>', "<Sha1>$sha1</Sha1>" `
 -Replace '<BuildNumber>\d+</BuildNumber>', "<BuildNumber>$buildNumber</BuildNumber>" `
 -Replace '<BranchName>.*</BranchName>', "<BranchName>$branchName</BranchName>" `
        | Set-Content $versionPropsPath

    $msbuild_exe = Get-MsBuildPath
    $changeVersionProj = (Resolve-RepoPath build\ChangeVersion.proj)
    Exec { & $msbuild_exe $changeVersionProj }

    $version = Get-Version
    Write-Host "Version successfully set to '${version}'"
}

function Get-Version {
    [xml]$versionProps = Get-Content (Resolve-RepoPath ".\build\Version.props")
    return $versionProps.Project.PropertyGroup.MainVersion + "." + $versionProps.Project.PropertyGroup.BuildNumber
}

function Restore-Packages (
    [Parameter(Mandatory = $true, Position = 0)][ValidateSet("14.0", "15.0", "16.0")][string]$msbuildVersion,
    [Parameter(Mandatory = $true, Position = 1)][string]$solutionPath) {

    $solutionName = Split-Path $solutionPath -Leaf
    Write-Header "Restoring NuGet packages for ${solutionName}"

    $msbuildBinDir = Split-Path -Parent (Get-MsBuildPath $msbuildVersion)

    if (Test-Debug) {
        Exec { & (Get-NuGetPath) restore $solutionPath -MSBuildPath $msbuildBinDir -Verbosity detailed `
        } -errorMessage "ERROR: Restoring NuGet packages FAILED."
    }
    else {
        Exec { & (Get-NuGetPath) restore $solutionPath -MSBuildPath $msbuildBinDir `
        } -errorMessage "ERROR: Restoring NuGet packages FAILED."
    }
}

function Invoke-SonarBeginAnalysis([array][parameter(ValueFromRemainingArguments = $true)] $remainingArgs) {
    Write-Header "Running SonarCloud Analysis begin step"

    if (Test-Debug) {
        $remainingArgs += "/d:sonar.verbose=true"
    }

    Exec { & (Get-ScannerMsBuildPath) begin `
        /k:sonarlint-visualstudio `
        /n:"SonarLint for Visual Studio" `
        /d:sonar.host.url=${sonarCloudUrl} `
        /d:sonar.login=$sonarCloudToken `
        /o:sonarsource `
        /d:sonar.cs.vstest.reportsPaths="**\*.trx" `
        /d:sonar.cs.vscoveragexml.reportsPaths="**\*.coveragexml" `
        /d:sonar.analysis.sha1=$githubSha1 `
        /d:sonar.analysis.repository=$githubRepo `
        $remainingArgs `
    } -errorMessage "ERROR: SonarCloud Analysis begin step FAILED."
}

function Invoke-SonarEndAnalysis() {
    Write-Header "Running SonarCloud Analysis end step"

    Exec { & (Get-ScannerMsBuildPath) end `
        /d:sonar.login=$sonarCloudToken `
    } -errorMessage "ERROR: SonarCloud Analysis end step FAILED."
}

function Invoke-MSBuild (
    [Parameter(Mandatory = $true, Position = 0)][ValidateSet("14.0", "15.0", "16.0")][string]$msbuildVersion,
    [Parameter(Mandatory = $true, Position = 1)][string]$solutionPath,
    [parameter(ValueFromRemainingArguments = $true)][array]$remainingArgs) {

    $solutionName = Split-Path $solutionPath -leaf
    Write-Header "Building solution ${solutionName}"

    if (Test-Debug) {
        $remainingArgs += "/v:detailed"
    }
    else {
        $remainingArgs += "/v:quiet"
    }

    $remainingArgs += "/t:rebuild"

    $msbuildExe = Get-MsBuildPath $msbuildVersion
    Exec { & $msbuildExe $solutionPath $remainingArgs `
    } -errorMessage "ERROR: Build FAILED."
}

function Invoke-UnitTests() {
    Write-Header "Running unit tests"

    $testFiles = @()
    $testFiles += Collect-UnitTestAssemblies("sonarqube-webclient")
    $testFiles += Collect-UnitTestAssemblies("src")

    & (Get-VsTestPath) $testFiles /Parallel /Enablecodecoverage /InIsolation /Logger:trx /UseVsixExtensions:true
    Test-ExitCode "ERROR: Unit Tests execution FAILED."
}

function Collect-UnitTestAssemblies([string] $rootSearchDirectory) {
    Write-Host "Collecting unit test assemblies under ${rootSearchDirectory}..."

    $testFiles = @()    
    Get-ChildItem (Resolve-RepoPath $rootSearchDirectory) -Recurse -Include @("*.UnitTests.dll", "*.Tests.dll") `
        | Where-Object { $_.DirectoryName -Match "bin" } `
        | ForEach-Object {
            $currentFile = $_
            Write-Host "   - ${currentFile}"
            $testFiles += $currentFile
        }

    return $testFiles
}

function Invoke-CodeCoverage() {
    Write-Header "Creating coverage report"

    $codeCoverageExe = Get-CodeCoveragePath

    Write-Host "Generating code coverage reports for"
    Get-ChildItem "TestResults" -Recurse -Include "*.coverage" | ForEach-Object {
        Write-Host "    -" $_.FullName

        $filePathWithNewExtension = $_.FullName + "xml"
        if (Test-Path $filePathWithNewExtension) {
            Write-Debug "Coveragexml report already exists, removing it"
            Remove-Item -Force $filePathWithNewExtension
        }

        Exec { & $codeCoverageExe analyze /output:$filePathWithNewExtension $_.FullName `
        } -errorMessage "ERROR: Code coverage reports generation FAILED."
    }
}