#requires -Version 7.4
[CmdletBinding()]
param(
    [ValidateSet('Birth', 'Health', 'LifeLink', 'HealthUpdates')][string[]] $Case = @('Birth', 'Health', 'LifeLink', 'HealthUpdates'),
    [switch] $SkipBuild
)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE -ne 0) { throw 'True-final-boss checker build failed.' }
}
$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
$manifest = Import-Csv -LiteralPath (Join-Path $workspace 'tests/fixtures/manifest.tsv') -Delimiter "`t"
foreach ($item in $Case) {
    $archive = if ($item -eq 'Birth') { 'true-final-boss-birth-calibration.mt2f' } elseif ($item -eq 'Health') {
        'true-final-boss-linked-health-calibration.mt2f'
    } elseif ($item -eq 'LifeLink') { 'true-final-boss-life-link-calibration.mt2f' }
    else { 'true-final-boss-health-updates-calibration.mt2f' }
    $path = Join-Path $workspace "tests/fixtures/$archive"
    $rows = @($manifest | Where-Object archive -CEQ $archive)
    if ($rows.Count -ne 1 -or (Get-Item -LiteralPath $path).Length -ne [long]$rows[0].archive_bytes -or
        (Get-FileHash -LiteralPath $path).Hash -ine $rows[0].archive_sha256) { throw "True-final-boss archive metadata differs: $archive" }
    $mode = if ($item -eq 'Birth') { '--true-final-boss-birth-only' } elseif ($item -eq 'Health') {
        '--true-final-boss-health-only'
    } elseif ($item -eq 'LifeLink') { '--true-final-boss-life-link-only' }
    else { '--true-final-boss-health-updates-only' }
    dotnet $checker $mode $path
    if ($LASTEXITCODE -ne 0) { throw "True-final-boss $item focused check failed." }
}
Write-Output "TRUE-FINAL-BOSS-FOCUSED PASS cases=$($Case -join ','); no full regression executed."
