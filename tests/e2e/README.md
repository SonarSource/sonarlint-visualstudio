# Extension E2E test

This test runs once after every successful push build on `master`, using Visual
Studio Community 18.11.12328.437 from [image PR #513](https://github.com/SonarSource/ci-ami-images/pull/513)
on `warp-custom-pr-513-sonarlint-visualstudio-v20261109151945`.
PR builds do not run it automatically. It can also be enabled explicitly when
manually dispatching Build on a PR branch. Earliest-supported-version coverage
is deferred to a separate PR.

## Scenario

The test installs the signed VSIX from the same workflow run's `binaries`
artifact into a unique experimental IDE profile. It does not download an older
published package or rebuild the extension.

1. Verify the actual IDE product/version and supplied VSIX identity.
2. Install the VSIX and verify its installed manifest in the requested profile.
3. Open the C# fixture through DTE and require the Sonar S1481 message at the
   exact sample path and line 5: `Remove the unused local variable 'e2eUnused'.`
4. Require a second S1481 finding for `e2eRemaining` at line 6.
5. Remove only `e2eUnused` through the IDE editor and save the document.
6. Check that its declaration is gone from disk and its finding remains absent
   for five continuous seconds while the unedited `e2eRemaining` finding stays
   visible. Unreadable Error List snapshots reset that stability window.

There is one installation and one test execution, with no repetition loop or
version matrix. CI requires a TRX containing exactly one executed, passed test
and a completed harness result; zero selected or skipped tests fail. The five-second absence check is part of that single execution.
Compiler unused-variable warnings cannot satisfy the Sonar-message assertion.
Other unrelated diagnostics may remain. This exercises native IDE/DTE behavior;
it does not validate foreground/pixel rendering or a connection wizard.

## CI integration

The Build workflow calls the reusable [E2E workflow](../../.github/workflows/extension-e2e.yml)
after its `build` job succeeds on a push to `master`, or on a manual dispatch
with the `extension-e2e` checkbox enabled. The normal build and its
publication conditions are unchanged. E2E failures fail the workflow, but the
E2E job runs after publication and does not gate promotion or dogfood delivery.

The runner requires Visual Studio Community 18.11.12328.437, MSBuild/VSTest,
the .NET SDK and .NET Framework targeting packs. The harness checks the exact IDE
build supplied by image #513. The DEV runner label is pinned to that candidate;
when the image is promoted, update the runner label to its stable equivalent.
The test project targets .NET Framework 4.8; the fixture targets 4.7.2.
NuGet restoration is locked and IDE interop assemblies come from the target IDE.

Temporarily, PR #6854 also enables this job automatically to validate the revised
workflow. Remove that PR-specific condition after collecting validation evidence.

## Run manually on a PR

In Actions, select **Build → Run workflow**, choose the PR's source branch,
and enable **extension-e2e**. This builds the selected branch and runs one E2E
test against that run's signed VSIX. The branch must contain these workflow
changes. Manual dispatch is available once the workflow exists on the default
branch; it is not a rerun of the existing `pull_request` event.

Or use the CLI, replacing the branch name:

```sh
gh workflow run build.yml --repo SonarSource/sonarlint-visualstudio \
  --ref <pr-source-branch> -f extension-e2e=true
```

Leaving the checkbox off retains the usual manual build. Use an in-repository
PR branch that can run the normal Build workflow with its signing credentials.

## Evidence and failures

The `extension-e2e-vs18.11` artifact is retained for seven days and contains a TRX
report plus `run-1/evidence`, including:

- Result/phase, actual and expected IDE versions, product and profile identity.
- Package SHA-256, package/installed manifests and extension-file inventory.
- Before/after Error Lists and the saved sample source.
- IDE activity, installer, cache/configuration and MEF diagnostic logs.

TRX attachments may duplicate canonical evidence. Installer success alone does
not prove deployment into the intended profile; manifest verification must pass
before opening the sample. Each phase has a bounded wait.

Profiles use a unique `ExtensionE2E...` suffix. The harness terminates only the IDE
process it launches. Profiles are retained for diagnosis; persistent runners
need scoped cleanup after investigation.

## Run locally

On Windows with the prerequisites above and permission to install a VSIX:

```powershell
$idePath = & "${env:ProgramFiles(x86)}/Microsoft Visual Studio/Installer/vswhere.exe" -latest -products Microsoft.VisualStudio.Product.Community -version '[18.11,18.12)' -property installationPath
dotnet build tests/e2e/Extension.E2E.Tests.csproj -c Release -p:VsTargetVersion=2022 "-p:VisualStudioInstallPath=$idePath" -p:RestoreLockedMode=true
$env:E2E_VSIX_PATH = 'C:\artifacts\SonarLint.VSIX-<version>-2022.vsix'
$env:E2E_EXPECTED_VS_VERSION = '18.11.12328.437'
$env:E2E_EXPECTED_VS_PRODUCT = 'Microsoft.VisualStudio.Product.Community'
$env:E2E_RESULTS_DIRECTORY = 'C:\temp\extension-e2e\run-1\evidence'
dotnet test tests/e2e/Extension.E2E.Tests.csproj -c Release --no-build --no-restore --filter TestCategory=ExtensionE2E --logger trx --results-directory C:\temp\extension-e2e\run-1 -p:VsTargetVersion=2022
```

Use a new results directory for every execution.

## Implementation

- `ExtensionE2ETests.cs`: MSTest scenario and assertions on an STA thread.
- `VisualStudioSession.cs`: installation, isolated profile, bounded waits,
  sample edits, configuration preparation and evidence collection.
- `VisualStudioAutomation.cs`: attach to the launched process's DTE2 object and
  read its actual Error List and Sonar output.
- `Fixture`: sample solution/project and source template, copied to the attempt's
  isolated directory; `Sample.cs.txt` becomes `Sample.cs`.

The VSIX installer targets the discovered instance and unique profile. No loose
extension binaries or analysis responses are injected.
