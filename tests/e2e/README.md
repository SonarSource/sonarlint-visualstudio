# Extension E2E test

This test runs once after every successful push build on `master`, using Visual
Studio Community installed by the `sonarlint-visualstudio` image
([ci-ami-images #513](https://github.com/SonarSource/ci-ami-images/pull/513))
on the `warp-custom-sonarlint-visualstudio` runner, the same one used by Build.
PR builds run it automatically only when the PR changes
`.github/workflows/extension-e2e.yml` or anything under `tests/e2e/`. It can
also be enabled explicitly when manually dispatching Build on a PR branch. Earliest-supported-version coverage
is deferred to a separate PR.

## Scenario

The test installs the signed VSIX that the same workflow run published to repox
(`sonarsource-public-qa`, at the run's four-part version) into a unique
experimental IDE profile. It does not download an older package or rebuild the
extension.

1. Verify the actual IDE product/version and supplied VSIX identity.
2. Install the VSIX and verify its installed manifest in the requested profile.
3. Open the C# fixture through DTE and require the Sonar S1481 message at the
   exact sample path and line 5: `Remove the unused local variable 'e2eUnused'.`
4. Require a second S1481 finding for `e2eRemaining` at line 6.
5. Remove only `e2eUnused` through the IDE editor and save the document.
6. Check that its declaration is gone from disk and its finding remains absent
   for five continuous seconds while the unedited `e2eRemaining` finding stays
   visible. Unreadable Error List snapshots and COM busy retries reset that
   stability window.

There is one installation and one test execution, with no repetition loop or
version matrix. `TreatNoTestsAsError=true` fails the job if no tests are selected.
The five-second absence check is part of that single execution.
Compiler unused-variable warnings cannot satisfy the Sonar-message assertion.
Other unrelated diagnostics may remain. This exercises native IDE/DTE behavior;
it does not validate foreground/pixel rendering or a connection wizard.

## Connected mode

`ConnectedModeE2ETests` (`TestCategory=ConnectedModeE2E`) runs as its own job with
the same triggers, in parallel with the standalone test and on the same signed VSIX.
It checks that the installed extension applies a bound server's quality profile.

The test starts a disposable SonarQube Community Build 25.11 on loopback, using the
[Orchestrator](server/src/main/java/ConnectedServer.java) launcher (Orchestrator
6.4.4.4797, JDK 21). `SonarQubeServer` starts the launcher the same way the product
starts SLCore (`java -classpath … MainClass`) and stops it when the test ends.

1. **Unbound control:** the `Fixture/ConnectedMode` sample (the standalone sample plus
   two nested `if`s) shows both S1481 findings and no S3776 finding.
2. **Server fixture:** a unique project and C# quality profile that activates only
   `csharpsquid:S3776` with `threshold=1`. The server's default of 15 is checked
   first, and the profile is queried to confirm S1481 is off.
3. **Binding:** the connection and binding are written in the extension's own
   formats, and the token through the product's `Microsoft.Alm.Authentication`
   credential store. The binding wizard UI is not automated.
4. **Bound analysis:** a second fresh profile must show exactly
   `Refactor this method to reduce its Cognitive Complexity from 3 to the 1 allowed.`
   at line 3, and no S1481.
5. **Edit:** removing the nested `if`s through DTE must clear S3776, and S1481 must
   stay absent for five continuous seconds, although the unused variables remain.

Server APIs only arrange the fixture; the analysis comes from the installed extension.
SonarQube Cloud and enterprise/taint rules are not covered.

The repox credentials are passed only to the launcher and removed from the test
process before any IDE starts. The token is never written to evidence, and the
exported launcher and server logs are redacted. The fixture refuses to overwrite
existing connections, bindings or credentials and deletes only what it created, so
run it only on an ephemeral CI account or a dedicated Windows account.

Ideas for further cases (see the sonarlint-core, -intellij and -eclipse connected
ITs): rule changes after binding, server exclusions, issue suppression, reconnect
and invalid tokens, and shared binding.

## CI integration

The Build workflow calls the reusable [E2E workflow](../../.github/workflows/extension-e2e.yml)
after its `build` job succeeds on a push to `master`, on a pull request that
changes the E2E workflow or `tests/e2e/`, or on a manual dispatch with the
`extension-e2e` checkbox enabled. The normal build and its
publication conditions are unchanged. E2E failures fail the workflow, but the
E2E job runs after publication and does not gate promotion or dogfood delivery.

The runner requires the Visual Studio Community build pinned in the E2E workflow, MSBuild/VSTest,
the .NET SDK and .NET Framework targeting packs. The expected full build is declared once as `E2E_EXPECTED_VS_VERSION` in the
workflow. Discovery derives its minor-version range from that setting, and the
harness independently checks the exact installed build. Update it together with
the image's Visual Studio version when changing releases.
The test project targets .NET Framework 4.8; the fixture targets 4.7.2.
NuGet restoration is locked and IDE interop assemblies come from the target IDE.

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

The `extension-e2e-vs<expected-IDE-build>` artifact is retained for seven days and contains a TRX
report plus `run-1/evidence`, including:

- Result/phase, actual and expected IDE versions, product and profile identity.
- Package SHA-256, package/installed manifests and extension-file inventory.
- Before/after Error Lists and the saved sample source.
- IDE activity, installer, cache/configuration and MEF diagnostic logs.

The connected-mode job uploads `extension-e2e-connected-vs<expected-IDE-build>`, with
the same per-session evidence under `evidence/standalone` and `evidence/connected`, the
server's rule defaults and assigned profile, and redacted logs in `evidence/server-diagnostics`.

TRX attachments may duplicate canonical evidence. Installer success alone does
not prove deployment into the intended profile; manifest verification must pass
before opening the sample. Each phase has a bounded wait.

Profiles use a unique `ExtensionE2E...` suffix. The harness terminates only the IDE
process it launches. Profiles are retained for diagnosis; persistent runners
need scoped cleanup after investigation.

## Run locally

On Windows with the prerequisites above and permission to install a VSIX:

```powershell
# Copy the intended full build from extension-e2e.yml; do not infer it from the runner.
$env:E2E_EXPECTED_VS_VERSION = '<expected-four-part-IDE-build>'
$expectedVersion = [version]$env:E2E_EXPECTED_VS_VERSION
$versionRange = "[$($expectedVersion.Major).$($expectedVersion.Minor),$($expectedVersion.Major).$($expectedVersion.Minor + 1))"
$idePath = & "${env:ProgramFiles(x86)}/Microsoft Visual Studio/Installer/vswhere.exe" -latest -products Microsoft.VisualStudio.Product.Community -version $versionRange -property installationPath
dotnet build tests/e2e/Extension.E2E.Tests.csproj -c Release -p:VsTargetVersion=2022 "-p:VisualStudioInstallPath=$idePath" -p:RestoreLockedMode=true
$env:E2E_VSIX_PATH = 'C:\artifacts\SonarLint.VSIX-<version>-2022.vsix'
$env:E2E_EXPECTED_VS_PRODUCT = 'Microsoft.VisualStudio.Product.Community'
$env:E2E_RESULTS_DIRECTORY = 'C:\temp\extension-e2e\run-1\evidence'
dotnet test tests/e2e/Extension.E2E.Tests.csproj -c Release --no-build --no-restore --filter TestCategory=ExtensionE2E --logger trx --results-directory C:\temp\extension-e2e\run-1 -p:VsTargetVersion=2022
```

Use a new results directory for every execution.

## Implementation

- `ExtensionE2ETests.cs`: MSTest scenario and assertions on an STA thread.
- `VisualStudioSession.cs`: installation, isolated profile, bounded waits,
  sample edits, configuration preparation and evidence collection.
- `ComMessageFilter.cs`: retries temporary COM busy responses on the STA test
  thread for up to 30 seconds per call, then restores the previous thread filter
  at cleanup. Permanent rejections and unrelated COM errors are not retried by
  the filter. The harness records the busy-response count in `result.json`.
- `VisualStudioAutomation.cs`: attach to the launched process's DTE2 object and
  read its actual Error List and Sonar output.
- `Fixture`: sample solution/project and source template, copied to the attempt's
  isolated directory; `Sample.cs.txt` becomes `Sample.cs`. `Fixture/ConnectedMode`
  holds the connected-mode test's source variant.
- `ConnectedModeE2ETests.cs`: the connected-mode scenario.
- `SonarQubeServer.cs`: starts and stops the launcher, and exports redacted server logs.
- `ConnectedServerFixture.cs`: the server project/profile, connection, binding and
  credential, with cleanup.
- `server/`: the Orchestrator launcher (`pom.xml`, `ConnectedServer.java`) and the Maven
  settings, which read the repox credentials from the environment.

The VSIX installer targets the discovered instance and unique profile. No loose
extension binaries or analysis responses are injected.
