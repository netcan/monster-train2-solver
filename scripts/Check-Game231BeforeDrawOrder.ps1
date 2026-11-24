#requires -Version 7.4
param([switch]$SkipBuild, [switch]$ComponentOnly, [switch]$PlayerAdvisor)
$ErrorActionPreference = 'Stop'
if ($PlayerAdvisor -and $ComponentOnly) { throw 'Player advisor receipt requires the complete native battle check.' }
$workspace = Split-Path $PSScriptRoot -Parent
if (!$SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace $(if ($PlayerAdvisor) {
    'tests/compatibility/game-2.3.1/mechanics/before-draw-order/player-advisor'
} else { 'tests/compatibility/game-2.3.1/mechanics/before-draw-order' })
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if ($manifest.Count -ne 2 -or (($manifest.path | Sort-Object) -join ',') -cne 'energy-first.mt2f,heal-first.mt2f') { throw 'Unexpected before-draw order manifest.' }
$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
if ($PlayerAdvisor) {
    Add-Type -Path (Join-Path $workspace 'src/FixtureArchive/bin/Release/net8.0/FixtureArchive.dll')
}
foreach ($entry in $manifest) {
    $path = Join-Path $directory $entry.path
    if ((Get-Item -LiteralPath $path).Length -ne [long]$entry.bytes -or
        (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $entry.sha256) { throw "Before-draw order archive differs: $path" }
    if ($PlayerAdvisor) {
        $archive = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read($path)
        try {
            if ($archive.RootElement.GetProperty('PlayerAdvisor').ValueKind.ToString() -cne 'Object') {
                throw "Player advisor receipt lacks real player evidence: $path"
            }
        } finally { $archive.Dispose() }
    }
    $mode = if ($ComponentOnly) { '--before-draw-pair-only' } else { '--before-draw-order-only' }
    dotnet $checker $mode $path
    if ($LASTEXITCODE) { throw "Native before-draw order check failed: $($entry.path)" }
}
Write-Output "GAME-231-BEFORE-DRAW-ORDER PASS: unchanged original owners in both orders, distinct native income and immutable dispatch; player advisor=$([bool]$PlayerAdvisor); complete battles included=$(!$ComponentOnly)."
