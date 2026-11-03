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

The candidate images now select **Visual Studio Community**. The matrix asserts
`Microsoft.VisualStudio.Product.Community` and records the actual product ID
in `result.json`; the earlier passing runs below used Professional. Community
image builds and a fresh six-attempt matrix must pass before claiming equivalence.

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
push/PR builds do not automatically execute the smoke scenario. Only the normal
PR #6854 build selects the DEV candidate for toolchain validation; all other
normal builds, including master/release builds after merge, retain the stable
`warp-custom-sonarlint-visualstudio` runner.

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
$idePath = & "${env:ProgramFiles(x86)}/Microsoft Visual Studio/Installer/vswhere.exe" -latest -products Microsoft.VisualStudio.Product.Community -property installationPath
dotnet build tests/smoke/VsixSmoke.Tests.csproj -c Release -p:VsTargetVersion=2022 "-p:VisualStudioInstallPath=$idePath" -p:RestoreLockedMode=true
$env:SMOKE_VSIX_PATH = 'C:\artifacts\SonarLint.VSIX-11.3.0.17474-2022.vsix'
$env:SMOKE_EXPECTED_VS_VERSION = '18.11.12328.437'
$env:SMOKE_EXPECTED_VS_PRODUCT = 'Microsoft.VisualStudio.Product.Community'
$env:SMOKE_RESULTS_DIRECTORY = 'C:\temp\slvs3097-run1\evidence'
dotnet test tests/smoke/VsixSmoke.Tests.csproj -c Release --no-build --no-restore --logger trx --results-directory C:\temp\slvs3097-run1 -p:VsTargetVersion=2022
```

The harness creates a unique `SLVS3097...` suffix and only terminates its launched
IDE process. It never resets the normal/shared Exp profile. Profiles remain for
failure inspection; persistent agents need scoped cleanup after investigation.

## Dependency-clean image validation (3 November 2026)

[Matrix 41643881110](https://github.com/SonarSource/sonarlint-visualstudio/actions/runs/41643881110)
passed six independently verified tests (three per IDE) at `6b0c7afb6`.
Each canonical attempt passed the exact version/package/experimental-profile
manifest checks, detected the target finding, saved the exact edit and observed
its disappearance, with one executed passing test and no skips per TRX.
[Normal build 41643887843](https://github.com/SonarSource/sonarlint-visualstudio/actions/runs/41643887843)
also passed: 6,801 passed and 15 skipped out of 6,816 tests.

These images remove unused DigiCert tools and replace SDK 8.0.415/runtime 8.0.21
with SDK 8.0.427/runtime 8.0.33. Final provisioning rejected stale .NET 8 runtimes
and verified DigiCert absence. Latest image `ee8dea7` has zero vulnerability
findings in [scan 41634758814](https://github.com/SonarSource/ci-ami-images/actions/runs/41634758814)
(actual verdict `WARN_BY_POLICY`, zero exposed secret findings). Minimum image
`bbb8e54` has one critical finding in
[scan 41634848833](https://github.com/SonarSource/ci-ami-images/actions/runs/41634848833):
CVE-2021-3711 in VS 17.0's bundled OpenSSL 1.1.1k. Its actual verdict remains
`FAILED_BY_POLICY`; isolation/security approval is still required.

Subsequent Gitar fixes change pre-reboot discovery and workflow rollout guards;
their fresh image builds are pending. No production promotion is authorized.

## Verified earliest/latest matrix (3 November 2026)

[Run 41628515971](https://github.com/SonarSource/sonarlint-visualstudio/actions/runs/41628515971)
passed **three fresh-profile attempts per target (six executed tests, no skips)**
at runtime commit `327f7f40696e6a2c64eeb9a048f56e6d6c06838a`.
Both artifacts were downloaded and independently verified using only their three
canonical `run-N/evidence` directories, not duplicated TRX attachments.

| Target | Actual / expected IDE build | Results | Scenario durations |
| --- | --- | --- | --- |
| Earliest VS 2022 17.0 | 17.0.31903.59 | 3/3 passed | 115.6s, 106.6s, 107.0s |
| Latest VS 2026 18.11.3 | 18.11.12328.437 | 3/3 passed | 101.0s, 85.3s, 85.1s |

Every result reports passed/complete, one executed passing intended test per TRX,
a unique profile, the target finding before and absent after the edit, and the
exact expected saved source. The installed extension manifest matches the supplied
extension ID/version and its actual directory is inside that requested profile.
All six use signed VSIX 11.3.0.17474, SHA-256
`23FD04E3109F6BF6A050B94A0B663E02AF5079249FB849A25A77DD6BF371D894`.

- [Minimum artifact](https://github.com/SonarSource/sonarlint-visualstudio/actions/runs/41628515971/artifacts/13228400102)
- [Latest artifact](https://github.com/SonarSource/sonarlint-visualstudio/actions/runs/41628515971/artifacts/13227705864)

Both current DEV images built and registered from their respective PR heads:

- Latest #513 revision `927b61f`, [image run 41611014286](https://github.com/SonarSource/ci-ami-images/actions/runs/41611014286), AMI `ami-081028ac9e92dde1a`.
- Minimum #520 revision `c85a3a8`, [image run 41612090162](https://github.com/SonarSource/ci-ami-images/actions/runs/41612090162), AMI `ami-0ae7f36db65e2bc0d`.

All six successful results record Session 0 and `userInteractive=false`. This
establishes DTE automation in that environment, not foreground interaction,
screenshot support or visual-rendering correctness. Three passes per version
establish feasibility; they are not a measured long-term flake rate.

## Minimum-version installer routing

The initial [matrix run 41620039967](https://github.com/SonarSource/sonarlint-visualstudio/actions/runs/41620039967)
passed on latest but failed on minimum with no Sonar finding. The focused
[diagnostic run 41623202726](https://github.com/SonarSource/sonarlint-visualstudio/actions/runs/41623202726)
proved the RTM VS 17 installer had accepted RootSuffix but deployed into the
normal profile. The requested test profile contained only cache/list files.

For VS 17.0 the harness therefore uses the installer's explicit application/SKU
route; VS 18 retains its instance-ID route. A mandatory deployment guard checks
the exact installed manifest in the experimental profile before launching the
IDE. It does not copy loose binaries or change the signed package. Both targets
passed the focused [routing verification 41626340328](https://github.com/SonarSource/sonarlint-visualstudio/actions/runs/41626340328)
before the final six-pass confirmation above.

The helper retains extension-file inventory, MEF error files and default VSIX
installer logs, so a successful installer exit alone cannot be mistaken for
correct deployment into the intended profile. Post-install cache/configuration
preparation follows Microsoft's integration-harness sequence.

## Security and rollout still require review

Fresh Wiz reports return FAILED_BY_POLICY despite green scan jobs: the latest
image reports five critical findings and the minimum reports ten. Those are
scanner findings, not a completed exploitability assessment. Separate triage
identified possible Bouncy Castle module-matching false positives, but actual
bundled-JAR confirmation is pending; no finding has been accepted or suppressed.
Native smoke passes do not establish security clearance.

The minimum image deliberately tests the user-selected VS 17.0 compatibility
boundary, which is outside Microsoft's maintained servicing lines. Keep it
isolated for compatibility/security review. Approved IDE licensing, supported
vendor remediation or reviewed security decisions, stable runner rollout and
broader stability measurements remain prerequisites to production adoption.
Both image PRs and the test PR remain drafts; nothing is merged or promoted.
#520 is stacked on #513 and must be retargeted after the parent is merged.
