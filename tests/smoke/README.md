# Installed VSIX smoke tests

These tests install a published SonarQube for Visual Studio extension in a fresh
experimental IDE profile and exercise analysis through the real Visual Studio
DTE API. They run independently of the product solution and unit tests.

## Scenario

For each installation, the test:

1. Verifies the actual IDE product/build and the supplied VSIX identity.
2. Installs the signed VSIX into a unique experimental profile and verifies its
   installed manifest in that profile.
3. Opens the checked-in C# sample and requires Sonar S1481 at the exact sample
   path and line: `Remove the unused local variable 'smokeUnused'.`
4. Removes the unused variable through DTE, saves the document and checks the
   exact saved source.
5. Requires that specific finding to remain absent for five seconds.

The compiler's unused-variable warning cannot satisfy the Sonar assertion. Other
unrelated diagnostics may remain. Tests use native IDE automation; they do not
establish foreground/pixel-rendering behavior or automate a connection wizard.

## Run in CI

The reusable [smoke workflow](../../.github/workflows/vsix-smoke.yml) defines the
minimum/latest IDE matrix and its expected installation builds. Each target
must have Visual Studio Community, MSBuild/VSTest, the .NET SDK and the required
.NET Framework targeting packs. Copy the exact runner labels from the image
registration output; do not infer them from a timestamp or use one image for both
targets. CI currently accepts isolated DEV image labels only.

Dispatch the main Build workflow on a revision containing the tests:

```sh
gh workflow run build.yml --ref <branch-or-tag> \
  -f vsix-smoke-version=<published-four-part-VSIX-version> \
  -f vsix-smoke-earliest-runner=<minimum-IDE-DEV-runner-label> \
  -f vsix-smoke-latest-runner=<latest-IDE-DEV-runner-label> \
  -f vsix-smoke-repetitions=3
```

A nonempty smoke version runs only the smoke matrix; an empty value runs the
normal product build. Ordinary PR/push builds keep their existing stable runner
and do not execute the installed-VSIX scenario automatically. Release and
publication conditions are unchanged.

The default is three fresh installations per target. Choose one for focused
validation; one to five are supported. Targets run independently with fail-fast
disabled. The workflow uses read-only artifact credentials to download the same
published VSIX on both targets. It never modifies the package manifest/signature
to force compatibility. The downloaded artifact retains its `2022.vsix` filename
while installation verifies compatibility with the actual target IDE.

## Evidence and failures

Artifacts are named `vsix-smoke-earliest-vs2022-17.0` and
`vsix-smoke-latest-vs18`. Each contains a TRX report and canonical
`run-N/evidence` directories with:

- Result/phase, actual and expected IDE builds, product ID and profile/session identity.
- Package SHA-256, VSIX manifests and installed extension-file inventory.
- Before/after Error List snapshots and the saved sample source.
- IDE activity, installer, cache/configuration and MEF diagnostic logs.

TRX attachments can duplicate evidence; count canonical directories as separate
attempts. A successful installer exit alone does not prove that the extension
was deployed to the requested profile. The deployment guard must pass before
the IDE opens the sample.

Profiles use a unique `VsixSmoke...` suffix. The harness only terminates the IDE
process it launches and never resets the normal/shared experimental profile.
Profiles are retained for diagnosis; persistent runners need scoped cleanup after
investigation. Artifacts have a seven-day retention period.

## Run locally

Use Windows and the prerequisites above, with an account allowed to install a
VSIX. Supply the expected IDE build from the workflow matrix and a new results
directory. Replace the placeholders before running:

```powershell
$idePath = & "${env:ProgramFiles(x86)}/Microsoft Visual Studio/Installer/vswhere.exe" -latest -products Microsoft.VisualStudio.Product.Community -property installationPath
dotnet build tests/smoke/VsixSmoke.Tests.csproj -c Release -p:VsTargetVersion=2022 "-p:VisualStudioInstallPath=$idePath" -p:RestoreLockedMode=true
$env:SMOKE_VSIX_PATH = 'C:\artifacts\SonarLint.VSIX-<version>-2022.vsix'
$env:SMOKE_EXPECTED_VS_VERSION = '<expected-four-part-IDE-build>'
$env:SMOKE_EXPECTED_VS_PRODUCT = 'Microsoft.VisualStudio.Product.Community'
$env:SMOKE_RESULTS_DIRECTORY = 'C:\temp\vsix-smoke\run-1\evidence'
dotnet test tests/smoke/VsixSmoke.Tests.csproj -c Release --no-build --no-restore --filter TestCategory=InstalledVsixSmoke --logger trx --results-directory C:\temp\vsix-smoke\run-1 -p:VsTargetVersion=2022
```

## Implementation

- `VsixSmokeTests.cs` defines the MSTest scenario and assertions.
- `VisualStudioSession.cs` manages installation, bounded waits, sample edits,
  post-install cache/configuration preparation and evidence collection.
- `VisualStudioAutomation.cs` attaches to the launched process's typed DTE2 object
  and reads its actual Error List.
- `Fixture` contains the sample solution/project and source template. The `.txt`
  template becomes `Smoke.cs` in each attempt's isolated directory.

VS 17 RTM requires the VSIX installer's explicit application/SKU route to target
its experimental profile; newer IDEs use the instance-ID route. Both routes
require the same installed-manifest verification. No loose extension binaries
or analysis responses are copied/injected. The project uses locked NuGet
restoration and resolves interop assemblies from the target IDE.
