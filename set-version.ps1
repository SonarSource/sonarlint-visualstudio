[CmdletBinding()]
Param(
    [Parameter(Mandatory = $True, Position = 1)]
    [ValidatePattern("^\d{1,3}\.\d{1,3}\.\d{1,3}$")]
    [string]$version
)

function Write-Header([string]$message) {
    Write-Host "================================================"
    Write-Host $message
    Write-Host "================================================"
}

function Set-VersionForDotNet {
    Write-Header "Updating version in .Net files"

    try {
        Push-Location ".\build"
        $versionPropsFile = Resolve-Path "Version.props"
        $xml = [xml](Get-Content $versionPropsFile)
        $xml.Project.PropertyGroup.MainVersion = ${version}
        $xml.Save($versionPropsFile)
        msbuild "ChangeVersion.proj"
    }
    finally {
        Pop-Location
    }
}

Set-VersionForDotNet