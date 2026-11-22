param(
    [Parameter(Mandatory)] [string] $Log,
    [Parameter(Mandatory)] [string] $Marker,
    [Parameter(Mandatory)] [int] $DelayMilliseconds,
    [string] $SaveOnSignal = '',
    [int] $Code = 0
)
[IO.File]::WriteAllText($Log, $Marker)
if ($SaveOnSignal) {
    $timer = [Diagnostics.Stopwatch]::StartNew()
    while ($timer.ElapsedMilliseconds -lt $DelayMilliseconds) {
        if (Test-Path -LiteralPath $SaveOnSignal) {
            [IO.File]::WriteAllText($Log + '.saved', 'preserved')
            exit $Code
        }
        Start-Sleep -Milliseconds 20
    }
} else { Start-Sleep -Milliseconds $DelayMilliseconds }
exit $Code
