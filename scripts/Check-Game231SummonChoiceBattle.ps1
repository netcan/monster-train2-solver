#requires -Version 7.4
[CmdletBinding(DefaultParameterSetName = 'Replay')]
param(
    [Parameter(ParameterSetName = 'Replay')] [switch] $SkipBuild,
    [Parameter(ParameterSetName = 'Replay')] [switch] $SelectionSearchOnly,
    [Parameter(ParameterSetName = 'Replay')] [switch] $RepeatedSearchOnly,
    [Parameter(Mandatory, ParameterSetName = 'Checkpoint')] [string] $AdvisorCheckpoint
)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
if ($PSCmdlet.ParameterSetName -eq 'Checkpoint') {
    $profile = (Resolve-Path -LiteralPath $AdvisorCheckpoint).Path.TrimEnd('\')
    if ((Split-Path -Parent $profile) -ine (Join-Path $workspace '.probe-runs')) {
        throw 'AdvisorCheckpoint must be a direct child of the workspace .probe-runs directory.'
    }
    $savePath = Join-Path $profile 'saves/save-singlePlayer.json'
    $save = Get-Content -LiteralPath $savePath -Raw | ConvertFrom-Json -AsHashtable
    if ($save.runType -cne 'Class' -or $save.gameSequence -ne 6 -or
        $null -eq $save.runState.distance -or $save.runState.distance -notin (0..20) -or $save.wasDevCheatUsed -ne $false) {
        throw 'Expected an original Class run at BattleIntro.'
    }
    # ShinyShoe.DLC.Punished in the pinned 2.3.1 save format.
    if (2 -notin $save.startingConditions.enabledDlcs) {
        throw 'The saved run does not enable Punished DLC.'
    }
    $champion = @($save.deckState | Where-Object cardDataID -CEQ 'c58f8d77-8578-4d97-bcf1-beb4cdbe2f27')
    if ($champion.Count -ne 1) { throw 'Expected one original PunishedChamp1 in the saved deck.' }
    $upgradeIds = @('823ab526-9cfe-4383-9e17-a033d49a7fa9', '6b9e1108-af4e-468f-a912-48853b0f0413',
        '3de67996-0562-41ce-b892-1cedabf0bffc')
    $upgrades = @($champion[0].cardModifiers.cardUpgrades | Where-Object { $_.cardUpgradeDataId -cin $upgradeIds })
    if ($upgrades.Count -ne 1) { throw 'Expected one original GrimFlight rank attached to the saved champion.' }
    [pscustomobject]@{
        InputProfile = $profile
        BattleDistance = [int]$save.runState.distance
        GrimFlightRank = [Array]::IndexOf($upgradeIds, $upgrades[0].cardUpgradeDataId) + 1
        SaveSHA256 = (Get-FileHash -LiteralPath $savePath -Algorithm SHA256).Hash
        Entitlement = 'Requires native installed and enabled checks'
    } | ConvertTo-Json -Compress
    return
}
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
