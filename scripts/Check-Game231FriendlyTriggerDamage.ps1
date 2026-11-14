#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild)

$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/friendly-trigger-damage'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if ($manifest.Count -ne 2 -or ($manifest.path -join ',') -cne 'eaten.mt2f,burnout.mt2f') {
    throw 'Unexpected friendly-trigger damage manifest.'
}
foreach ($entry in $manifest) {
    $path = Join-Path $directory $entry.path
    if ((Get-Item -LiteralPath $path).Length -ne [long]$entry.bytes -or
        (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $entry.sha256) {
        throw "Friendly-trigger damage capture hash/size differs: $($entry.path)"
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --game231-friendly-trigger-damage-only $path
    if ($LASTEXITCODE) { throw "Independent friendly-trigger damage validation failed: $($entry.path)" }
}
Write-Output 'GAME-231-FRIENDLY-TRIGGER-DAMAGE-SUITE PASS: both unchanged owners coexist, piercing/ordinary armor controls, complete post-setup battle/RNG, 16 immutable branches and explicit source-owner selection.'
