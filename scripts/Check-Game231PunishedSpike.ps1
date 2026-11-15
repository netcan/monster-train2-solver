#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/punished-spike'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if ($manifest.Count -ne 1 -or $manifest[0].path -cne 'battle.mt2f') { throw 'Unexpected Punished Spike manifest.' }
$path = Join-Path $directory $manifest[0].path
if ((Get-Item -LiteralPath $path).Length -ne [long]$manifest[0].bytes -or
    (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $manifest[0].sha256) { throw 'Punished Spike archive hash/size differs.' }
dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --original-punished-spike-only $path
if ($LASTEXITCODE) { throw 'Independent original Punished Spike/chained spell validation failed.' }
Write-Output 'GAME-231-PUNISHED-SPIKE PASS: three unchanged originals, X zero/one/three, once-only zeal and repeated damage, armor/enemy/remote/lethal targets, Imprison natural ascent, complete chain/16 branches and zero-unsupported serial/parallel search.'
