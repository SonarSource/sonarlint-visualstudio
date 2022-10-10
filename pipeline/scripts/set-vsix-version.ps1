function SetBuildProperty
{
    Param([string] $propertyPath, [string] $propertyValue)

    Write-Host "Setting the build property $propertyPath to $propertyValue ..."

    # NB: the WebClient class defaults to TLS v1, which is no longer supported by some online providers.
    # This setting isn't required on MS-hosted agents, but it can be for local agents.
    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12

    $rootApiUrl = "$($env:SYSTEM_TEAMFOUNDATIONCOLLECTIONURI)$env:SYSTEM_TEAMPROJECTID/_apis/" 

    # Update build properties
    $url =$rootApiUrl + "build/builds/$env:BUILD_BUILDID/properties?api-version=5.0-preview.1"

    #$jsonPatch = '[{ "op": "replace", "path": "/$($propertyPath)", "value": "' + $($propertyValue) + '" }]'

    $jsonPatch = "[{ 'op': 'replace', 'path': '/$propertyPath', 'value': '$propertyValue' }]"

    $pipeline = Invoke-RestMethod -Uri $url -Headers @{
        "Authorization" = "Bearer $env:SYSTEM_ACCESSTOKEN";
    } -Body $jsonPatch -Method Patch -ContentType "application/json-patch+json"

}


SetBuildProperty -propertyPath "VsixVersion" -propertyValue $env:SONAR_PROJECT_VERSION