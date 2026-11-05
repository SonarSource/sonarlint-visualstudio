# Connected-mode feasibility spike (VS18)

This separate draft is stacked on #6854 so its diff adds connected-mode coverage
to the already verified custom C# MSTest/DTE harness. It changes no product code
or runner image. Native connected-mode execution passed three confirmation
attempts; the evidence is recorded below.

## Scenario and discriminating assertions

A disposable SonarQube Community Build 25.11.0.114957 starts on loopback in the
same Windows job, managed by Orchestrator 6.4.4.4797 (the IntelliJ ITs' pinned
version). It uses a full JDK21 for the server and keeps bundled C# analyzers.
No staging project, shared cloud account or long-lived token is required.

1. Install signed VSIX11.3.0.17474 into a fresh VS18 Community experimental
   profile. Open the same C# sample unbound; require standalone S1481 and absence
   of the server-only cognitive-complexity diagnostic.
2. Create a unique server project and empty C# quality profile through public
   Web APIs. Activate only `csharpsquid:S3776` with `threshold=1`; assign
   the profile and independently query its rule list. S1481 is disabled.
3. Store the disposable connection/binding using the extension's actual JSON
   contracts and its `Microsoft.Alm.Authentication` credential-store namespace.
   This restores a persisted binding. **The connection/binding wizard UI is not
   automated.** Refuse to overwrite existing connections/credentials.
4. Install the same VSIX in another fresh profile, open a uniquely named solution
   and require the exact cognitive-complexity finding at the sample path/line3:
   complexity3 exceeds the server limit1. Require standalone S1481 to be absent.
   The input has two nested ifs; default threshold15 cannot satisfy that finding.
5. Remove the nested ifs through DTE and save. Assert the exact
   saved edit and stable absence of both target diagnostics for five seconds.

This calls the actual installed extension/IDE analysis pipeline; server Web APIs
only arrange the fixture. No analysis responses/rules are injected into the IDE.
The proof covers authentication/binding restoration, profile synchronization and
connected analysis, not UI rendering or initial wizard interaction.

## Run on the verified VS18 DEV image

```sh
gh workflow run build.yml --ref codex/slvs-3097-connected-smoke-spike \
  -f connected-smoke-version=11.3.0.17474 \
  -f connected-smoke-runner=warp-custom-pr-513-sonarlint-visualstudio-v20261104092520 \
  -f connected-smoke-repetitions=1
```

Use three repetitions after a focused native pass. Each repetition has distinct
standalone/connected profiles, a unique local server project/profile and solution.
The configure job rejects production runner labels. Normal PR builds for this
branch also use the DEV image; release promotion and dogfood conditions retain
existing behavior and are false for this codex branch.

Artifact `slvs3097-connected-vsix-smoke-vs18` contains the intended test TRX,
per-profile result/IDE version/package hashes, installed manifest, exact source,
server profile query, before/after real Error List and sanitized server logs.
One executed passing intended test/no skips per repetition is required. An
installed profile, bootstrap-only result or normal build alone is not proof.

Server credentials/settings are kept outside the artifact tree. The server token
and artifact credentials are masked/redacted from exported text. Artifact
credentials are removed from the test/IDE environment after server readiness.
The fixture deletes only its own binding/credential/project/profile; the server
stops in finally and has a bounded lifetime. The ephemeral CI account must have
no pre-existing connections. Local Windows users should use a dedicated account.

## Examples inspected and possible follow-up cases

- **sonarlint-core:** `its/tests/.../AbstractConnectedTests.java` and
  `SonarQubeDeveloperEditionTests.java` provision real servers/projects and
  change profile activation; `RulesInConnectedModeMediumTests` exercises active
  rules/parameters; connected issue tests cover server status/suppression.
- **sonarlint-intellij:** `its/.../ConnectedAnalysisTests.kt` and
  `ConfigurationTests.kt` arrange Orchestrator profiles and bind through IDE UI;
  helpers create temporary tokens and clean projects. Those tests also cover
  cloud staging, taint/hotspots, branches and shared binding configuration.
- **sonarlint-eclipse:** `its/.../SonarQubeConnectedModeTest.java` explicitly
  compares unbound versus bound diagnostics for custom secret rules;
  `AbstractSonarQubeConnectedModeTest.java` restores profiles, provisions projects
  and binds through UI. This spike follows the same positive/negative distinction
  with a C# rule parameter and the custom DTE harness.

Good next cases are profile rule activation/parameter changes, server exclusions,
accepted/false-positive issue suppression, reconnect/invalid-token behavior and
shared-binding restoration. Taint/enterprise rules need the relevant server
edition/license and baseline server analysis. They are not claimed by this PoC.
Staging Cloud can later use a separately provisioned project/user, but would add
credential/cleanup/shared-state dependencies that this local first proof avoids.

## Verified native result (5 November 2026)

[Confirmation run 42025124331](https://github.com/SonarSource/sonarlint-visualstudio/actions/runs/42025124331)
passed **3/3 executed intended tests, no skips**, at runtime revision
`3df38bca2764508b935ac0fc8525c9c2dacfbed0`.
[Artifact 13414806635](https://github.com/SonarSource/sonarlint-visualstudio/actions/runs/42025124331/artifacts/13414806635)
was downloaded and independently verified using canonical `run-1`, `run-2` and
`run-3` directories rather than duplicate TRX attachments.

- All six standalone/connected profiles were distinct and recorded actual
  Community 18.11.12328.437 and the expected signed VSIX11.3.0.17474 SHA256
  `23FD04E3109F6BF6A050B94A0B663E02AF5079249FB849A25A77DD6BF371D894`.
- Each exact extension manifest/version was inside its requested experimental
  profile. Each TRX contained the intended executed passing test and no skips.
- Three distinct server projects, quality profiles and connected solution names
  were recorded. Server defaults showed threshold15; assigned profiles contained
  only S3776 with the test's threshold1 configuration.
- Every standalone snapshot contained S1481 at line5 and no server-specific
  S3776 diagnostic. Every bound snapshot contained the exact 3-to1 cognitive
  finding at the sample path/line3 and no S1481. After the DTE edit, neither target
  finding remained. Unrelated compiler/IDE diagnostics are allowed.
- Every saved connected source exactly matched its standalone control with the
  nested-if statement removed. The unused variable remains, so the absence of
  S1481 after editing continues to exercise the server's rule deactivation.
- Test durations: 224.4s, 170.2s and 162.9s. The focused
  [42022643379](https://github.com/SonarSource/sonarlint-visualstudio/actions/runs/42022643379)
  also passed and was independently verified before confirmation.

The artifact contains no private `server.json` or Maven settings and no recorded
fixture-cleanup failure. All runs used Session0/userInteractive=false. This proves
persisted binding authentication/profile synchronization and native IDE connected
analysis on this configuration. It does not prove wizard interaction, rendered UI,
Cloud staging behavior, enterprise rules or a long-term flake rate.

Initial attempts exposed an unavailable C# naming-rule choice and profile API
parameter mistakes before connected assertions. The implemented C# S3776 and its
actual `threshold` contract were then verified against analyzer source. Those
fixture errors were corrected before the native focused and confirmation passes.
All PRs remain drafts for the user's review; no merge or production promotion.
