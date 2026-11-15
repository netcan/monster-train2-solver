#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild, [switch] $SelectionSearchOnly, [switch] $RepeatedSearchOnly)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
if ($SelectionSearchOnly -and $RepeatedSearchOnly) { throw 'Choose one search scope.' }
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/summon-choice-battle'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if ($manifest.Count -ne 1 -or $manifest[0].path -cne 'battle.mt2f') { throw 'Unexpected summon choice battle manifest.' }
$path = Join-Path $directory $manifest[0].path
if ((Get-Item -LiteralPath $path).Length -ne [long]$manifest[0].bytes -or
    (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $manifest[0].sha256) { throw 'Summon choice battle archive hash/size differs.' }
$mode = if ($RepeatedSearchOnly) { '--summon-choice-repeated-search-only' }
    elseif ($SelectionSearchOnly) { '--summon-choice-selection-search-only' } else { '--original-summon-choice-only' }
dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') $mode $path
if ($LASTEXITCODE) { throw 'Independent original summon choice battle validation failed.' }
if ($RepeatedSearchOnly) { Write-Output 'GAME-231-SUMMON-CHOICE-REPEATED-SEARCH PASS'; return }
if ($SelectionSearchOnly) { Write-Output 'GAME-231-SUMMON-CHOICE-SELECTION-SEARCH PASS'; return }
Write-Output 'GAME-231-SUMMON-CHOICE-BATTLE PASS: original champion upgrades, twelve native dialogs/cardless births, subtype skips, over-capacity birth, fragment callbacks/skills, full chain/16 branches and serial/parallel search choosing a fragment.'
