# SLVS-3097: installed VSIX smoke feasibility

Status: end-to-end smoke scenario verified on the candidate full-IDE image.
Three fresh profiles installed the VSIX, detected the expected Sonar finding,
and cleared it after editing and saving the file. Evidence is linked below.

The spike asks whether a built SonarQube VSIX can be installed into a fresh Visual
Studio 2022 experimental profile, report an actual finding, and clear it after an
edit. It runs separately from the solution's unit/backend tests and release gates.

## Run on Windows

Use Windows with exactly one full Visual Studio 2022 installation, a usable
IDE automation session, the .NET SDK, and the .NET Framework 4.7.2 targeting pack.
Build Tools alone is insufficient. Use a trusted, signed candidate VSIX.

```powershell
$idePath = & "${env:ProgramFiles(x86)}/Microsoft Visual Studio/Installer/vswhere.exe" -latest -products Microsoft.VisualStudio.Product.Professional -property installationPath
dotnet build tests/smoke/VsixSmoke.Tests.csproj -c Release -p:VsTargetVersion=2022 "-p:VisualStudioInstallPath=$idePath" -p:RestoreLockedMode=true
$env:SMOKE_VSIX_PATH = 'C:\artifacts\SonarLint.VSIX-11.3.0.17474-2022.vsix'
$env:SMOKE_RESULTS_DIRECTORY = 'C:\temp\slvs3097-run1\evidence'
dotnet test tests/smoke/VsixSmoke.Tests.csproj -c Release --no-build --no-restore --logger trx --results-directory C:\temp\slvs3097-run1 -p:VsTargetVersion=2022
```

The results directory must not already exist. Each test invocation uses a unique
`SLVS3097...` root suffix. It never resets the normal profile or the shared `Exp`
profile, and only terminates processes it launched. The experimental profile is
retained for inspection. On persistent agents, remove only the recorded test
profile with the VS SDK's `CreateExpInstance` utility after investigating failures.

The manual `VSIX smoke feasibility (SLVS-3097)` workflow downloads a specified
promoted build and performs three independent installations by default. The
smoke job currently uses the temporary full-IDE runner from image PR #513
recorded below. The test requires VS 2022, but does not pin its servicing patch.
Record the reported version; a rebuilt image PR may emit a different runner label.
The standard `warp-custom-sonarlint-visualstudio` image still contains only Build
Tools unless separately updated and promoted.

Before the new workflow exists on the default branch, dispatch the existing
`build.yml` workflow on the spike branch with `vsix-smoke-version` set. This calls
the reusable smoke workflow and skips the normal build/publish/promote job:

```sh
gh workflow run build.yml --ref codex/slvs-3097-vsix-smoke-spike \
  -f vsix-smoke-version=<promoted-build-version>
```

For a focused debugging run, add `-f vsix-smoke-repetitions=1`. The default is
three fresh profiles. The standalone smoke workflow also offers a repetitions
input once available on the default branch.

An empty input preserves the normal build. Push and pull-request builds also
continue to use the normal build path. The spike does not change release gates.

## Scenario and evidence

1. Initialize an isolated profile, then install the exact supplied VSIX through
   `VSIXInstaller`, checking its exit code.
2. Launch VS 2022 and attach to that process's DTE object through the Running
   Object Table. An unrelated Visual Studio instance cannot satisfy this lookup.
3. Open a fresh copy of the checked-in `Fixture` C# solution and a file with an unused local variable.
4. Wait for Sonar's S1481 message about `smokeUnused` in the real Error List,
   including a matching file and line 5. The C# compiler's own
   unused-variable warning does not satisfy this message assertion.
5. Edit and save through the IDE, then require the finding to remain absent for
   five seconds. Each asynchronous phase has a bounded timeout.

Evidence includes the VSIX SHA-256, VS version, unique profile suffix, failed
phase, Error List snapshots, TRX test results, Sonar output-pane text and IDE/installer
logs. Snapshots are JSON arrays, including when empty. A missing finding
is a failure, including if DTE cannot expose the modern diagnostic table. It is
not silently treated as a skip. The assertion uses an English message because
legacy DTE ErrorItems do not expose a diagnostic code; the IDE starts with LCID
1033. If this surface is inadequate, evaluate the modern table APIs from an
in-process harness before adding UI automation.

## Harness decision

The scenario is an MSTest C# test in `VsixSmokeTests.cs`, with setup, polling,
evidence capture and process cleanup in `VisualStudioSession.cs`. The existing
`VisualStudioAutomation.cs` helper provides typed DTE2 access to the Error List
and the Running Object Table lookup. `Fixture/Smoke.cs.txt`, `.csproj` and `.sln` are
copied to a fresh results directory for every attempt. The `.txt` suffix marks
the deliberately faulty source as test input; the copied file is `Smoke.cs`.

`[STATestMethod]` supplies the STA thread required by COM automation, and
`[DoNotParallelize]` isolates the scenario from other tests in this test assembly.
MSTest 4.4.1 and Newtonsoft.Json 13.0.3 match existing repository versions; the
NuGet lock file pins the dependency graph. Visual Studio interop is referenced
from the selected IDE, with no reference to product implementation assemblies.
The project stays outside the product solution so ordinary unit-test runs do
not install an extension or launch the IDE. CI launches a separate test host
for each repetition and retains both TRX results and the scenario evidence.

The original PowerShell scenario is replaced by this C# implementation. The
28 October results below describe the original harness, not validation of the
C# port. Results for the port must be recorded separately after a Windows run.

Candidates for a maintained suite:

- [Microsoft VS Extension Testing](https://github.com/microsoft/vs-extension-testing)
  supplies experimental-instance tests and VSIX deployment. Its original repo is
  archived and points to Roslyn; verify the current distribution/support path.
- [VsixTesting](https://github.com/josetr/VsixTesting) supplies in-process tests and
  automatic VSIX deployment. Verify current VS compatibility and repository
  package-signing policy before introducing it.
- [VS SDK Test Framework](https://github.com/microsoft/vssdktestfx) supports VS
  services outside the IDE and does not establish installed-extension coverage.

The native automation baseline is verified below. The alternative frameworks
have not been evaluated by this spike.

## Exit criteria

Run at least three fresh installations of the same candidate and retain all
results. Record elapsed time and which phases fail. Deliberately use a non-VSIX
input to verify failure propagation. Before adopting a gate, confirm the expected
failure when Sonar is absent/disabled, confirm file-and-line assertions against
the chosen diagnostic API, and assess stability over more builds. Three passes
establish initial feasibility, not a measured flake rate.

Rollout decisions remain: approved IDE edition/licensing, the promoted runner
label, profile cleanup, supported VS versions, and stability across more builds.

## Windows CI evidence (27 October 2026)

[Run 40520267897](https://github.com/SonarSource/sonarlint-visualstudio/actions/runs/40520267897)
ran on `warp-custom-sonarlint-visualstudio` against promoted VSIX
`11.3.0.17474`. Authentication, artifact download and evidence upload succeeded.
All three independent attempts failed in `prerequisites`:

> Expected exactly one full Visual Studio 2022 IDE; found 0. Installed products: Visual Studio Build Tools 2022.

The captured inventory identifies `Microsoft.VisualStudio.Product.BuildTools`,
version `17.10.35230.96`. Installation and analysis were not reached. This is an
infrastructure blocker, not evidence for or against the diagnostic API or either
candidate framework. The next step is an image with the full VS 2022 IDE, C#
project system, targeting pack, and usable desktop automation session, followed
by the same three-run experiment. No release/promotion job ran in this dispatch.

## Candidate full-IDE evidence (28 October 2026)

[Run 40679681144](https://github.com/SonarSource/sonarlint-visualstudio/actions/runs/40679681144)
used DEV AMI `ami-0efa0d916e52ff996` from
[ci-ami-images PR #513](https://github.com/SonarSource/ci-ami-images/pull/513),
through runner `warp-custom-pr-513-sonarlint-visualstudio-v20261028102646`.
The installed IDE reports version `17.14.37824.6`.

All three attempts passed prerequisites, profile initialization, VSIX installer
exit checks, IDE launch, DTE attachment, solution opening and file opening. They
then failed in `detect-finding` with:

> The property 'ToolWindows' cannot be found on this object. Verify that the property exists.

This is a harness automation-interface failure, not a failed diagnostic assertion.
The IDE ActivityLog also records successful loading of the SonarQube extension.
No diagnostic snapshot was obtained, and editing/clearing the finding was not
reached. Installation and IDE automation are now demonstrated on this candidate;
analysis output and refresh remain unverified.

This failure was resolved by using the typed DTE2 helper described above. The
failed run's `slvs3097-vsix-smoke` artifact retains the original evidence.

## Verified end-to-end result (28 October 2026)

[Run 40688355572](https://github.com/SonarSource/sonarlint-visualstudio/actions/runs/40688355572)
passed all three fresh-profile attempts at harness commit
`f0715b0af9ccb7d7429dfa44fa43240596e3804b`. It used the same candidate image and
VSIX versions listed above. Each run's result, before/after JSON snapshots and
saved fixture source were independently checked after download.

| Attempt | Result | Matching finding before / after edit | Scenario duration |
| --- | --- | --- | --- |
| 1 | Passed | 1 / 0 | 129.5 seconds |
| 2 | Passed | 1 / 0 | 82.9 seconds |
| 3 | Passed | 1 / 0 | 82.3 seconds |

All three profiles had distinct root suffixes. The before snapshot contains
`Remove the unused local variable 'smokeUnused'.` at line 5 of `Smoke.cs`; after
editing, the matching Sonar finding is absent for at least five seconds. The
retained file matches the expected saved edit with the unused declaration
removed. Other fixture diagnostics may remain in the Error List.

All three attempts used VSIX SHA-256
`23FD04E3109F6BF6A050B94A0B663E02AF5079249FB849A25A77DD6BF371D894`.
The job completed in 5m37s, excluding queue time. The earlier focused
[run 40687290556](https://github.com/SonarSource/sonarlint-visualstudio/actions/runs/40687290556)
also passed the scenario once.

Coverage is one standalone C# analysis scenario on Visual Studio Professional
2022 17.14.43 with SonarQube VSIX 11.3.0.17474. The smoke workflow remains
manual, uses the temporary DEV image, and does not change release gates. Both
PRs remain drafts.

## C# port validation (29 October 2026)

Windows execution of the C# port is pending. Local compilation and workflow
validation do not establish VSIX installation or diagnostic refresh; those must
be verified by a new Windows smoke run.
