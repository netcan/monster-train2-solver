#requires -Version 7.4
$ErrorActionPreference = 'Stop'
$workspace = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$fixtures = @('full-battle-steward-once.json', 'full-battle-no-cards.json', 'full-battle-units-and-junk.json',
    'full-battle-units-spells-and-junk.json', 'full-battle-statistics.json.gz', 'full-battle-numeric-upgrades.json.gz',
    'full-battle-dynamic-upgrades.json.gz', 'full-battle-sacrifice-upgrades.json.gz',
    'card-modifier-calibration.json.gz', 'rng-calibration.json') |
    ForEach-Object { Join-Path $workspace ('results\' + $_) }
dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release -- @fixtures
if ($LASTEXITCODE -ne 0) { throw 'Independent model checks failed.' }
