#requires -Version 7.4
[CmdletBinding()]
param(
    [ValidateSet('NativeCovenantCampaign03Ring3Default', 'NativeCovenantCampaign03Default', 'MoonRoomHealingRelic', 'NativeCovenantStartup', 'NativeCovenantStartup02', 'NativeCovenantStartup03', 'NativeCovenantStartup04', 'CovenantEnemyStats', 'CovenantEnemyStatsFtue', 'TrueFinalBossCovenants', 'TrueFinalBossCovenant00', 'TrueFinalBossCovenant01', 'TrueFinalBossCovenant04', 'TrueFinalBossCovenant08',
        'Moon', 'OuterBoss', 'OuterBossRolling', 'Fragile', 'FragileShortBudget', 'FragileBounded', 'Corrupter', 'CorrupterBounded', 'TrueFinalBossBounded', 'TrueFinalBossOrdinary', 'TrueFinalBossOrdinaryUpgraded', 'TrueFinalBossAvenger', 'TrueFinalBossAvengerUpgraded', 'TrialAllyBuffer', 'TrialFragile', 'TrialCorrupter', 'Conduit', 'RoomMoon', 'RoomMoonMovement', 'RoomMoonEnemy', 'RoomMoonEnemyConduit', 'MixedConduit', 'MoonRoomSpells', 'MoonRoomLunacy', 'MoonRoomCostRelic', 'MoonRoomAttackRelic', 'EventGraft', 'NaturalRing7', 'CampaignTrialGold', 'TerminalCopyConsume', 'TerminalPurgeCopyConsume', 'TerminalCopy', 'TerminalPurgeCopy')]
    [string[]] $Case = @('NativeCovenantCampaign03Ring3Default', 'NativeCovenantCampaign03Default', 'MoonRoomHealingRelic', 'NativeCovenantStartup', 'NativeCovenantStartup02', 'NativeCovenantStartup03', 'NativeCovenantStartup04', 'CovenantEnemyStats', 'CovenantEnemyStatsFtue', 'TrueFinalBossCovenants', 'TrueFinalBossCovenant00', 'TrueFinalBossCovenant01', 'TrueFinalBossCovenant04', 'TrueFinalBossCovenant08',
        'Moon', 'OuterBoss', 'OuterBossRolling', 'Fragile', 'FragileShortBudget', 'FragileBounded', 'Corrupter', 'CorrupterBounded', 'TrueFinalBossBounded', 'TrueFinalBossOrdinary', 'TrueFinalBossOrdinaryUpgraded', 'TrueFinalBossAvenger', 'TrueFinalBossAvengerUpgraded', 'TrialAllyBuffer', 'TrialFragile', 'TrialCorrupter', 'Conduit', 'RoomMoon', 'RoomMoonMovement', 'RoomMoonEnemy', 'RoomMoonEnemyConduit', 'MixedConduit', 'MoonRoomSpells', 'MoonRoomLunacy', 'MoonRoomCostRelic', 'MoonRoomAttackRelic', 'EventGraft', 'NaturalRing7', 'CampaignTrialGold', 'TerminalCopyConsume', 'TerminalPurgeCopyConsume', 'TerminalCopy', 'TerminalPurgeCopy'),
    [switch] $SkipBuild
)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE -ne 0) { throw 'Player acceptance checker build failed.' }
}
$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
$manifest = Import-Csv -LiteralPath (Join-Path $workspace 'tests/fixtures/manifest.tsv') -Delimiter "`t"
foreach ($name in $Case) {
    $prefix = switch ($name) {
        'NativeCovenantCampaign03Ring3Default' { 'player-native-covenant-campaign-03-ring3-default' }
        'NativeCovenantCampaign03Default' { 'player-native-covenant-campaign-03-ring2-default' }
        'Moon' { 'player-moon' }
        'OuterBoss' { 'player-boss' }
        'OuterBossRolling' { 'player-boss-rolling' }
        'Fragile' { 'player-boss-fragile' }
        'FragileShortBudget' { 'player-boss-fragile-short-budget' }
        'FragileBounded' { 'player-boss-fragile-bounded' }
        'Corrupter' { 'player-boss-corrupter' }
        'CorrupterBounded' { 'player-boss-corrupter-bounded' }
        'TrueFinalBossBounded' { 'player-true-final-boss-bounded' }
        'CovenantEnemyStats' { 'player-covenant-enemy-stats' }
        'NativeCovenantStartup' { 'player-native-covenant-startup' }
        'NativeCovenantStartup02' { 'player-native-covenant-startup-02' }
        'NativeCovenantStartup03' { 'player-native-covenant-startup-03' }
        'NativeCovenantStartup04' { 'player-native-covenant-startup-04' }
        'CovenantEnemyStatsFtue' { 'player-covenant-enemy-stats-ftue' }
        'TrueFinalBossCovenants' { 'player-true-final-boss-covenants' }
        'TrueFinalBossCovenant04' { 'player-true-final-boss-covenant04' }
        'TrueFinalBossCovenant00' { 'player-true-final-boss-covenant00' }
        'TrueFinalBossCovenant01' { 'player-true-final-boss-covenant01' }
        'TrueFinalBossCovenant08' { 'player-true-final-boss-covenant08' }
        'TrueFinalBossOrdinary' { 'player-true-final-boss-ordinary' }
        'TrueFinalBossOrdinaryUpgraded' { 'player-true-final-boss-ordinary-upgraded' }
        'TrueFinalBossAvenger' { 'player-true-final-boss-avenger' }
        'TrueFinalBossAvengerUpgraded' { 'player-true-final-boss-avenger-upgraded' }
        'TrialAllyBuffer' { 'player-boss-trial-allybuffer-bounded' }
        'TrialFragile' { 'player-boss-trial-fragile-bounded' }
        'TrialCorrupter' { 'player-boss-trial-corrupter-bounded' }
        'Conduit' { 'player-conduit' }
        'RoomMoon' { 'player-room-moon' }
        'RoomMoonMovement' { 'player-room-moon-movement' }
        'RoomMoonEnemy' { 'player-room-moon-enemy' }
        'RoomMoonEnemyConduit' { 'player-room-moon-enemy-conduit' }
        'MixedConduit' { 'player-mixed-conduit' }
        'MoonRoomSpells' { 'player-moon-room-spells' }
        'MoonRoomLunacy' { 'player-moon-room-lunacy' }
        'MoonRoomCostRelic' { 'player-moon-room-cost-relic' }
        'MoonRoomAttackRelic' { 'player-moon-room-attack-relic' }
        'MoonRoomHealingRelic' { 'player-moon-room-healing-relic' }
        'EventGraft' { 'player-event-graft' }
        'NaturalRing7' { 'player-natural-ring7' }
        'CampaignTrialGold' { 'player-campaign-trial-gold' }
        'TerminalCopyConsume' { 'player-terminal-copy-consume' }
        'TerminalPurgeCopyConsume' { 'player-terminal-purge-copy-consume' }
        'TerminalCopy' { 'player-terminal-copy' }
        'TerminalPurgeCopy' { 'player-terminal-purge-copy' }
    }
    $search = Join-Path $workspace "tests/fixtures/full-battle-$prefix-search.mt2f"
    $replay = Join-Path $workspace "tests/fixtures/full-battle-$prefix-replay.mt2f"
    $archives = @($search, $replay)
    $budgetFailure = Join-Path $workspace 'tests/fixtures/advisor-ordinary-budget-failure.mt2f'
    if ($name -eq 'FragileShortBudget') { $archives += $budgetFailure }
    foreach ($path in $archives) {
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing player acceptance archive: $path" }
        $rows = @($manifest | Where-Object archive -CEQ ([IO.Path]::GetFileName($path)))
        if ($rows.Count -ne 1 -or (Get-Item -LiteralPath $path).Length -ne [long]$rows[0].archive_bytes -or
            (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $rows[0].archive_sha256) {
            throw "Player acceptance archive metadata differs: $path"
        }
    }
    $mode = if ($name -eq 'OuterBossRolling') { '--player-boss-winning-pair-only' }
        elseif ($name -eq 'MoonRoomHealingRelic') { '--player-moon-room-healing-relic-pair-only' }
        elseif ($name -in @('MoonRoomLunacy', 'MoonRoomCostRelic', 'MoonRoomAttackRelic', 'MoonRoomHealingRelic')) { '--player-moon-room-spells-pair-only' }
        elseif ($name -in @('TerminalCopyConsume', 'TerminalPurgeCopyConsume', 'TerminalCopy', 'TerminalPurgeCopy')) { '--player-terminal-copy-pair-only' }
        else { "--$prefix-pair-only" }
    $modeArguments = if ($name.StartsWith('Trial')) { @('--player-boss-trial-bounded-pair-only', $name.Substring(5), $search, $replay) }
        elseif ($name -eq 'NativeCovenantCampaign03Ring3Default') { @('--player-native-covenant-campaign-default-pair-only', $search, $replay, '3', '2') }
        elseif ($name -eq 'NativeCovenantCampaign03Default') { @('--player-native-covenant-campaign-default-pair-only', $search, $replay, '3', '1') }
        else { @($mode, $search, $replay) }
    dotnet $checker @modeArguments
    if ($LASTEXITCODE -ne 0) { throw "Player $name acceptance failed." }
    if ($name -eq 'Fragile') {
        dotnet $checker --tactical-shift-only $search
        if ($LASTEXITCODE -ne 0) { throw 'Natural Fragile tactical and budget regression failed.' }
    }
    if ($name -eq 'FragileShortBudget') {
        dotnet $checker --ordinary-advice-budget-recovery-only $budgetFailure $search
        if ($LASTEXITCODE -ne 0) { throw 'Ordinary advice native timeout recovery failed.' }
    }
}
Write-Output "PLAYER-ADVISOR-FOCUSED PASS cases=$($Case -join ','); no full regression executed."
