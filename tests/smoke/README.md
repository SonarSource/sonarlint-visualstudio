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
in `result.json`. The Community matrix below has passed on both exact versions.

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

## Verified minimal Community image (4 November 2026)

[Matrix 41815343155](https://github.com/SonarSource/sonarlint-visualstudio/actions/runs/41815343155)
passed **6/6 intended tests, no skips**: three fresh profiles on each Community
version, earliest 17.0.31903.59 and latest 18.11.12328.437. The latest image is the
minimal Community-only revision `2560082` in
[41807224664](https://github.com/SonarSource/ci-ami-images/actions/runs/41807224664),
AMI `ami-0719459c10c7d75ad`. Its dependency/security provisioning matches master.
The minimum comparison uses its previously verified Community image.

Every canonical attempt's downloaded evidence was independently verified:
actual Community product ID and exact expected IDE build; one executed passing
intended test per TRX/no skips; six distinct profiles; exact installed manifest
inside its requested suffix; one target Sonar finding before/zero after; exact
saved source edit. All use signed VSIX 11.3.0.17474, SHA256
`23FD04E3109F6BF6A050B94A0B663E02AF5079249FB849A25A77DD6BF371D894`.

- [Minimum artifact 13315235892](https://github.com/SonarSource/sonarlint-visualstudio/actions/runs/41815343155/artifacts/13315235892)
- [Latest artifact 13315400387](https://github.com/SonarSource/sonarlint-visualstudio/actions/runs/41815343155/artifacts/13315400387)
- [Normal build 41815423140](https://github.com/SonarSource/sonarlint-visualstudio/actions/runs/41815423140): 6,808 passed / 15 skipped / 6,823 total on the minimal latest image.

The smoke dispatch references `2e539f49c`; subsequent `60e5107f0` changes only the
normal-build runner label, leaving smoke runtime identical. All six tests ran in
Session 0/userInteractive=false. This establishes native DTE behavior, not
foreground/pixel rendering or a measured long-term flake rate.

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

## Security and rollout remain separate

Image PR #513 now contains only full Community installation, required tool-path
changes and IDE checks. No custom security scripts, dependency remediation or
policy suppression are included; all other provisioning matches master.

Fresh Wiz artifact 13314698752 reports `FAILED_BY_POLICY`, 27 critical file/CVE
matches across 18 unique CVEs, zero exposed secret findings and additional
wider-SBOM risks. A [read-only metadata inspection](https://github.com/SonarSource/ci-ami-images/actions/runs/41818010102)
verified actual module hashes and TLS features. The
[SLVS-3097 infra handoff](https://sonarsource.atlassian.net/browse/SLVS-3097?focusedCommentId=1046402)
separates artifact/build false matches from present affected code and conditional
or unknown exploitability, with primary sources. No hostile-input exploitation
was demonstrated; unknown reachability is not declared harmless. Infra owns the
follow-up, as requested. Passing smoke tests do not clear these findings.

All PRs remain drafts for the user's review. Coordinate image promotion with
this PR's dynamic MSVC discovery; existing stable CI hard-codes the old Build
Tools path. No merge or production promotion has been performed. The separate
Roslyn experiment #6859 expects desktop helpers absent from these images; this
verified DTE harness does not need them.
