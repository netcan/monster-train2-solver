#requires -Version 7.4
[CmdletBinding()]
param(
    [switch] $SkipBuild,
    [ValidateSet('All', 'Component', 'Battle')]
    [string] $Case = 'All'
)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/health-percent'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if ($manifest.Count -ne 2 -or ($manifest.path -join ',') -cne 'component.mt2f,battle.mt2f') { throw 'Unexpected percentage health manifest.' }
foreach ($entry in $manifest) {
    $path = Join-Path $directory $entry.path
    if ((Get-Item -LiteralPath $path).Length -ne [long]$entry.bytes -or
        (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $entry.sha256) { throw 'Percentage health archive hash/size differs.' }
}
foreach ($entry in $manifest) {
    if ($Case -eq 'Component' -and $entry.path -eq 'battle.mt2f' -or $Case -eq 'Battle' -and $entry.path -eq 'component.mt2f') { continue }
    $flag = if ($entry.path -eq 'battle.mt2f') { '--original-arbitrary-verdict-only' } else { '--health-percent-only' }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') $flag (Join-Path $directory $entry.path)
    if ($LASTEXITCODE) { throw "Independent percentage health validation failed: $($entry.path)" }
}
Write-Output "GAME-231-HEALTH-PERCENT PASS: $Case; native raw setter, unchanged original spell and exact successors/RNG; retained nonpositive actor transitions remain guarded."
