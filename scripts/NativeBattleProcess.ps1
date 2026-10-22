# Shared process deadline handling; native capture acceptance remains in the caller.
function Wait-NativeBattleExit {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [Diagnostics.Process] $Process,
        [Parameter(Mandatory)] [Diagnostics.Stopwatch] $Timer,
        [Parameter(Mandatory)] [string] $UnityLog,
        [Parameter(Mandatory)] [string] $Profile,
        [Parameter(Mandatory)] [ValidateRange(1, 3600)] [int] $TimeoutSeconds,
        [ValidateRange(1, 60)] [int] $TerminalExitGraceSeconds = 60,
        [ValidateRange(10, 1000)] [int] $PollMilliseconds = 250,
        [switch] $FailFastOnIsolatedUnitReject
    )
    $exitDeadlineSeconds = $TimeoutSeconds
    $terminalExitGrace = $false
    while (-not $Process.WaitForExit($PollMilliseconds)) {
        if ($FailFastOnIsolatedUnitReject -and (Test-Path -LiteralPath $UnityLog) -and
            (Select-String -LiteralPath $UnityLog -Pattern 'ORIGINAL-ISOLATED-(UNIT|EQUIPMENT|SPELL)-REJECT' -Quiet)) {
            $Process.Kill()
            $Process.WaitForExit()
            throw "Isolated card model preflight rejected a requested card; native run stopped early; inspect $UnityLog"
        }
        if ($Timer.Elapsed.TotalSeconds -lt $exitDeadlineSeconds) { continue }
        # Late-game trace export and Unity teardown can cross the battle deadline.
        # Grant exactly one bounded extension after a captured terminal result.
        if (-not $terminalExitGrace -and (Test-Path -LiteralPath $UnityLog) -and
            (Select-String -LiteralPath $UnityLog -Pattern 'BATTLE-TERMINAL won=(True|False) pyre=' -Quiet)) {
            $terminalExitGrace = $true
            $exitDeadlineSeconds = $TimeoutSeconds + $TerminalExitGraceSeconds
            Write-Output "FULL-BATTLE-EXIT-GRACE terminalCaptured=True seconds=$TerminalExitGraceSeconds"
            continue
        }
        $Process.Kill()
        $Process.WaitForExit()
        throw "Full battle timed out after $exitDeadlineSeconds seconds; profile: $Profile"
    }
}
