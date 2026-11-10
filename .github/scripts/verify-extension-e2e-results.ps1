param([Parameter(Mandatory = $true)][string]$ResultsDirectory)

$ErrorActionPreference = 'Stop'
[xml]$trx = Get-Content -LiteralPath (Join-Path $ResultsDirectory 'e2e.trx') -Raw
$counters = $trx.TestRun.ResultSummary.Counters
$results = @($trx.TestRun.Results.UnitTestResult)
if ($null -eq $counters -or $counters.total -ne '1' -or $counters.executed -ne '1' -or $counters.passed -ne '1' -or
    $results.Count -ne 1 -or $results[0].outcome -ne 'Passed' -or
    $results[0].testName -ne 'InstalledExtension_DetectsIssue_AndClearsItAfterFix') {
    throw 'Expected exactly one executed and passed Extension E2E test in e2e.trx.'
}
$evidence = Get-Content -LiteralPath (Join-Path $ResultsDirectory 'evidence/result.json') -Raw | ConvertFrom-Json
if ($evidence.status -ne 'passed' -or $evidence.phase -ne 'complete') {
    throw 'The Extension E2E harness did not complete successfully.'
}
$summary = "Extension E2E: 1 executed, 1 passed; test duration: $($results[0].duration)."
Write-Output $summary
if ($env:GITHUB_STEP_SUMMARY) {
    Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value $summary
}
