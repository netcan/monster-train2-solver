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
    MT2_PROBE_MODIFIERS = $(if ($CrossRoomTargets) { 'cross-room-targets' } elseif ($CrossRoomSpells) { 'cross-room-spells' } elseif ($RandomStatus) { 'random-status' } elseif ($RandomSpells) { 'random-spells' } elseif ($PostKillSpells) { 'post-kill-spells' } elseif ($TerminalSpells) { 'terminal-spells' } elseif ($RoomSpells) { 'room-spells' } elseif ($HealingTriggers) { 'healing-triggers' } elseif ($Healing) { 'healing' } elseif ($TargetedHandUpgrades) { 'targeted-hand-upgrades' } elseif ($HandUpgrades) { 'hand-upgrades' } elseif ($SacrificeUpgrades) { 'sacrifice-upgrades' } elseif ($DynamicUpgrades) { 'dynamic-upgrades' } elseif ($NumericUpgrades) { 'numeric-upgrades' } else { '' })
    MT2_PROBE_DIRECT_BRANCH = '1'
    MT2_PROBE_DEPTH = '100'
    MT2_PROBE_TARGET_TURN = '0'
    MT2_PROBE_SOURCE_PLAY_TURNS = ''
    MT2_PROBE_BRANCH_ANY_UNIT = '0'
    MT2_PROBE_FAST_REPLAY = '0'
    MT2_PROBE_NO_TIMEOUT = '1'
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
    Trace = $tracePath
}
$result | ConvertTo-Json
if ($null -eq $trace.NativeWon -or $process.ExitCode -ne 0 -or -not $nativePassed -or -not $originalUnchanged -or -not $modifierCoverage -or -not $healingCoverage -or -not $onHealCoverage -or -not $roomSpellCoverage -or -not $terminalSettled -or -not $terminalSpellCoverage -or
    -not $postKillCoverage -or -not $randomCoverage -or -not $randomStatusCoverage -or -not $crossRoomCoverage -or $trace.CaptureFailures -ne 0 -or $trace.Mismatches -ne 0 -or $trace.Unsupported -ne 0 -or $trace.Pending -ne 0) {
    throw "Full battle differential probe failed; inspect $tracePath and $unityLog"
}
