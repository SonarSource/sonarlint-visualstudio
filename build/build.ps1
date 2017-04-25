[CmdletBinding(PositionalBinding = $false)]
param (
    [switch]$analyze = $false,
    [switch]$test = $false,
    [switch]$coverage = $false,
    [switch]$debugBuild = $false,

    [string]$githubRepo,
    [string]$githubToken,
    [string]$githubPullRequest,

    [string]$isPullRequest,

    [string]$sonarQubeProjectName = "SonarLint for Visual Studio",
    [string]$sonarQubeProjectKey = "sonarlint-visualstudio",
    [string]$sonarQubeUrl = "http://localhost:9000",
    [string]$sonarQubeToken = $null,

    [string]$solutionName = "SonarLint.VisualStudio.Integration.sln",
    [string]$certificatePath,

    [parameter(ValueFromRemainingArguments = $true)] $badArgs)

$ErrorActionPreference = "Stop"

try {
    if ($badArgs -Ne $null) {
        throw "Bad arguments: $badArgs"
    }

    . (Join-Path $PSScriptRoot "build-utils.ps1")

    $branchName = Get-BranchName
    $isMaster = $branchName -Eq "master"

    Write-Header "Temporary info Analyze=${analyze} Branch=${branchName} PR=${isPullRequest}"

    Set-Version

    $skippedAnalysis = $false
    if ($analyze -And $isPullRequest -Eq "true") {
        Write-Host "Pull request '${githubPullRequest}'"

        Begin-Analysis $sonarQubeUrl $sonarQubeToken $sonarQubeProjectKey $sonarQubeProjectName `
            /d:sonar.github.pullRequest=$githubPullRequest `
            /d:sonar.github.repository=$githubRepo `
            /d:sonar.github.oauth=$githubToken `
            /d:sonar.analysis.mode="issues" `
            /d:sonar.scanAllFiles="true" `
            /v:"latest"
    }
    elseif ($analyze -And $isMaster) {
        Write-Host "Is master '${isMaster}'"

        $testResultsPath = Resolve-RepoPath ""
        Write-Host "Looking for reports in: ${testResultsPath}"

        Begin-Analysis $sonarQubeUrl $sonarQubeToken $sonarQubeProjectKey $sonarQubeProjectName `
            /v:"master" `
            /d:sonar.cs.vstest.reportsPaths="${testResultsPath}\**\*.trx" `
            /d:sonar.cs.vscoveragexml.reportsPaths="${testResultsPath}\**\*.coveragexml"
    }
    else {
        $skippedAnalysis = $true
    }

    $solutionRelativePath="src\${solutionName}"
    if ($solutionRelativePath.Contains("2017")) {
        Restore-Packages $solutionRelativePath
        Start-Process "build/vs2017.bat" -NoNewWindow -Wait
    } else {
        if ($debugBuild) {
            Build-Solution (Resolve-RepoPath $solutionRelativePath)
        }
        else {
            Build-ReleaseSolution (Resolve-RepoPath $solutionRelativePath) $certificatePath
        }
    }

    if ($test) {
        Run-Tests $coverage
    }

    if (-Not $skippedAnalysis) {
        End-Analysis $sonarQubeToken
    }
} catch {
    Write-Host $_
    exit 1
}