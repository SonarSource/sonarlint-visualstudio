# PoC: Testing Sign CLI with DigiCert

## Quick Start (5 minutes)

### Step 1: Create a test branch
```bash
git checkout -b sign-test-sign-cli
git push origin sign-test-sign-cli
```

**Important**: Branch name MUST start with `sign-` to trigger the signed build.

### Step 2: Create a PR

Create a PR from your branch → This will trigger the build with signing.

### Step 3: Watch the build logs

Go to Actions tab → Look for your build → Check the "Build Signed" step

**What to look for in logs:**
```
🧪 PoC: Testing Sign CLI with DigiCert KSP...
Signing with Sign CLI...
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
1. NuGet sign (current method):
❌ NuGet: NOT VERIFIED                  ← Expected (this is the bug)

2. Sign CLI (PoC test):
✅ Sign CLI: VERIFIED                   ← We want to see this!
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

### Step 4: Download and test

1. Download the "binaries" artifact from the workflow run
2. Extract both VSIX files:
   - `SonarLint.VSIX-X.X.X-2022.vsix` (original nuget sign)
   - `SonarLint.VSIX-X.X.X-2022-signcli.vsix` (Sign CLI test)
3. Try installing the `-signcli.vsix` in Visual Studio
4. Check if it shows "SonarSource SA" as verified publisher

## Expected Outcomes

### ✅ Success (likely)
- Sign CLI step completes without error
- `signtool verify` shows "✅ Sign CLI: VERIFIED"
- Visual Studio shows verified publisher badge

**Action**: Update line 141 to use Sign CLI instead of nuget sign, remove PoC code, merge to master.

### ❌ Failure (unlikely based on community reports)
- Sign CLI throws certificate errors
- `signtool verify` still shows "NOT VERIFIED"

**Action**: Proceed with Azure Trusted Signing implementation.

## What Changed

I added a PoC test section to `.github/workflows/build.yml` (lines 143-176) that:
1. Installs Sign CLI
2. Creates a copy of the VSIX
3. Signs it with Sign CLI using DigiCert KSP
4. Verifies both versions (nuget vs Sign CLI)
5. Uploads both to artifacts

## Clean Up

After testing, you can:
- Close the PR (don't merge)
- Delete the test branch
- Remove the PoC code block (lines 143-176) from build.yml if moving to Azure Trusted Signing
- OR keep and update to production Sign CLI code if it works

## Questions During Test?

Check these:
- Branch name starts with `sign-`? (Required to trigger signed build)
- DigiCert secrets accessible? (Should work if existing builds work)
- Sign CLI installed successfully? (Check logs)
- Certificate fingerprint format? (Script handles removing colons)

---

**Time investment:** ~5 minutes to set up, ~10 minutes for build, ~5 minutes to test = **20 minutes total**
