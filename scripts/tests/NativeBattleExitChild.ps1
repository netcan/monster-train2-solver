param(
    [Parameter(Mandatory)] [string] $Log,
    [Parameter(Mandatory)] [string] $Marker,
    [Parameter(Mandatory)] [int] $DelayMilliseconds,
    [int] $Code = 0
)
[IO.File]::WriteAllText($Log, $Marker)
Start-Sleep -Milliseconds $DelayMilliseconds
exit $Code
