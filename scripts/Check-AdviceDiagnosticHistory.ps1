#requires -Version 7.4
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $Directory,
    [Parameter(Mandatory)] [int] $CardPlays,
    [Parameter(Mandatory)] [int] $EndTurns,
    [Parameter(Mandatory)] [int] $Publications,
    [Parameter(Mandatory)] [int] $MainThreadId,
    [Parameter(Mandatory)] [int] $WriterThreadId
)
$ErrorActionPreference = 'Stop'
$records = @()
$files = @(Get-ChildItem -LiteralPath $Directory -File)
if ($files.Count -lt 1 -or $files.Count -gt 2 -or @($files | Where-Object {
    $_.Name -notin @('advice.jsonl', 'advice.previous.jsonl') -or $_.Length -gt 2MB
}).Count) { throw 'Advice history exceeded its file names/count/size limit.' }
foreach ($name in @('advice.previous.jsonl', 'advice.jsonl')) {
    $path = Join-Path $Directory $name
    if (Test-Path -LiteralPath $path) {
        $records += @(Get-Content -LiteralPath $path -Encoding utf8 | ForEach-Object { $_ | ConvertFrom-Json -Depth 100 })
    }
}
if ($WriterThreadId -lt 1 -or $WriterThreadId -eq $MainThreadId -or
    @($records.Session | Sort-Object -Unique).Count -ne 1 -or
    @($records | Where-Object { $_.Schema -ne 1 -or $_.DroppedRecords -ne 0 }).Count) {
    throw 'Advice history lost records or did not use a separate writer thread.'
}
for ($index = 0; $index -lt $records.Count; $index++) {
    if ($records[$index].Sequence -ne $index + 1) { throw 'Advice history has missing or reordered events.' }
}
$entries = @($records.Entry)
$advice = @($entries | Where-Object Kind -EQ 'advice')
$cards = @($entries | Where-Object Kind -EQ 'card-accepted')
$turns = @($entries | Where-Object Kind -EQ 'end-turn-accepted')
if ($advice.Count -ne $Publications -or $cards.Count -ne $CardPlays -or $turns.Count -ne $EndTurns) {
    throw "Advice history differs from native evidence: advice=$($advice.Count)/$Publications plays=$($cards.Count)/$CardPlays EndTurns=$($turns.Count)/$EndTurns."
}
foreach ($entry in $advice) {
    $forecast = $entry.Forecast
    if (-not $entry.DisplayText -or $forecast.Steps.Count -ne [Math]::Min(256, $forecast.PlanStepCount) -or
        $forecast.FutureLoss -ne [Math]::Max(0, $entry.CurrentPyreHp - $forecast.ForecastPyreHp) -or
        $forecast.Terminal -ne ($forecast.Outcome -in @('BattleWon', 'PlayerDefeated', 'Stalemate')) -or
        (-not $forecast.Terminal -and -not $entry.DisplayText.Contains('整场战损尚未确定'))) {
        throw 'Advice history lacks complete step identities or forecast scope/health evidence.'
    }
}
foreach ($entry in $cards) {
    if (-not $entry.Input.CardDataId -or -not $entry.Input.CardIdentity -or
        ($null -eq $entry.Input.CardInstanceId -and $null -eq $entry.Input.SourceUnitId -and $entry.Input.SourceRoom -lt 0)) {
        throw 'Accepted native input lacks a card/ability identity.'
    }
}
if (@($entries | Where-Object Kind -NotIn @('advice', 'view', 'reset', 'card-accepted', 'end-turn-accepted')).Count) {
    throw 'Advice history contains an unexpected event (floor browsing must not write events).'
}
Write-Output "PLAYER-ADVICE-HISTORY-PASS advice=$($advice.Count) plays=$($cards.Count) EndTurns=$($turns.Count) dropped=0 backgroundWriter=$WriterThreadId"
