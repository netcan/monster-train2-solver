#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild)

$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if ($manifest.Count -ne 1 -or $manifest[0].path -cne 'vanguard-enchantment.mt2f') { throw 'Unexpected Vanguard fixture manifest.' }
$path = Join-Path $directory $manifest[0].path
if ((Get-Item -LiteralPath $path).Length -ne [long]$manifest[0].bytes -or
    (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $manifest[0].sha256) { throw 'Vanguard capture bytes/hash differ.' }
Add-Type -Path (Join-Path $workspace 'src/FixtureArchive/bin/Release/net8.0/FixtureArchive.dll')
$document = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read($path)
try {
    $root = $document.RootElement
    if ($root.GetProperty('GameVersion').GetString() -ne '2.3.1' -or
        $root.GetProperty('GameModuleMvid').GetString() -ne 'bf93f060-beda-46b1-b808-95ebf1e9a99d' -or
        $root.GetProperty('FrontTriggerKind').GetString() -ne 'OnVanguard') { throw 'Unexpected Vanguard game version or trigger predicate.' }
} finally { $document.Dispose() }
dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --enchantment-combat-only $path
if ($LASTEXITCODE) { throw 'Independent Vanguard component validation failed.' }
Write-Output 'GAME-231-VANGUARD PASS: binary hash and 41 exact native component transitions with 32 immutable branches; surrounding battle and half-health Deathwish remain separate acceptance.'
