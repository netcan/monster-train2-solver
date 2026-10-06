#requires -Version 7.4
$ErrorActionPreference = 'Stop'
$workspace = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$fixtures = @('full-battle-steward-once.json', 'full-battle-no-cards.json', 'full-battle-units-and-junk.json',
    'full-battle-units-spells-and-junk.json', 'full-battle-statistics.json.gz', 'full-battle-numeric-upgrades.json.gz',
    'full-battle-dynamic-upgrades.json.gz', 'full-battle-sacrifice-upgrades.json.gz',
    'full-battle-hand-upgrades.json.gz', 'full-battle-targeted-hand-upgrades.json.gz',
    'full-battle-healing.json.gz', 'full-battle-healing-triggers.json.gz', 'full-battle-room-spells.json.gz',
    'full-battle-terminal-spells.json.gz',
    'full-battle-post-kill-spells.json.gz',
    'full-battle-random-spells.json.gz',
    'full-battle-random-status.json.gz',
    'full-battle-cross-room-spells.json.gz', 'full-battle-cross-room-targets.json.gz',
    'full-battle-attack-buffs.json.gz',
    'full-battle-max-health-spells.json.gz', 'full-battle-max-health-lethal.json.gz',
    'card-modifier-calibration.json.gz', 'rng-calibration.json', 'gold-reward-calibration.json.gz',
    'standby-routing-calibration.json.gz', 'ui-rng-isolation-calibration.json.gz') |
    ForEach-Object { Join-Path $workspace ('results\' + $_) }
dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release -- @fixtures
if ($LASTEXITCODE -ne 0) { throw 'Independent model checks failed.' }
