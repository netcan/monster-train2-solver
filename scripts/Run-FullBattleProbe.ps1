#requires -Version 7.4
param(
    [ValidateSet('no-cards', 'steward-once', 'units-and-junk', 'units-spells-and-junk')]
    [string] $Policy = 'steward-once',
    [ValidateSet('Normal', 'Fast', 'Ultra', 'SuperUltra', 'Instant')]
    [string] $GameSpeed = 'Instant',
    [switch] $NumericUpgrades,
    [switch] $DynamicUpgrades,
    [switch] $SacrificeUpgrades,
    [switch] $HandUpgrades,
    [switch] $TargetedHandUpgrades,
    [switch] $Healing,
    [switch] $HealingTriggers,
    [switch] $RoomSpells,
    [switch] $TerminalSpells,
    [switch] $PostKillSpells,
    [switch] $RandomSpells,
    [switch] $RandomStatus,
    [switch] $CrossRoomSpells,
    [switch] $CrossRoomTargets,
    [switch] $AttackBuffs,
    [switch] $MaxHealthSpells,
    [switch] $MaxHealthLethal,
    [switch] $NumericRanges,
    [switch] $NumericRangesLethal,
    [switch] $TargetFilters,
    [switch] $Drawing,
    [switch] $HandRemoval,
    [switch] $HandRemovalLethal,
    [switch] $Generation,
    [switch] $GenerationLethal,
    [switch] $StatisticQueries,
    [switch] $StatisticOverflow,
    [switch] $DamageScaling,
    [switch] $DynamicStatistics,
    [switch] $StatusScaling,
    [switch] $UnitUpgradeScaling,
    [switch] $UnitTriggerUpgrades,
    [switch] $SpawnTriggers,
    [switch] $SpawnTriggersLethal,
    [switch] $UnitTurnBegin,
    [switch] $TeamTurnBegin,
    [switch] $PreHandDiscard,
    [switch] $PreHandDiscardLethal,
    [switch] $CloneUpgradeRefresh,
    [switch] $PreCombat,
    [switch] $PreCombatLethal,
    [switch] $TriggeredHealing,
    [switch] $PostCombatHealing,
    [switch] $TriggeredDamage,
    [switch] $DamageDeathQueue,
    [switch] $TerminalDeathDamage,
    [switch] $HitKill,
    [switch] $DyingUpgrades,
    [switch] $AttackTriggers,
    [switch] $TriggeredStatus,
    [switch] $StatusCallbacks,
    [switch] $StatusCallbackActions,
    [switch] $EnergyEffects,
    [switch] $EnergyEffectsLethal,
    [switch] $XCost,
    [switch] $XCostLethal,
    [switch] $RoomCapacity,
    [switch] $RoomCapacityLethal,
    [switch] $DirectUnitUpgrades,
    [switch] $Equipment,
    [switch] $EquipmentExhausted,
    [switch] $EquipmentOverflow,
    [switch] $EquipmentTriggers,
    [switch] $TriggerMutation,
    [switch] $DetachedBonusDraw,
    [switch] $ConditionalTriggers,
    [switch] $BonusDraw,
    [switch] $BonusDrawLethal,
    [switch] $BinaryCapture = $true,
    [switch] $CaptureJson,
    [switch] $SkipBuild
)

$ErrorActionPreference = 'Stop'
if ($EquipmentExhausted -or $EquipmentOverflow -or $EquipmentTriggers) { $Equipment = $true }
if ($StatusCallbackActions) { $StatusCallbacks = $true }
if ($StatusCallbacks) { $TriggeredStatus = $true }
if ($CaptureJson) { $BinaryCapture = $true }
$workspace = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$gameRoot = Join-Path $workspace '.sandbox-game'
$runId = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
$profile = Join-Path $workspace ('.probe-runs\full-battle-' + $Policy + '-' + $runId)
$originalLogRoot = Join-Path ([Environment]::GetFolderPath('UserProfile')) 'AppData\LocalLow\Shiny Shoe\MonsterTrain2'

function Get-OriginalSignature {
    $parts = Get-ChildItem -LiteralPath $originalLogRoot -File -Force -Recurse |
        Sort-Object FullName | ForEach-Object {
            $_.FullName + ':' + $_.LastWriteTimeUtc.Ticks + ':' +
                (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
        }
    return [string]::Join('|', [string[]] @($parts))
}

if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src\Probe\Probe.csproj') -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Probe build failed.' }
    if ($BinaryCapture) {
        dotnet build (Join-Path $workspace 'src\FixtureArchive\FixtureArchive.csproj') -c Release -f net8.0
        if ($LASTEXITCODE -ne 0) { throw 'Fixture inspection build failed.' }
    }
}
$pluginDir = Join-Path $gameRoot 'BepInEx\plugins'
Copy-Item -LiteralPath (Join-Path $workspace 'src\Probe\bin\Release\netstandard2.1\Probe.dll') -Destination $pluginDir -Force
Copy-Item -LiteralPath (Join-Path $workspace 'src\Model\bin\Release\netstandard2.1\Model.dll') -Destination $pluginDir -Force
Copy-Item -LiteralPath (Join-Path $workspace 'src\FixtureArchive\bin\Release\netstandard2.1\FixtureArchive.dll') -Destination $pluginDir -Force
Copy-Item -LiteralPath (Join-Path $workspace '.probe-baseline') -Destination $profile -Recurse
$environment = @{
    MT2_PROBE_DATA_DIR = $profile
    MT2_PROBE_SCENARIO = 'native-replay'
    MT2_PROBE_FULL_BATTLE = '1'
    MT2_PROBE_FULL_BATTLE_POLICY = $Policy
    MT2_PROBE_MODIFIERS = $(if ($ConditionalTriggers) { 'conditional-triggers' } elseif ($DetachedBonusDraw) { 'detached-bonus-draw' } elseif ($TriggerMutation) { 'trigger-mutation' } elseif ($EquipmentTriggers) { 'equipment-triggers' } elseif ($EquipmentOverflow) { 'equipment-overflow' } elseif ($EquipmentExhausted) { 'equipment-exhausted' } elseif ($Equipment) { 'equipment' } elseif ($DirectUnitUpgrades) { 'direct-unit-upgrades' } elseif ($RoomCapacityLethal) { 'room-capacity-lethal' } elseif ($RoomCapacity) { 'room-capacity' } elseif ($BonusDrawLethal) { 'bonus-draw-lethal' } elseif ($BonusDraw) { 'bonus-draw' } elseif ($XCostLethal) { 'x-cost-lethal' } elseif ($XCost) { 'x-cost' } elseif ($EnergyEffectsLethal) { 'energy-effects-lethal' } elseif ($EnergyEffects) { 'energy-effects' } elseif ($TriggeredStatus) { 'triggered-status' } elseif ($AttackTriggers) { 'attack-triggers' } elseif ($DyingUpgrades) { 'dying-upgrades' } elseif ($HitKill) { 'hit-kill' } elseif ($TerminalDeathDamage) { 'terminal-death-damage' } elseif ($DamageDeathQueue) { 'damage-death-queue' } elseif ($TriggeredDamage) { 'triggered-damage' } elseif ($PostCombatHealing) { 'post-combat-healing' } elseif ($TriggeredHealing) { 'triggered-healing' } elseif ($PreCombatLethal) { 'pre-combat-lethal' } elseif ($PreCombat) { 'pre-combat' } elseif ($CloneUpgradeRefresh) { 'clone-upgrade-refresh' } elseif ($PreHandDiscardLethal) { 'pre-hand-discard-lethal' } elseif ($PreHandDiscard) { 'pre-hand-discard' } elseif ($TeamTurnBegin) { 'team-turn-begin' } elseif ($UnitTurnBegin) { 'unit-turn-begin' } elseif ($SpawnTriggersLethal) { 'spawn-triggers-lethal' } elseif ($SpawnTriggers) { 'spawn-triggers' } elseif ($UnitTriggerUpgrades) { 'unit-trigger-upgrades' } elseif ($UnitUpgradeScaling) { 'unit-upgrade-scaling' } elseif ($StatusScaling) { 'status-scaling' } elseif ($DynamicStatistics) { 'dynamic-statistics' } elseif ($DamageScaling) { 'damage-scaling' } elseif ($GenerationLethal) { 'generation-lethal' } elseif ($Generation) { 'generation' } elseif ($HandRemovalLethal) { 'hand-removal-lethal' } elseif ($HandRemoval) { 'hand-removal' } elseif ($Drawing) { 'drawing' } elseif ($TargetFilters) { 'target-filters' } elseif ($NumericRangesLethal) { 'numeric-ranges-lethal' } elseif ($NumericRanges) { 'numeric-ranges' } elseif ($MaxHealthLethal) { 'max-health-lethal' } elseif ($MaxHealthSpells) { 'max-health-spells' } elseif ($AttackBuffs) { 'attack-buffs' } elseif ($CrossRoomTargets) { 'cross-room-targets' } elseif ($CrossRoomSpells) { 'cross-room-spells' } elseif ($RandomStatus) { 'random-status' } elseif ($RandomSpells) { 'random-spells' } elseif ($PostKillSpells) { 'post-kill-spells' } elseif ($TerminalSpells) { 'terminal-spells' } elseif ($RoomSpells) { 'room-spells' } elseif ($HealingTriggers) { 'healing-triggers' } elseif ($Healing) { 'healing' } elseif ($TargetedHandUpgrades) { 'targeted-hand-upgrades' } elseif ($HandUpgrades) { 'hand-upgrades' } elseif ($SacrificeUpgrades) { 'sacrifice-upgrades' } elseif ($DynamicUpgrades) { 'dynamic-upgrades' } elseif ($NumericUpgrades) { 'numeric-upgrades' } else { '' })
    MT2_PROBE_DIRECT_BRANCH = '1'
    MT2_PROBE_DEPTH = '100'
    MT2_PROBE_TARGET_TURN = '0'
    MT2_PROBE_SOURCE_PLAY_TURNS = ''
    MT2_PROBE_BRANCH_ANY_UNIT = '0'
    MT2_PROBE_FAST_REPLAY = '0'
    MT2_PROBE_GAME_SPEED = $GameSpeed
    MT2_PROBE_STATUS_CALLBACKS = $(if ($StatusCallbacks) { '1' } else { '0' })
    MT2_PROBE_STATUS_CALLBACK_ACTIONS = $(if ($StatusCallbackActions) { '1' } else { '0' })
    MT2_PROBE_BINARY_CAPTURE = $(if ($BinaryCapture) { '1' } else { '0' })
    MT2_PROBE_CAPTURE_JSON = $(if ($CaptureJson) { '1' } else { '0' })
    MT2_PROBE_NO_TIMEOUT = '1'
    MT2_PROBE_ISOLATE_PREVIEW_RNG = $(if ($TriggeredStatus) { '1' } else { '0' })
    MT2_PROBE_ISOLATE_UI_RNG = $(if ($ConditionalTriggers -or $DetachedBonusDraw -or $RoomCapacity -or $RoomCapacityLethal -or $BonusDraw -or $BonusDrawLethal -or $NumericRanges -or $NumericRangesLethal -or $Drawing -or $TriggeredHealing -or $PostCombatHealing -or $TriggeredDamage -or $DamageDeathQueue -or $TerminalDeathDamage -or $HitKill -or $DyingUpgrades -or $AttackTriggers -or $TriggeredStatus) { '1' } else { '0' })
    MT2_PROBE_STATISTIC_QUERIES = $(if ($StatisticQueries) { '1' } else { '0' })
    MT2_PROBE_STATISTIC_OVERFLOW = $(if ($StatisticOverflow) { '1' } else { '0' })
}
$originalBefore = Get-OriginalSignature
$unityLog = Join-Path $profile 'unity-scenario.log'
$nativeTimer = [System.Diagnostics.Stopwatch]::StartNew()
$process = Start-Process -FilePath (Join-Path $gameRoot 'MonsterTrain2.exe') `
    -WorkingDirectory $gameRoot -ArgumentList @('-logFile', $unityLog) `
    -Environment $environment -WindowStyle Hidden -PassThru
Write-Output "FULL-BATTLE-RUN pid=$($process.Id) policy=$Policy profile=$profile"
if (-not $process.WaitForExit(600000)) {
    $process.Kill()
    $process.WaitForExit()
    throw "Full battle timed out; profile: $profile"
}
$nativeTimer.Stop()
Write-Output "FULL-BATTLE-TIMING speed=$GameSpeed elapsedSeconds=$($nativeTimer.Elapsed.TotalSeconds.ToString('F2', [Globalization.CultureInfo]::InvariantCulture))"
$tracePath = Join-Path $profile $(if ($BinaryCapture) { 'full-battle.mt2f' } else { 'full-battle.json' })
if (-not (Test-Path -LiteralPath $tracePath)) { throw "Missing battle trace (native exit code $($process.ExitCode)); inspect $unityLog" }
$inspectionTimer = [System.Diagnostics.Stopwatch]::StartNew()
if ($BinaryCapture) {
    Add-Type -Path (Join-Path $workspace 'src\FixtureArchive\bin\Release\net8.0\FixtureArchive.dll')
    $archive = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read($tracePath)
    $trace = $archive.RootElement.ToObjectGraph()
    $archive.Dispose()
} else {
    $trace = Get-Content -LiteralPath $tracePath -Raw | ConvertFrom-Json
}
$inspectionTimer.Stop()
Write-Output "FULL-BATTLE-INSPECTION elapsedSeconds=$($inspectionTimer.Elapsed.TotalSeconds.ToString('F3', [Globalization.CultureInfo]::InvariantCulture))"
$nativePassed = [bool] (Select-String -LiteralPath $unityLog -Pattern 'DEPTH-PASS' -Quiet)
$terminalSettled = $trace.TerminalCaptureBoundary -eq 'AfterStopCombatLoop' -and $trace.TerminalEffectsSettled -eq $true
$originalUnchanged = (Get-OriginalSignature) -ceq $originalBefore
$modifierActions = @($trace.Actions | Where-Object {
    $actionEntry = $_
    $playedCard = $actionEntry.Before.Spawn.Train.Context.Cards.Hand |
        Where-Object InstanceId -EQ $actionEntry.Action.CardInstanceId
    $definition = $actionEntry.Before.PlayRules.Cards | Where-Object DataId -EQ $playedCard.DataId
    @($definition.Effects | Where-Object { $_.Type -eq 'UnitUpgrade' -or $_.Type -eq 'HandUpgrade' }).Count -gt 0
})
$modifierCoverage = (-not ($DynamicUpgrades -or $SacrificeUpgrades -or $HandUpgrades -or $TargetedHandUpgrades)) -or $modifierActions.Count -gt 0
$healActions = @($trace.Actions | Where-Object {
    $entry = $_
    $card = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $definition = $entry.Before.PlayRules.Cards | Where-Object DataId -EQ $card.DataId
    @($definition.Effects | Where-Object Type -EQ 'Heal').Count -gt 0
})
$healingCoverage = -not ($Healing -or $HealingTriggers)
if ($Healing -or $HealingTriggers) {
    $healingCoverage = $healActions.Count -gt 0 -and @($healActions | Where-Object {
        $entry = $_
        $beforeUnit = $entry.Before.Spawn.Train.Rooms.Units | Where-Object Id -EQ $entry.Action.TargetUnitId
        $afterUnit = $entry.Actual.Spawn.Train.Rooms.Units | Where-Object Id -EQ $entry.Action.TargetUnitId
        $null -ne $afterUnit -and $afterUnit.Health -gt $beforeUnit.Health
    }).Count -gt 0
}
$onHealCoverage = -not $HealingTriggers
if ($HealingTriggers) {
    $onHealCoverage = @($healActions | Where-Object {
        $entry = $_
        $target = $entry.Before.Spawn.Train.Rooms.Units | Where-Object Id -EQ $entry.Action.TargetUnitId
        @($target.Statuses | Where-Object Id -EQ 'heal immunity').Count -gt 0 -and
            $entry.Actual.Spawn.Train.Context.Gold - $entry.Before.Spawn.Train.Context.Gold -eq 15
    }).Count -gt 0
}
$roomSpellCoverage = -not $RoomSpells
if ($RoomSpells) {
    $multiHeal = @($healActions | Where-Object {
        $entry = $_
        $beforeRoom = $entry.Before.Spawn.Train.Rooms | Where-Object RoomIndex -EQ $entry.Action.RoomIndex
        $afterRoom = $entry.Actual.Spawn.Train.Rooms | Where-Object RoomIndex -EQ $entry.Action.RoomIndex
        @($beforeRoom.Units | Where-Object {
            $beforeUnit = $_
            $afterUnit = $afterRoom.Units | Where-Object Id -EQ $beforeUnit.Id
            $beforeUnit.Team -eq 1 -and $null -ne $afterUnit -and $afterUnit.MaxHealth -eq $beforeUnit.MaxHealth + 4 -and
                $afterUnit.Health -eq $afterUnit.MaxHealth
        }).Count -ge 2
    }).Count -gt 0
    $multiDamage = @($healActions | Where-Object {
        $entry = $_
        $beforeRoom = $entry.Before.Spawn.Train.Rooms | Where-Object RoomIndex -EQ $entry.Action.RoomIndex
        $afterRoom = $entry.Actual.Spawn.Train.Rooms | Where-Object RoomIndex -EQ $entry.Action.RoomIndex
        @($beforeRoom.Units | Where-Object {
            $beforeUnit = $_
            $afterUnit = $afterRoom.Units | Where-Object Id -EQ $beforeUnit.Id
            $beforeUnit.Team -eq 0 -and ($null -eq $afterUnit -or $afterUnit.Health -lt $beforeUnit.Health)
        }).Count -ge 2
    }).Count -gt 0
    $roomSpellCoverage = $multiHeal -and $multiDamage
}
$terminalSpellCoverage = -not ($TerminalSpells -or $PostKillSpells) -or @($trace.Actions | Where-Object ActualOutcome -EQ 3).Count -gt 0
$postKillCoverage = -not $PostKillSpells
$randomCoverage = -not $RandomSpells -or @($trace.Actions | Where-Object {
    $entry = $_
    $card = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $rule = $entry.Before.PlayRules.Cards | Where-Object DataId -EQ $card.DataId
    @($rule.Effects | Where-Object Target -EQ 'RandomInRoom').Count -gt 0 -and
        (ConvertTo-Json -InputObject $entry.Before.Spawn.Train.Context.BattleRng -Compress) -cne
        (ConvertTo-Json -InputObject $entry.Actual.Spawn.Train.Context.BattleRng -Compress)
}).Count -gt 0
$randomStatusCoverage = -not $RandomStatus -or @($trace.Actions | Where-Object {
    $entry = $_
    $card = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
    $rule = $entry.Before.PlayRules.Cards | Where-Object DataId -EQ $card.DataId
    @($rule.Effects | Where-Object { $_.Type -eq 'AddStatus' -and $_.Statuses.Count -gt 1 }).Count -gt 0 -and
        $entry.ActualOutcome -eq 3 -and @($entry.Before.Spawn.Train.Rooms.Units | Where-Object {
            @($_.Statuses | Where-Object Id -EQ 'immune').Count -gt 0 -and -not $_.IsPyre
        }).Count -gt 0
}).Count -gt 0
$crossRoomCoverage = -not ($CrossRoomSpells -or $CrossRoomTargets) -or @($trace.Actions | Where-Object {
    $entry = $_
    (-not $CrossRoomSpells -or $entry.ActualOutcome -eq 3) -and @($entry.Before.Spawn.Train.Rooms | Where-Object {
        $oldRoom = $_
        $newRoom = $entry.Actual.Spawn.Train.Rooms | Where-Object RoomIndex -EQ $oldRoom.RoomIndex
        $oldRoom.RoomIndex -ne $entry.Action.RoomIndex -and @($oldRoom.Units | Where-Object {
            $old = $_
            $next = $newRoom.Units | Where-Object Id -EQ $old.Id
            $old.Team -eq 1 -and -not $old.IsPyre -and $null -ne $next -and
                $next.MaxHealth -eq $old.MaxHealth + 6 -and $next.Health -eq $next.MaxHealth
        }).Count -gt 0
    }).Count -gt 0
}).Count -gt 0
$attackCoverage = -not $AttackBuffs
if ($AttackBuffs) {
    $attackActions = @($trace.Actions | Where-Object {
        $entry = $_
        $card = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
        $rule = $entry.Before.PlayRules.Cards | Where-Object DataId -EQ $card.DataId
        $rule.Effects[0].Type -eq 'BuffAttack' -and $rule.Effects[0].Target -eq 'Tower'
    })
    $remoteAttack = $false
    $incapableAttack = $false
    $zeroAttack = $false
    foreach ($entry in $attackActions) {
        foreach ($room in $entry.Before.Spawn.Train.Rooms) {
            foreach ($old in $room.Units) {
                $next = $entry.Actual.Spawn.Train.Rooms.Units | Where-Object Id -EQ $old.Id
                if ($old.IsPyre -or $null -eq $next) { continue }
                if ($old.Team -eq 0 -and -not $old.CanAttack -and $next.Modifiers.DamageBuff -eq $old.Modifiers.DamageBuff) { $incapableAttack = $true }
                if ($old.CanAttack -and $old.BaseAttack -eq 0 -and $next.BaseAttack -gt 0) { $zeroAttack = $true }
                if ($room.RoomIndex -ne $entry.Action.RoomIndex -and $old.Team -eq 1 -and $old.CanAttack -and
                    ($next.Modifiers.DamageBuff - $old.Modifiers.DamageBuff) -in @(3, 4)) { $remoteAttack = $true }
            }
        }
    }
    $attackCoverage = $attackActions.Count -gt 0 -and $remoteAttack -and $incapableAttack -and $zeroAttack
}
$maxHealthCoverage = -not ($MaxHealthSpells -or $MaxHealthLethal)
if ($MaxHealthSpells -or $MaxHealthLethal) {
    $maxHealthActions = @($trace.Actions | Where-Object {
        $entry = $_
        $card = $entry.Before.Spawn.Train.Context.Cards.Hand | Where-Object InstanceId -EQ $entry.Action.CardInstanceId
        $rule = $entry.Before.PlayRules.Cards | Where-Object DataId -EQ $card.DataId
        @($rule.Effects | Where-Object Type -EQ 'BuffHealth').Count -gt 0
    })
    $maxHealthRemote = $false
    $maxHealthDeath = $false
    $maxHealthSpawner = $false
    $maxHealthNoTrigger = $false
    foreach ($entry in $maxHealthActions) {
        foreach ($room in $entry.Before.Spawn.Train.Rooms) {
            foreach ($old in $room.Units) {
                $next = $entry.Actual.Spawn.Train.Rooms.Units | Where-Object Id -EQ $old.Id
                if ($old.IsPyre) { continue }
                if ($old.Team -eq 0 -and $null -eq $next) { $maxHealthDeath = $true }
                if ($old.Team -ne 1 -or $null -eq $next) { continue }
                if ($room.RoomIndex -ne $entry.Action.RoomIndex -and $next.MaxHealth -gt $old.MaxHealth) { $maxHealthRemote = $true }
                $oldCard = $entry.Before.Spawn.Train.Context.CardRegistry | Where-Object InstanceId -EQ $old.SpawnerCardId
                $nextCard = $entry.Actual.Spawn.Train.Context.CardRegistry | Where-Object InstanceId -EQ $old.SpawnerCardId
                if ($nextCard.Temporary.Offsets.Health - $oldCard.Temporary.Offsets.Health -eq $(if ($MaxHealthLethal) { 4 } else { 2 })) {
                    $maxHealthSpawner = $true
                }
                $maxHealthHealingTriggers = @($next.Triggers | Where-Object Kind -EQ 'OnHeal')
                if ($maxHealthHealingTriggers.Count -gt 0 -and @($maxHealthHealingTriggers | Where-Object HasTriggered -EQ $true).Count -eq 0) { $maxHealthNoTrigger = $true }
            }
        }
    }
    $maxHealthCoverage = $maxHealthActions.Count -gt 0 -and $maxHealthRemote -and $maxHealthDeath -and $maxHealthSpawner -and $maxHealthNoTrigger -and
        (-not $MaxHealthLethal -or @($maxHealthActions | Where-Object ActualOutcome -EQ 3).Count -gt 0)
}
if ($PostKillSpells) {
    $postKillCoverage = @($trace.Actions | Where-Object {
        $entry = $_
        if ($entry.ActualOutcome -ne 3) { return $false }
        $beforeRoom = $entry.Before.Spawn.Train.Rooms | Where-Object RoomIndex -EQ $entry.Action.RoomIndex
        $afterRoom = $entry.Actual.Spawn.Train.Rooms | Where-Object RoomIndex -EQ $entry.Action.RoomIndex
        $entry.Actual.Spawn.Train.Context.AllScenarioBossesDead -eq $true -and @($beforeRoom.Units | Where-Object {
            $old = $_
            $next = $afterRoom.Units | Where-Object Id -EQ $old.Id
            $card = $entry.Actual.Spawn.Train.Context.CardRegistry | Where-Object InstanceId -EQ $old.SpawnerCardId
            $old.Team -eq 1 -and $null -ne $next -and $next.MaxHealth -eq $old.MaxHealth + 4 -and
                $next.Health -eq $next.MaxHealth -and
                @($card.Permanent.Upgrades | Where-Object AssetKey -EQ 'PojuPostKillPermanent').Count -gt 0
        }).Count -ge 2
    }).Count -gt 0
}
if ($HandUpgrades -or $TargetedHandUpgrades) {
    $modifierCoverage = $modifierCoverage -and @($modifierActions | Where-Object {
        @($_.Actual.Spawn.Train.Context.CardInstances.Permanent.Upgrades | Where-Object AssetKey -Like 'PojuHand*').Count -gt 0
    }).Count -gt 0
} elseif ($SacrificeUpgrades) {
    $modifierCoverage = $modifierCoverage -and @($modifierActions | Where-Object {
        $modifierAction = $_
        @($modifierAction.Actual.Spawn.Train.Rooms.Units | Where-Object Id -EQ $modifierAction.Action.TargetUnitId).Count -eq 0
    }).Count -gt 0
} elseif ($DynamicUpgrades) {
    $modifierCoverage = $modifierCoverage -and @($modifierActions | Where-Object {
        @($_.Actual.Spawn.Train.Rooms.Units.Modifiers.Upgrades | Where-Object AssetKey -Like 'PojuProbe*').Count -gt 0
    }).Count -gt 0
}
$numericRangeCoverage = -not ($NumericRanges -or $NumericRangesLethal) -or (@($trace.NumericRanges | Where-Object Phase -EQ 'Cast').Count -gt 0 -and
    @($trace.NumericRanges | Where-Object Phase -EQ 'Test').Count -gt 0 -and @($trace.NumericRanges | Where-Object Phase -EQ 'Apply').Count -gt 0)
$targetFilterCoverage = -not $TargetFilters -or (@($trace.FilteredTargets | Where-Object Mode -EQ 'DropTargetCharacter').Count -gt 0 -and @($trace.FilteredTargets | Where-Object Mode -EQ 'Tower').Count -gt 0)
$drawCoverage = -not $Drawing -or (@($trace.CardCycles | Where-Object Kind -EQ 'SpellDraw').Count -gt 0 -and
    @($trace.NumericRanges | Where-Object Phase -EQ 'Cast').Count -gt 0 -and @($trace.NumericRanges | Where-Object Phase -EQ 'Test').Count -gt 0 -and
    @($trace.NumericRanges | Where-Object Phase -EQ 'Apply').Count -gt 0)
$handRemovalCoverage = -not ($HandRemoval -or $HandRemovalLethal) -or (@($trace.HandRemovals | Where-Object Mode -EQ 0).Count -gt 0 -and
    @($trace.HandRemovals | Where-Object Mode -EQ 1).Count -gt 0 -and @($trace.HandRemovals | Where-Object Difference -NE $null).Count -eq 0)
$generationCoverage = -not ($Generation -or $GenerationLethal) -or (@($trace.CardGenerations | ForEach-Object { $_.Rule.Destination } | Sort-Object -Unique).Count -eq 5 -and
    @($trace.CardGenerations | Where-Object { $_.Predicted.AddedCards.Count -gt 0 }).Count -gt 0 -and
    @($trace.CardGenerations | Where-Object Difference -NE $null).Count -eq 0)
if ($Generation -and -not $GenerationLethal) {
    $generationCoverage = $generationCoverage -and @($trace.CardGenerations | Where-Object Origin -EQ 'Unit').Count -gt 0
}
$scalingCoverage = -not ($DamageScaling -or $DynamicStatistics)
if ($DamageScaling -or $DynamicStatistics) {
    $samples = @($trace.DamageScaling)
    $scalingCoverage = $samples.Count -gt 0 -and @($samples | Where-Object DamageSourceCardId -GT 0).Count -gt 0 -and
        @($samples | Where-Object DamageSourceCardId -EQ 0).Count -gt 0 -and
        @($samples | Where-Object { $_.CaptureError -or $_.Difference -or $null -eq $_.After }).Count -eq 0
}
if ($DynamicStatistics) {
    $queryTypes = @($samples | ForEach-Object { $_.Trait.Query.Type } | Sort-Object -Unique)
    foreach ($queryType in @('Gold', 'TurnCount', 'MoonPhase', 'ForgePoints', 'DragonsHoardAmount', 'EnergyRemainingEndOfTurn', 'PyreHeartResurrection')) {
        $scalingCoverage = $scalingCoverage -and $queryTypes.Contains($queryType)
    }
    $scalingCoverage = $scalingCoverage -and
        @($samples | Where-Object { $_.Before.Statistics.EnergyRemainingEndOfTurn -gt 0 }).Count -gt 0 -and
        @($samples | Where-Object { $_.Before.Gold -ne $_.Before.Statistics.GoldStartOfThisTurn }).Count -gt 0 -and
        @($samples | ForEach-Object { $_.Before.QueryFrame.MoonPhase } | Sort-Object -Unique).Count -eq 2
}
$statusScalingCoverage = -not $StatusScaling
if ($StatusScaling) {
    $statusSamples = @($trace.StatusScaling)
    $applications = @($trace.StatusApplications)
    $statusScalingCoverage = $statusSamples.Count -ge 60 -and $applications.Count -gt 0 -and
        @($statusSamples | ForEach-Object { $_.Trait.Filter } | Sort-Object -Unique).Count -eq 3 -and
        @($statusSamples | Where-Object ActualBonus -LT 0).Count -gt 0 -and
        @($statusSamples | Where-Object ActualBonus -GT 0).Count -gt 0 -and
        @($statusSamples | Where-Object { $_.Trait.Filter -eq 1 -and $_.ActualBonus -gt 0 }).Count -gt 0 -and
        @($statusSamples | Where-Object { $_.Trait.OnlyWhenSourceZero -and $_.SourceStacks -eq 0 -and $_.ActualBonus -gt 0 }).Count -gt 0 -and
        @($statusSamples | Where-Object { $_.Trait.OnlyWhenSourceZero -and $_.SourceStacks -ne 0 -and $_.ActualBonus -eq 0 }).Count -gt 0 -and
        @($statusSamples | ForEach-Object { $_.Before.QueryFrame.MoonPhase } | Sort-Object -Unique).Count -eq 2 -and
        @($statusSamples | Where-Object { $_.CaptureError -or $_.Difference -or $null -eq $_.After }).Count -eq 0 -and
        @($applications | Where-Object { $_.CaptureError -or $_.Difference -or $null -eq $_.After }).Count -eq 0 -and
        @($applications | Where-Object { @($_.After.Units.Statuses | Where-Object Stacks -EQ 9999).Count -gt 0 }).Count -gt 0
}
$unitUpgradeScalingCoverage = -not $UnitUpgradeScaling
if ($UnitUpgradeScaling) {
    $upgradeSamples = @($trace.UnitUpgradeScaling)
    $unitUpgradeScalingCoverage = $trace.UnitUpgradeScalingCalibrationContextUnchanged -and
        @($upgradeSamples | Where-Object Origin -EQ 'Calibration').Count -eq 30 -and
        @($upgradeSamples | Where-Object Origin -EQ 'Live').Count -ge 35 -and
        @($upgradeSamples | ForEach-Object { $_.Trait.Stat } | Sort-Object -Unique).Count -eq 2 -and
        @($upgradeSamples | Where-Object TriggerKind -EQ 'OnSpawn').Count -gt 0 -and
        @($upgradeSamples | Where-Object TriggerKind -EQ 'OnHeal').Count -gt 0 -and
        @($upgradeSamples | Where-Object { $_.BeforeUpgrade.MagicPowerTraitScalingOnly }).Count -gt 0 -and
        @($upgradeSamples | Where-Object { $_.AfterUpgrade.Stats.Damage -lt $_.BeforeUpgrade.Stats.Damage }).Count -gt 0 -and
        @($upgradeSamples | Where-Object { $_.AfterUpgrade.Stats.Health -gt $_.BeforeUpgrade.Stats.Health }).Count -gt 0 -and
        @($upgradeSamples | Where-Object { $_.CaptureError -or $_.Difference -or $null -eq $_.After }).Count -eq 0
}
$unitTriggerUpgradeCoverage = -not $UnitTriggerUpgrades
if ($UnitTriggerUpgrades) {
    $triggerSamples = @($trace.UnitUpgradeScaling)
    $unitTriggerUpgradeCoverage = $triggerSamples.Count -ge 30 -and
        @($triggerSamples | Where-Object TriggerKind -EQ 'OnHeal').Count -gt 0 -and
        @($triggerSamples | Where-Object TriggerKind -EQ 'PostCombat').Count -gt 0 -and
        @($triggerSamples | Where-Object { $_.Trait.Restriction -eq 1 }).Count -gt 0 -and
        @($triggerSamples | Where-Object { $_.BeforeUpgrade.MagicPowerTraitScalingOnly }).Count -gt 0 -and
        @($triggerSamples | Where-Object { $_.Difference -or $_.CaptureError -or $null -eq $_.After }).Count -eq 0
}
$preHandDiscardCoverage = -not ($PreHandDiscard -or $PreHandDiscardLethal -or $CloneUpgradeRefresh)
if ($PreHandDiscard -or $PreHandDiscardLethal -or $CloneUpgradeRefresh) {
    $prePhases = @($trace.PreHandDiscards)
    $preHandDiscardCoverage = $prePhases.Count -gt 0 -and
        @($prePhases | Where-Object Team -EQ 1).Count -gt 0 -and @($prePhases | Where-Object Team -EQ 0).Count -gt 0 -and
        @($prePhases | Where-Object { $_.Difference -or $null -eq $_.Actual }).Count -eq 0 -and
        @($trace.UnitUpgradeScaling | Where-Object TriggerKind -EQ 'EndTurnPreHandDiscard').Count -ge 4 -and
        @($trace.UnitUpgradeScaling | Where-Object { $_.Difference -or $_.CaptureError -or $null -eq $_.After }).Count -eq 0 -and
        @($trace.CardGenerations | Where-Object { $_.Origin -eq 'Unit' -and $_.Rule.Destination -eq 'HandPile' }).Count -gt 0
}
$preCombatCoverage = -not ($PreCombat -or $PreCombatLethal -or $TriggeredHealing -or $TriggeredDamage -or $DamageDeathQueue -or $TerminalDeathDamage)
if ($PreCombat -or $PreCombatLethal -or $TriggeredHealing -or $TriggeredDamage -or $DamageDeathQueue -or $TerminalDeathDamage) {
    $prePhases = @($trace.PreCombats)
    $preCombatCoverage = $prePhases.Count -gt 0 -and
        @($prePhases | Where-Object Team -EQ 1).Count -gt 0 -and @($prePhases | Where-Object Team -EQ 0).Count -gt 0 -and
        @($prePhases | Where-Object { $_.Difference -or $null -eq $_.Actual }).Count -eq 0 -and
        @($trace.UnitUpgradeScaling | Where-Object TriggerKind -EQ 'PreCombat').Count -ge 4 -and
        @($trace.UnitUpgradeScaling | Where-Object { $_.Difference -or $_.CaptureError -or $null -eq $_.After }).Count -eq 0 -and
        @($trace.CardGenerations | Where-Object { $_.Origin -eq 'Unit' -and $_.Rule.Destination -eq 'HandPile' }).Count -gt 0
}
$triggeredHealingCoverage = -not $TriggeredHealing
if ($TriggeredHealing) {
    $heals = @($trace.TriggeredHeals)
    $triggeredHealingCoverage = $heals.Count -gt 0 -and
        @($heals | Where-Object { $_.Difference -or -not $_.Completed -or -not $_.Sampled }).Count -eq 0 -and
        @($heals | Where-Object { $_.Effect.Target -eq 'RandomInRoom' -and $null -ne $_.Effect.Range }).Count -gt 0 -and
        @($heals | Where-Object { $_.Effect.Target -eq 'Room' -and $_.Targets.Count -eq 0 -and $null -ne $_.Effect.Range }).Count -gt 0 -and
        @($heals | Where-Object { $_.Targets.Count -ge 2 }).Count -gt 0 -and
        @($heals.Requests | Where-Object { $_.AfterHealth -gt $_.Health }).Count -gt 0 -and
        @($heals.Requests | Where-Object Amount -LT 0).Count -gt 0 -and
        @($heals.Requests | Where-Object Amount -EQ 0).Count -gt 0 -and
        @($trace.UnitUpgradeScaling | Where-Object { $_.TriggerKind -eq 'OnHeal' -and $_.BeforeUpgrade.AssetKey -eq 'PojuOnHealDeferredMaxHealth' }).Count -gt 0
}
$postCombatHealingCoverage = -not $PostCombatHealing
if ($PostCombatHealing) {
    $postPhases = @($trace.UnitPostCombats)
    $postCombatHealingCoverage = $postPhases.Count -gt 0 -and
        @($postPhases | Where-Object { $_.Difference -or $null -eq $_.Actual }).Count -eq 0 -and
        @($trace.TriggeredHeals | Where-Object TriggerKind -EQ 'PostCombatHealing').Count -gt 0 -and
        @($postPhases | Where-Object { $_.CannotAttackOrHeal.Count -gt 0 }).Count -gt 0 -and
        @($trace.UnitUpgradeScaling | Where-Object TriggerKind -EQ 'PostCombat').Count -gt 0
}
$triggeredDamageCoverage = -not $TriggeredDamage
if ($TriggeredDamage) {
    $damageRecords = @($trace.TriggeredDamage)
    $applications = @($damageRecords | Where-Object Stage -EQ 'Application')
    $triggeredDamageCoverage = $applications.Count -gt 0 -and
        @($damageRecords | Where-Object { $_.Difference -or -not $_.Completed -or -not $_.Sampled }).Count -eq 0 -and
        @($applications | Where-Object { $_.Effect.Target -eq 'RandomInRoom' -and $null -ne $_.Effect.Range }).Count -gt 0 -and
        @($applications | Where-Object { $_.Targets.Count -eq 0 -and $null -ne $_.Effect.Range }).Count -gt 0 -and
        @($applications | Where-Object { $_.Targets.Count -ge 2 }).Count -gt 0 -and
        @($applications | Where-Object { $null -ne $_.StatusMultiplier -and $_.MultiplierStacks -gt 0 }).Count -gt 0 -and
        @($applications | Where-Object TriggerKind -EQ 'OnDeath').Count -gt 0 -and
        @($damageRecords | Where-Object { $_.Stage -eq 'Test' -and $_.Amount -lt 0 -and -not $_.TestPassed }).Count -gt 0 -and
        @($applications.Requests | Where-Object { -not $_.AttackerPreserved -or -not $_.DefaultDamage }).Count -eq 0
}
$damageDeathQueueCoverage = -not ($DamageDeathQueue -or $TerminalDeathDamage)
if ($DamageDeathQueue -or $TerminalDeathDamage) {
    $deathDamage = @($trace.TriggeredDamage | Where-Object { $_.Stage -eq 'Application' -and $_.TriggerKind -eq 'OnDeath' })
    $damageDeathQueueCoverage = $deathDamage.Count -gt 0 -and @($trace.PreCombats).Count -ge 2 -and
        @($deathDamage.Requests | Where-Object { @($_.Before.Modifiers.Upgrades | Where-Object AssetKey -EQ 'PojuOnHealBeforeDeathArmor').Count -gt 0 }).Count -gt 0 -and
        @($trace.UnitUpgradeScaling | Where-Object { $_.TriggerKind -eq 'OnHeal' -and $_.BeforeUpgrade.AssetKey -eq 'PojuOnHealBeforeDeathArmor' }).Count -gt 0
}
$cloneUpgradeRefreshCoverage = -not $CloneUpgradeRefresh
$triggeredStatusCoverage = -not $TriggeredStatus
if ($TriggeredStatus) {
    $status = @($trace.TriggeredStatuses)
    $previewRng = @($trace.PreviewRngIsolation)
    $triggeredStatusCoverage = $previewRng.Count -gt 0 -and
        @($previewRng | Where-Object { -not $_.Completed -or ($_.BattleBefore | ConvertTo-Json -Compress) -ne ($_.BattleAfter | ConvertTo-Json -Compress) -or ($_.TestBefore | ConvertTo-Json -Compress) -ne ($_.TestAfter | ConvertTo-Json -Compress) }).Count -eq 0 -and
        $status.Count -gt 10 -and
        @($status | Where-Object { -not $_.Completed -or $null -eq $_.Actual -or $null -eq $_.ActualUnits -or $_.Interactions.Count -gt 0 }).Count -eq 0 -and
        @($status | Where-Object { $_.Targets.Count -eq 0 }).Count -gt 0 -and
        @($status | Where-Object { $_.Targets.Count -gt 1 }).Count -gt 0 -and
        @($status | Where-Object { $_.Effect.Action.Statuses.Count -gt 1 }).Count -gt 0 -and
        @($status | Where-Object { $_.Effect.StatusScaling.MissingHealth -and $_.Effect.StatusScaling.MagicPower }).Count -gt 0 -and
        @($status | Where-Object { @($_.BeforeUnits | Where-Object Health -EQ 0).Count -gt 0 }).Count -gt 0 -and
        @($trace.UnitTurns | Where-Object { $null -eq $_.Actual -or $null -ne $_.Difference -or -not $_.Predicted.Supported }).Count -eq 0
}
$statusCallbackCoverage = -not $StatusCallbacks
if ($StatusCallbacks) {
    $callbackFires = @($trace.StatusCallbackFires)
    $statusCallbackCoverage = $callbackFires.Count -gt 20 -and $callbackFires.Count -eq @($trace.StatusCallbacks).Count -and
        @($callbackFires | Where-Object { -not $_.Completed -or $null -eq $_.Actual -or $null -eq $_.ActualUnit -or $_.Interactions.Count -gt 0 }).Count -eq 0 -and
        @($callbackFires | Where-Object { $_.Kind -eq 'OnSilenceLost' -and $_.GoldAfter -gt $_.GoldBefore }).Count -gt 0 -and
        @($callbackFires | Where-Object { $_.Kind -eq 'OnStatusEffectChanged' -and $_.ParamString -eq 'armor' -and $_.ParamInt2 -eq 0 -and $_.GoldAfter -gt $_.GoldBefore }).Count -gt 0 -and
        @($callbackFires | Where-Object { $_.BeforeUnit.Health -eq 0 }).Count -gt 0 -and
        @($callbackFires | Where-Object { $_.BeforeUnit.Team -eq 0 -or $_.BeforeUnit.Team -eq 'Enemy' }).Count -gt 0
    foreach ($callbackKind in 'OnStatusEffectChanged','OnArmorAdded','OnPyregelAdded','OnValiant','OnSilence','OnSilenceLost','OnNewStatusEffectAdded') {
        $statusCallbackCoverage = $statusCallbackCoverage -and @($callbackFires | Where-Object { $_.Kind -eq $callbackKind }).Count -gt 0
    }
}
$attackTriggerCoverage = -not $AttackTriggers
if ($AttackTriggers) {
    $attackFires = @($trace.AttackTriggers | Where-Object Stage -EQ 'Fire')
    $attackTriggerCoverage = $attackFires.Count -gt 10 -and
        @($attackFires | Where-Object Kind -EQ 'OnAttackingBeforeDamage').Count -gt 0 -and
        @($attackFires | Where-Object Kind -EQ 'OnAttacking').Count -gt 0 -and
        @($attackFires | Where-Object { $_.Health -eq 0 -and -not $_.DeadBoss }).Count -gt 0 -and
        @($trace.AttackTriggers | Where-Object { $_.Stage -eq 'Queue' -and $_.QueueRunning }).Count -gt 0 -and
        @($trace.AttackTriggers | Where-Object { -not $_.Completed -or $null -eq $_.GoldAfter -or $null -eq $_.AfterTriggered -or $_.OverrideTargetId -le 0 -or $_.ParamInt -ne 0 }).Count -eq 0 -and
        @($trace.UnitTurns | Where-Object { $null -eq $_.Actual -or $null -ne $_.Difference -or -not $_.Predicted.Supported }).Count -eq 0 -and
        @($trace.UnitTurns | Where-Object { $_.AttackedTargetIds.Count -gt 1 }).Count -gt 0
}
$dyingUpgradeCoverage = -not $DyingUpgrades
if ($DyingUpgrades) {
    $dying = @($trace.DyingUpgrades | Where-Object { @($_.BeforeUnits | Where-Object Health -EQ 0).Count -gt 0 })
    $dyingUpgradeCoverage = $dying.Count -ge 6 -and
        @($dying | Where-Object Kind -EQ 'OnKill').Count -gt 0 -and
        @($dying | Where-Object Kind -EQ 'OnHit').Count -gt 0 -and
        @($dying | Where-Object Kind -EQ 'OnDeath').Count -gt 0 -and
        @($dying | Where-Object { $_.Effect.Type -eq 'RemoveUnitUpgrade' }).Count -gt 0 -and
        @($dying | Where-Object { $_.Effect.Type -ne 'RemoveUnitUpgrade' -and $_.Effect.Upgrade.Stats.Health -lt 0 -and
            $_.ActualUnits[0].MaxHealth -lt $_.BeforeUnits[0].MaxHealth }).Count -gt 0 -and
        @($dying | Where-Object { $_.Effect.Upgrade.UnhealedHealth -lt 0 }).Count -gt 0 -and
        @($trace.DyingUpgrades | Where-Object { -not $_.Completed -or $null -eq $_.Actual -or $null -eq $_.ActualUnits -or $_.Interactions.Count -gt 0 }).Count -eq 0 -and
        @($trace.UnitTurns | Where-Object { $null -eq $_.Actual -or $null -ne $_.Difference -or -not $_.Predicted.Supported }).Count -eq 0
}
$hitKillCoverage = -not $HitKill
if ($HitKill) {
    $fires = @($trace.HitKills | Where-Object Stage -EQ 'Fire')
    $hitKillCoverage = $fires.Count -ge 10 -and
        @($fires | Where-Object { $_.Kind -eq 'OnHit' -and $_.Health -gt 0 -and $_.ParamInt -eq 0 -and $_.GoldAfter -gt $_.GoldBefore }).Count -gt 0 -and
        @($fires | Where-Object { $_.Kind -eq 'OnHit' -and $_.Health -eq 0 -and -not $_.DeadBoss -and $_.GoldAfter -gt $_.GoldBefore }).Count -gt 0 -and
        @($fires | Where-Object { $_.Kind -eq 'OnHit' -and $_.ParamInt -ge 2 }).Count -gt 0 -and
        @($fires | Where-Object { $_.Kind -eq 'OnKill' -and $_.DyingId -gt 0 -and $_.GoldAfter -gt $_.GoldBefore }).Count -gt 0 -and
        @($trace.HitKills | Where-Object { $_.Stage -eq 'Queue' -and $_.QueueRunning }).Count -gt 0 -and
        @($trace.UnitTurns | Where-Object { $_.AttackedTargetIds.Count -gt 1 }).Count -gt 0 -and
        @($trace.HitKills | Where-Object { -not $_.Completed -or $null -eq $_.GoldAfter -or $null -eq $_.AfterTriggered }).Count -eq 0 -and
        @($trace.UnitTurns | Where-Object { $null -eq $_.Actual -or $null -ne $_.Difference -or -not $_.Predicted.Supported }).Count -eq 0
}
$terminalDeathDamageCoverage = -not $TerminalDeathDamage
if ($TerminalDeathDamage) {
    $terminalRecords = @($trace.TerminalDeaths | Where-Object Terminal)
    $postClearDeaths = @($trace.TerminalDeaths | Where-Object { $_.Before.KillCamActivated -and $_.Team -eq 1 })
    $terminalDeathDamageCoverage = $terminalRecords.Count -gt 0 -and $postClearDeaths.Count -gt 0 -and @($trace.KillCams).Count -gt 0 -and
        @($trace.KillCams | Where-Object { -not $_.Completed -or $null -eq $_.Actual }).Count -eq 0 -and
        @($trace.TerminalDeaths | Where-Object { -not $_.Completed -or -not $_.FinishedDying -or $null -eq $_.Actual -or $_.Sacrifice }).Count -eq 0 -and
        @($terminalRecords | Where-Object { -not $_.Actual.KillCamActivated -or $_.Actual.CardInstances.Count -ne 0 }).Count -eq 0 -and
        @($trace.TriggeredDamage.Requests | Where-Object { $_.Context.KillCamActivated -and $_.Context.CardInstances.Count -eq 0 -and $_.AfterHealth -eq 0 }).Count -gt 0
}
if ($CloneUpgradeRefresh) {
    $cloneUpgradeRefreshCoverage = @($trace.CardGenerations | Where-Object { $_.Origin -eq 'Unit' -and $_.Rule.CopyModifiers }).Count -ge 2 -and
        @($trace.CardGenerations | Where-Object { $_.Difference -or $null -eq $_.Actual }).Count -eq 0
}
$teamTurnBeginCoverage = -not $TeamTurnBegin
if ($TeamTurnBegin) {
    $teamSamples = @($trace.UnitUpgradeScaling | Where-Object TriggerKind -EQ 'OnTeamTurnBegin')
    $teamPhases = @($trace.TeamTurnBegins)
    $teamTurnBeginCoverage = $teamSamples.Count -ge 6 -and $teamPhases.Count -gt 0 -and
        @($teamPhases | Where-Object Team -EQ 0).Count -gt 0 -and @($teamPhases | Where-Object Team -EQ 1).Count -gt 0 -and
        @($teamPhases | Where-Object { $_.Difference -or $null -eq $_.Actual }).Count -eq 0 -and
        @($trace.UnitTurns).Count -gt 0 -and @($trace.UnitTurns | Where-Object { $_.Difference -or $null -eq $_.Actual }).Count -eq 0 -and
        @($trace.UnitUpgradeScaling | Where-Object { $_.Difference -or $_.CaptureError -or $null -eq $_.After }).Count -eq 0
}
$unitTurnBeginCoverage = -not $UnitTurnBegin
if ($UnitTurnBegin) {
    $turnSamples = @($trace.UnitUpgradeScaling)
    $unitTurnBeginCoverage = $turnSamples.Count -ge 12 -and
        @($turnSamples | Where-Object TriggerKind -NE 'OnTurnBegin').Count -eq 0 -and
        @($turnSamples | Where-Object { $_.Trait.Restriction -eq 1 }).Count -gt 0 -and
        @($turnSamples | Where-Object { $_.Trait.Query.Type -eq 'TurnCount' -and $_.AfterUpgrade.Stats.Damage -gt $_.BeforeUpgrade.Stats.Damage }).Count -gt 0 -and
        @($turnSamples | Where-Object { $_.Difference -or $_.CaptureError -or $null -eq $_.After }).Count -eq 0 -and
        @($trace.UnitTurns).Count -gt 0 -and @($trace.UnitTurns | Where-Object { $_.Difference -or $null -eq $_.Actual }).Count -eq 0
}
$spawnTriggerCoverage = -not ($SpawnTriggers -or $SpawnTriggersLethal)
if ($SpawnTriggers -or $SpawnTriggersLethal) {
    $spawnSamples = @($trace.UnitUpgradeScaling)
    $spawnTriggerCoverage = @($spawnSamples | Where-Object TriggerKind -EQ 'OnSpawn').Count -ge 8 -and
        @($spawnSamples | Where-Object { $_.TriggerKind -eq 'OnSpawn' -and $_.Trait.Query.Type -eq 'AnyMonsterSpawned' -and $_.AfterUpgrade.Stats.Health -gt $_.BeforeUpgrade.Stats.Health }).Count -gt 0 -and
        @($spawnSamples | Where-Object { $_.Difference -or $_.CaptureError -or $null -eq $_.After }).Count -eq 0 -and
        @($trace.CardGenerations | Where-Object { $_.Origin -eq 'Unit' -and $_.SourceCardId -gt 0 -and $_.Rule.Count -gt 0 }).Count -gt 0
    if (-not $SpawnTriggersLethal) {
        $spawnTriggerCoverage = $spawnTriggerCoverage -and @($spawnSamples | Where-Object TriggerKind -EQ 'OnUnscaledSpawn').Count -gt 0
    }
}
$energyCoverage = $true
if ($EnergyEffects -or $EnergyEffectsLethal) {
    $energySamples = @($trace.EnergyEffects)
    $energyCoverage = $energySamples.Count -gt 0 -and @($energySamples.Effect.Type | Sort-Object -Unique).Count -eq 5 -and
        @($energySamples | Where-Object { -not $_.Completed -or -not $_.Sampled }).Count -eq 0 -and
        @($energySamples | Where-Object Amount -EQ 0).Count -gt 0 -and @($energySamples | Where-Object Amount -LT 0).Count -gt 0
    if ($EnergyEffectsLethal) {
        $energyCoverage = $energyCoverage -and @($trace.Actions | Where-Object { $_.Actual.Spawn.Train.Context.AllScenarioBossesDead -eq $true }).Count -eq 1
    }
}
$xCostCoverage = $true
if ($XCost -or $XCostLethal) {
    $xCasts = @($trace.Actions | Where-Object {
        $cardId = $_.Action.CardInstanceId
        $instance = $_.Before.Spawn.Train.Context.CardInstances | Where-Object InstanceId -EQ $cardId
        $definition = $_.Before.PlayRules.Cards | Where-Object DataId -EQ $instance.DataId
        $definition.CostType -eq 'ConsumeRemainingEnergy'
    })
    $xCostCoverage = $xCasts.Count -gt 0 -and @($xCasts | Where-Object { $_.Before.Energy -eq 0 }).Count -gt 0 -and
        @($xCasts | Where-Object { $_.Before.Energy -gt 0 }).Count -gt 0 -and
        @($trace.DamageScaling | Where-Object { $_.Difference -or $_.CaptureError -or $null -eq $_.After }).Count -eq 0 -and
        @($trace.DamageScaling.Trait.Query.Type | Sort-Object -Unique).Count -eq 2
    if ($XCostLethal) { $xCostCoverage = $xCostCoverage -and @($xCasts | Where-Object ActualOutcome -EQ 3).Count -eq 1 }
}
$bonusDrawCoverage = $true
if ($BonusDraw -or $BonusDrawLethal) {
    $bonusSamples = @($trace.BonusDrawEffects)
    $bonusDrawCoverage = $bonusSamples.Count -gt 0 -and @($bonusSamples | Where-Object { -not $_.Completed -or -not $_.Sampled -or $null -eq $_.After }).Count -eq 0 -and
        @($bonusSamples | Where-Object Amount -LT 0).Count -gt 0 -and @($bonusSamples | Where-Object Amount -EQ 0).Count -gt 0 -and
        @($bonusSamples | Where-Object { $_.Key -like 'unit:*' }).Count -gt 0 -and @($bonusSamples | Where-Object { $null -ne $_.Effect.Range }).Count -gt 0
    if ($BonusDrawLethal) { $bonusDrawCoverage = $bonusDrawCoverage -and @($trace.Actions | Where-Object ActualOutcome -EQ 3).Count -eq 1 }
}
$directUpgradeCoverage = -not $DirectUnitUpgrades -or (@($trace.DirectUnitUpgrades).Count -ge 21 -and @($trace.DirectUnitUpgrades | Where-Object { $null -eq $_.After -or $_.Difference -or $_.UnsupportedReason }).Count -eq 0)
$equipmentCoverage = -not $Equipment -or (@($trace.EquipmentOperations).Count -gt 3 -and
    @($trace.EquipmentOperations | Where-Object { -not $_.Completed -or $null -eq $_.After -or $_.Difference -or $_.UnsupportedReason }).Count -eq 0 -and
    @($trace.EquipmentOperations | Where-Object { $_.Remove -and $_.CardId -eq 0 }).Count -gt 0)
$capacityCoverage = $true
if ($RoomCapacity -or $RoomCapacityLethal) {
    $capacitySamples = @($trace.CapacityEffects)
    $capacityCoverage = $capacitySamples.Count -gt 0 -and @($capacitySamples | Where-Object { -not $_.Completed -or $null -eq $_.After }).Count -eq 0 -and
        @($capacitySamples | Where-Object { $_.Effect.Value -lt 0 }).Count -gt 0 -and @($capacitySamples | Where-Object { $_.Effect.Value -eq 0 }).Count -gt 0 -and
        @($capacitySamples | Where-Object SourceCardId -EQ 0).Count -gt 0 -and @($capacitySamples | Where-Object { $null -ne $_.Effect.Range }).Count -gt 0 -and
        @($trace.CapacityTests | Where-Object { -not $_.Actual }).Count -gt 0
    if ($RoomCapacityLethal) { $capacityCoverage = $capacityCoverage -and @($trace.Actions | Where-Object ActualOutcome -EQ 3).Count -eq 1 }
}
$result = [pscustomobject]@{
    Policy = $Policy
    Profile = $profile
    ExitCode = $process.ExitCode
    NativePassed = $nativePassed
    NativeWon = $trace.NativeWon
    CaptureFailures = $trace.CaptureFailures
    Mismatches = $trace.Mismatches
    Unsupported = $trace.Unsupported
    Pending = $trace.Pending
    Stages = @($trace.Stages).Count
    CardCycles = @($trace.CardCycles).Count
    TrainPhases = @($trace.TrainPhases).Count
    Spawns = @($trace.Spawns).Count
    EndTurns = @($trace.Turns).Count
    Actions = @($trace.Actions).Count
    DecisionTurns = @($trace.Checkpoints).Count - 1
    OriginalFilesUnchanged = $originalUnchanged
    ModifierActions = $modifierActions.Count
    ModifierCoverage = $modifierCoverage
    HealActions = $healActions.Count
    HealingCoverage = $healingCoverage
    OnHealCoverage = $onHealCoverage
    RoomSpellCoverage = $roomSpellCoverage
    TerminalEffectsSettled = $terminalSettled
    TerminalSpellCoverage = $terminalSpellCoverage
    PostKillCoverage = $postKillCoverage
    RandomCoverage = $randomCoverage
    RandomStatusCoverage = $randomStatusCoverage
    CrossRoomCoverage = $crossRoomCoverage
    AttackCoverage = $attackCoverage
    MaxHealthCoverage = $maxHealthCoverage
    NumericRangeCoverage = $numericRangeCoverage
    TargetFilterCoverage = $targetFilterCoverage
    DrawCoverage = $drawCoverage
    HandRemovalCoverage = $handRemovalCoverage
    GenerationCoverage = $generationCoverage
    DamageScalingCoverage = $scalingCoverage
    StatusScalingCoverage = $statusScalingCoverage
    UnitUpgradeScalingCoverage = $unitUpgradeScalingCoverage
    UnitTriggerUpgradeCoverage = $unitTriggerUpgradeCoverage
    SpawnTriggerCoverage = $spawnTriggerCoverage
    UnitTurnBeginCoverage = $unitTurnBeginCoverage
    TeamTurnBeginCoverage = $teamTurnBeginCoverage
    PreHandDiscardCoverage = $preHandDiscardCoverage
    CloneUpgradeRefreshCoverage = $cloneUpgradeRefreshCoverage
    PreCombatCoverage = $preCombatCoverage
    DamageDeathQueueCoverage = $damageDeathQueueCoverage
    TerminalDeathDamageCoverage = $terminalDeathDamageCoverage
    DyingUpgradeCoverage = $dyingUpgradeCoverage
    DyingUpgradeSamples = @($trace.DyingUpgrades).Count
    TriggeredStatusCoverage = $triggeredStatusCoverage
    StatusCallbackCoverage = $statusCallbackCoverage
    EnergyCoverage = $energyCoverage
    EnergySamples = @($trace.EnergyEffects).Count
    XCostCoverage = $xCostCoverage
    BonusDrawCoverage = $bonusDrawCoverage
    BonusDrawSamples = @($trace.BonusDrawEffects).Count
    DirectUnitUpgradeCoverage = $directUpgradeCoverage
    EquipmentCoverage = $equipmentCoverage
    EquipmentOperations = @($trace.EquipmentOperations).Count
    CapacityCoverage = $capacityCoverage
    CapacitySamples = @($trace.CapacityEffects).Count
    CapacityTests = @($trace.CapacityTests).Count
    TriggeredStatusSamples = @($trace.TriggeredStatuses).Count
    AttackTriggerCoverage = $attackTriggerCoverage
    AttackTriggerSamples = @($trace.AttackTriggers).Count
    HitKillCoverage = $hitKillCoverage
    HitKillSamples = @($trace.HitKills).Count
    TriggeredDamageCoverage = $triggeredDamageCoverage
    TriggeredHealingCoverage = $triggeredHealingCoverage
    TriggeredHealSamples = @($trace.TriggeredHeals).Count
    PostCombatHealingCoverage = $postCombatHealingCoverage
    UnitPostCombatPhases = @($trace.UnitPostCombats).Count
    StatisticQueryCalibration = $(if ($StatisticQueries) { Join-Path $profile 'statistic-query-calibration.json' } else { $null })
    StatisticOverflowCalibration = $(if ($StatisticOverflow) { Join-Path $profile 'statistic-overflow-calibration.json' } else { $null })
    StatisticZeroIncrementCalibration = $(if ($StatisticOverflow) { Join-Path $profile 'statistic-zero-increment-calibration.json' } else { $null })
    Trace = $tracePath
}
$result | ConvertTo-Json
if ($StatisticQueries) {
    $queryPath = Join-Path $profile 'statistic-query-calibration.json'
    if (-not (Test-Path -LiteralPath $queryPath)) { throw 'Missing native statistic-query calibration.' }
    $queryCalibration = Get-Content -LiteralPath $queryPath -Raw | ConvertFrom-Json
    if (-not $queryCalibration.LiveContextUnchanged -or @($queryCalibration.Batches).Count -ne 6 -or
        @($queryCalibration.Batches.Samples).Count -lt 500) { throw 'Native statistic-query coverage is incomplete.' }
}
if ($StatisticOverflow) {
    $overflow = Get-Content -LiteralPath (Join-Path $profile 'statistic-overflow-calibration.json') -Raw | ConvertFrom-Json
    if (-not $overflow.LiveContextUnchanged -or @($overflow.Samples).Count -ne 65) { throw 'Native statistic-overflow coverage is incomplete.' }
    $zero = Get-Content -LiteralPath (Join-Path $profile 'statistic-zero-increment-calibration.json') -Raw | ConvertFrom-Json
    if (-not $zero.LiveContextUnchanged -or @($zero.Samples).Count -ne 48 -or
        @($zero.Samples | Where-Object DetachedSource -EQ $true).Count -ne 24 -or
        @($zero.Samples | Where-Object Amount -NE 0).Count -ne 0) { throw 'Native zero-increment coverage is incomplete.' }
}
if ($null -eq $trace.NativeWon -or $process.ExitCode -ne 0 -or -not $nativePassed -or -not $originalUnchanged -or -not $modifierCoverage -or -not $healingCoverage -or -not $onHealCoverage -or -not $roomSpellCoverage -or -not $terminalSettled -or -not $terminalSpellCoverage -or
    -not $postKillCoverage -or -not $randomCoverage -or -not $randomStatusCoverage -or -not $crossRoomCoverage -or -not $attackCoverage -or -not $maxHealthCoverage -or -not $numericRangeCoverage -or -not $targetFilterCoverage -or -not $drawCoverage -or -not $handRemovalCoverage -or -not $generationCoverage -or -not $scalingCoverage -or -not $statusScalingCoverage -or -not $unitUpgradeScalingCoverage -or -not $unitTriggerUpgradeCoverage -or -not $spawnTriggerCoverage -or -not $unitTurnBeginCoverage -or -not $teamTurnBeginCoverage -or -not $preHandDiscardCoverage -or -not $cloneUpgradeRefreshCoverage -or -not $preCombatCoverage -or -not $triggeredHealingCoverage -or -not $postCombatHealingCoverage -or -not $triggeredDamageCoverage -or -not $damageDeathQueueCoverage -or -not $terminalDeathDamageCoverage -or -not $hitKillCoverage -or -not $dyingUpgradeCoverage -or -not $attackTriggerCoverage -or -not $triggeredStatusCoverage -or -not $statusCallbackCoverage -or -not $energyCoverage -or -not $xCostCoverage -or -not $bonusDrawCoverage -or -not $capacityCoverage -or -not $directUpgradeCoverage -or -not $equipmentCoverage -or $trace.CaptureFailures -ne 0 -or $trace.Mismatches -ne 0 -or $trace.Unsupported -ne 0 -or $trace.Pending -ne 0) {
    throw "Full battle differential probe failed; inspect $tracePath and $unityLog"
}
