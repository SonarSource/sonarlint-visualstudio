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

    Write-Header "Temporary info Analyze=${analyze} Branch=${branchName} PR=${isPullRequest}"

    Set-Version

    $solutionRelativePath="src\${solutionName}"
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

    ConvertTo-SignedExtension
} catch {
    Write-Host $_
    exit 1
}
