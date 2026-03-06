<#
.SYNOPSIS
Imports the Azure Artifact Signing certificate chain required by test signing profiles.

.DESCRIPTION
This script:
1. Installs the Az.ArtifactSigning module.
2. Uses the current authenticated Az context (prepared by azure-login.ps1).
3. Downloads the active certificate chain from Azure Artifact Signing.
4. Imports certificates into Root/CA/My and TrustedPublisher stores (LocalMachine and CurrentUser).

The script reads inputs from environment variables.
Precondition: an authenticated Az context must already exist.

.INPUTS (from environment)
SIGNING_ACCOUNT: Artifact Signing account name (for example: codesigning-test).
SIGNING_CERTIFICATE_PROFILE: Artifact Signing certificate profile name (for example: sonarsource-test).
SIGNING_ENDPOINT: Artifact Signing endpoint URL (for example: https://weu.codesigning.azure.net/).
RUNNER_TEMP: Temporary working directory used to store downloaded chain files.

.EXAMPLE
.\import-test-signing-certificate-chain.ps1
#>
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$signingAccountName = $env:SIGNING_ACCOUNT
$certificateProfileName = $env:SIGNING_CERTIFICATE_PROFILE
$signingEndpoint = $env:SIGNING_ENDPOINT
$tempDirectory = $env:RUNNER_TEMP

if ([string]::IsNullOrWhiteSpace($signingAccountName)) {
  throw "Missing required environment variable: SIGNING_ACCOUNT"
}
if ([string]::IsNullOrWhiteSpace($certificateProfileName)) {
  throw "Missing required environment variable: SIGNING_CERTIFICATE_PROFILE"
}
if ([string]::IsNullOrWhiteSpace($signingEndpoint)) {
  throw "Missing required environment variable: SIGNING_ENDPOINT"
}
if ([string]::IsNullOrWhiteSpace($tempDirectory)) {
  throw "Missing required environment variable: RUNNER_TEMP"
}

$signingAccountName = $signingAccountName.Trim()
$certificateProfileName = $certificateProfileName.Trim()
$signingEndpoint = $signingEndpoint.Trim()
$tempDirectory = $tempDirectory.Trim()

if (-not $signingEndpoint.StartsWith("https://")) {
  throw "SIGNING_ENDPOINT must start with https://"
}
if (-not (Test-Path -LiteralPath $tempDirectory)) {
  throw "Temp directory does not exist: $tempDirectory"
}

function Add-CertificateToStore {
  param(
    [Parameter(Mandatory = $true)][string]$StoreName,
    [Parameter(Mandatory = $true)][string]$Location,
    [Parameter(Mandatory = $true)][System.Security.Cryptography.X509Certificates.X509Certificate2]$Certificate
  )

  $store = New-Object System.Security.Cryptography.X509Certificates.X509Store($StoreName, $Location)
  $store.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
  try {
    $match = $store.Certificates.Find(
      [System.Security.Cryptography.X509Certificates.X509FindType]::FindByThumbprint,
      $Certificate.Thumbprint,
      $false
    )
    if ($match.Count -eq 0) {
      $store.Add($Certificate)
      Write-Host "Imported $($Certificate.Subject) into $Location\$StoreName"
    } else {
      Write-Host "$($Certificate.Subject) already present in $Location\$StoreName"
    }
  } finally {
    $store.Close()
  }
}

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Install-Module -Name Az.ArtifactSigning -RequiredVersion 0.1.5 -Scope CurrentUser -Force -AllowClobber

$azContext = Get-AzContext -ErrorAction SilentlyContinue
if ($null -eq $azContext) {
  throw "No active Azure context found. Run azure-login.ps1 before importing the Artifact Signing chain."
}

$chainPath = Join-Path $tempDirectory "artifact-signing-chain.der"
$chainInfo = Get-AzArtifactSigningCertificateChain `
  -AccountName $signingAccountName `
  -ProfileName $certificateProfileName `
  -EndpointUrl $signingEndpoint `
  -Destination $chainPath

if (-not (Test-Path -LiteralPath $chainPath)) {
  throw "Artifact Signing certificate chain file was not downloaded: $chainPath"
}

$certBytes = [System.IO.File]::ReadAllBytes($chainPath)
$certCollection = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2Collection
$certCollection.Import($certBytes)
if ($certCollection.Count -eq 0) {
  throw "Downloaded certificate chain is empty."
}

Write-Host "Retrieved $($certCollection.Count) certificates from Artifact Signing chain."
foreach ($info in $chainInfo) {
  $thumbprintProp = $info.PSObject.Properties["Thumbprint"]
  $subjectProp = $info.PSObject.Properties["Subject"]
  if ($null -ne $thumbprintProp -or $null -ne $subjectProp) {
    $thumbprintValue = if ($null -ne $thumbprintProp) { $thumbprintProp.Value } else { "<n/a>" }
    $subjectValue = if ($null -ne $subjectProp) { $subjectProp.Value } else { "<n/a>" }
    Write-Host "Chain cert: $thumbprintValue - $subjectValue"
  } else {
    Write-Host "Chain cert metadata: $($info | ConvertTo-Json -Depth 5 -Compress)"
  }
}

foreach ($cert in $certCollection) {
  $isRoot = $cert.Subject -eq $cert.Issuer
  $basicConstraints = $cert.Extensions | Where-Object { $_.Oid.Value -eq "2.5.29.19" } | Select-Object -First 1
  $isCa = $false
  if ($basicConstraints) {
    $isCa = ([System.Security.Cryptography.X509Certificates.X509BasicConstraintsExtension]$basicConstraints).CertificateAuthority
  }

  $primaryStore = if ($isRoot) {
    "Root"
  } elseif ($isCa) {
    "CA"
  } else {
    "My"
  }

  foreach ($location in @("LocalMachine", "CurrentUser")) {
    Add-CertificateToStore -StoreName $primaryStore -Location $location -Certificate $cert
    Add-CertificateToStore -StoreName "TrustedPublisher" -Location $location -Certificate $cert
  }
}


Write-Host "Artifact Signing certificate chain import completed."
