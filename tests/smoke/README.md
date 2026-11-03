# Installed-VSIX smoke tests (SLVS-3097)

The same C# MSTest scenario runs against the extension's minimum supported IDE
and the latest pinned IDE. Each matrix job installs the supplied signed VSIX in
fresh experimental profiles, opens a sample file, observes the Sonar finding,
edits/saves the file and verifies that the finding remains absent for five seconds.
This is a manual DEV compatibility check; it does not change production release gates.

## Image matrix

| Target | Pinned release | Required installed build | Image PR |
| --- | --- | --- | --- |
| Earliest | VS 2022 17.0.0 | 17.0.31903.59 | [ci-ami-images #520](https://github.com/SonarSource/ci-ami-images/pull/520) |
| Latest | VS 2026 18.11.3 | 18.11.12328.437 | [ci-ami-images #513](https://github.com/SonarSource/ci-ami-images/pull/513) |

The user chose the extension's minimum VS 17.0 compatibility boundary, rather
than Microsoft's maintained VS 2022 servicing line. The minimum image is a
separate isolated compatibility candidate. Each image contains one full IDE.
Image #520 is stacked on #513 and reuses its installation recipe.

Both DEV images must finish building and registering before dispatch. Copy their
exact temporary WarpBuild labels from the registration output; do not guess a
label or point both axes at the same image. The workflow rejects missing,
non-DEV and duplicate labels. The harness independently compares the actual VS
build with the target's expected build before installing the extension.

## Run the matrix

```sh
gh workflow run build.yml --ref codex/slvs-3097-vsix-smoke-spike \
  -f vsix-smoke-version=11.3.0.17474 \
  -f vsix-smoke-earliest-runner=<actual-minimum-image-DEV-label> \
  -f vsix-smoke-latest-runner=<actual-latest-image-DEV-label> \
  -f vsix-smoke-repetitions=3
```

This downloads the same promoted VSIX independently on both images. The existing
`2022.vsix` filename is retained; installation must establish compatibility with
each actual IDE. It never changes the package manifest or signature to force a pass.

The default is three fresh profiles per target (six attempts overall). Use
`vsix-smoke-repetitions=1` for focused validation of both targets. Matrix jobs run
independently with fail-fast disabled, so a failure on one version does not cancel
the other. An empty smoke-version input runs the normal build instead. Ordinary
push/PR builds do not automatically execute the smoke scenario.

Artifacts are named `slvs3097-vsix-smoke-earliest-vs2022-17.0` and
`slvs3097-vsix-smoke-latest-vs18`. Each contains per-attempt TRX reports, result JSON,
actual VSIX manifest, package SHA-256, actual/expected IDE versions, profile/session
identity, before/after Error List snapshots, saved fixture and IDE/installer logs.
TRX attachments can duplicate evidence files; count only canonical `run-N/evidence`
directories as independent attempts.

## Test structure

- `VsixSmokeTests.cs` contains the scenario and assertions.
- `VisualStudioSession.cs` installs/launches the IDE, polls bounded conditions,
  prepares the post-install extension cache/configuration and collects evidence.
- `VisualStudioAutomation.cs` attaches to the launched process's DTE object and
  reads the real Error List through typed DTE2. It supports VS 17/18 monikers.
- `Fixture/Smoke.cs.txt`, `Smoke.csproj` and `Smoke.sln` are copied for each attempt;
  the `.txt` template becomes `Smoke.cs` at runtime.

The target finding is Sonar S1481: `Remove the unused local variable 'smokeUnused'.`
at line 5 of the exact sample path. The compiler's own unused-variable warning
does not satisfy this check. Other diagnostics can remain after the targeted
finding clears. Assertions are unchanged between the two IDE versions.

The project uses MSTest 4.4.1 and Newtonsoft.Json 13.0.3 with a locked dependency
graph. It references the IDE's unified interop assembly or older EnvDTE assemblies
when the unified assembly is absent. It does not call the product analysis backend
directly and remains outside the normal product solution.

## Run one target locally

Use Windows, a full supported IDE, the .NET SDK and the .NET Framework 4.7.2/4.8
targeting packs. The job account must be able to install a VSIX. Set the expected
build explicitly; use a new evidence directory every time.

```powershell
$idePath = & "${env:ProgramFiles(x86)}/Microsoft Visual Studio/Installer/vswhere.exe" -latest -products Microsoft.VisualStudio.Product.Professional -property installationPath
dotnet build tests/smoke/VsixSmoke.Tests.csproj -c Release -p:VsTargetVersion=2022 "-p:VisualStudioInstallPath=$idePath" -p:RestoreLockedMode=true
$env:SMOKE_VSIX_PATH = 'C:\artifacts\SonarLint.VSIX-11.3.0.17474-2022.vsix'
$env:SMOKE_EXPECTED_VS_VERSION = '18.11.12328.437'
$env:SMOKE_RESULTS_DIRECTORY = 'C:\temp\slvs3097-run1\evidence'
dotnet test tests/smoke/VsixSmoke.Tests.csproj -c Release --no-build --no-restore --logger trx --results-directory C:\temp\slvs3097-run1 -p:VsTargetVersion=2022
```

The harness creates a unique `SLVS3097...` suffix and only terminates its launched
IDE process. It never resets the normal/shared Exp profile. Profiles remain for
failure inspection; persistent agents need scoped cleanup after investigation.

## Verified evidence and pending work

The original single-target scenario passed three profiles on VS 17.14 in
[run 40863247579](https://github.com/SonarSource/sonarlint-visualstudio/actions/runs/40863247579).
On VS 18, the initial experiment failed before assertions; adding Microsoft's
post-install ClearCache/UpdateConfiguration/ResetSettings sequence resolved that
setup issue. The focused retry passed, followed by independently verified 3/3 in
[run 41454767827](https://github.com/SonarSource/sonarlint-visualstudio/actions/runs/41454767827)
at runtime commit `52e92dfc84bbcbf0c3737cd901e4c7538b7271c0`.
The same VS 18 image passed the normal product build: 6,801 tests passed, 15 skipped.

Those results prove the scenario on VS 17.14 and VS 18, **not VS 17.0 or the new
matrix yet**. Require new native matrix artifacts before claiming both boundaries.
The earlier successful VS 18 executions recorded Session 0/non-interactive; they
establish DTE automation, not foreground, screenshot or visual-rendering coverage.

The latest image's last verified Wiz report had five critical findings in
DigiCert's bundled Bouncy Castle 1.77 jar. Native compatibility passes do not imply
security clearance. Keep vendor/security review, approved IDE licensing, stable
runner rollout and broader stability measurements separate from the smoke result.
Both image PRs and the test PR remain drafts; nothing is merged or promoted by
this spike. Before production adoption, replace the temporary DEV labels/policy
with the explicitly approved stable runner configuration.

## First matrix execution (3 November 2026)

[Run 41620039967](https://github.com/SonarSource/sonarlint-visualstudio/actions/runs/41620039967)
executed three real tests on each pinned IDE at code revision `d15255548`.
All six attempts used distinct profiles and the same signed VSIX/hash. Actual and
expected versions matched each matrix target. Latest VS 18 passed 3/3; downloaded
TRX, 1/0 finding snapshots and exact saved edits were independently verified.

VS 17.0 reached the sample and Error List in all three attempts but timed out
waiting for Sonar S1481. Compiler/IDE diagnostics were present, Sonar output was
empty and no Sonar package loading was recorded in the retained ActivityLog.
This is not yet a proven product compatibility defect: extension deployment and
composition need further evidence. A focused diagnostic follow-up retains the
experimental profile's extension-file inventory, MEF `.err` logs and default
VSIX installer logs while leaving the scenario assertions unchanged.

Fresh image scans still fail policy: latest VS 18 has five DigiCert Bouncy Castle
findings; the minimum image has ten critical findings including its bundled
OpenSSL and .NET/ASP.NET runtime findings. These are not suppressed. Both images
remain isolated DEV candidates and production rollout/security review is pending.

## Minimum installer-routing diagnosis

The focused diagnostic run
[41623202726](https://github.com/SonarSource/sonarlint-visualstudio/actions/runs/41623202726)
identified why no Sonar finding appeared: the VS 17.0 installer returned success
but its log records deployment under the normal `17.0_<instance>` profile, while
the requested experimental profile contains only three cache/list files and no
Sonar binaries. The root suffix was present in the captured command line. This
is a deployment-routing failure, not evidence that the analyzer cannot run on
VS 17.0.

The follow-up uses the explicit application/SKU installer route for VS 17.0 and
keeps the current instance-ID route for VS 18. It additionally requires the exact
extension ID/version manifest inside the requested experimental profile before
launching the IDE, retaining that installed manifest as evidence. It does not
copy loose binaries, change signed manifests or accept a normal-profile install
as an experimental-profile pass. Native verification of this routing change is
still required before claiming minimum-version coverage.
