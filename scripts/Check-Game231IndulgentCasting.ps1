#requires -Version 7.4
param([switch]$SkipBuild)

$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}

$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/indulgent-capacity'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
$birth = Join-Path $directory 'native-birth.mt2f'
$phases = Join-Path $directory 'native-casting-phases.mt2f'
foreach ($archive in $birth, $phases) {
    $entry = @($manifest | Where-Object path -CEQ (Split-Path $archive -Leaf))
    if ($entry.Count -ne 1 -or (Get-Item -LiteralPath $archive).Length -ne [long]$entry[0].bytes -or
        (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -ine $entry[0].sha256) {
        throw "Indulgent casting archive differs: $archive"
    }
}

$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
dotnet $checker --native-indulgent-trample-casting-only $birth $phases
if ($LASTEXITCODE) { throw 'Indulgent casting replay failed.' }
