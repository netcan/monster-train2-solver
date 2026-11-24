#requires -Version 7.4
[CmdletBinding()]
param([Parameter(Mandatory)] [string] $Archive)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
Add-Type -Path (Join-Path $workspace 'src/FixtureArchive/bin/Release/net8.0/FixtureArchive.dll')
$document = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read((Resolve-Path -LiteralPath $Archive).Path)
try {
    $trace = $document.RootElement.ToObjectGraph()
    $scope = $trace.AdvisorDevoteObservation
    $before = $scope.Before
    $accepted = $scope.Accepted
    $after = $scope.After
    $inputs = @($trace.Actions | Where-Object { $_.Action.Operation -ceq 'Devote' })
    if ($trace.GameVersion -cne '2.3.1' -or
        $trace.GameModuleMvid -cne 'bf93f060-beda-46b1-b808-95ebf1e9a99d' -or
        $trace.NativeRunInput.Seed -ne 424242 -or
        $trace.NativeRunInput.PrimaryClan -cne 'Banished' -or
        $trace.NativeRunInput.SecondaryClan -cne 'Pyreborne' -or
        $trace.NativeRunInput.WasDevCheatUsed -ne $false -or
        $scope.Scope -cne 'controlled-native-helper-api-only' -or -not $scope.Passed -or
        $before.PlayerDevoteEnabled -eq $true -or $before.DevoteCandidates -ne 0 -or
        $before.RootKey -cne $before.AdviceRootKey -or $after.RootKey -cne $after.AdviceRootKey -or
        $before.RootKey -ceq $after.RootKey -or
        $before.DevoteInputs -ne 0 -or $accepted.DevoteInputs -ne 1 -or $after.DevoteInputs -ne 1 -or
        $accepted.Generation -le $before.Generation -or $after.Generation -lt $accepted.Generation -or
        $accepted.Publications -ne $before.Publications -or $after.Publications -le $before.Publications -or
        -not $accepted.CurrentAdviceAbsent -or -not $accepted.CapturePending -or
        -not $accepted.NativeDevoteInProgress -or -not $after.NativeResourcesMatch -or -not $after.FreshRoot -or
        $trace.PlayerAdvisor.Assembly -cne 'MonsterTrain2Poju.Advisor' -or $null -ne $trace.Advisor -or
        $trace.PlayerAdvisor.Overlay.Published -lt $after.Publications -or
        $trace.PlayerAdvisor.Overlay.DiagnosticHistory.Dropped -ne 0 -or
        $trace.PlayerAdvisor.Overlay.DiagnosticHistory.Error -or
        $trace.PlayerAdvisor.Overlay.DiagnosticHistory.WriterThreadId -eq $trace.PlayerAdvisor.Overlay.MainThreadId -or
        $inputs.Count -ne 14 -or -not $trace.NativeWon -or -not $trace.TerminalEffectsSettled -or
        @($trace.Actions | Where-Object { $null -ne $_.Difference -or $null -eq $_.Actual }).Count -ne 0 -or
        $trace.CaptureFailures -ne 0 -or $trace.Mismatches -ne 0 -or
        $trace.Unsupported -ne 0 -or $trace.Pending -ne 0) {
        throw 'Binary Devote advisor receipt lacks its controlled native boundary or complete victory.'
    }
    $first = $inputs[0]
    $nativeBefore = $first.Before
    $nativeAfter = $first.Actual
    if ($accepted.TargetRoom -ne $first.Action.RoomIndex -or
        $before.Turn -ne $nativeBefore.Spawn.Turn -or $after.Turn -ne $nativeAfter.Spawn.Turn -or
        $before.Energy -ne $nativeBefore.Energy -or $after.Energy -ne $nativeAfter.Energy -or
        $before.Hand -ne @($nativeBefore.Spawn.Train.Context.Cards.Hand).Count -or
        $after.Hand -ne @($nativeAfter.Spawn.Train.Context.Cards.Hand).Count -or
        $before.Devotion.Level -ne $nativeBefore.Spawn.Train.Context.QueryFrame.Devotion.Level -or
        $before.Devotion.Points -ne $nativeBefore.Spawn.Train.Context.QueryFrame.Devotion.Points -or
        $after.Devotion.Level -ne $nativeAfter.Spawn.Train.Context.QueryFrame.Devotion.Level -or
        $after.Devotion.Points -ne $nativeAfter.Spawn.Train.Context.QueryFrame.Devotion.Points) {
        throw 'Binary player Devote snapshots disagree with the first original action or its settled successor.'
    }
    foreach ($name in @('S0', 'S1', 'S2', 'S3')) {
        if ($before.BattleRng.$name -ne $nativeBefore.Spawn.Train.Context.BattleRng.$name -or
            $after.BattleRng.$name -ne $nativeAfter.Spawn.Train.Context.BattleRng.$name) {
            throw "Binary player Devote Battle RNG differs at $name."
        }
    }
    Write-Output "PLAYER-ADVISOR-DEVOTE-ARCHIVE-PASS scope=controlled-helper-api-only firstRoom=$($accepted.TargetRoom) oldGeneration=$($before.Generation) freshGeneration=$($after.Generation) nativeInputs=$($inputs.Count)"
} finally { $document.Dispose() }
