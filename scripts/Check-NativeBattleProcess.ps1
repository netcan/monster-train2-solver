#requires -Version 7.4
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'NativeBattleProcess.ps1')
$workspace = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$profile = Join-Path $workspace ('.probe-runs/native-exit-check-' + [Guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $profile
$child = Join-Path $PSScriptRoot 'tests/NativeBattleExitChild.ps1'
$pwsh = (Get-Process -Id $PID).Path
$cases = @(
    @{ Name = 'normal'; Marker = 'ready'; Delay = 200; Grace = $false; Timeout = $false; Code = 0 },
    @{ Name = 'terminal-win'; Marker = 'BATTLE-TERMINAL won=True pyre=80'; Delay = 1600; Grace = $true; Timeout = $false; Code = 0 },
    @{ Name = 'terminal-loss'; Marker = 'BATTLE-TERMINAL won=False pyre=0'; Delay = 1600; Grace = $true; Timeout = $false; Code = 0 },
    @{ Name = 'nonzero-terminal-exit'; Marker = 'BATTLE-TERMINAL won=True pyre=80'; Delay = 1600; Grace = $true; Timeout = $false; Code = 7 },
    @{ Name = 'unterminated'; Marker = 'ready'; Delay = 10000; Grace = $false; Timeout = $true; Code = 0 },
    @{ Name = 'terminal-stuck'; Marker = 'BATTLE-TERMINAL won=True pyre=80'; Delay = 10000; Grace = $true; Timeout = $true; Code = 0 },
    @{ Name = 'isolated-reject'; Marker = 'ORIGINAL-ISOLATED-UNIT-REJECT'; Delay = 10000; Grace = $false; Timeout = $false; Code = 0 }
)
foreach ($case in $cases) {
    $log = Join-Path $profile ($case.Name + '.log')
    $arguments = @('-NoProfile', '-File', ('"' + $child + '"'), '-Log', ('"' + $log + '"'),
        '-Marker', ('"' + $case.Marker + '"'), '-DelayMilliseconds', $case.Delay, '-Code', $case.Code)
    $process = Start-Process -FilePath $pwsh -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $timer = [Diagnostics.Stopwatch]::StartNew()
    try {
        # Exclude interpreter startup from these deliberately short deadlines.
        while (-not (Test-Path -LiteralPath $log) -and -not $process.HasExited -and $timer.Elapsed.TotalSeconds -lt 10) {
            Start-Sleep -Milliseconds 10
        }
        if (-not (Test-Path -LiteralPath $log)) { throw 'Child process did not reach its controlled boundary.' }
        $timer.Restart()
        $events = [Collections.Generic.List[string]]::new()
        $errorText = $null
        try {
            Wait-NativeBattleExit -Process $process -Timer $timer -UnityLog $log -Profile $profile `
                -TimeoutSeconds 1 -TerminalExitGraceSeconds 1 -PollMilliseconds 20 `
                -FailFastOnIsolatedUnitReject:($case.Name -eq 'isolated-reject') | ForEach-Object { $events.Add($_) }
        } catch { $errorText = $_.Exception.Message }
        $timer.Stop()
        if (-not $process.HasExited -or $events.Count -ne [int]$case.Grace) { throw "Unexpected exit/grace count: $($case.Name)" }
        if ($case.Timeout) {
            $deadline = 1 + [int]$case.Grace
            if ($errorText -notlike "Full battle timed out after $deadline seconds*" -or
                $timer.Elapsed.TotalSeconds -gt $deadline + 1) { throw "Timeout did not remain bounded: $($case.Name)" }
        } elseif ($case.Name -eq 'isolated-reject') {
            if ($errorText -notlike 'Isolated card model preflight rejected*' -or $timer.Elapsed.TotalSeconds -ge 1) {
                throw 'Isolated rejection was not stopped immediately.'
            }
        } elseif ($null -ne $errorText -or $process.ExitCode -ne $case.Code) {
            throw "Exit code was lost or ordinary teardown failed: $($case.Name): $errorText"
        }
        Write-Output "NATIVE-EXIT-CASE PASS name=$($case.Name) graceCount=$($events.Count) exit=$($process.ExitCode)"
    } finally {
        if (-not $process.HasExited) { $process.Kill(); $process.WaitForExit() }
        $process.Dispose()
    }
}
Write-Output "NATIVE-EXIT-CHECK PASS cases=$($cases.Count) terminalGraceBounded=True nonzeroExitPreserved=True profile=$profile"
