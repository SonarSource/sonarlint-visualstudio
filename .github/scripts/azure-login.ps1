<#
.SYNOPSIS
Retrieves a GitHub OIDC token, performs Azure federated login, and exports auth environment variables.

.DESCRIPTION
This script requests an OIDC token from GitHub Actions with a specified audience,
writes the token to a file, logs into Azure with Connect-AzAccount using workload
identity federation, and exports Azure auth variables for subsequent steps.
It installs Az.Accounts in CurrentUser scope before invoking Connect-AzAccount.

The script reads its required inputs from standard GitHub Actions environment
variables and validates they are present before executing.

.INPUTS (from environment)
ACTIONS_ID_TOKEN_REQUEST_URL: GitHub OIDC request URL.
ACTIONS_ID_TOKEN_REQUEST_TOKEN: GitHub OIDC bearer token.
RUNNER_TEMP: Runner temporary directory where oidc-token.txt is written.
GITHUB_ENV: GitHub environment file path to persist AZURE_FEDERATED_TOKEN_FILE.
AZURE_TENANT_ID: Azure Entra tenant ID.
AZURE_CLIENT_ID: Federated identity app/client ID used for workload identity login.
AZURE_SUBSCRIPTION_ID: Azure subscription ID to select for the session.
AZURE_OIDC_AUDIENCE (optional): OIDC audience. Defaults to api://AzureADTokenExchange.

.OUTPUTS (to environment)
AZURE_FEDERATED_TOKEN_FILE, AZURE_TENANT_ID, AZURE_CLIENT_ID, AZURE_SUBSCRIPTION_ID
are appended to the GitHub environment file for downstream steps/tools.

.EXAMPLE
.\azure-login.ps1
#>
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$oidcRequestUrl = $env:ACTIONS_ID_TOKEN_REQUEST_URL
$oidcRequestToken = $env:ACTIONS_ID_TOKEN_REQUEST_TOKEN
$runnerTemp = $env:RUNNER_TEMP
$githubEnvFile = $env:GITHUB_ENV
$azureTenantId = $env:AZURE_TENANT_ID
$azureClientId = $env:AZURE_CLIENT_ID
$azureSubscriptionId = $env:AZURE_SUBSCRIPTION_ID
$audience = if ([string]::IsNullOrWhiteSpace($env:AZURE_OIDC_AUDIENCE)) { "api://AzureADTokenExchange" } else { $env:AZURE_OIDC_AUDIENCE }

if ([string]::IsNullOrWhiteSpace($oidcRequestUrl)) {
  throw "Missing required environment variable: ACTIONS_ID_TOKEN_REQUEST_URL"
}
if (-not $oidcRequestUrl.StartsWith("https://")) {
  throw "ACTIONS_ID_TOKEN_REQUEST_URL must start with https://"
}
if ([string]::IsNullOrWhiteSpace($oidcRequestToken)) {
  throw "Missing required environment variable: ACTIONS_ID_TOKEN_REQUEST_TOKEN"
}
if ([string]::IsNullOrWhiteSpace($runnerTemp)) {
  throw "Missing required environment variable: RUNNER_TEMP"
}
if ([string]::IsNullOrWhiteSpace($githubEnvFile)) {
  throw "Missing required environment variable: GITHUB_ENV"
}
if ([string]::IsNullOrWhiteSpace($azureTenantId)) {
  throw "Missing required environment variable: AZURE_TENANT_ID"
}
if ([string]::IsNullOrWhiteSpace($azureClientId)) {
  throw "Missing required environment variable: AZURE_CLIENT_ID"
}
if ([string]::IsNullOrWhiteSpace($azureSubscriptionId)) {
  throw "Missing required environment variable: AZURE_SUBSCRIPTION_ID"
}
$azureTenantId = $azureTenantId.Trim()
$azureClientId = $azureClientId.Trim()
$azureSubscriptionId = $azureSubscriptionId.Trim()
$audience = $audience.Trim()

$tokenOutputFile = Join-Path $runnerTemp "oidc-token.txt"

$tokenOutputDirectory = Split-Path -Parent $tokenOutputFile
if (-not [string]::IsNullOrWhiteSpace($tokenOutputDirectory) -and -not (Test-Path -LiteralPath $tokenOutputDirectory)) {
  throw "Token output directory does not exist: $tokenOutputDirectory"
}

$githubEnvDirectory = Split-Path -Parent $githubEnvFile
if (-not [string]::IsNullOrWhiteSpace($githubEnvDirectory) -and -not (Test-Path -LiteralPath $githubEnvDirectory)) {
  throw "GitHub env file directory does not exist: $githubEnvDirectory"
}

$separator = if ($oidcRequestUrl.Contains("?")) { "&" } else { "?" }
$requestUrl = "$oidcRequestUrl${separator}audience=$audience"
$response = Invoke-WebRequest `
  -Uri $requestUrl `
  -Headers @{ Authorization = "bearer $oidcRequestToken" } `
  -UseBasicParsing

$token = ($response.Content | ConvertFrom-Json).value
if ([string]::IsNullOrWhiteSpace($token)) {
  throw "OIDC response did not contain a token value."
}

[System.IO.File]::WriteAllText($tokenOutputFile, $token)
Add-Content -Path $githubEnvFile -Value "AZURE_FEDERATED_TOKEN_FILE=$tokenOutputFile"
Add-Content -Path $githubEnvFile -Value "AZURE_TENANT_ID=$azureTenantId"
Add-Content -Path $githubEnvFile -Value "AZURE_CLIENT_ID=$azureClientId"
Add-Content -Path $githubEnvFile -Value "AZURE_SUBSCRIPTION_ID=$azureSubscriptionId"

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Install-Module Az.Accounts -RequiredVersion 5.3.3 -Scope CurrentUser -Force -AllowClobber

Connect-AzAccount `
  -ServicePrincipal `
  -ApplicationId $azureClientId `
  -FederatedToken $token `
  -Subscription $azureSubscriptionId `
  -Environment "AzureCloud" `
  -TenantId $azureTenantId

Write-Host "Azure federated login completed and token file prepared at $tokenOutputFile"
