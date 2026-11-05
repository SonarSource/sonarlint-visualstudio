$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$results = Join-Path $env:RUNNER_TEMP 'slvs3097-connected-results'
$private = Join-Path $env:RUNNER_TEMP 'slvs3097-connected-private'
$null = New-Item -ItemType Directory -Force -Path $results, $private
$redactions = @($env:ARTIFACTORY_USER, $env:ARTIFACTORY_PASSWORD, [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes("${env:ARTIFACTORY_USER}:${env:ARTIFACTORY_PASSWORD}")))
$settings = Join-Path $private 'settings.xml'
# Environment references are expanded by Maven, not written as credentials to disk.
@'
<settings><mirrors><mirror><id>repox</id><mirrorOf>*</mirrorOf><url>https://repox.jfrog.io/repox/sonarsource</url></mirror></mirrors>
<servers><server><id>repox</id><username>${env.ARTIFACTORY_USER}</username><password>${env.ARTIFACTORY_PASSWORD}</password></server></servers></settings>
'@ | Set-Content -LiteralPath $settings -Encoding UTF8
$serverProcess = $null
$info = $null
try {
    & mvn -s $settings -B -ntp -f tests/smoke/server/pom.xml compile dependency:build-classpath '-Dmdep.outputFile=target/classpath.txt'
    if ($LASTEXITCODE -ne 0) { throw 'Orchestrator helper build failed.' }
    $target = Join-Path (Get-Location) 'tests/smoke/server/target'
    $classpath = (Join-Path $target 'classes') + ';' + [IO.File]::ReadAllText((Join-Path $target 'classpath.txt')).Trim()
    $java = Join-Path $env:JAVA_HOME 'bin/java.exe'
    $serverProcess = Start-Process -FilePath $java -ArgumentList @('-cp', "`"$classpath`"", 'ConnectedServer', "`"$private`"") -PassThru -RedirectStandardOutput (Join-Path $private 'launcher.log') -RedirectStandardError (Join-Path $private 'launcher-error.log')
    $env:SMOKE_SERVER_INFO = Join-Path $private 'server.json'
    $deadline = [DateTime]::UtcNow.AddMinutes(10)
    while (-not (Test-Path -LiteralPath $env:SMOKE_SERVER_INFO)) {
        if ($serverProcess.HasExited) { throw 'Disposable server exited before readiness.' }
        if ([DateTime]::UtcNow -gt $deadline) { throw 'Disposable server readiness timed out.' }
        Start-Sleep -Seconds 2
    }
    $info = Get-Content -LiteralPath $env:SMOKE_SERVER_INFO -Raw | ConvertFrom-Json
    Write-Output "::add-mask::$($info.token)"
    $redactions += $info.token
    $redactions += [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes("$($info.token):"))
    # Analysis and the IDE do not need the artifact credentials used to bootstrap the server.
    Remove-Item Env:ARTIFACTORY_USER, Env:ARTIFACTORY_PASSWORD -ErrorAction SilentlyContinue
    $failed = @()
    for ($i = 1; $i -le [int]$env:REPETITIONS; $i++) {
        $attempt = Join-Path $results "run-$i"
        $env:SMOKE_RESULTS_DIRECTORY = Join-Path $attempt 'evidence'
        $env:SMOKE_VSIX_PATH = Join-Path $env:RUNNER_TEMP 'candidate.vsix'
        & dotnet test tests/smoke/VsixSmoke.Tests.csproj -c Release --no-build --no-restore --filter TestCategory=ConnectedInstalledVsixSmoke --logger 'trx;LogFileName=connected.trx' --results-directory $attempt -p:VsTargetVersion=2022
        if ($LASTEXITCODE -ne 0) { $failed += $i }
    }
    if ($failed.Count) { throw "Connected smoke failed: $($failed -join ', ')" }
}
finally {
    if ($serverProcess) {
        $null = New-Item -ItemType File -Force -Path (Join-Path $private 'stop')
        if (-not $serverProcess.WaitForExit(60000)) { & taskkill.exe /PID $serverProcess.Id /T /F | Out-Null }
    }
    # Only sanitized log text is exported; no server.json, settings or credential store.
    $diagnostics = Join-Path $results 'server-diagnostics'
    $null = New-Item -ItemType Directory -Force -Path $diagnostics
    foreach ($name in @('launcher.log', 'launcher-error.log')) {
        $path = Join-Path $private $name
        if (Test-Path -LiteralPath $path) { Copy-Item -LiteralPath $path -Destination $diagnostics }
    }
    if ($info -and $info.logs) {
        foreach ($name in @('sonar.log', 'web.log', 'ce.log', 'es.log')) {
            $path = Join-Path $info.logs $name
            if (Test-Path -LiteralPath $path) { Copy-Item -LiteralPath $path -Destination $diagnostics }
        }
    }
    foreach ($file in Get-ChildItem -LiteralPath $results -Recurse -File | Where-Object { $_.Extension -in '.log', '.json', '.txt', '.trx', '.xml' }) {
        $reader = [IO.StreamReader]::new($file.FullName, $true)
        try { $text = $reader.ReadToEnd(); $encoding = $reader.CurrentEncoding } finally { $reader.Dispose() }
        foreach ($secret in $redactions) { if ($secret) { $text = $text.Replace($secret, '[REDACTED]') } }
        [IO.File]::WriteAllText($file.FullName, $text, $encoding)
    }
    Remove-Item -LiteralPath $private -Recurse -Force -ErrorAction SilentlyContinue
}
