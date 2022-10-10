function GetAllBuildProperties{
    Param([string] $propertyPath, [string] $propertyValue)

    Write-Host "Getting all build properties ..."

    # NB: the WebClient class defaults to TLS v1, which is no longer supported by some online providers.
    # This setting isn't required on MS-hosted agents, but it can be for local agents.
    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12

    # GET https://dev.azure.com/{organization}/{project}/_apis/build/builds/{buildId}/properties?filter={filter}&api-version=5.1-preview.1
    $rootApiUrl = "$($env:SYSTEM_TEAMFOUNDATIONCOLLECTIONURI)$env:SYSTEM_TEAMPROJECTID/_apis/" 
    $url =$rootApiUrl + "build/builds/$env:BUILD_BUILDID/properties?api-version=5.0-preview.1"

    $properties = Invoke-RestMethod -Uri $url -Headers @{
      Authorization = "Bearer $env:SYSTEM_ACCESSTOKEN"
    }

    Write-Host "Properties = $($properties | ConvertTo-Json -Depth 100)"
}

function GetBuildProperty{
    Param([string] $propertyPath)

    Write-Host "Getting build property $propertyPath"

    # NB: the WebClient class defaults to TLS v1, which is no longer supported by some online providers.
    # This setting isn't required on MS-hosted agents, but it can be for local agents.
    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12

    # GET https://dev.azure.com/{organization}/{project}/_apis/build/builds/{buildId}/properties?filter={filter}&api-version=5.1-preview.1
    $rootApiUrl = "$($env:SYSTEM_TEAMFOUNDATIONCOLLECTIONURI)$env:SYSTEM_TEAMPROJECTID/_apis/" 
    $url =$rootApiUrl + "build/builds/$env:BUILD_BUILDID/properties?filter=$propertyPath&api-version=5.0-preview.1"

    $result = Invoke-RestMethod -Uri $url -Headers @{
      Authorization = "Bearer $env:SYSTEM_ACCESSTOKEN"
    }

    $extractedValue = $result.value."$propertyPath".'$value'

    Write-Host "Extracted value = $extractedValue"
    return $extractedValue
}


GetAllBuildProperties
$version = GetBuildProperty -propertyPath "VsixVersion"
Write-Host "VsixVersion build property = $version"
