#requires -Version 7.4
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [object] $Trace,
    [Parameter(Mandatory)] [string] $Directory
)
$ErrorActionPreference = 'Stop'
$observation = $Trace.AdvisorDevoteObservation
$before = $observation.Before
$accepted = $observation.Accepted
$after = $observation.After
if ($observation.Scope -cne 'controlled-native-helper-api-only' -or -not $observation.Passed -or
    $null -eq $before -or $null -eq $accepted -or $null -eq $after -or
    $before.PlayerDevoteEnabled -eq $true -or $before.DevoteCandidates -ne 0 -or
    $before.RootKey -cne $before.AdviceRootKey -or $after.RootKey -cne $after.AdviceRootKey -or
    $before.RootKey -ceq $after.RootKey -or $accepted.DevoteInputs -ne $before.DevoteInputs + 1 -or
    $after.DevoteInputs -ne $accepted.DevoteInputs -or
    $accepted.Generation -le $before.Generation -or $after.Generation -lt $accepted.Generation -or
    $accepted.Publications -ne $before.Publications -or $after.Publications -le $before.Publications -or
    -not $accepted.CurrentAdviceAbsent -or -not $accepted.CapturePending -or
    -not $accepted.NativeDevoteInProgress -or -not $after.NativeResourcesMatch -or -not $after.FreshRoot) {
    throw 'Controlled native Devote did not retire stale player advice and publish a fresh settled root.'
}
$actions = @($Trace.Actions | Where-Object { $_.Action.Operation -ceq 'Devote' })
if ($actions.Count -ne 14 -or $accepted.TargetRoom -ne $actions[0].Action.RoomIndex -or
    $before.Energy -ne $actions[0].Before.Energy -or $after.Energy -ne $actions[0].Actual.Energy -or
    $before.Hand -ne @($actions[0].Before.Spawn.Train.Context.Cards.Hand).Count -or
    $after.Hand -ne @($actions[0].Actual.Spawn.Train.Context.Cards.Hand).Count) {
    throw 'Player Devote checkpoints disagree with the first native action and its verified successor.'
}
$files = @(Get-ChildItem -LiteralPath $Directory -File)
if ($files.Count -lt 1 -or $files.Count -gt 2 -or @($files | Where-Object {
    $_.Name -notin @('advice.jsonl', 'advice.previous.jsonl') -or $_.Length -gt 2MB
}).Count) { throw 'Devote advice history exceeds its bounded file set.' }
$records = @()
foreach ($name in @('advice.previous.jsonl', 'advice.jsonl')) {
    $path = Join-Path $Directory $name
    if (Test-Path -LiteralPath $path) {
        $records += @(Get-Content -LiteralPath $path -Encoding utf8 | ForEach-Object { $_ | ConvertFrom-Json -Depth 100 })
    }
}
$history = $Trace.PlayerAdvisor.Overlay.DiagnosticHistory
if ($history.Dropped -ne 0 -or $history.Error -or
    $history.WriterThreadId -lt 1 -or $history.WriterThreadId -eq $Trace.PlayerAdvisor.Overlay.MainThreadId -or
    @($records.Session | Sort-Object -Unique).Count -ne 1) {
    throw 'Devote advice journal lost records or did not use its independent writer.'
}
for ($i = 0; $i -lt $records.Count; $i++) {
    if ($records[$i].Schema -ne 1 -or $records[$i].DroppedRecords -ne 0 -or $records[$i].Sequence -ne $i + 1) {
        throw 'Devote advice journal has an invalid or missing sequence.'
    }
}
$devotes = @($records | Where-Object { $_.Entry.Kind -ceq 'devote-accepted' })
if ($devotes.Count -ne $actions.Count) { throw "Devote advice journal has $($devotes.Count)/$($actions.Count) native accepted inputs." }
for ($i = 0; $i -lt $devotes.Count; $i++) {
    if ($devotes[$i].Entry.Input.Operation -cne 'Devote' -or
        $devotes[$i].Entry.Input.TargetRoom -ne $actions[$i].Action.RoomIndex) {
        throw "Devote journal input $i disagrees with the accepted native action room."
    }
}
$firstSequence = $devotes[0].Sequence
$secondSequence = $devotes[1].Sequence
$oldAdvice = @($records | Where-Object { $_.Entry.Kind -ceq 'advice' -and $_.Sequence -lt $firstSequence })
$freshAdvice = @($records | Where-Object {
    $_.Entry.Kind -ceq 'advice' -and $_.Sequence -gt $firstSequence -and $_.Sequence -lt $secondSequence
})
if ($oldAdvice.Count -lt 1 -or $freshAdvice.Count -lt 1 -or
    $devotes[0].Entry.Generation -ne $before.Generation -or
    @($freshAdvice | Where-Object { $_.Entry.Generation -eq $after.Generation }).Count -lt 1) {
    throw 'Devote advice history lacks old advice, accepted boundary, and fresh settled advice in that order.'
}
Write-Output "PLAYER-ADVISOR-DEVOTE-PASS scope=controlled-helper-api-only nativeInputs=$($devotes.Count) oldGeneration=$($before.Generation) freshGeneration=$($after.Generation)"
