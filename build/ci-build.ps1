[CmdletBinding(PositionalBinding = $false)]
param (
    [ValidateSet("vs2015", "vs2017")]
    [string]$vsTargetVersion = "vs2017",

    # GitHub related parameters
    [string]$githubRepo = $env:GITHUB_REPO,
    [string]$githubToken = $env:GITHUB_TOKEN,
    [string]$githubSha1 = $env:GIT_SHA1,
    # GitHub PR related parameters
    [string]$githubPullRequest = $env:PULL_REQUEST,
    [string]$githubIsPullRequest = $env:IS_PULLREQUEST,
    [string]$githubPRBaseBranch = $env:GITHUB_BASE_BRANCH,
    [string]$githubPRTargetBranch = $env:GITHUB_TARGET_BRANCH,

    # SonarCloud related parameters
    [string]$sonarCloudUrl = $env:SONARCLOUD_HOST_URL,
    [string]$sonarCloudToken = $env:SONARCLOUD_TOKEN,

    # Others
    [string]$snkCertificatePath = $env:CERT_PATH,
    [string]$pfxCertificatePath = $env:PFX_PATH,
    [string]$pfxPassword = $env:PFX_PASSWORD,

    [parameter(ValueFromRemainingArguments = $true)] $badArgs
)

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

if ($PSBoundParameters['Verbose'] -Or $PSBoundParameters['Debug']) {
    $global:DebugPreference = "Continue"
}

function Get-LeakPeriodVersion() {
    [xml]$versionProps = Get-Content "${PSScriptRoot}\Version.props"
    $mainVersion = $versionProps.Project.PropertyGroup.MainVersion

    Write-Debug "Leak period version is '${mainVersion}'"

    return $mainVersion
}

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
    $isMaster = $branchName -eq "master"
    # See https://xtranet.sonarsource.com/display/DEV/Release+Procedures for info about maintenance branches
    $isMaintenanceBranch = $branchName -like 'branch-*'
    $isFeatureBranch = $branchName -like 'feature/*'
    $isPullRequest = $githubIsPullRequest -eq "true"
    $solutionName = "SonarLint.VisualStudio.Integration.sln"

    Write-Host "Branch: ${branchName}"

    Set-Version

    $skippedAnalysis = $false

    $solutionRelativePath = "${solutionName}"
    if ($vsTargetVersion -Eq "vs2017") {
        Write-Host "VS target version: 2017"
        $skippedAnalysis = $true # We only want to analyze one of the VS2015 / VS2017 builds, not both, so we skip analyzing VS2017
        Write-Host "  NB: this build will not be analyzed. Check the VS2015 build for analysis results"
        Restore-Packages "15.0" $solutionRelativePath
        Start-Process "build/vs2017.bat" -NoNewWindow -Wait
    }
    else {
        $leakPeriodVersion = Get-LeakPeriodVersion

        if ($isPullRequest) {
            Write-Host "Build kind: PR"
            Write-Host "PR: ${githubPullRequest}"
            Write-Host "PR source: ${githubPRBaseBranch}"
            Write-Host "PR target: ${githubPRTargetBranch}"

            Invoke-SonarBeginAnalysis `
                /v:$leakPeriodVersion `
                /d:sonar.analysis.prNumber=$githubPullRequest `
                /d:sonar.pullrequest.key=$githubPullRequest `
                /d:sonar.pullrequest.branch=$githubPRBaseBranch `
                /d:sonar.pullrequest.base=$githubPRTargetBranch `
                /d:sonar.pullrequest.provider=github
        }
        elseif ($isMaster) {
            Write-Host "Build kind: master"

            Invoke-SonarBeginAnalysis `
                /v:$leakPeriodVersion `
                /d:sonar.analysis.buildNumber=$buildNumber `
                /d:sonar.analysis.pipeline=$buildNumber
        }
        elseif ($isMaintenanceBranch -or $isFeatureBranch) {
            if ($isMaintenanceBranch) {
                Write-Host "Build kind: maintenance branch"
            }
            else {
                Write-Host "Build kind: feature branch"
            }

            Invoke-SonarBeginAnalysis `
                /v:$leakPeriodVersion `
                /d:sonar.analysis.buildNumber=$buildNumber `
                /d:sonar.analysis.pipeline=$buildNumber `
                /d:sonar.branch.name=$branchName
        }
        else {
            Write-Host "Build kind: branch"
            Write-Host "  Skipping analysis - branch builds are not analyzed"

            $skippedAnalysis = $true
        }

        Write-Host "VS target version: 2015"

        Restore-Packages "14.0" $solutionRelativePath
        Invoke-MSBuild "14.0" $solutionRelativePath `
            /consoleloggerparameters:Summary `
            /m `
            /p:configuration="Release" `
            /p:DeployExtension=false `
            /p:ZipPackageCompressionLevel=normal `
            /p:defineConstants="SignAssembly" `
            /p:SignAssembly=true `
            /p:AssemblyOriginatorKeyFile=$snkCertificatePath
    }

    Invoke-UnitTests
    Invoke-CodeCoverage

    if (-Not $skippedAnalysis) {
        Invoke-SonarEndAnalysis
    }

    ConvertTo-SignedExtension

    Write-Host -ForegroundColor Green "SUCCESS: BUILD job was successful!"
    exit 0
} catch {
    Write-Host -ForegroundColor Red $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    exit 1
}