# SLVS-3097: installed VSIX smoke feasibility

Status: the candidate full-IDE image can install the VSIX and launch Visual
Studio. Diagnostic verification is blocked by the prototype's Error List
automation access; end-to-end feasibility has not yet been established.

The spike asks whether a built SonarQube VSIX can be installed into a fresh Visual
Studio 2022 experimental profile, report an actual finding, and clear it after an
edit. It runs separately from the solution's unit/backend tests and release gates.

## Run on Windows

Use Windows PowerShell 5.1 in an interactive desktop session, with exactly one
full Visual Studio 2022 installation, the C# project system and .NET Framework
4.7.2 targeting pack. Build Tools alone is insufficient. Use a trusted, signed
candidate VSIX from an existing build; the spike does not build the product or
alter certificate trust.

```powershell
powershell.exe -NoProfile -STA -ExecutionPolicy Bypass `
  -File tests/smoke/Invoke-VsixSmoke.ps1 `
  -VsixPath C:\artifacts\SonarLint.VSIX-11.1.0.17360-2022.vsix `
  -ResultsDirectory C:\temp\slvs3097-run1
```

The results directory must not already exist. Each invocation uses a unique
`SLVS3097...` root suffix. It never resets the normal profile or the shared `Exp`
profile, and only terminates processes it launched. The experimental profile is
retained for inspection. On persistent agents, remove only the recorded test
profile with the VS SDK's `CreateExpInstance` utility after investigating failures.

The manual `VSIX smoke feasibility (SLVS-3097)` workflow downloads a specified
promoted build and performs three independent installations by default. The
smoke job currently uses the temporary full-IDE runner from image PR #513
recorded below. The script requires VS 2022, but does not pin its servicing patch.
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

An empty input preserves the normal build. Push and pull-request builds also
continue to use the normal build path. The spike does not change release gates.

## Scenario and evidence

1. Initialize an isolated profile, then install the exact supplied VSIX through
   `VSIXInstaller`, checking its exit code.
2. Launch VS 2022 and attach to that process's DTE object through the Running
   Object Table. An unrelated Visual Studio instance cannot satisfy this lookup.
3. Open a generated C# solution and a file with an unused local variable.
4. Wait for Sonar's S1481 message about `smokeUnused` in the real Error List,
   including a matching file and line 5. The C# compiler's own
   unused-variable warning does not satisfy this message assertion.
5. Edit and save through the IDE, then require the finding to remain absent for
   five seconds. Each asynchronous phase has a bounded timeout.

Evidence includes the VSIX SHA-256, VS version, unique profile suffix, failed
phase, Error List snapshots, transcript and IDE/installer logs. A missing finding
is a failure, including if DTE cannot expose the modern diagnostic table. It is
not silently treated as a skip. The assertion uses an English message because
legacy DTE ErrorItems do not expose a diagnostic code; the IDE starts with LCID
1033. If this surface is inadequate, evaluate the modern table APIs from an
in-process harness before adding UI automation.

## Harness decision

This baseline uses the installed VSIX installer plus DTE, with a small ROT helper
compiled by PowerShell. It adds no NuGet dependencies and does not reference the
product's assemblies or call its analysis backend directly.

Candidates for a maintained suite:

- [Microsoft VS Extension Testing](https://github.com/microsoft/vs-extension-testing)
  supplies experimental-instance tests and VSIX deployment. Its original repo is
  archived and points to Roslyn; verify the current distribution/support path.
- [VsixTesting](https://github.com/josetr/VsixTesting) supplies in-process tests and
  automatic VSIX deployment. Verify current VS compatibility and repository
  package-signing policy before introducing it.
- [VS SDK Test Framework](https://github.com/microsoft/vssdktestfx) supports VS
  services outside the IDE and does not establish installed-extension coverage.

No framework has been proven compatible by this spike yet. Native automation is
the first baseline, not a conclusion that a custom framework should be maintained.

## Exit criteria

Run at least three fresh installations of the same candidate and retain all
results. Record elapsed time and which phases fail. Deliberately use a non-VSIX
input to verify failure propagation. Before adopting a gate, confirm the expected
failure when Sonar is absent/disabled, confirm file-and-line assertions against
the chosen diagnostic API, and assess stability over more builds. Three passes
establish initial feasibility, not a measured flake rate.

Remaining decisions: framework choice, an agent with a dependable desktop
session, modern Error List API support if needed, profile cleanup, and supported
VS versions. Until the diagnostic checks execute successfully, the outcome
remains inconclusive.

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

The next step is to fix Error List access through a suitable automation interface
or an in-process test harness, then repeat the three-run experiment. The run's
`slvs3097-vsix-smoke` artifact contains all three result files and IDE logs. Both
PRs remain drafts and no production image promotion was performed by this spike.

## Validation performed on macOS

- PowerShell parsed the runner and all three inline workflow scripts.
- The ROT helper compiled successfully through `Add-Type`.
- The generated fixture project parsed as XML and its C# source compiled.
- Invoking the runner on macOS returned a nonzero exit with the Windows
  prerequisite error, rather than a skipped or successful smoke result.
- The workflow parsed as YAML.

These checks do not validate VSIX installation, COM registration, Error List
visibility, or diagnostic refresh. Those require the Windows execution above.
