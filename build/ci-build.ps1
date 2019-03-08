[CmdletBinding(PositionalBinding = $false)]
param (
    [switch]$analyze = $false,
    [switch]$test = $false,
    [switch]$coverage = $false,
    [switch]$debugBuild = $false,

    [string]$githubRepo,
    [string]$githubToken,
    [string]$githubSha1 = $env:GIT_SHA1,

    # GitHub PR related parameters
    [string]$githubPullRequest,
    [string]$isPullRequest,
    [string]$githubPRBaseBranch = $env:GITHUB_BASE_BRANCH,
    [string]$githubPRTargetBranch = $env:GITHUB_TARGET_BRANCH,

    [string]$sonarQubeProjectName = "SonarLint for Visual Studio",
    [string]$sonarQubeProjectKey = "sonarlint-visualstudio",
    [string]$sonarQubeUrl = "http://localhost:9000",
    [string]$sonarQubeToken = $null,

    [string]$solutionName = "SonarLint.VisualStudio.Integration.sln",
    [string]$snkCertificatePath,

    [string]$pfxCertificatePath,
    [string]$pfxPassword,

    [parameter(ValueFromRemainingArguments = $true)] $badArgs)
    
$ErrorActionPreference = "Stop"

function Get-VsixSignTool() {
    $vsixSignTool = Get-ChildItem (Resolve-RepoPath "packages") -Recurse -Include "vsixsigntool.exe" | Select-Object -First 1
    if (!$vsixsigntool) {
        throw "ERROR: Cannot find vsixsigntool.exe, please make sure the NuGet package is properly installed."
    }

    return $vsixsigntool.FullName
}

function ConvertTo-SignedExtension() {
    Write-Header "Signing the VSIX extensions..."
    $vsixSignTool = Get-VsixSignTool

    $binariesFolder = Resolve-RepoPath "binaries"

    if (!$pfxCertificatePath) {
        throw "ERROR: Path to the PFX is not set."
    }
    if (!$pfxPassword) {
        throw "ERROR: PFX password is not set."
    }

    $anyVsixFileFound = $false
    Get-ChildItem $binariesFolder -Recurse -Include "*.vsix" | ForEach-Object {
        $anyVsixFileFound = $true
        & $vsixSignTool sign /f $pfxCertificatePath /p $pfxPassword /sha1 658bcf5f55f33bfe4699fffca667468c15e42c40 $_
        Test-ExitCode "ERROR: VSIX Extension signing FAILED."
    }

    if (!$anyVsixFileFound) {
        throw "ERROR: Cannot find any VSIX file to sign in '${binariesFolder}'."
    }
}

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

        Write-Host "PR: ${githubPullRequest}"
        Write-Host "PR Sha1: ${githubSha1}"
        Write-Host "PR source: ${githubPRBaseBranch}"
        Write-Host "PR target: ${githubPRTargetBranch}"

        Begin-Analysis $sonarQubeUrl $sonarQubeToken $sonarQubeProjectKey $sonarQubeProjectName `
            /d:sonar.analysis.prNumber=$githubPullRequest `
            /d:sonar.analysis.sha1=$githubSha1 `
            /d:sonar.pullrequest.key=$githubPullRequest `
            /d:sonar.pullrequest.branch=$githubPRBaseBranch `
            /d:sonar.pullrequest.base=$githubPRTargetBranch `
            /d:sonar.pullrequest.provider=github `
            /d:sonar.pullrequest.github.repository=$githubRepo `
            /v:"latest"

    }
    elseif ($analyze -And $isMaster) {
        Write-Host "Is master '${isMaster}'"

        $buildNumber = Get-BuildNumber
        $sha1 = Get-Sha1

        $testResultsPath = Resolve-RepoPath ""
        Write-Host "Looking for reports in: ${testResultsPath}"

        Begin-Analysis $sonarQubeUrl $sonarQubeToken $sonarQubeProjectKey $sonarQubeProjectName `
            /d:sonar.analysis.buildNumber=$buildNumber `
            /d:sonar.analysis.pipeline=$buildNumber `
            /d:sonar.analysis.sha1=$sha1 `
            /d:sonar.analysis.repository=$githubRepo `
            /v:"master" `
            /d:sonar.cs.vstest.reportsPaths="${testResultsPath}\**\*.trx" `
            /d:sonar.cs.vscoveragexml.reportsPaths="${testResultsPath}\**\*.coveragexml"
    }
    else {
        $skippedAnalysis = $true
    }

    $solutionRelativePath = "${solutionName}"
    if ($solutionRelativePath.Contains("2017")) {
        Restore-Packages $solutionRelativePath
        Start-Process "build/vs2017.bat" -NoNewWindow -Wait
    } else {
        if ($debugBuild) {
            Build-Solution (Resolve-RepoPath $solutionRelativePath)
        }
        else {
            Build-ReleaseSolution (Resolve-RepoPath $solutionRelativePath) $snkCertificatePath
        }
    }

    if ($test) {
        Run-Tests $coverage
    }

    if (-Not $skippedAnalysis) {
        End-Analysis $sonarQubeToken
    }

    ConvertTo-SignedExtension
} catch {
    Write-Host $_
    exit 1
}
