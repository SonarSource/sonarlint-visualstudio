<#
.SYNOPSIS
Verifies that all SonarLint assemblies inside a VSIX package carry a valid Authenticode signature.

.DESCRIPTION
This script:
1. Validates that the VSIX file exists at the expected path.
2. Extracts the VSIX (which is a ZIP/OPC package) into a temporary directory.
3. Enumerates all SonarLint*.dll files inside the extracted contents.
4. Checks each DLL for a valid Authenticode signature using Get-AuthenticodeSignature.
5. Fails with a non-zero exit code if any assembly is unsigned or has an invalid signature.

The script reads its required inputs from environment variables.

.INPUTS (from environment)
PROJECT_VERSION: The full project version string (e.g. 9.9.0.16400) used to locate the VSIX file.
RUNNER_TEMP: Runner temporary directory used for extracting the VSIX contents.

.EXAMPLE
.\verify-signature.ps1
#>
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$projectVersion = $env:PROJECT_VERSION
$tempDirectory = $env:RUNNER_TEMP

if ([string]::IsNullOrWhiteSpace($projectVersion)) {
  throw "Missing required environment variable: PROJECT_VERSION"
}
if ([string]::IsNullOrWhiteSpace($tempDirectory)) {
  throw "Missing required environment variable: RUNNER_TEMP"
}

$projectVersion = $projectVersion.Trim()
$tempDirectory = $tempDirectory.Trim()

if (-not (Test-Path -LiteralPath $tempDirectory)) {
  throw "Temp directory does not exist: $tempDirectory"
}

$vsixPath = "binaries/SonarLint.VSIX-$projectVersion-2022.vsix"
if (-not (Test-Path -LiteralPath $vsixPath)) {
  throw "VSIX file not found at expected path: $vsixPath"
}

$extractDir = Join-Path $tempDirectory "vsix-verify"
if (Test-Path -LiteralPath $extractDir) {
  Remove-Item -Recurse -Force -LiteralPath $extractDir
}

# VSIX is a ZIP archive but Expand-Archive only accepts .zip extensions.
# Copy to a .zip temporary file before extracting.
$zipCopy = Join-Path $tempDirectory "vsix-verify.zip"
Copy-Item -LiteralPath $vsixPath -Destination $zipCopy -Force

Write-Host "Extracting $vsixPath to $extractDir"
Expand-Archive -Path $zipCopy -DestinationPath $extractDir -Force
Remove-Item -LiteralPath $zipCopy -Force

$dlls = Get-ChildItem -Path $extractDir -Recurse -Filter "SonarLint*.dll"
if ($dlls.Count -eq 0) {
  throw "No SonarLint*.dll files found inside the VSIX. Verification cannot proceed."
}

Write-Host "Found $($dlls.Count) SonarLint assembly(ies) to verify."

$failed = $false
foreach ($dll in $dlls) {
  $sig = Get-AuthenticodeSignature -FilePath $dll.FullName
  if ($sig.Status -ne 'Valid') {
    Write-Error "Invalid or missing signature on $($dll.Name): $($sig.Status)"
    $failed = $true
  } else {
    Write-Host "Valid signature on $($dll.Name): $($sig.SignerCertificate.Subject)"
  }
}

if ($failed) {
  throw "One or more SonarLint assemblies in the VSIX have invalid or missing signatures."
}

Write-Host "All SonarLint assemblies in the VSIX are correctly signed."
