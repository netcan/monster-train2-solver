#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild)

$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/lethal-maximum'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
$expected = @('plain', 'undying', 'horde', 'horde-undying', 'plain-zero', 'undying-zero', 'horde-zero', 'horde-undying-zero')
if ($manifest.Count -ne 8 -or ($manifest.path -join ',') -cne (($expected | ForEach-Object { $_ + '.mt2f' }) -join ',')) {
    throw 'Unexpected lethal maximum-health manifest.'
}
$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
Add-Type -Path (Join-Path $workspace 'src/FixtureArchive/bin/Release/net8.0/FixtureArchive.dll')
foreach ($entry in $manifest) {
    $path = Join-Path $directory $entry.path
    if ((Get-Item -LiteralPath $path).Length -ne [long]$entry.bytes -or
        (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $entry.sha256) { throw 'Lethal maximum-health capture hash/size differs.' }
    $document = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read($path)
    try {
        if ($document.RootElement.GetProperty('LethalMaximumBattle').GetProperty('Mode').GetString() -cne
            [System.IO.Path]::GetFileNameWithoutExtension($entry.path)) { throw 'Lethal maximum-health variant does not match its archive name.' }
    } finally { $document.Dispose() }
    dotnet $checker --lethal-maximum-battle-only $path
    if ($LASTEXITCODE) { throw ('Independent lethal maximum-health validation failed: ' + $entry.path) }
}
Write-Output 'GAME-231-LETHAL-MAXIMUM PASS: eight native five-play/seven-turn victories, ordinary/Horde/Undying and zero-max controls, skipped ordinary half-health/casualty phase, physical compaction/exhaustion or one-HP revival, complete successors/RNG, 16 immutable branches and 128-state serial/parallel search per battle. Original card/relic coverage is separate.'
