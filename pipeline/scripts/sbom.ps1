# Calculate the file name
$sbomName = "SonarLint.visualstudio.sbom-$env:SONAR_PROJECT_VERSION.$env:BUILD_BUILDID-$env:VSTARGETVERSION.json"
Write-Host "SBOM file is: $sbomName"
Write-Host "signkey file path is: $env:SIGNKEY_SECUREFILEPATH"

Rename-Item -Path "binaries\bom.json" -NewName "${sbomName}"

# Set the variable to it can be used by other tasks
Write-Host "##vso[task.setvariable variable=SBOM_NAME;]$sbomName"

Install-Package Gpg.Windows.x64 
gpg --batch --passphrase "$env:PGP_PASSPHRASE --allow-secret-key-import --import "$env:SIGNKEY_SECUREFILEPATH"
gpg --list-secret-keys
cd binaries
Write-Host "About to sign $sbomName"
gpg --pinentry-mode loopback  --passphrase "$env:PGP_PASSPHRASE" --armor --detach-sig --default-key infra@sonarsource.com "$sbomName"
Write-Host "Signed $sbomName"