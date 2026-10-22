#requires -Version 7.4
[CmdletBinding()]
param(
    [ValidateSet('Moon', 'OuterBoss', 'OuterBossRolling', 'Fragile', 'FragileBounded', 'CorrupterBounded', 'TrueFinalBossBounded', 'TrueFinalBossOrdinary', 'TrueFinalBossOrdinaryUpgraded', 'TrueFinalBossAvenger', 'TrueFinalBossAvengerUpgraded', 'TrialAllyBuffer', 'TrialFragile', 'TrialCorrupter', 'Conduit', 'RoomMoon', 'RoomMoonMovement', 'RoomMoonEnemy', 'RoomMoonEnemyConduit', 'MixedConduit', 'MoonRoomSpells', 'MoonRoomLunacy', 'MoonRoomCostRelic', 'MoonRoomAttackRelic', 'EventGraft', 'NaturalRing7', 'TerminalCopyConsume', 'TerminalPurgeCopyConsume', 'TerminalCopy', 'TerminalPurgeCopy')]
    [string[]] $Case = @('Moon', 'OuterBoss', 'OuterBossRolling', 'Fragile', 'FragileBounded', 'CorrupterBounded', 'TrueFinalBossBounded', 'TrueFinalBossOrdinary', 'TrueFinalBossOrdinaryUpgraded', 'TrueFinalBossAvenger', 'TrueFinalBossAvengerUpgraded', 'TrialAllyBuffer', 'TrialFragile', 'TrialCorrupter', 'Conduit', 'RoomMoon', 'RoomMoonMovement', 'RoomMoonEnemy', 'RoomMoonEnemyConduit', 'MixedConduit', 'MoonRoomSpells', 'MoonRoomLunacy', 'MoonRoomCostRelic', 'MoonRoomAttackRelic', 'EventGraft', 'NaturalRing7', 'TerminalCopyConsume', 'TerminalPurgeCopyConsume', 'TerminalCopy', 'TerminalPurgeCopy'),
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
        'Moon' { 'player-moon' }
        'OuterBoss' { 'player-boss' }
        'OuterBossRolling' { 'player-boss-rolling' }
        'Fragile' { 'player-boss-fragile' }
        'FragileBounded' { 'player-boss-fragile-bounded' }
        'CorrupterBounded' { 'player-boss-corrupter-bounded' }
        'TrueFinalBossBounded' { 'player-true-final-boss-bounded' }
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
        'EventGraft' { 'player-event-graft' }
        'NaturalRing7' { 'player-natural-ring7' }
        'TerminalCopyConsume' { 'player-terminal-copy-consume' }
        'TerminalPurgeCopyConsume' { 'player-terminal-purge-copy-consume' }
        'TerminalCopy' { 'player-terminal-copy' }
        'TerminalPurgeCopy' { 'player-terminal-purge-copy' }
    }
    $search = Join-Path $workspace "tests/fixtures/full-battle-$prefix-search.mt2f"
    $replay = Join-Path $workspace "tests/fixtures/full-battle-$prefix-replay.mt2f"
    foreach ($path in @($search, $replay)) {
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing player acceptance archive: $path" }
        $rows = @($manifest | Where-Object archive -CEQ ([IO.Path]::GetFileName($path)))
        if ($rows.Count -ne 1 -or (Get-Item -LiteralPath $path).Length -ne [long]$rows[0].archive_bytes -or
            (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $rows[0].archive_sha256) {
            throw "Player acceptance archive metadata differs: $path"
        }
    }
    $mode = if ($name -eq 'OuterBossRolling') { '--player-boss-winning-pair-only' }
        elseif ($name -in @('MoonRoomLunacy', 'MoonRoomCostRelic', 'MoonRoomAttackRelic')) { '--player-moon-room-spells-pair-only' }
        elseif ($name -in @('TerminalCopyConsume', 'TerminalPurgeCopyConsume', 'TerminalCopy', 'TerminalPurgeCopy')) { '--player-terminal-copy-pair-only' }
        else { "--$prefix-pair-only" }
    $modeArguments = if ($name.StartsWith('Trial')) { @('--player-boss-trial-bounded-pair-only', $name.Substring(5), $search, $replay) }
        else { @($mode, $search, $replay) }
    dotnet $checker @modeArguments
    if ($LASTEXITCODE -ne 0) { throw "Player $name acceptance failed." }
    if ($name -eq 'Fragile') {
        dotnet $checker --tactical-shift-only $search
        if ($LASTEXITCODE -ne 0) { throw 'Natural Fragile tactical and budget regression failed.' }
    }
}
Write-Output "PLAYER-ADVISOR-FOCUSED PASS cases=$($Case -join ','); no full regression executed."
