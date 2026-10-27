# SLVS-3097: exercise a built VSIX through the real Visual Studio Error List.
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $VsixPath,
    [string] $ResultsDirectory = (Join-Path ([IO.Path]::GetTempPath()) ('slvs3097-' + [guid]::NewGuid().ToString('N'))),
    [ValidateRange(30, 600)]
    [int] $TimeoutSeconds = 180
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
    throw 'This spike requires Windows and a full Visual Studio 2022 installation.'
}
if ([Threading.Thread]::CurrentThread.ApartmentState -ne 'STA') {
    throw 'Run this script using powershell.exe -STA.'
}

$VsixPath = (Resolve-Path -LiteralPath $VsixPath).Path
if ([IO.Path]::GetExtension($VsixPath) -ne '.vsix') {
    throw 'VsixPath must identify a built .vsix package.'
}
$ResultsDirectory = [IO.Path]::GetFullPath($ResultsDirectory)
if (Test-Path -LiteralPath $ResultsDirectory) {
    throw 'Use a new ResultsDirectory for each run so evidence cannot be overwritten.'
}
$null = New-Item -ItemType Directory -Path $ResultsDirectory
$rootSuffix = 'SLVS3097' + [guid]::NewGuid().ToString('N').Substring(0, 8)
$report = [ordered]@{
    status = 'failed'
    phase = 'prerequisites'
    rootSuffix = $rootSuffix
    vsixSha256 = (Get-FileHash -LiteralPath $VsixPath -Algorithm SHA256).Hash
    startedAt = [DateTime]::UtcNow.ToString('o')
}
$ideProcess = $null
$dte = $null
$script:lastErrors = @()
$script:lastComError = $null
$transcriptStarted = $false

function Invoke-BoundedProcess([string] $FilePath, [string[]] $Arguments) {
    $process = Start-Process -FilePath $FilePath -ArgumentList $Arguments -PassThru
    if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
        Stop-Process -Id $process.Id -ErrorAction SilentlyContinue
        throw "Process timed out: $FilePath"
    }
    if ($process.ExitCode -ne 0) {
        throw "Process failed with exit code $($process.ExitCode): $FilePath"
    }
}

function Wait-ForCondition([string] $Description, [scriptblock] $Condition) {
    $watch = [Diagnostics.Stopwatch]::StartNew()
    while ($watch.Elapsed.TotalSeconds -lt $TimeoutSeconds) {
        try {
            if (& $Condition) { return }
        }
        catch [Runtime.InteropServices.COMException] {
            $script:lastComError = $_.Exception.Message
        }
        catch [Management.Automation.MethodInvocationException] {
            if ($_.Exception.InnerException -isnot [Runtime.InteropServices.COMException]) { throw }
            $script:lastComError = $_.Exception.InnerException.Message
        }
        Start-Sleep -Milliseconds 500
    }
    throw "Timed out waiting for $Description. Last COM error: $script:lastComError"
}

function Get-ErrorListEntries {
    $items = $dte.ToolWindows.ErrorList.ErrorItems
    $entries = @()
    for ($index = 1; $index -le $items.Count; $index++) {
        $item = $items.Item($index)
        $entries += [pscustomobject]@{
            file = $item.FileName
            line = $item.Line
            description = $item.Description
        }
    }
    $script:lastErrors = $entries
    return $entries
}

function Get-SmokeFinding {
    Get-ErrorListEntries | Where-Object {
        $_.file -eq $sourcePath -and
        $_.line -eq 5 -and
        $_.description -like "*Remove the unused local variable 'smokeUnused'*"
    }
}

try {
    Start-Transcript -Path (Join-Path $ResultsDirectory 'transcript.txt') | Out-Null
    $transcriptStarted = $true
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::OpenRead($VsixPath)
    try {
        $entry = $archive.GetEntry('extension.vsixmanifest')
        if ($null -eq $entry) { throw 'The VSIX has no extension.vsixmanifest.' }
        $reader = [IO.StreamReader]::new($entry.Open())
        try { [xml] $manifest = $reader.ReadToEnd() }
        finally { $reader.Dispose() }
        $identity = $manifest.SelectSingleNode("/*[local-name()='PackageManifest']/*[local-name()='Metadata']/*[local-name()='Identity']")
        if ($null -eq $identity -or $identity.Id -ne 'SonarLint.0DE19254-1DCA-4479-8EAF-58E5D677FF4D') {
            throw 'The supplied VSIX is not SonarQube for Visual Studio.'
        }
        $report['vsixVersion'] = $identity.Version
    }
    finally { $archive.Dispose() }
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    $instances = @(& $vswhere -products '*' -version '[17.0,18.0)' -requires Microsoft.VisualStudio.Component.CoreEditor Microsoft.Net.Component.4.7.2.TargetingPack -format json | ConvertFrom-Json)
    if ($LASTEXITCODE -ne 0 -or $instances.Count -ne 1) {
        throw 'Expected exactly one VS 2022 installation with the editor and .NET Framework 4.7.2 targeting pack.'
    }
    $instance = $instances[0]
    $devenv = Join-Path $instance.installationPath 'Common7\IDE\devenv.exe'
    $installer = Join-Path $instance.installationPath 'Common7\IDE\VSIXInstaller.exe'
    $settings = Join-Path $instance.installationPath 'Common7\IDE\Profiles\General.vssettings'
    foreach ($required in @($devenv, $installer, $settings)) {
        if (-not (Test-Path -LiteralPath $required)) { throw "Missing prerequisite: $required" }
    }
    $report['visualStudioVersion'] = $instance.installationVersion
    $report['instanceId'] = $instance.instanceId
    Add-Type -Path (Join-Path $PSScriptRoot 'VisualStudioAutomation.cs')

    $report.phase = 'initialize-profile'
    Invoke-BoundedProcess $devenv @('/RootSuffix', $rootSuffix, '/ResetSettings', "`"$settings`"", '/Command', 'File.Exit', '/NoSplash', '/LCID', '1033')

    $report.phase = 'install-vsix'
    $installLog = Join-Path $ResultsDirectory 'vsix-installer.log'
    Invoke-BoundedProcess $installer @('/quiet', "/instanceIds:$($instance.instanceId)", "/rootSuffix:$rootSuffix", "/logFile:`"$installLog`"", "`"$VsixPath`"")

    $workspace = Join-Path $ResultsDirectory 'fixture'
    $null = New-Item -ItemType Directory -Path $workspace
    $sourcePath = Join-Path $workspace 'Smoke.cs'
    $projectPath = Join-Path $workspace 'Smoke.csproj'
    $solutionPath = Join-Path $workspace 'Smoke.sln'
    $badSource = @'
public class Smoke
{
    public int Run()
    {
        int smokeUnused = 42;
        return 0;
    }
}
'@
    Set-Content -LiteralPath $sourcePath -Value $badSource -Encoding UTF8
    @'
<Project ToolsVersion="15.0" DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <ProjectGuid>{657836E3-1079-4BFB-BA79-974702074374}</ProjectGuid>
    <OutputType>Library</OutputType>
    <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
    <OutputPath>bin\</OutputPath>
  </PropertyGroup>
  <ItemGroup><Compile Include="Smoke.cs" /></ItemGroup>
  <Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />
</Project>
'@ | Set-Content -LiteralPath $projectPath -Encoding UTF8
    @'
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Smoke", "Smoke.csproj", "{657836E3-1079-4BFB-BA79-974702074374}"
EndProject
Global
    GlobalSection(SolutionConfigurationPlatforms) = preSolution
        Debug|Any CPU = Debug|Any CPU
    EndGlobalSection
    GlobalSection(ProjectConfigurationPlatforms) = postSolution
        {657836E3-1079-4BFB-BA79-974702074374}.Debug|Any CPU.ActiveCfg = Debug|AnyCPU
        {657836E3-1079-4BFB-BA79-974702074374}.Debug|Any CPU.Build.0 = Debug|AnyCPU
    EndGlobalSection
EndGlobal
'@ | Set-Content -LiteralPath $solutionPath -Encoding UTF8

    $report.phase = 'launch-ide'
    $activityLog = Join-Path $ResultsDirectory 'ActivityLog.xml'
    $ideProcess = Start-Process -FilePath $devenv -ArgumentList @('/RootSuffix', $rootSuffix, '/NoSplash', '/LCID', '1033', '/Log', "`"$activityLog`"") -PassThru
    Wait-ForCondition 'the launched IDE to publish its DTE object' {
        $script:dte = [SonarLint.VisualStudio.Smoke.VisualStudioAutomation]::FindDte($ideProcess.Id)
        return $null -ne $script:dte
    }
    Wait-ForCondition 'the solution to open' {
        if (-not $dte.Solution.IsOpen) { $dte.Solution.Open($solutionPath) }
        return $dte.Solution.IsOpen
    }
    Wait-ForCondition 'the fixture file to open' {
        $null = $dte.ItemOperations.OpenFile($sourcePath)
        return $null -ne $dte.ActiveDocument -and $dte.ActiveDocument.FullName -eq $sourcePath
    }
    $dte.ExecuteCommand('View.ErrorList')

    $report.phase = 'detect-finding'
    Wait-ForCondition 'Sonar S1481 in the real Error List' { return @((Get-SmokeFinding)).Count -gt 0 }
    $report['finding'] = @(Get-SmokeFinding)
    $script:lastErrors | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $ResultsDirectory 'errors-before-fix.json') -Encoding UTF8

    $report.phase = 'fix-file'
    $document = $dte.ActiveDocument
    $selection = $document.Selection
    $selection.SelectAll()
    $selection.Insert($badSource.Replace('        int smokeUnused = 42;', ''))
    $document.Save()

    $report.phase = 'clear-finding'
    $clearSince = [Diagnostics.Stopwatch]::new()
    Wait-ForCondition 'the finding to remain absent for five seconds after saving' {
        if (@((Get-SmokeFinding)).Count -gt 0) {
            $clearSince.Reset()
            return $false
        }
        $clearSince.Start()
        return $clearSince.Elapsed.TotalSeconds -ge 5
    }
    $script:lastErrors | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $ResultsDirectory 'errors-after-fix.json') -Encoding UTF8
    $report.status = 'passed'
    $report.phase = 'complete'
}
catch {
    $report['error'] = $_.Exception.ToString()
    throw
}
finally {
    $report['finishedAt'] = [DateTime]::UtcNow.ToString('o')
    $report | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $ResultsDirectory 'result.json') -Encoding UTF8
    ConvertTo-Json -InputObject @($script:lastErrors) -Depth 5 | Set-Content (Join-Path $ResultsDirectory 'last-error-list.json') -Encoding UTF8
    if ($null -ne $ideProcess -and -not $ideProcess.HasExited) {
        Stop-Process -Id $ideProcess.Id -ErrorAction SilentlyContinue
    }
    if ($transcriptStarted) { Stop-Transcript | Out-Null }
}
