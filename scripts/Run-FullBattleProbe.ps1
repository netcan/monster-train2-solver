#requires -Version 7.4
param(
    [ValidateSet('no-cards', 'steward-once', 'units-and-junk', 'units-spells-and-junk')]
    [string] $Policy = 'steward-once',
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
    [switch] $SkipBuild
)

$ErrorActionPreference = 'Stop'
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
}
$pluginDir = Join-Path $gameRoot 'BepInEx\plugins'
Copy-Item -LiteralPath (Join-Path $workspace 'src\Probe\bin\Release\netstandard2.1\Probe.dll') -Destination $pluginDir -Force
Copy-Item -LiteralPath (Join-Path $workspace 'src\Model\bin\Release\netstandard2.1\Model.dll') -Destination $pluginDir -Force
Copy-Item -LiteralPath (Join-Path $workspace '.probe-baseline') -Destination $profile -Recurse
$environment = @{
    MT2_PROBE_DATA_DIR = $profile
    MT2_PROBE_SCENARIO = 'native-replay'
    MT2_PROBE_FULL_BATTLE = '1'
    MT2_PROBE_FULL_BATTLE_POLICY = $Policy
    MT2_PROBE_MODIFIERS = $(if ($UnitTurnBegin) { 'unit-turn-begin' } elseif ($SpawnTriggersLethal) { 'spawn-triggers-lethal' } elseif ($SpawnTriggers) { 'spawn-triggers' } elseif ($UnitTriggerUpgrades) { 'unit-trigger-upgrades' } elseif ($UnitUpgradeScaling) { 'unit-upgrade-scaling' } elseif ($StatusScaling) { 'status-scaling' } elseif ($DynamicStatistics) { 'dynamic-statistics' } elseif ($DamageScaling) { 'damage-scaling' } elseif ($GenerationLethal) { 'generation-lethal' } elseif ($Generation) { 'generation' } elseif ($HandRemovalLethal) { 'hand-removal-lethal' } elseif ($HandRemoval) { 'hand-removal' } elseif ($Drawing) { 'drawing' } elseif ($TargetFilters) { 'target-filters' } elseif ($NumericRangesLethal) { 'numeric-ranges-lethal' } elseif ($NumericRanges) { 'numeric-ranges' } elseif ($MaxHealthLethal) { 'max-health-lethal' } elseif ($MaxHealthSpells) { 'max-health-spells' } elseif ($AttackBuffs) { 'attack-buffs' } elseif ($CrossRoomTargets) { 'cross-room-targets' } elseif ($CrossRoomSpells) { 'cross-room-spells' } elseif ($RandomStatus) { 'random-status' } elseif ($RandomSpells) { 'random-spells' } elseif ($PostKillSpells) { 'post-kill-spells' } elseif ($TerminalSpells) { 'terminal-spells' } elseif ($RoomSpells) { 'room-spells' } elseif ($HealingTriggers) { 'healing-triggers' } elseif ($Healing) { 'healing' } elseif ($TargetedHandUpgrades) { 'targeted-hand-upgrades' } elseif ($HandUpgrades) { 'hand-upgrades' } elseif ($SacrificeUpgrades) { 'sacrifice-upgrades' } elseif ($DynamicUpgrades) { 'dynamic-upgrades' } elseif ($NumericUpgrades) { 'numeric-upgrades' } else { '' })
    MT2_PROBE_DIRECT_BRANCH = '1'
    MT2_PROBE_DEPTH = '100'
    MT2_PROBE_TARGET_TURN = '0'
    MT2_PROBE_SOURCE_PLAY_TURNS = ''
    MT2_PROBE_BRANCH_ANY_UNIT = '0'
    MT2_PROBE_FAST_REPLAY = '0'
    MT2_PROBE_NO_TIMEOUT = '1'
    MT2_PROBE_ISOLATE_UI_RNG = $(if ($NumericRanges -or $NumericRangesLethal -or $Drawing) { '1' } else { '0' })
    MT2_PROBE_STATISTIC_QUERIES = $(if ($StatisticQueries) { '1' } else { '0' })
    MT2_PROBE_STATISTIC_OVERFLOW = $(if ($StatisticOverflow) { '1' } else { '0' })
}
$originalBefore = Get-OriginalSignature
$unityLog = Join-Path $profile 'unity-scenario.log'
$process = Start-Process -FilePath (Join-Path $gameRoot 'MonsterTrain2.exe') `
    -WorkingDirectory $gameRoot -ArgumentList @('-logFile', $unityLog) `
    -Environment $environment -WindowStyle Hidden -PassThru
Write-Output "FULL-BATTLE-RUN pid=$($process.Id) policy=$Policy profile=$profile"
if (-not $process.WaitForExit(600000)) {
    $process.Kill()
    $process.WaitForExit()
    throw "Full battle timed out; profile: $profile"
}
$tracePath = Join-Path $profile 'full-battle.json'
if (-not (Test-Path -LiteralPath $tracePath)) { throw "Missing battle trace; inspect $unityLog" }
$trace = Get-Content -LiteralPath $tracePath -Raw | ConvertFrom-Json
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
    -not $postKillCoverage -or -not $randomCoverage -or -not $randomStatusCoverage -or -not $crossRoomCoverage -or -not $attackCoverage -or -not $maxHealthCoverage -or -not $numericRangeCoverage -or -not $targetFilterCoverage -or -not $drawCoverage -or -not $handRemovalCoverage -or -not $generationCoverage -or -not $scalingCoverage -or -not $statusScalingCoverage -or -not $unitUpgradeScalingCoverage -or -not $unitTriggerUpgradeCoverage -or -not $spawnTriggerCoverage -or -not $unitTurnBeginCoverage -or $trace.CaptureFailures -ne 0 -or $trace.Mismatches -ne 0 -or $trace.Unsupported -ne 0 -or $trace.Pending -ne 0) {
    throw "Full battle differential probe failed; inspect $tracePath and $unityLog"
}
