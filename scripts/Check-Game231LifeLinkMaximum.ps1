#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild)

$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/lifelink-maximum'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if ($manifest.Count -ne 2 -or ($manifest.path -join ',') -cne 'lifecycle.mt2f,full-battle.mt2f') { throw 'Unexpected LifeLink maximum-health manifest.' }
foreach ($entry in $manifest) {
    $path = Join-Path $directory $entry.path
    if ((Get-Item -LiteralPath $path).Length -ne [long]$entry.bytes -or
        (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $entry.sha256) { throw 'LifeLink maximum-health capture hash/size differs.' }
}
$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
dotnet $checker --true-final-boss-maximum-health-only (Join-Path $directory 'lifecycle.mt2f')
if ($LASTEXITCODE) { throw 'Independent LifeLink maximum-health lifecycle validation failed.' }
dotnet $checker --true-final-boss-maximum-battle-only (Join-Path $directory 'full-battle.mt2f')
if ($LASTEXITCODE) { throw 'Independent LifeLink maximum-health battle validation failed.' }
Write-Output 'GAME-231-LIFE-LINK-MAXIMUM PASS: 79 actual native API calls and complete train successors, 16 concurrent chains; original five-body victory with ten authored plays/four EndTurns, shared HP/max, independent protection, native Pyre terminal Harvest, complete state/RNG replay and bounded serial/parallel search. Terminal maximum-health sacrifice and temporary previews remain separate.'
