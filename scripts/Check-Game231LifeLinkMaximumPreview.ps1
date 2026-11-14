#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild)

$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/lifelink-maximum-preview'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if ($manifest.Count -ne 1 -or $manifest[0].path -cne 'lifecycle.mt2f') { throw 'Unexpected LifeLink maximum-preview manifest.' }
$entry = $manifest[0]
$path = Join-Path $directory $entry.path
if ((Get-Item -LiteralPath $path).Length -ne [long]$entry.bytes -or
    (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $entry.sha256) { throw 'LifeLink maximum-preview archive hash/size differs.' }
dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --true-final-boss-maximum-preview-only $path
if ($LASTEXITCODE) { throw 'Independent LifeLink maximum-preview validation failed.' }
Write-Output 'GAME-231-LIFE-LINK-MAXIMUM-PREVIEW PASS: 465 real API calls, 30 temporary-copy sessions, full primary gameplay/gameplay-RNG cancellation and 16 immutable concurrent chains each; full card-hover/UI battle preview remains separate.'
