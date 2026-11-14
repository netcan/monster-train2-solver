#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild)

$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/maximum-health-lethal-preview'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if ($manifest.Count -ne 1 -or $manifest[0].path -cne 'lifecycle.mt2f') { throw 'Unexpected lethal maximum-health hover manifest.' }
$entry = $manifest[0]
$path = Join-Path $directory $entry.path
if ((Get-Item -LiteralPath $path).Length -ne [long]$entry.bytes -or
    (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $entry.sha256) { throw 'Lethal maximum-health hover archive hash/size differs.' }
dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --maximum-health-lethal-preview-only $path
if ($LASTEXITCODE) { throw 'Independent lethal maximum-health hover validation failed.' }
Write-Output 'GAME-231-MAXIMUM-HEALTH-LETHAL-PREVIEW PASS: 16 ordinary/Horde/Undying/half-health hovers, full temporary targets and copy/effect/cancel states, gameplay RNG and 16 immutable branches per root; linked terminal and other hover callbacks remain separate.'
