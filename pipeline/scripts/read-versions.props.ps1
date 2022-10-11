# Calculate the file path

$versionFilePath = "$env:BUILD_SOURCESDIRECTORY\build\Version.props"

Write-Host "Reading the Sonar project version from '${versionFilePath}' ..."


# Read the version from the file

[xml]$versionProps = Get-Content "$versionFilePath"

$sonarProjectVersion = $versionProps.Project.PropertyGroup.MainVersion

Write-Host "Sonar project version is '${sonarProjectVersion}'"


# Set the variable to it can be used by other tasks

Write-Host "##vso[task.setvariable variable=SONAR_PROJECT_VERSION;]$sonarProjectVersion"