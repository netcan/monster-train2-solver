#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild)

$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/monster-damage'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if ($manifest.Count -ne 2 -or @($manifest.path | Sort-Object -Unique).Count -ne 2 -or
    @($manifest.path | Where-Object { $_ -notin @('statistics.mt2f', 'steward-battle.mt2f') }).Count) {
    throw 'Unexpected monster damage fixture manifest.'
}
foreach ($entry in $manifest) {
    $path = Join-Path $directory $entry.path
    if ((Get-Item -LiteralPath $path).Length -ne [long]$entry.bytes -or
        (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $entry.sha256) { throw "Monster damage fixture hash/size differs: $path" }
}
$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
dotnet $checker --monster-damage-statistics-only (Join-Path $directory 'statistics.mt2f')
if ($LASTEXITCODE) { throw 'Independent native damage statistics validation failed.' }
Add-Type -Path (Join-Path $workspace 'src/FixtureArchive/bin/Release/net8.0/FixtureArchive.dll')
$battle = Join-Path $directory 'steward-battle.mt2f'
$document = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read($battle)
try {
    $root = $document.RootElement
    if ($root.GetProperty('GameVersion').GetString() -ne '2.3.1' -or
        $root.GetProperty('GameModuleMvid').GetString() -ne 'bf93f060-beda-46b1-b808-95ebf1e9a99d' -or
        -not $root.GetProperty('NativeWon').GetBoolean() -or
        -not $root.GetProperty('TerminalEffectsSettled').GetBoolean()) { throw 'Unexpected game version or incomplete monster damage battle.' }
    foreach ($counter in @('CaptureFailures', 'Mismatches', 'Unsupported', 'Pending')) {
        if ($root.GetProperty($counter).GetInt32() -ne 0) { throw "Native battle counter is not zero: $counter" }
    }
    $statistics = $root.GetProperty('Actions')[0].GetProperty('Before').GetProperty('Spawn').GetProperty('Train').GetProperty('Context').GetProperty('Statistics')
    if ($statistics.GetProperty('MonsterDamage').ValueKind -eq 'Null') { throw 'Native monster damage counters were omitted.' }
} finally { $document.Dispose() }
dotnet $checker --native-room-train-only $battle
if ($LASTEXITCODE) { throw 'Independent native room/train validation failed.' }
dotnet $checker --recorded-chain-diagnose $battle
if ($LASTEXITCODE) { throw 'Independent recorded chain validation failed.' }
Write-Output 'GAME-231-MONSTER-DAMAGE PASS: native counter controls, six queries, zero-counter seven-turn battle, room/train replay and 16 immutable full-battle branches.'
