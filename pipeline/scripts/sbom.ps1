# Calculate the file name
$sbomName = "SonarLint.visualstudio.sbom-$env:SONAR_PROJECT_VERSION.$env:BUILD_BUILDID-$(vsTargetVersion).json"
Write-Host "SBOM file is: $sbomName"

Rename-Item -Path "binaries\bom.json" -NewName "${sbomName}"

# Set the variable to it can be used by other tasks
Write-Host "##vso[task.setvariable variable=SBOM_NAME;]$sbomName"

Install-Package Gpg.Windows.x64 
gpg --batch --passphrase $(PGP_PASSPHRASE) --allow-secret-key-import --import $(signKey.secureFilePath)
gpg --list-secret-keys
cd binaries
Write-Host "About to sign $sbomName"
gpg --pinentry-mode loopback  --passphrase $(PGP_PASSPHRASE) --armor --detach-sig --default-key infra@sonarsource.com "$sbomName"
Write-Host "Signed $sbomName"