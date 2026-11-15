#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild, [switch] $LifecycleOnly, [switch] $SearchOnly, [switch] $DeepSearch)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
if (@($LifecycleOnly, $SearchOnly, $DeepSearch).Where({ $_ }).Count -gt 1) { throw 'Choose at most one scoped mode.' }
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/room-health-attack'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if ($manifest.Count -ne 1 -or $manifest[0].path -cne 'battle.mt2f') { throw 'Unexpected room-health aura manifest.' }
$path = Join-Path $directory 'battle.mt2f'
if ((Get-Item -LiteralPath $path).Length -ne [long]$manifest[0].bytes -or
    (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $manifest[0].sha256) { throw 'Room-health aura archive hash/size differs.' }
$checkArguments = @('--original-room-health-attack-only', $path)
if ($LifecycleOnly) { $checkArguments += '--lifecycle-only' }
if ($SearchOnly) { $checkArguments += '--search-only' }
if ($DeepSearch) { $checkArguments += '--deep-search' }
dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @checkArguments
if ($LASTEXITCODE) { throw 'Independent original room-health aura validation failed.' }
if ($SearchOnly) { Write-Output 'GAME-231-ROOM-HEALTH-ATTACK PASS: focused serial/parallel search only.' }
else { Write-Output ('GAME-231-ROOM-HEALTH-ATTACK PASS: original ranks, shared aura lifecycle and complete replay; ' +
    $(if ($LifecycleOnly) { 'frontier/search deferred in this run.' } else { 'all recorded frontiers and serial/parallel search passed.' })) }
