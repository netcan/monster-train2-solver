#requires -Version 7.4
param(
    [string[]]$Fixture = @(),
    [ValidateRange(1, 32)][int]$Workers = 1,
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'FixturePaths.ps1')
$workspace = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$fixtureNames = @('full-battle-event-grafted-equipment.mt2f', 'titanite-calibration.mt2f',
    'player-untouchable-troop-aura-counterexample.mt2f',
    'player-untouchable-troop-aura-observation.mt2f',
    'player-dusk-mantle-recruit-boss-preview-calibration.mt2f',
    'player-dusk-mantle-recruit-boss-preview-replay-calibration.mt2f',
    'player-underlegion-third-troop-preview-calibration.mt2f',
    'player-underlegion-third-troop-preview-replay-calibration.mt2f',
    'parallel-lower-opening-model-counterexample.mt2f',
    'parallel-lower-opening-route-model-calibration.mt2f',
    'player-generated-moon-first-transition-calibration.mt2f',
    'player-generated-moon-first-transition-replay-calibration.mt2f',
    'player-generated-moon-tracking-calibration.mt2f',
    'player-generated-moon-retired-enemies-calibration.mt2f',
    'player-generated-moon-retired-enemies-replay-calibration.mt2f',
    'player-generated-moon-mixed-retired-calibration.mt2f',
    'player-generated-moon-mixed-retired-replay-calibration.mt2f',
    'player-generated-moon-ten-retired-calibration.mt2f',
    'player-generated-moon-ten-retired-replay-calibration.mt2f',
    'player-generated-moon-tracking-replay-calibration.mt2f',
    'player-inactive-moon-room-attachment-calibration.mt2f',
    'player-room-pre-combat-world-root.mt2f',
    'player-green-recruit-boss-preview-calibration.mt2f',
    'player-green-recruit-boss-preview-replay-calibration.mt2f',
    'player-inferno-removal-queue-calibration.mt2f',
    'player-inferno-removal-queue-replay-calibration.mt2f',
    'player-champion-meat-shield-boss-preview-calibration.mt2f',
    'player-champion-meat-shield-boss-preview-replay-calibration.mt2f',
    'player-champion-meat-shield-preview-calibration.mt2f',
    'player-champion-meat-shield-preview-replay-calibration.mt2f',
    'full-battle-random-campaign-01-ring1-search.mt2f',
    'full-battle-random-campaign-01-ring1-replay.mt2f',
    'full-battle-random-campaign-12-ring1-release112-budget10000-search.mt2f',
    'full-battle-random-campaign-12-ring1-release112-budget10000-replay.mt2f',
    'full-battle-random-campaign-12-ring2-release112-budget10000-search.mt2f',
    'full-battle-random-campaign-12-ring2-release112-budget10000-replay.mt2f',
    'full-battle-random-campaign-12-ring3-release112-budget10000-search.mt2f',
    'full-battle-random-campaign-12-ring3-release112-budget10000-replay.mt2f',
    'full-battle-random-campaign-12-ring4-release112-budget10000-search.mt2f',
    'full-battle-random-campaign-12-ring4-release112-budget10000-replay.mt2f',
    'full-battle-random-campaign-12-ring5-release112-budget10000-search.mt2f',
    'full-battle-random-campaign-12-ring5-release112-budget10000-replay.mt2f',
    'full-battle-random-campaign-12-ring6-release112-budget10000-loss.mt2f',
    'full-battle-random-campaign-12-ring6-refined-search.mt2f',
    'full-battle-random-campaign-12-ring6-refined-replay.mt2f',
    'full-battle-random-campaign-12-ring7-refined-search.mt2f',
    'full-battle-random-campaign-12-ring7-refined-replay.mt2f',
    'full-battle-random-campaign-12-ring8-refined-search.mt2f',
    'full-battle-random-campaign-12-ring8-refined-replay.mt2f',
    'full-battle-random-campaign-12-ring1-release114-budget10000-search.mt2f',
    'full-battle-random-campaign-12-ring1-release114-budget10000-replay.mt2f',
    'full-battle-random-campaign-12-ring6-release113-budget10000-loss.mt2f',
    'full-battle-random-campaign-12-ring7-release113-budget10000-loss.mt2f',
    'full-battle-random-campaign-12-ring8-release115-budget10000-loss.mt2f',
    'full-battle-random-campaign-13-ring1-release114-budget10000-search.mt2f',
    'full-battle-random-campaign-13-ring1-release114-budget10000-replay.mt2f',
    'full-battle-random-campaign-13-ring2-release114-budget10000-search.mt2f',
    'full-battle-random-campaign-13-ring2-release114-budget10000-replay.mt2f',
    'full-battle-random-campaign-13-ring3-refined-search.mt2f',
    'full-battle-random-campaign-13-ring3-refined-replay.mt2f',
    'full-battle-random-campaign-13-ring4-release119-budget10000-search.mt2f',
    'full-battle-random-campaign-13-ring4-release119-budget10000-replay.mt2f',
    'full-battle-random-campaign-13-ring5-release119-budget10000-loss.mt2f',
    'full-battle-random-campaign-13-ring5-refined-search.mt2f',
    'full-battle-random-campaign-13-ring5-refined-replay.mt2f',
    'full-battle-random-campaign-13-ring5-release122-refined-search.mt2f',
    'full-battle-random-campaign-13-ring5-release122-refined-replay.mt2f',
    'full-battle-random-campaign-13-ring6-release122-budget10000-loss.mt2f',
    'full-battle-random-campaign-13-ring6-release127-budget10000-loss.mt2f',
    'full-battle-random-campaign-13-ring6-release128-refined-search.mt2f',
    'full-battle-random-campaign-13-ring6-release128-refined-replay.mt2f',
    'full-battle-random-campaign-13-ring6-release134-refined-search.mt2f',
    'full-battle-random-campaign-13-ring6-release134-refined-replay.mt2f',
    'full-battle-random-campaign-13-ring6-release137-refined-search.mt2f',
    'full-battle-random-campaign-13-ring6-release137-refined-replay.mt2f',
    'full-battle-random-campaign-13-ring7-release128-budget10000-loss.mt2f',
    'full-battle-random-campaign-13-ring7-release131-budget10000-loss.mt2f',
    'full-battle-random-campaign-13-ring7-release132-budget10000-loss.mt2f',
    'full-battle-random-campaign-13-ring7-release134-budget10000-loss.mt2f',
    'full-battle-random-campaign-13-ring7-release136-budget10000-loss.mt2f',
    'full-battle-random-campaign-13-ring7-release137-budget10000-loss.mt2f',
    'full-battle-random-campaign-13-ring7-release137-refined-loss.mt2f',
    'full-battle-random-campaign-13-ring7-release138-budget10000-loss.mt2f',
    'pending-boss-loss-order-model-calibration.mt2f',
    'uninjured-pending-boss-model-counterexample-calibration.mt2f',
    'uninjured-pending-boss-model-calibration.mt2f',
    'player-resumed-moon-capture-counterexample.mt2f',
    'full-battle-random-campaign-14-ring1-release138-budget10000-search.mt2f',
    'full-battle-random-campaign-14-ring1-release138-budget10000-replay.mt2f',
    'full-battle-random-campaign-14-ring2-release143-budget10000-search.mt2f',
    'full-battle-random-campaign-14-ring2-release143-budget10000-replay.mt2f',
    'full-battle-random-campaign-14-ring3-release148-budget10000-loss.mt2f',
    'full-battle-random-campaign-14-ring3-release149-refined-search.mt2f',
    'full-battle-random-campaign-14-ring3-release149-refined-replay.mt2f',
    'full-battle-random-campaign-14-ring4-release150-budget10000-search.mt2f',
    'full-battle-random-campaign-14-ring4-release150-budget10000-replay.mt2f',
    'full-battle-random-campaign-14-ring5-release151-budget10000-search.mt2f',
    'full-battle-random-campaign-14-ring5-release151-budget10000-replay.mt2f',
    'full-battle-random-campaign-14-ring5-release165-budget10000-search.mt2f',
    'full-battle-random-campaign-14-ring5-release165-budget10000-replay.mt2f',
    'full-battle-random-campaign-15-ring1-release165-budget10000-search.mt2f',
    'full-battle-random-campaign-15-ring1-release165-budget10000-replay.mt2f',
    'full-battle-random-campaign-15-ring2-release166-budget10000-search.mt2f',
    'full-battle-random-campaign-15-ring2-release166-budget10000-replay.mt2f',
    'full-battle-random-campaign-15-ring3-release167-budget10000-search.mt2f',
    'full-battle-random-campaign-15-ring3-release167-budget10000-replay.mt2f',
    'full-battle-random-campaign-15-ring4-release167-budget10000-search.mt2f',
    'full-battle-random-campaign-15-ring4-release167-budget10000-replay.mt2f',
    'full-battle-random-campaign-15-ring5-release167-budget10000-search.mt2f',
    'full-battle-random-campaign-15-ring5-release167-budget10000-replay.mt2f',
    'full-battle-random-campaign-15-ring6-release167-budget10000-search.mt2f',
    'full-battle-random-campaign-15-ring6-release167-budget10000-replay.mt2f',
    'full-battle-random-campaign-15-ring7-release167-budget10000-loss.mt2f',
    'full-battle-random-campaign-16-ring1-release169-budget10000-search.mt2f',
    'full-battle-random-campaign-16-ring1-release169-budget10000-replay.mt2f',
    'full-battle-random-campaign-16-ring2-release171-budget10000-search.mt2f',
    'full-battle-random-campaign-16-ring2-release171-budget10000-replay.mt2f',
    'full-battle-random-campaign-16-ring3-release172-budget10000-search.mt2f',
    'full-battle-random-campaign-16-ring3-release172-budget10000-replay.mt2f',
    'full-battle-random-campaign-16-ring4-release174-budget10000-search.mt2f',
    'full-battle-random-campaign-16-ring4-release174-budget10000-replay.mt2f',
    'full-battle-random-campaign-17-ring1-covenant3-release174-budget10000-search.mt2f',
    'full-battle-random-campaign-17-ring1-covenant3-release174-budget10000-replay.mt2f',
    'full-battle-random-campaign-17-ring2-covenant3-release174-budget10000-search.mt2f',
    'full-battle-random-campaign-17-ring2-covenant3-release174-budget10000-replay.mt2f',
    'player-ability-presentation-mutation-calibration.mt2f',
    'player-ability-cache-display-replay-calibration.mt2f',
    'player-ability-cache-winning-continuation-calibration.mt2f',
    'full-battle-random-campaign-17-ring3-release178-offline-search.mt2f',
    'full-battle-random-campaign-17-ring3-release178-offline-replay.mt2f',
    'full-battle-random-campaign-17-ring4-release178-budget10000-search.mt2f',
    'full-battle-random-campaign-17-ring4-release178-budget10000-replay.mt2f',
    'full-battle-random-campaign-17-ring5-release178-budget10000-search.mt2f',
    'full-battle-random-campaign-17-ring5-release178-budget10000-replay.mt2f',
    'full-battle-random-campaign-17-ring6-release178-budget10000-loss.mt2f',
    'full-battle-random-campaign-18-ring1-release182-budget10000-search.mt2f',
    'full-battle-random-campaign-18-ring1-release182-budget10000-replay.mt2f',
    'full-battle-random-campaign-18-ring2-release182.mt2f',
    'full-battle-random-campaign-18-ring2-release182-replay.mt2f',
    'full-battle-random-campaign-16-ring6-release183-budget10000-search.mt2f',
    'full-battle-random-campaign-16-ring6-release183-budget10000-replay.mt2f',
    'full-battle-random-campaign-16-ring7-release183-budget10000-loss.mt2f',
    'player-untouchable-front-relic-counterexample-calibration.mt2f',
    'player-stasis-shatter-counterexample-calibration.mt2f',
    'player-stasis-shatter-calibration.mt2f',
    'player-stasis-shatter-replay-calibration.mt2f',
    'player-triple-juice-spike-refusal-calibration.mt2f',
    'player-double-juice-spike-refusal-calibration.mt2f',
    'player-double-juice-spike-calibration.mt2f',
    'player-double-juice-spike-replay-calibration.mt2f',
    'player-double-juice-curse-calibration.mt2f',
    'player-double-juice-curse-replay-calibration.mt2f',
    'player-room-upgrade-provenance-counterexample-calibration.mt2f',
    'player-room-upgrade-provenance-calibration.mt2f',
    'full-battle-random-campaign-18-ring4-release189-budget10000-search.mt2f',
    'full-battle-random-campaign-18-ring4-release189-budget10000-replay.mt2f',
    'full-battle-random-campaign-18-ring5-release190-budget10000-search.mt2f',
    'full-battle-random-campaign-18-ring5-release190-budget10000-replay.mt2f',
    'full-battle-random-campaign-16-ring8-release190-budget10000-loss.mt2f',
    'full-battle-random-campaign-16-ring8-release191-budget10000-loss.mt2f',
    'full-battle-random-campaign-18-ring7-release192-budget10000-loss.mt2f',
    'full-battle-random-campaign-18-ring7-release200-refined-search.mt2f',
    'full-battle-random-campaign-18-ring7-release200-refined-replay.mt2f',
    'full-battle-random-campaign-19-ring3-release200-budget10000-loss.mt2f',
    'full-battle-random-campaign-18-ring8-release200-budget10000-loss.mt2f',
    'full-battle-random-campaign-19-ring3-release200-refined-search.mt2f',
    'full-battle-random-campaign-19-ring3-release200-refined-replay.mt2f',
    'full-battle-random-campaign-19-ring4-release202-budget10000-search.mt2f',
    'full-battle-random-campaign-19-ring4-release202-budget10000-replay.mt2f',
    'full-battle-random-campaign-19-ring5-release202-budget10000-loss.mt2f',
    'full-battle-random-campaign-19-ring5-release207-budget10000-loss.mt2f',
    'full-battle-random-campaign-19-ring4-release207-refined-search.mt2f',
    'full-battle-random-campaign-19-ring4-release207-refined-replay.mt2f',
    'full-battle-random-campaign-19-ring5-release208-refined-search.mt2f',
    'full-battle-random-campaign-19-ring5-release208-refined-replay.mt2f',
    'full-battle-random-campaign-19-ring5-release208-budget10000-pyre44-loss.mt2f',
    'full-battle-random-campaign-19-ring6-release209-budget10000-pyre12-loss.mt2f',
    'full-battle-random-campaign-19-ring5-release209-budget10000-pyre44-search.mt2f',
    'full-battle-random-campaign-19-ring5-release209-budget10000-pyre44-replay.mt2f',
    'full-battle-random-campaign-19-ring5-release209-refined-pyre40-search.mt2f',
    'full-battle-random-campaign-19-ring5-release209-refined-pyre40-replay.mt2f',
    'full-battle-random-campaign-19-ring6-release209-budget10000-pyre26-loss.mt2f',
    'full-battle-random-campaign-19-ring5-release210-budget10000-pyre44-search.mt2f',
    'full-battle-random-campaign-19-ring5-release210-budget10000-pyre44-replay.mt2f',
    'full-battle-random-campaign-19-ring5-release212-budget10000-pyre44-search.mt2f',
    'full-battle-random-campaign-19-ring5-release212-budget10000-pyre44-replay.mt2f',
    'full-battle-random-campaign-19-ring6-release212-budget10000-loss.mt2f',
    'full-battle-random-campaign-19-ring6-release210-budget10000-pyre26-loss.mt2f',
    'full-battle-random-campaign-19-ring6-release210-budget10000-pyre40-loss.mt2f',
    'full-battle-random-campaign-20-ring1-release212-budget10000-search.mt2f',
    'full-battle-random-campaign-20-ring1-release212-budget10000-replay.mt2f',
    'full-battle-random-campaign-20-ring2-release212-budget10000-search.mt2f',
    'full-battle-random-campaign-20-ring2-release212-budget10000-replay.mt2f',
    'full-battle-random-campaign-20-ring3-release215-budget10000-search.mt2f',
    'full-battle-random-campaign-20-ring3-release215-budget10000-replay.mt2f',
    'full-battle-random-campaign-20-ring4-release215-budget10000-search.mt2f',
    'full-battle-random-campaign-20-ring4-release215-budget10000-replay.mt2f',
    'full-battle-random-campaign-20-ring5-release215-budget10000-search.mt2f',
    'full-battle-random-campaign-20-ring5-release215-budget10000-replay.mt2f',
    'full-battle-random-campaign-20-ring5-release223-budget10000-loss.mt2f',
    'full-battle-random-campaign-20-ring6-release223-budget10000-loss.mt2f',
    'full-battle-random-campaign-21-ring3-release215-budget10000-loss.mt2f',
    'full-battle-random-campaign-21-ring3-release228-refined-loss.mt2f',
    'full-battle-random-campaign-22-ring1-release228-budget10000-search.mt2f',
    'full-battle-random-campaign-22-ring1-release228-budget10000-replay.mt2f',
    'full-battle-random-campaign-22-ring2-release228-budget10000-search.mt2f',
    'full-battle-random-campaign-22-ring2-release228-budget10000-replay.mt2f',
    'full-battle-random-campaign-22-ring3-release232-budget10000-search.mt2f',
    'full-battle-random-campaign-22-ring3-release232-budget10000-replay.mt2f',
    'full-battle-random-campaign-22-ring4-release233-budget10000-search.mt2f',
    'full-battle-random-campaign-22-ring4-release233-budget10000-replay.mt2f',
    'full-battle-random-campaign-22-ring5-release235-budget10000-search.mt2f',
    'full-battle-random-campaign-22-ring5-release235-budget10000-replay.mt2f',
    'full-battle-random-campaign-22-ring6-release235-budget10000-loss.mt2f',
    'full-battle-random-campaign-22-ring4-release236-budget10000-search.mt2f',
    'full-battle-random-campaign-22-ring4-release236-budget10000-replay.mt2f',
    'full-battle-random-campaign-22-ring4-release237-budget10000-search.mt2f',
    'full-battle-random-campaign-22-ring4-release237-budget10000-replay.mt2f',
    'full-battle-random-campaign-22-ring5-release237-budget10000-loss.mt2f',
    'full-battle-random-campaign-22-ring5-release236-budget10000-search.mt2f',
    'full-battle-random-campaign-22-ring5-release236-budget10000-replay.mt2f',
    'full-battle-random-campaign-22-ring6-release236-budget10000-loss.mt2f',
    'full-battle-random-campaign-22-ring6-release238-budget10000-loss.mt2f',
    'full-battle-random-campaign-22-ring5-release238-budget10000-loss.mt2f',
    'full-battle-random-campaign-23-ring1-release240-budget10000-search.mt2f',
    'full-battle-random-campaign-23-ring1-release240-budget10000-replay.mt2f',
    'full-battle-random-campaign-23-ring2-release241-budget10000-search.mt2f',
    'full-battle-random-campaign-23-ring2-release241-budget10000-replay.mt2f',
    'full-battle-random-campaign-23-ring3-release245-budget10000-search.mt2f',
    'full-battle-random-campaign-23-ring3-release245-budget10000-replay.mt2f',
    'full-battle-random-campaign-23-ring4-release245-budget10000-search.mt2f',
    'full-battle-random-campaign-23-ring4-release245-budget10000-replay.mt2f',
    'full-battle-random-campaign-23-ring5-release247-budget10000-loss.mt2f',
    'full-battle-random-campaign-23-ring5-release249-budget10000-search.mt2f',
    'full-battle-random-campaign-23-ring5-release249-budget10000-replay.mt2f',
    'full-battle-random-campaign-23-ring6-release254-budget10000-loss.mt2f',
    'full-battle-random-campaign-23-ring6-release256-budget10000-settled-loss.mt2f',
    'full-battle-random-campaign-23-ring6-release256-budget10000-settled-loss-replay.mt2f',
    'full-battle-random-campaign-24-ring1-release259-budget10000-search.mt2f',
    'full-battle-random-campaign-24-ring1-release259-budget10000-replay.mt2f',
    'full-battle-random-campaign-24-ring2-release260-budget10000-search.mt2f',
    'full-battle-random-campaign-24-ring2-release260-budget10000-replay.mt2f',
    'full-battle-random-campaign-24-ring3-release260-budget10000-search.mt2f',
    'full-battle-random-campaign-24-ring3-release260-budget10000-replay.mt2f',
    'full-battle-random-campaign-24-ring4-release260-budget10000-search.mt2f',
    'full-battle-random-campaign-24-ring4-release260-budget10000-replay.mt2f',
    'full-battle-random-campaign-24-ring5-release261-budget10000-search.mt2f',
    'full-battle-random-campaign-24-ring5-release261-budget10000-replay.mt2f',
    'full-battle-random-campaign-24-ring6-release261-budget10000-settled-loss.mt2f',
    'full-battle-random-campaign-24-ring6-release261-budget10000-settled-loss-replay.mt2f',
    'full-battle-random-campaign-25-ring3-release264-budget10000-settled-loss.mt2f',
    'full-battle-random-campaign-25-ring3-release264-budget10000-settled-loss-replay.mt2f',
    'full-battle-random-campaign-24-ring6-refined261-search.mt2f',
    'full-battle-random-campaign-24-ring6-refined261-replay.mt2f',
    'full-battle-random-campaign-24-ring7-release265-budget10000-search.mt2f',
    'full-battle-random-campaign-24-ring7-release265-budget10000-replay.mt2f',
    'full-battle-random-campaign-24-ring7-refined267-search.mt2f',
    'full-battle-random-campaign-24-ring7-refined267-replay.mt2f',
    'full-battle-random-campaign-24-ring8-release265-budget10000-settled-loss.mt2f',
    'full-battle-random-campaign-24-ring8-release265-budget10000-settled-loss-replay.mt2f',
    'full-battle-random-campaign-25-ring1-release262-budget10000-search.mt2f',
    'full-battle-random-campaign-25-ring1-release262-budget10000-replay.mt2f',
    'full-battle-random-campaign-25-ring2-release263-budget10000-search.mt2f',
    'full-battle-random-campaign-25-ring2-release263-budget10000-replay.mt2f',
    'full-battle-random-campaign-25-ring3-release266-budget10000-search.mt2f',
    'full-battle-random-campaign-25-ring3-release266-budget10000-replay.mt2f',
    'full-battle-random-campaign-25-ring4-release267-budget10000-search.mt2f',
    'full-battle-random-campaign-25-ring4-release267-budget10000-replay.mt2f',
    'full-battle-random-campaign-25-ring5-release268-budget10000-settled-loss.mt2f',
    'full-battle-random-campaign-24-ring8-release269-pyre63-budget10000-settled-loss.mt2f',
    'full-battle-random-campaign-24-ring8-release269-pyre63-budget10000-settled-loss-replay.mt2f',
    'full-battle-random-campaign-25-ring5-release268-budget10000-settled-loss-replay.mt2f',
    'full-battle-random-campaign-25-ring5-release273-budget10000-search.mt2f',
    'full-battle-random-campaign-25-ring5-release273-budget10000-replay.mt2f',
    'full-battle-random-campaign-25-ring6-release278-budget10000-search.mt2f',
    'full-battle-random-campaign-25-ring6-release278-budget10000-replay.mt2f',
    'full-battle-random-campaign-25-ring7-release288-budget10000-search.mt2f',
    'full-battle-random-campaign-25-ring7-release288-budget10000-replay.mt2f',
    'full-battle-random-campaign-23-ring5-refined248-search.mt2f',
    'full-battle-random-campaign-23-ring5-refined248-replay.mt2f',
    'player-underlegion-troop-upgrade-calibration.mt2f',
    'player-underlegion-troop-upgrade-replay-calibration.mt2f',
    'player-underlegion-second-troop-upgrade-calibration.mt2f',
    'player-underlegion-third-troop-upgrade-calibration.mt2f',
    'player-terminal-pyre-capture-calibration.mt2f',
    'player-terminal-pyre-capture-replay-calibration.mt2f',
    'player-terminal-pyre-capture-raw-replay-calibration.mt2f',
    'player-underlegion-third-troop-upgrade-replay-calibration.mt2f',
    'player-underlegion-second-troop-upgrade-replay-calibration.mt2f',
    'player-moon-trial-gold-relic-calibration.mt2f',
    'player-moon-trial-gold-relic-replay-calibration.mt2f',
    'player-consumed-curse-cleanse-calibration.mt2f',
    'player-consumed-curse-cleanse-replay-calibration.mt2f',
    'player-consumed-curse-cleanse-terminal-calibration.mt2f',
    'player-triple-juice-starstruck-calibration.mt2f',
    'player-triple-juice-starstruck-replay-calibration.mt2f',
    'player-triple-juice-starstruck-cast-calibration.mt2f',
    'player-triple-juice-starstruck-cast-replay-calibration.mt2f',
    'player-triple-juice-starstruck-curse-calibration.mt2f',
    'player-triple-juice-starstruck-curse-replay-calibration.mt2f',
    'player-unit-copy-birth-catalog-calibration.mt2f',
    'player-unit-copy-birth-catalog-replay-calibration.mt2f',
    'player-green-recruit-preview-retirement-calibration.mt2f',
    'player-reanimate-terminal-point-calibration.mt2f',
    'player-moon-advance-consume-calibration.mt2f',
    'player-moon-advance-consume-replay-calibration.mt2f',
    'player-starstruck-reduce-cost-calibration.mt2f',
    'player-starstruck-reduce-cost-replay-calibration.mt2f',
    'player-starstruck-reduce-cost-cast-calibration.mt2f',
    'player-starstruck-reduce-cost-cast-replay-calibration.mt2f',
    'player-starstruck-conduit-calibration.mt2f',
    'player-starstruck-retired-target-calibration.mt2f',
    'player-starstruck-retired-target-replay-calibration.mt2f',
    'player-mirrored-meddler-moon-calibration.mt2f',
    'player-mirrored-meddler-moon-replay-calibration.mt2f',
    'player-mirrored-meddler-moon-coherent-calibration.mt2f',
    'player-discard-equipment-turn-calibration.mt2f',
    'player-discard-equipment-turn-replay-calibration.mt2f',
    'player-titanskin-hit-calibration.mt2f',
    'player-titanskin-hit-replay-calibration.mt2f',
    'player-treasure-despawn-movement-calibration.mt2f',
    'player-treasure-despawn-movement-replay-calibration.mt2f',
    'player-champion-enemy-death-preview-calibration.mt2f',
    'player-champion-enemy-death-preview-replay-calibration.mt2f',
    'player-steward-whelp-preview-id-calibration.mt2f',
    'player-steward-whelp-preview-id-replay-calibration.mt2f',
    'player-steward-fan-six-horde-preview-calibration.mt2f',
    'player-steward-fan-six-horde-preview-replay-calibration.mt2f',
    'player-steward-fan-five-triple-preview-calibration.mt2f',
    'player-steward-fan-five-triple-preview-replay-calibration.mt2f',
    'player-fight-club-wounded-steward-preview-calibration.mt2f',
    'player-fight-club-wounded-steward-preview-replay-calibration.mt2f',
    'player-steward-companion-six-horde-preview-calibration.mt2f',
    'player-steward-companion-six-horde-preview-replay-calibration.mt2f',
    'player-steward-companion-three-horde-preview-calibration.mt2f',
    'player-steward-companion-three-horde-preview-replay-calibration.mt2f',
    'player-steward-companion-seven-writes-preview-calibration.mt2f',
    'player-steward-companion-seven-writes-preview-replay-calibration.mt2f',
    'player-fight-club-small-steward-missing-preview-calibration.mt2f',
    'player-fight-club-small-steward-missing-preview-replay-calibration.mt2f',
    'player-two-plain-companions-preview-calibration.mt2f',
    'player-two-plain-companions-preview-replay-calibration.mt2f',
    'player-pyregel-juice-calibration.mt2f',
    'player-pyregel-powerstone-juice-calibration.mt2f',
    'player-pyregel-powerstone-juice-replay-calibration.mt2f',
    'player-pyregel-powerstone-juice-cast-calibration.mt2f',
    'player-inactive-fragile-moon-birth-calibration.mt2f',
    'player-inactive-fragile-inferno-exit-calibration.mt2f',
    'player-inactive-fragile-inferno-new-moon-entry-calibration.mt2f',
    'player-inactive-fragile-inferno-new-moon-entry-replay-calibration.mt2f',
    'player-inactive-fragile-inferno-entry-calibration.mt2f',
    'player-inactive-fragile-inferno-entry-replay-calibration.mt2f',
    'player-inactive-heavy-inferno-exit-calibration.mt2f',
    'player-inactive-fullmoon-inferno-exit-calibration.mt2f',
    'player-inactive-fullmoon-inferno-exit-replay-calibration.mt2f',
    'player-inactive-heavy-inferno-exit-replay-calibration.mt2f',
    'player-inactive-fragile-inferno-exit-replay-calibration.mt2f',
    'player-inactive-fragile-moon-birth-replay-calibration.mt2f',
    'player-pyregel-juice-replay-calibration.mt2f',
    'player-pyregel-juice-cast-calibration.mt2f',
    'player-troop-birth-world-calibration.mt2f',
    'player-troop-birth-world-replay-calibration.mt2f',
    'player-athane-summon-calibration.mt2f',
    'player-athane-summon-replay-calibration.mt2f',
    'player-champion-aura-preview-retirement-calibration.mt2f',
    'player-starstruck-conduit-replay-calibration.mt2f',
    'player-targeted-damage-calibration.mt2f',
    'player-targeted-damage-replay-calibration.mt2f',
    'player-moon-burnout-relic-calibration.mt2f',
    'player-moon-burnout-relic-replay-calibration.mt2f',
    'player-duplicate-retain-order-calibration.mt2f',
    'player-duplicate-retain-order-replay-calibration.mt2f',
    'parallel-horizon-shallow-model-calibration.mt2f',
    'parallel-horizon-route-model-calibration.mt2f',
    'parallel-opening-portfolio-model-calibration.mt2f',
    'player-triple-juice-purifying-calibration.mt2f',
    'player-triple-juice-purifying-replay-calibration.mt2f',
    'player-triple-juice-purifying-cast-calibration.mt2f',
    'player-triple-juice-purifying-cast-replay-calibration.mt2f',
    'player-triple-juice-consume-cleanse-calibration.mt2f',
    'player-triple-juice-consume-cleanse-replay-calibration.mt2f',
    'player-triple-juice-consume-cleanse-cast-calibration.mt2f',
    'player-triple-juice-consume-cleanse-cast-replay-calibration.mt2f',
    'player-relentless-outer-bump-counterexample-calibration.mt2f',
    'player-relentless-outer-bump-calibration.mt2f',
    'player-relentless-outer-bump-replay-calibration.mt2f',
    'player-burnout-heal-target-calibration.mt2f',
    'player-ascend-deferred-destruction-counterexample-calibration.mt2f',
    'full-battle-random-campaign-21-ring1-release215-budget10000-search.mt2f',
    'full-battle-random-campaign-21-ring1-release215-budget10000-replay.mt2f',
    'full-battle-random-campaign-21-ring2-release215-budget10000-search.mt2f',
    'full-battle-random-campaign-21-ring2-release215-budget10000-replay.mt2f',
    'player-eaten-unstable-shield-calibration.mt2f',
    'player-eaten-unstable-shield-replay-calibration.mt2f',
    'player-moon-silence-shift-calibration.mt2f',
    'player-moon-silence-shift-replay-calibration.mt2f',
    'player-lazarus-juice-multi-conduit-calibration.mt2f',
    'player-lazarus-juice-multi-conduit-cast-calibration.mt2f',
    'player-lazarus-juice-multi-conduit-cast-replay-calibration.mt2f',
    'player-unbox-moon-relic-calibration.mt2f',
    'player-unbox-moon-relic-replay-calibration.mt2f',
    'full-battle-random-campaign-18-ring6-release190-budget10000-search.mt2f',
    'full-battle-random-campaign-18-ring6-release190-budget10000-replay.mt2f',
    'player-inherent-juice-spike-refusal-calibration.mt2f',
    'player-inherent-juice-spike-calibration.mt2f',
    'player-inherent-juice-spike-replay-calibration.mt2f',
    'player-inherent-juice-curse-calibration.mt2f',
    'player-inherent-juice-curse-replay-calibration.mt2f',
    'player-combat-victory-statistics-counterexample-calibration.mt2f',
    'player-combat-victory-statistics-calibration.mt2f',
    'player-combat-victory-cache-retention-counterexample-calibration.mt2f',
    'player-echo-waxen-spike-copy-calibration.mt2f',
    'player-echo-waxen-relics-copy-calibration.mt2f',
    'player-echo-waxen-relics-endturn-calibration.mt2f',
    'player-echo-waxen-relics-endturn-replay-calibration.mt2f',
    'player-echo-remnant-pact-copy-calibration.mt2f',
    'player-echo-remnant-pact-endturn-calibration.mt2f',
    'player-echo-remnant-pact-endturn-replay-calibration.mt2f',
    'player-echo-juice-spike-copy-calibration.mt2f',
    'player-echo-just-cause-copy-calibration.mt2f',
    'player-echo-just-cause-endturn-calibration.mt2f',
    'player-echo-just-cause-endturn-replay-calibration.mt2f',
    'player-echo-juice-spike-endturn-calibration.mt2f',
    'player-echo-juice-spike-endturn-replay-calibration.mt2f',
    'player-echo-juice-middle-preview-calibration.mt2f',
    'player-echo-juice-middle-preview-replay-calibration.mt2f',
    'player-unit-ability-mix-choices-calibration.mt2f',
    'player-unit-ability-mix-choices-replay-calibration.mt2f',
    'player-summon-capacity-rejection-calibration.mt2f',
    'player-summon-capacity-rejection-replay-calibration.mt2f',
    'player-lazarus-juice-conduit-calibration.mt2f',
    'player-lazarus-juice-conduit-replay-calibration.mt2f',
    'player-lazarus-juice-spike-deployment-refusal-calibration.mt2f',
    'player-lazarus-juice-spike-cast-calibration.mt2f',
    'player-lazarus-juice-spike-cast-replay-calibration.mt2f',
    'player-echo-waxen-spike-endturn-calibration.mt2f',
    'player-echo-waxen-spike-endturn-replay-calibration.mt2f',
    'player-moremass-moon-birth-calibration.mt2f',
    'player-moremass-moon-birth-replay-calibration.mt2f',
    'player-unplaced-moremass-moon-counterexample-calibration.mt2f',
    'player-unplaced-moremass-moon-replay-calibration.mt2f',
    'full-battle-random-campaign-19-ring1-release196-budget10000-search.mt2f',
    'full-battle-random-campaign-19-ring1-release196-budget10000-replay.mt2f',
    'full-battle-random-campaign-19-ring2-release197-budget10000-search.mt2f',
    'full-battle-random-campaign-19-ring2-release197-budget10000-replay.mt2f',
    'player-triple-juice-spike-calibration.mt2f',
    'player-triple-juice-spike-replay-calibration.mt2f',
    'player-triple-juice-search-failure-calibration.mt2f',
    'player-triple-juice-curse-calibration.mt2f',
    'player-triple-juice-curse-replay-calibration.mt2f',
    'player-triple-juice-curse-cast-calibration.mt2f',
    'player-triple-juice-curse-cast-replay-calibration.mt2f',
    'full-battle-random-campaign-18-ring3-release186-budget10000-search.mt2f',
    'full-battle-random-campaign-18-ring3-release186-budget10000-replay.mt2f',
    'full-battle-random-campaign-16-ring7-release186-refined-search.mt2f',
    'full-battle-random-campaign-16-ring7-release186-refined-replay.mt2f',
    'player-untouchable-front-relic-calibration.mt2f',
    'player-untouchable-front-relic-replay-calibration.mt2f',
    'player-auto-focus-no-preview-calibration.mt2f',
    'player-auto-focus-no-preview-prefix-calibration.mt2f',
    'player-moon-silence-reset-counterexample-calibration.mt2f',
    'player-moon-silence-reset-calibration.mt2f',
    'player-moon-silence-reset-replay-calibration.mt2f',
    'player-untouchable-room-counterexample-calibration.mt2f',
    'player-untouchable-room-calibration.mt2f',
    'player-untouchable-room-replay-calibration.mt2f',
    'player-cael-moon-birth-calibration.mt2f',
    'player-cael-lifesteal-moon-birth-calibration.mt2f',
    'player-cael-moon-null-relic-calibration.mt2f',
    'player-cael-moon-null-relic-replay-calibration.mt2f',
    'player-mirrored-horde-birth-calibration.mt2f',
    'player-mirrored-horde-birth-replay-calibration.mt2f',
    'player-mirrored-troop-aura-birth-calibration.mt2f',
    'player-mirrored-troop-aura-birth-replay-calibration.mt2f',
    'player-mirrored-existing-horde-calibration.mt2f',
    'player-mirrored-existing-horde-replay-calibration.mt2f',
    'cael-lifesteal-moon-creation-calibration.mt2f',
    'player-cael-lifesteal-moon-birth-replay-calibration.mt2f',
    'cael-lifesteal-moon-creation-replay-calibration.mt2f',
    'player-fragile-moon-birth-calibration.mt2f',
    'fragile-moon-creation-calibration.mt2f',
    'player-fragile-moon-birth-replay-calibration.mt2f',
    'fragile-moon-creation-replay-calibration.mt2f',
    'cael-moon-creation-calibration.mt2f',
    'player-cael-moon-birth-replay-calibration.mt2f',
    'cael-moon-creation-replay-calibration.mt2f',
    'full-battle-random-campaign-16-ring5-release175-budget10000-search.mt2f',
    'full-battle-random-campaign-16-ring5-release175-budget10000-replay.mt2f',
    'player-fleshweaver-merchant-birth-calibration.mt2f',
    'fleshweaver-merchant-creation-calibration.mt2f',
    'player-fleshweaver-merchant-birth-replay-calibration.mt2f',
    'fleshweaver-merchant-creation-replay-calibration.mt2f',
    'full-battle-random-campaign-16-ring2-release173-budget10000-search.mt2f',
    'full-battle-random-campaign-16-ring2-release173-budget10000-replay.mt2f',
    'player-arkion-moon-birth-calibration.mt2f',
    'arkion-moon-creation-calibration.mt2f',
    'player-arkion-moon-birth-replay-calibration.mt2f',
    'arkion-moon-creation-replay-calibration.mt2f',
    'player-terminal-coroutine-planes-counterexample-calibration.mt2f',
    'full-battle-random-campaign-11-ring1-budget10000-search.mt2f',
    'full-battle-random-campaign-11-ring1-budget10000-replay.mt2f',
    'full-battle-random-campaign-11-ring1-release107-budget10000-search.mt2f',
    'full-battle-random-campaign-11-ring1-release107-budget10000-replay.mt2f',
    'full-battle-random-campaign-11-ring1-release108-budget10000-search.mt2f',
    'full-battle-random-campaign-11-ring1-release108-budget10000-replay.mt2f',
    'full-battle-random-campaign-11-ring1-release111-budget10000-search.mt2f',
    'full-battle-random-campaign-11-ring1-release111-budget10000-replay.mt2f',
    'full-battle-random-campaign-11-ring1-release112-budget10000-search.mt2f',
    'full-battle-random-campaign-11-ring1-release112-budget10000-replay.mt2f',
    'full-battle-random-campaign-11-ring8-pyre21-release111-budget10000-loss.mt2f',
    'full-battle-random-campaign-11-ring8-pyre31-release111-budget10000-loss.mt2f',
    'full-battle-random-campaign-11-ring8-pyre48-release112-budget10000-loss.mt2f',
    'parallel-planner-merge-model-counterexample.mt2f',
    'parallel-planner-merge-model-calibration.mt2f',
    'full-battle-random-campaign-11-ring1-release110-budget10000-search.mt2f',
    'full-battle-random-campaign-11-ring1-release110-budget10000-replay.mt2f',
    'full-battle-random-campaign-11-ring7-pyre31-refined-search.mt2f',
    'full-battle-random-campaign-11-ring7-pyre31-refined-replay.mt2f',
    'full-battle-random-campaign-11-ring7-pyre48-refined-search.mt2f',
    'full-battle-random-campaign-11-ring7-pyre48-refined-replay.mt2f',
    'full-battle-random-campaign-11-ring7-pyre-refined-search.mt2f',
    'full-battle-random-campaign-11-ring7-pyre-refined-replay.mt2f',
    'full-battle-native-covenant-03-ring6-refined-search.mt2f',
    'full-battle-native-covenant-03-ring6-refined-replay.mt2f',
    'full-battle-native-covenant-03-ring7-release110-budget10000-loss.mt2f',
    'full-battle-random-campaign-11-ring2-budget10000-search.mt2f',
    'full-battle-random-campaign-11-ring2-budget10000-replay.mt2f',
    'full-battle-random-campaign-11-ring3-budget10000-search.mt2f',
    'full-battle-random-campaign-11-ring3-budget10000-replay.mt2f',
    'full-battle-random-campaign-11-ring4-budget10000-search.mt2f',
    'full-battle-random-campaign-11-ring4-budget10000-replay.mt2f',
    'full-battle-random-campaign-11-ring5-budget10000-search.mt2f',
    'full-battle-random-campaign-11-ring5-budget10000-replay.mt2f',
    'full-battle-random-campaign-11-ring6-budget10000-search.mt2f',
    'full-battle-random-campaign-11-ring6-budget10000-replay.mt2f',
    'full-battle-random-campaign-11-ring7-release107-budget10000-loss.mt2f',
    'full-battle-random-campaign-11-ring8-release109-budget10000-loss.mt2f',
    'full-battle-random-campaign-11-ring7-refined-search.mt2f',
    'full-battle-random-campaign-11-ring7-refined-replay.mt2f',
    'full-battle-random-campaign-09-ring8-refined-suffix-search.mt2f',
    'full-battle-random-campaign-09-ring8-refined-suffix-replay.mt2f',
    'full-battle-random-campaign-01-ring1-release026-search.mt2f',
    'full-battle-random-campaign-01-ring1-release026-replay.mt2f',
    'full-battle-random-campaign-01-ring2-search.mt2f',
    'full-battle-random-campaign-01-ring2-replay.mt2f',
    'full-battle-random-campaign-01-ring3-search.mt2f',
    'full-battle-random-campaign-01-ring3-replay.mt2f',
    'full-battle-random-campaign-01-ring4-search.mt2f',
    'full-battle-random-campaign-01-ring4-replay.mt2f',
    'full-battle-random-campaign-01-ring5-search.mt2f',
    'full-battle-random-campaign-01-ring5-replay.mt2f',
    'full-battle-random-campaign-01-ring6-search.mt2f',
    'full-battle-random-campaign-01-ring6-replay.mt2f',
    'full-battle-random-campaign-01-ring7-loss.mt2f',
    'full-battle-random-campaign-01-ring7-budget10000-loss.mt2f',
    'player-random-campaign-02-ring3-upgrade-observed-prefix.mt2f',
    'player-random-campaign-02-ring3-permafrost-copy-observed-prefix.mt2f',
    'player-random-campaign-02-ring3-relentless-preview-observed.mt2f',
    'player-random-campaign-02-ring3-fixed-policy-native-win.mt2f',
    'full-battle-random-campaign-02-ring1-search.mt2f',
    'full-battle-random-campaign-02-ring1-replay.mt2f',
    'full-battle-random-campaign-02-ring2-search.mt2f',
    'full-battle-random-campaign-02-ring2-replay.mt2f',
    'full-battle-random-campaign-02-ring3-loss.mt2f',
    'full-battle-random-campaign-02-ring3-budget10000-search.mt2f',
    'full-battle-random-campaign-02-ring3-budget10000-replay.mt2f',
    'full-battle-random-campaign-02-ring4-budget10000-search.mt2f',
    'full-battle-random-campaign-02-ring4-budget10000-replay.mt2f',
    'full-battle-random-campaign-02-ring5-budget10000-loss.mt2f',
    'full-battle-random-campaign-06-ring5-budget10000-loss.mt2f',
    'full-battle-random-campaign-06-ring5-budget10000-search.mt2f',
    'full-battle-random-campaign-06-ring5-budget10000-replay.mt2f',
    'full-battle-random-campaign-06-ring6-budget10000-loss.mt2f',
    'full-battle-random-campaign-07-ring1-budget10000-search.mt2f',
    'full-battle-random-campaign-07-ring1-budget10000-replay.mt2f',
    'full-battle-random-campaign-07-ring2-budget10000-search.mt2f',
    'full-battle-random-campaign-07-ring2-budget10000-replay.mt2f',
    'full-battle-random-campaign-07-ring3-budget10000-search.mt2f',
    'full-battle-random-campaign-07-ring3-budget10000-replay.mt2f',
    'full-battle-random-campaign-07-ring4-budget10000-search.mt2f',
    'full-battle-random-campaign-07-ring4-budget10000-replay.mt2f',
    'full-battle-random-campaign-07-ring5-budget10000-loss.mt2f',
    'full-battle-metagame-campaign-ring1-budget10000-search.mt2f',
    'full-battle-metagame-campaign-ring1-budget10000-replay.mt2f',
    'full-battle-metagame-campaign-ring2-budget10000-search.mt2f',
    'full-battle-metagame-campaign-ring2-budget10000-replay.mt2f',
    'full-battle-metagame-campaign-ring3-budget10000-loss.mt2f',
    'full-battle-metagame-campaign-ring1-release070-search.mt2f',
    'full-battle-metagame-campaign-ring1-release070-replay.mt2f',
    'player-unbox-conduit-calibration.mt2f',
    'player-unbox-relic-calibration.mt2f',
    'player-unbox-relic-replay-calibration.mt2f',
    'player-corruption-attack-upgrade-calibration.mt2f',
    'player-corruption-attack-upgrade-replay-calibration.mt2f',
    'player-retained-sentry-target-calibration.mt2f',
    'player-retained-sentry-target-replay-calibration.mt2f',
    'player-slay-enchantment-order-calibration.mt2f',
    'player-slay-enchantment-order-replay-calibration.mt2f',
    'player-spell-shield-target-calibration.mt2f',
    'player-spell-shield-target-replay-calibration.mt2f',
    'player-boxed-copy-trait-calibration.mt2f',
    'player-boxed-copy-trait-replay-calibration.mt2f',
    'player-corrupt-diadem-calibration.mt2f',
    'player-corrupt-diadem-replay-calibration.mt2f',
    'player-corrupt-poison-combat-calibration.mt2f',
    'player-corrupt-poison-combat-replay-calibration.mt2f',
    'player-stronger-corruption-copy-calibration.mt2f',
    'player-stronger-corruption-copy-replay-calibration.mt2f',
    'player-pre-discard-spell-rng-calibration.mt2f',
    'player-pre-discard-spell-rng-replay-calibration.mt2f',
    'player-equipment-preview-owner-failure.mt2f',
    'player-equipment-preview-owner-calibration.mt2f',
    'player-equipment-preview-owner-replay-calibration.mt2f',
    'player-spell-angel-death-calibration.mt2f',
    'player-spell-angel-death-replay-calibration.mt2f',
    'player-spell-harvest-tower-calibration.mt2f',
    'player-spell-harvest-tower-replay-calibration.mt2f',
    'player-relic-damage-tower-calibration.mt2f',
    'player-terminal-companion-roster-counterexample.mt2f',
    'player-terminal-companion-slot-counterexample.mt2f',
    'player-terminal-companion-slot-observation.mt2f',
    'player-terminal-companion-slot-replay.mt2f',
    'player-unit-ability-duality-counterexample.mt2f',
    'player-effect-test-rng-counterexample.mt2f',
    'player-effect-test-rng-observed.mt2f',
    'player-effect-test-rng-calibration.mt2f',
    'player-effect-test-rng-replay-calibration.mt2f',
    'player-hand-highlight-test-rng-counterexample.mt2f',
    'player-hand-highlight-test-rng-observed.mt2f',
    'player-hand-highlight-test-rng-calibration.mt2f',
    'player-hand-highlight-test-rng-replay-calibration.mt2f',
    'player-unit-ability-duality-calibration.mt2f',
    'player-unit-ability-duality-replay-calibration.mt2f',
    'player-relic-damage-tower-replay-calibration.mt2f',
    'player-last-attack-statistic-refresh-calibration.mt2f',
    'player-last-attack-statistic-refresh-replay-calibration.mt2f',
    'player-triggered-corruption-calibration.mt2f',
    'player-triggered-corruption-replay-calibration.mt2f',
    'player-triggered-corruption-relic-calibration.mt2f',
    'player-triggered-corruption-relic-replay-calibration.mt2f',
    'player-draft-upgrade-combat-calibration.mt2f',
    'player-draft-upgrade-combat-replay-calibration.mt2f',
    'player-post-battle-reward-combat-calibration.mt2f',
    'player-post-battle-reward-combat-replay-calibration.mt2f',
    'player-justcause-monster-spell-calibration.mt2f',
    'player-justcause-monster-spell-replay-calibration.mt2f',
    'player-justcause-retain-copy-calibration.mt2f',
    'player-justcause-retain-copy-replay-calibration.mt2f',
    'player-justcause-consume-copy-calibration.mt2f',
    'player-justcause-consume-copy-replay-calibration.mt2f',
    'player-intrinsic-infusion-calibration.mt2f',
    'player-intrinsic-infusion-replay-calibration.mt2f',
    'player-intrinsic-infusion-zero-calibration.mt2f',
    'player-intrinsic-infusion-zero-replay-calibration.mt2f',
    'player-challenge-recast-calibration.mt2f',
    'player-challenge-recast-replay-calibration.mt2f',
    'player-echoes-past-recast-calibration.mt2f',
    'player-echoes-past-recast-replay-calibration.mt2f',
    'player-echoes-past-recast-zero-calibration.mt2f',
    'player-echoes-past-recast-zero-replay-calibration.mt2f',
    'player-corrupt-bloodsuit-calibration.mt2f',
    'player-corrupt-bloodsuit-replay-calibration.mt2f',
    'player-wurmkin-spike-copy-deployment-calibration.mt2f',
    'player-wurmkin-spike-copy-deployment-replay-calibration.mt2f',
    'player-wurmkin-spike-copy-calibration.mt2f',
    'player-wurmkin-spike-copy-replay-calibration.mt2f',
    'player-moonbeam-copy-deployment-calibration.mt2f',
    'player-moonbeam-copy-deployment-replay-calibration.mt2f',
    'player-moonbeam-copy-lifecycle-calibration.mt2f',
    'player-moonbeam-copy-lifecycle-replay-calibration.mt2f',
    'player-spent-monster-spell-relic-calibration.mt2f',
    'player-spent-monster-spell-relic-replay-calibration.mt2f',
    'player-egg-catalog-missing-observation.mt2f',
    'player-egg-catalog-calibration.mt2f',
    'player-egg-catalog-replay-calibration.mt2f',
    'player-unobserved-preview-birth-calibration.mt2f',
    'player-unobserved-preview-birth-replay-calibration.mt2f',
    'player-room-death-preview-identity-calibration.mt2f',
    'player-room-death-preview-identity-replay-calibration.mt2f',
    'player-outer-boss-moon-calibration.mt2f',
    'player-outer-boss-moon-replay-calibration.mt2f',
    'outer-boss-moon-creation-calibration.mt2f',
    'outer-boss-moon-creation-replay-calibration.mt2f',
    'player-angel-duo-moon-birth-calibration.mt2f',
    'angel-duo-moon-creation-calibration.mt2f',
    'player-angel-duo-moon-birth-replay-calibration.mt2f',
    'angel-duo-moon-creation-replay-calibration.mt2f',
    'player-troop-enchantment-noop-calibration.mt2f',
    'player-troop-cache-only-calibration.mt2f',
    'player-troop-cache-only-replay-calibration.mt2f',
    'player-troop-enchantment-noop-replay-calibration.mt2f',
    'player-retained-player-preview-calibration.mt2f',
    'player-retained-player-preview-replay-calibration.mt2f',
    'player-retired-actor-frame-calibration.mt2f',
    'player-retired-actor-frame-replay-calibration.mt2f',
    'player-generated-conduit-copy-calibration.mt2f',
    'player-generated-conduit-copy-counterexample-calibration.mt2f',
    'player-generated-conduit-copy-replay-calibration.mt2f',
    'player-conduit-retirement-calibration.mt2f',
    'player-conduit-retirement-counterexample-calibration.mt2f',
    'player-conduit-retirement-replay-calibration.mt2f',
    'player-troop-preview-addition-calibration.mt2f',
    'player-troop-preview-addition-replay-calibration.mt2f',
    'player-troop-birth-addition-calibration.mt2f',
    'player-troop-birth-addition-replay-calibration.mt2f',
    'player-troop-retired-source-calibration.mt2f',
    'player-troop-retired-source-replay-calibration.mt2f',
    'player-troop-combat-birth-calibration.mt2f',
    'player-troop-combat-birth-counterexample-calibration.mt2f',
    'player-troop-combat-birth-replay-calibration.mt2f',
    'player-troop-last-haste-calibration.mt2f',
    'player-troop-last-haste-replay-calibration.mt2f',
    'player-troop-write-world-calibration.mt2f',
    'player-troop-write-world-replay-calibration.mt2f',
    'player-troop-dead-cache-birth-calibration.mt2f',
    'player-troop-dead-cache-birth-replay-calibration.mt2f',
    'player-remote-preview-frame-calibration.mt2f',
    'player-remote-preview-frame-counterexample-calibration.mt2f',
    'player-remote-preview-frame-replay-calibration.mt2f',
    'full-battle-random-campaign-14-ring6-release157-refined-loss.mt2f',
    'full-battle-random-campaign-14-ring6-release165-refined-loss.mt2f',
    'player-moon-conduit-birth-order-calibration.mt2f',
    'player-moon-conduit-birth-order-replay-calibration.mt2f',
    'player-moon-null-boss-relic-calibration.mt2f',
    'player-moon-null-boss-relic-replay-calibration.mt2f',
    'player-moon-boss-ember-calibration.mt2f',
    'player-moon-boss-ember-replay-calibration.mt2f',
    'player-titan-curse-preview-calibration.mt2f',
    'player-titan-curse-preview-replay-calibration.mt2f',
    'player-room-slay-status-calibration.mt2f',
    'player-room-slay-status-replay-calibration.mt2f',
    'player-rank-three-room-slay-calibration.mt2f',
    'player-rank-three-room-slay-replay-calibration.mt2f',
    'player-rank-three-room-slay-execution-calibration.mt2f',
    'player-rank-three-room-slay-execution-replay-calibration.mt2f',
    'player-green-recruit-preview-identity-counterexample-calibration.mt2f',
    'player-green-recruit-preview-identity-calibration.mt2f',
    'player-green-recruit-preview-identity-replay-calibration.mt2f',
    'player-green-recruit-endturn-identity-counterexample-calibration.mt2f',
    'player-green-recruit-endturn-identity-calibration.mt2f',
    'player-green-recruit-endturn-identity-replay-calibration.mt2f',
    'player-preview-death-spawn-shift-calibration.mt2f',
    'player-preview-death-spawn-shift-replay-calibration.mt2f',
    'player-relentless-companion-bump-counterexample-calibration.mt2f',
    'player-relentless-companion-bump-calibration.mt2f',
    'player-relentless-companion-bump-replay-calibration.mt2f',
    'player-ephemeral-equipment-counterexample-calibration.mt2f',
    'player-ephemeral-equipment-calibration.mt2f',
    'player-ephemeral-equipment-replay-calibration.mt2f',
    'player-multistrike-equipment-target-calibration.mt2f',
    'player-harvest-angel-moon-birth-calibration.mt2f',
    'harvest-angel-moon-creation-calibration.mt2f',
    'player-harvest-angel-moon-birth-replay-calibration.mt2f',
    'harvest-angel-moon-creation-replay-calibration.mt2f',
    'player-room-death-retained-cleanup-calibration.mt2f',
    'player-room-death-retained-cleanup-replay-calibration.mt2f',
    'player-zero-energy-x-policy-calibration.mt2f',
    'player-zero-energy-x-policy-replay-calibration.mt2f',
    'player-x-room-status-policy-calibration.mt2f',
    'player-x-room-status-policy-replay-calibration.mt2f',
    'player-liquid-courage-recast-calibration.mt2f',
    'player-liquid-courage-recast-replay-calibration.mt2f',
    'player-monster-spell-no-teammates-calibration.mt2f',
    'player-monster-spell-no-teammates-replay-calibration.mt2f',
    'player-fracture-recast-calibration.mt2f',
    'player-fracture-recast-replay-calibration.mt2f',
    'player-unowned-relic-copy-calibration.mt2f',
    'player-unowned-relic-copy-replay-calibration.mt2f',
    'player-revived-sacrifice-attribution-calibration.mt2f',
    'player-revived-sacrifice-attribution-replay-calibration.mt2f',
    'player-retaliation-revival-calibration.mt2f',
    'player-unbox-conduit-replay-calibration.mt2f',
    'player-outer-boss-death-statistics-calibration.mt2f',
    'player-outer-boss-death-statistics-replay-calibration.mt2f',
    'full-battle-random-campaign-08-ring3-refined-search.mt2f',
    'full-battle-random-campaign-08-ring3-refined-replay.mt2f',
    'full-battle-metagame-campaign-ring3-refined-search.mt2f',
    'full-battle-metagame-campaign-ring3-refined-replay.mt2f',
    'full-battle-random-campaign-08-ring4-budget10000-search.mt2f',
    'full-battle-random-campaign-08-ring4-budget10000-replay.mt2f',
    'full-battle-random-campaign-08-ring5-budget10000-search.mt2f',
    'full-battle-random-campaign-08-ring5-budget10000-replay.mt2f',
    'full-battle-random-campaign-08-ring6-budget10000-loss.mt2f',
    'full-battle-random-campaign-09-ring1-budget10000-search.mt2f',
    'full-battle-random-campaign-09-ring1-budget10000-replay.mt2f',
    'full-battle-random-campaign-09-ring2-budget10000-loss.mt2f',
    'full-battle-random-campaign-09-ring2-budget10000-search.mt2f',
    'full-battle-random-campaign-09-ring2-budget10000-replay.mt2f',
    'full-battle-random-campaign-09-ring2-refined-search.mt2f',
    'full-battle-random-campaign-09-ring2-refined-replay.mt2f',
    'full-battle-random-campaign-09-ring3-budget10000-loss.mt2f',
    'full-battle-random-campaign-09-ring3-budget10000-search.mt2f',
    'full-battle-random-campaign-09-ring3-budget10000-replay.mt2f',
    'full-battle-random-campaign-09-ring3-refined-search.mt2f',
    'full-battle-random-campaign-09-ring3-refined-replay.mt2f',
    'full-battle-random-campaign-09-ring4-budget10000-search.mt2f',
    'full-battle-random-campaign-09-ring4-budget10000-replay.mt2f',
    'full-battle-random-campaign-09-ring4-release081-budget10000-search.mt2f',
    'full-battle-random-campaign-09-ring4-release081-budget10000-replay.mt2f',
    'full-battle-random-campaign-09-ring5-budget10000-loss.mt2f',
    'full-battle-random-campaign-09-ring5-budget10000-search.mt2f',
    'full-battle-random-campaign-09-ring5-budget10000-replay.mt2f',
    'full-battle-random-campaign-09-ring5-release082-budget10000-search.mt2f',
    'full-battle-random-campaign-09-ring5-release082-budget10000-replay.mt2f',
    'full-battle-random-campaign-09-ring6-budget10000-loss.mt2f',
    'full-battle-random-campaign-09-ring6-release082-budget10000-loss.mt2f',
    'full-battle-random-campaign-09-ring6-refined-search.mt2f',
    'full-battle-random-campaign-09-ring6-refined-replay.mt2f',
    'full-battle-random-campaign-09-ring6-release082-refined-search.mt2f',
    'full-battle-random-campaign-09-ring6-release082-refined-replay.mt2f',
    'full-battle-random-campaign-09-ring7-release082-budget10000-loss.mt2f',
    'full-battle-random-campaign-09-ring7-release083-budget10000-loss.mt2f',
    'full-battle-random-campaign-10-ring1-budget10000-search.mt2f',
    'full-battle-random-campaign-10-ring1-budget10000-replay.mt2f',
    'full-battle-random-campaign-09-ring5-refined-search.mt2f',
    'full-battle-random-campaign-09-ring5-refined-replay.mt2f',
    'full-battle-metagame-campaign-ring6-budget10000-loss.mt2f',
    'full-battle-metagame-campaign-ring4-budget10000-loss.mt2f',
    'full-battle-metagame-campaign-ring4-refined-search.mt2f',
    'full-battle-metagame-campaign-ring4-refined-replay.mt2f',
    'full-battle-metagame-campaign-ring5-budget10000-loss.mt2f',
    'full-battle-metagame-campaign-ring5-refined-search.mt2f',
    'full-battle-metagame-campaign-ring5-refined-replay.mt2f',
    'full-battle-random-campaign-08-ring1-budget10000-search.mt2f',
    'full-battle-random-campaign-08-ring1-budget10000-replay.mt2f',
    'full-battle-random-campaign-08-ring2-budget10000-search.mt2f',
    'full-battle-random-campaign-08-ring2-budget10000-replay.mt2f',
    'full-battle-random-campaign-08-ring3-budget10000-loss.mt2f',
    'full-battle-random-campaign-03-ring1-budget10000-search.mt2f',
    'full-battle-random-campaign-03-ring1-budget10000-replay.mt2f',
    'full-battle-random-campaign-03-ring2-budget10000-search.mt2f',
    'full-battle-random-campaign-03-ring2-budget10000-replay.mt2f',
    'full-battle-random-campaign-03-ring3-budget10000-search.mt2f',
    'full-battle-random-campaign-03-ring3-budget10000-replay.mt2f',
    'full-battle-random-campaign-03-ring4-budget10000-search.mt2f',
    'full-battle-random-campaign-03-ring4-budget10000-replay.mt2f',
    'full-battle-random-campaign-03-ring5-budget10000-search.mt2f',
    'full-battle-random-campaign-03-ring5-budget10000-replay.mt2f',
    'full-battle-random-campaign-03-ring6-budget10000-loss.mt2f',
    'full-battle-random-campaign-04-ring1-budget10000-search.mt2f',
    'full-battle-random-campaign-04-ring1-budget10000-replay.mt2f',
    'full-battle-random-campaign-04-ring2-budget10000-search.mt2f',
    'full-battle-random-campaign-04-ring2-budget10000-replay.mt2f',
    'full-battle-random-campaign-04-ring3-budget10000-search.mt2f',
    'full-battle-random-campaign-04-ring3-budget10000-replay.mt2f',
    'full-battle-random-campaign-04-ring4-budget10000-search.mt2f',
    'full-battle-random-campaign-04-ring4-budget10000-replay.mt2f',
    'full-battle-random-campaign-05-ring1-budget10000-search.mt2f',
    'full-battle-random-campaign-05-ring1-budget10000-replay.mt2f',
    'full-battle-random-campaign-05-ring2-budget10000-search.mt2f',
    'full-battle-random-campaign-05-ring2-budget10000-replay.mt2f',
    'full-battle-random-campaign-05-ring3-budget10000-search.mt2f',
    'full-battle-random-campaign-05-ring3-budget10000-keep-champion-search.mt2f',
    'full-battle-random-campaign-05-ring3-budget10000-keep-champion-replay.mt2f',
    'full-battle-random-campaign-05-ring4-budget10000-keep-champion-search.mt2f',
    'full-battle-random-campaign-05-ring4-budget10000-keep-champion-replay.mt2f',
    'full-battle-random-campaign-05-ring5-budget10000-keep-champion-search.mt2f',
    'full-battle-random-campaign-05-ring5-budget10000-keep-champion-replay.mt2f',
    'full-battle-random-campaign-05-ring6-root30000-keep-champion-search.mt2f',
    'full-battle-random-campaign-05-ring6-root30000-keep-champion-replay.mt2f',
    'full-battle-random-campaign-05-ring7-budget10000-keep-champion-loss.mt2f',
    'full-battle-random-campaign-06-ring4-budget10000-search.mt2f',
    'full-battle-random-campaign-06-ring4-budget10000-replay.mt2f',
    'full-battle-random-campaign-06-ring3-budget10000-search.mt2f',
    'full-battle-random-campaign-06-ring3-budget10000-replay.mt2f',
    'full-battle-random-campaign-06-ring2-budget10000-search.mt2f',
    'full-battle-random-campaign-06-ring2-budget10000-replay.mt2f',
    'full-battle-random-campaign-06-ring1-budget10000-search.mt2f',
    'full-battle-random-campaign-06-ring1-budget10000-replay.mt2f',
    'player-consume-kill-copy-calibration.mt2f',
    'player-sacrifice-preview-calibration.mt2f',
    'player-sacrifice-preview-replay-calibration.mt2f',
    'player-pyre-max-health-calibration.mt2f',
    'player-pyre-max-health-replay-calibration.mt2f',
    'player-card-type-sequence-reset-calibration.mt2f',
    'player-card-type-sequence-reset-replay-calibration.mt2f',
    'player-pyre-payment-world-calibration.mt2f',
    'player-pyre-payment-world-replay-calibration.mt2f',
    'player-removed-ability-self-calibration.mt2f',
    'player-removed-ability-self-replay-calibration.mt2f',
    'player-consume-kill-copy-observation-calibration.mt2f',
    'player-consume-kill-copy-replay-calibration.mt2f',
    'player-juice-ritual-calibration.mt2f',
    'player-juice-magic-calibration.mt2f',
    'player-juice-magic-observation-calibration.mt2f',
    'player-juice-magic-replay-calibration.mt2f',
    'player-juice-tome-calibration.mt2f',
    'player-juice-tome-observation-calibration.mt2f',
    'player-juice-tome-replay-calibration.mt2f',
    'player-juice-fracture-calibration.mt2f',
    'player-juice-justcause-calibration.mt2f',
    'player-juice-justcause-observation-calibration.mt2f',
    'player-juice-justcause-replay-calibration.mt2f',
    'player-corrupt-equipment-calibration.mt2f',
    'player-corrupt-equipment-replay-calibration.mt2f',
    'player-floor-transition-piercing-calibration.mt2f',
    'player-floor-transition-piercing-replay-calibration.mt2f',
    'player-deathwish-enchantment-registry-calibration.mt2f',
    'player-boss-aura-preview-pending-calibration.mt2f',
    'player-boss-aura-preview-pending-replay-calibration.mt2f',
    'player-consume-equipment-order-calibration.mt2f',
    'player-consume-equipment-order-replay-calibration.mt2f',
    'player-champion-troop-titanskin-calibration.mt2f',
    'player-third-troop-fragile-buff-calibration.mt2f',
    'player-third-troop-fragile-preview-calibration.mt2f',
    'player-third-troop-fragile-preview-rng-calibration.mt2f',
    'player-third-troop-fragile-preview-replay-calibration.mt2f',
    'player-third-troop-fragile-upper-preview-calibration.mt2f',
    'player-third-troop-fragile-upper-preview-replay-calibration.mt2f',
    'player-third-troop-fragile-upper-combat-calibration.mt2f',
    'player-third-troop-fragile-upper-combat-replay-calibration.mt2f',
    'player-third-troop-fragile-mixed-preview-calibration.mt2f',
    'player-third-troop-fragile-mixed-preview-replay-calibration.mt2f',
    'player-inactive-heavy-inferno-entry-calibration.mt2f',
    'player-inactive-heavy-inferno-entry-replay-calibration.mt2f',
    'player-inactive-heavy-inferno-new-moon-entry-calibration.mt2f',
    'player-inactive-heavy-inferno-new-moon-entry-replay-calibration.mt2f',
    'player-third-troop-fragile-two-live-calibration.mt2f',
    'player-third-troop-fragile-two-live-replay-calibration.mt2f',
    'player-preview-two-live-identity-calibration.mt2f',
    'player-preview-two-live-identity-replay-calibration.mt2f',
    'player-preview-existing-horde-identity-calibration.mt2f',
    'player-preview-existing-horde-identity-replay-calibration.mt2f',
    'player-third-troop-sycophant-two-live-calibration.mt2f',
    'player-third-troop-sycophant-two-live-replay-calibration.mt2f',
    'player-third-troop-fightclub-mixed-calibration.mt2f',
    'player-third-troop-fightclub-mixed-replay-calibration.mt2f',
    'player-third-troop-original-mixed-combat-calibration.mt2f',
    'player-third-troop-original-mixed-combat-replay-calibration.mt2f',
    'player-third-troop-triple-two-live-combat-calibration.mt2f',
    'player-third-troop-triple-two-live-combat-replay-calibration.mt2f',
    'player-third-troop-sycophant-preview-calibration.mt2f',
    'player-third-troop-sycophant-preview-replay-calibration.mt2f',
    'player-third-troop-recruit-combat-calibration.mt2f',
    'player-third-troop-recruit-combat-replay-calibration.mt2f',
    'player-third-troop-whelp-combat-calibration.mt2f',
    'player-third-troop-whelp-combat-replay-calibration.mt2f',
    'player-third-troop-four-horde-preview-calibration.mt2f',
    'player-four-horde-eight-writes-calibration.mt2f',
    'player-four-horde-eight-writes-replay-calibration.mt2f',
    'player-four-horde-two-writes-calibration.mt2f',
    'player-four-horde-two-writes-replay-calibration.mt2f',
    'player-bone-dog-preview-leak-calibration.mt2f',
    'player-bone-dog-preview-leak-replay-calibration.mt2f',
    'player-six-horde-short-owner-calibration.mt2f',
    'player-six-horde-short-owner-replay-calibration.mt2f',
    'player-third-troop-four-horde-preview-replay-calibration.mt2f',
    'player-third-troop-retired-four-horde-preview-calibration.mt2f',
    'player-third-troop-retired-four-horde-preview-replay-calibration.mt2f',
    'player-third-troop-two-retired-mixed-preview-calibration.mt2f',
    'player-third-troop-two-retired-mixed-preview-replay-calibration.mt2f',
    'player-third-troop-fightclub-retired-preview-calibration.mt2f',
    'player-wounded-horde-preview-identity-calibration.mt2f',
    'player-wounded-horde-steward-preview-calibration.mt2f',
    'player-lava-sycophant-preview-calibration.mt2f',
    'player-lava-fightclub-preview-calibration.mt2f',
    'player-lava-fightclub-preview-replay-calibration.mt2f',
    'player-single-horde-sycophant-calibration.mt2f',
    'player-single-horde-fightclub-lava-calibration.mt2f',
    'player-single-horde-fightclub-lava-replay-calibration.mt2f',
    'player-single-horde-sycophant-attack24-calibration.mt2f',
    'player-single-horde-sycophant-attack24-replay-calibration.mt2f',
    'player-retained-preview-physical-shift-calibration.mt2f',
    'player-triple-horde-target-death-calibration.mt2f',
    'player-triple-horde-lava-target-death-calibration.mt2f',
    'player-triple-horde-lava-target-death-replay-calibration.mt2f',
    'player-fight-club-whelp-death-calibration.mt2f',
    'player-fight-club-whelp-death-replay-calibration.mt2f',
    'player-four-horde-retired-companions-calibration.mt2f',
    'player-four-horde-retired-companions-replay-calibration.mt2f',
    'player-triple-horde-retired-companions-calibration.mt2f',
    'player-triple-horde-retired-companions-replay-calibration.mt2f',
    'player-triple-horde-eight-writes-calibration.mt2f',
    'player-triple-horde-eight-writes-replay-calibration.mt2f',
    'player-lava-steward-preview-calibration.mt2f',
    'player-lava-fan-preview-calibration.mt2f',
    'player-lava-fan-preview-replay-calibration.mt2f',
    'player-lava-six-writes-calibration.mt2f',
    'player-lava-six-writes-replay-calibration.mt2f',
    'player-lava-retired-recruits-preview-calibration.mt2f',
    'player-lava-retired-recruits-preview-replay-calibration.mt2f',
    'player-lava-steward-preview-replay-calibration.mt2f',
    'player-lava-steward-five-writes-calibration.mt2f',
    'player-lava-steward-five-writes-replay-calibration.mt2f',
    'player-lava-steward-horde-birth-calibration.mt2f',
    'player-lava-steward-horde-birth-replay-calibration.mt2f',
    'player-lava-steward-live-horde-calibration.mt2f',
    'player-lava-steward-live-horde-replay-calibration.mt2f',
    'player-six-horde-puffball-preview-calibration.mt2f',
    'player-fight-club-lava-boss-preview-calibration.mt2f',
    'player-fight-club-lava-boss-preview-replay-calibration.mt2f',
    'player-six-horde-retired-puffball-calibration.mt2f',
    'player-retired-puffball-five-writes-calibration.mt2f',
    'player-retired-puffball-five-writes-replay-calibration.mt2f',
    'player-six-horde-retired-puffball-replay-calibration.mt2f',
    'player-six-horde-companions-preview-calibration.mt2f',
    'player-six-horde-companions-health-calibration.mt2f',
    'player-six-horde-fight-club-companions-calibration.mt2f',
    'player-six-horde-fight-club-five-writes-calibration.mt2f',
    'player-six-horde-fight-club-five-writes-replay-calibration.mt2f',
    'player-club-nine-horde-calibration.mt2f',
    'player-club-steward-preview-calibration.mt2f',
    'player-club-upgraded-steward-preview-calibration.mt2f',
    'player-club-upgraded-steward-preview-replay-calibration.mt2f',
    'player-club-wounded-companions-preview-calibration.mt2f',
    'player-club-wounded-companions-preview-replay-calibration.mt2f',
    'player-armor-champion-preview-calibration.mt2f',
    'player-twin-sycophant-preview-calibration.mt2f',
    'player-twin-sycophant-preview-replay-calibration.mt2f',
    'player-twin-sycophant-seven-preview-calibration.mt2f',
    'player-twin-sycophant-seven-preview-replay-calibration.mt2f',
    'player-armor-champion-preview-replay-calibration.mt2f',
    'player-steward-whelp-preview-calibration.mt2f',
    'player-steward-whelp-preview-replay-calibration.mt2f',
    'player-four-horde-combat-birth-calibration.mt2f',
    'player-four-horde-combat-birth-replay-calibration.mt2f',
    'player-four-horde-goldy-preview-calibration.mt2f',
    'player-four-horde-goldy-preview-replay-calibration.mt2f',
    'player-four-horde-retired-enchanter-calibration.mt2f',
    'player-four-horde-retired-enchanter-replay-calibration.mt2f',
    'player-three-companions-missing-preview-calibration.mt2f',
    'player-three-companions-missing-preview-replay-calibration.mt2f',
    'player-club-steward-preview-replay-calibration.mt2f',
    'player-club-nine-horde-replay-calibration.mt2f',
    'player-fight-club-dying-preview-calibration.mt2f',
    'player-fight-club-dying-preview-replay-calibration.mt2f',
    'player-fight-club-steward-recruit-boss-calibration.mt2f',
    'player-fight-club-steward-recruit-boss-replay-calibration.mt2f',
    'player-fight-club-upgraded-recruit-boss-calibration.mt2f',
    'player-fight-club-upgraded-recruit-boss-replay-calibration.mt2f',
    'player-fight-club-wounded-steward-boss-calibration.mt2f',
    'player-fight-club-wounded-steward-boss-replay-calibration.mt2f',
    'player-fight-club-wounded-companions-boss-calibration.mt2f',
    'player-fight-club-wounded-companions-boss-replay-calibration.mt2f',
    'player-fight-club-fourth-write-calibration.mt2f',
    'player-fight-club-fourth-write-replay-calibration.mt2f',
    'player-fight-club-missing-horde-preview-calibration.mt2f',
    'player-fight-club-missing-horde-preview-replay-calibration.mt2f',
    'player-triple-horde-phase-boundary-calibration.mt2f',
    'player-triple-horde-phase-boundary-experiment.mt2f',
    'player-triple-horde-phase-boundary-stage.mt2f',
    'player-triple-horde-phase-boundary-timeline.mt2f',
    'player-triple-horde-phase-boundary-timeline-replay.mt2f',
    'player-triple-horde-dual-trace-calibration.mt2f',
    'player-triple-horde-dual-trace-replay-calibration.mt2f',
    'player-triple-horde-identity-map-calibration.mt2f',
    'player-triple-horde-identity-map-replay-calibration.mt2f',
    'player-triple-horde-retired-combat-calibration.mt2f',
    'player-triple-horde-retired-combat-replay-calibration.mt2f',
    'player-triple-horde-retired-combat-phase.mt2f',
    'player-triple-horde-retired-combat-replay-phase.mt2f',
    'player-fight-club-injured-companion-calibration.mt2f',
    'player-fight-club-injured-companion-replay-calibration.mt2f',
    'player-fight-club-injured-recruit-calibration.mt2f',
    'player-fight-club-injured-recruit-replay-calibration.mt2f',
    'player-six-horde-fight-club-companions-replay-calibration.mt2f',
    'player-six-horde-companions-health-replay-calibration.mt2f',
    'player-six-horde-companions-preview-replay-calibration.mt2f',
    'player-six-horde-puffball-preview-replay-calibration.mt2f',
    'player-triple-horde-target-death-replay-calibration.mt2f',
    'player-retained-preview-physical-shift-replay-calibration.mt2f',
    'player-single-horde-sycophant-replay-calibration.mt2f',
    'player-lava-sycophant-preview-replay-calibration.mt2f',
    'player-wounded-horde-steward-preview-replay-calibration.mt2f',
    'player-wounded-horde-preview-identity-replay-calibration.mt2f',
    'player-third-troop-fightclub-retired-preview-replay-calibration.mt2f',
    'player-third-troop-two-live-preview-calibration.mt2f',
    'player-third-troop-two-live-preview-replay-calibration.mt2f',
    'player-retired-titanskin-troop-calibration.mt2f',
    'player-retired-titanskin-troop-replay-calibration.mt2f',
    'player-inactive-pyre-damage-inferno-exit-calibration.mt2f',
    'player-inactive-once-buffed-multistrike-zealot-inferno-exit-calibration.mt2f',
    'player-inactive-once-buffed-multistrike-zealot-inferno-exit-replay-calibration.mt2f',
    'player-inactive-thrice-buffed-multistrike-zealot-inferno-exit-calibration.mt2f',
    'player-inactive-thrice-buffed-multistrike-zealot-inferno-exit-replay-calibration.mt2f',
    'player-inactive-twelve-buffed-multistrike-zealot-inferno-exit-calibration.mt2f',
    'player-inactive-twelve-buffed-multistrike-zealot-inferno-exit-replay-calibration.mt2f',
    'player-retired-six-horde-preview-calibration.mt2f',
    'player-retired-six-horde-preview-replay-calibration.mt2f',
    'player-inactive-once-buffed-multistrike-zealot-alternative-calibration.mt2f',
    'player-inactive-pyre-damage-inferno-exit-replay-calibration.mt2f',
    'player-pending-physical-harvest-calibration.mt2f',
    'player-pending-physical-harvest-replay-calibration.mt2f',
    'player-inactive-buffed-zealot-inferno-exit-calibration.mt2f',
    'player-inactive-buffed-zealot-inferno-exit-replay-calibration.mt2f',
    'player-inactive-multistrike-zealot-inferno-exit-calibration.mt2f',
    'player-inactive-multistrike-zealot-inferno-exit-replay-calibration.mt2f',
    'player-third-troop-fragile-buff-replay-calibration.mt2f',
    'player-champion-troop-titanskin-replay-calibration.mt2f',
    'player-champion-troop-titanskin-many-sources-calibration.mt2f',
    'player-champion-troop-titanskin-many-sources-replay-calibration.mt2f',
    'player-enemy-statistics-listeners-calibration.mt2f',
    'player-enemy-statistics-listeners-replay-calibration.mt2f',
    'full-battle-player-harvest-listener-snapshot-calibration.mt2f',
    'full-battle-player-harvest-listener-snapshot-replay-calibration.mt2f',
    'player-deathwish-enchantment-registry-replay-calibration.mt2f',
    'player-pyre-ascension-queue-calibration.mt2f',
    'player-pyre-ascension-queue-replay-calibration.mt2f',
    'player-corrupt-rage-room-calibration.mt2f',
    'player-corrupt-rage-room-replay-calibration.mt2f',
    'player-standby-etch-paired-boss-calibration.mt2f',
    'player-standby-etch-paired-boss-replay-calibration.mt2f',
    'player-standby-etch-final-boss-calibration.mt2f',
    'player-standby-etch-final-boss-replay-calibration.mt2f',
    'player-preview-birth-binding-calibration.mt2f',
    'player-preview-birth-binding-replay-calibration.mt2f',
    'player-preview-birth-stage-calibration.mt2f',
    'player-preview-birth-stage-replay-calibration.mt2f',
    'player-pyre-attachment-calibration.mt2f',
    'player-pyre-attachment-replay-calibration.mt2f',
    'player-companion-world-roster-calibration.mt2f',
    'player-companion-world-roster-replay-calibration.mt2f',
    'player-recursion-permafrost-calibration.mt2f',
    'player-recursion-permafrost-replay-calibration.mt2f',
    'full-battle-random-campaign-09-ring8-budget10000-loss.mt2f',
    'full-battle-random-campaign-10-ring6-budget10000-loss.mt2f',
    'full-battle-random-campaign-10-ring6-unbox-refined-search.mt2f',
    'full-battle-random-campaign-10-ring6-unbox-refined-replay.mt2f',
    'full-battle-random-campaign-10-ring6-release096-budget10000-search.mt2f',
    'full-battle-random-campaign-10-ring6-release096-budget10000-replay.mt2f',
    'full-battle-random-campaign-10-ring7-release097-budget10000-loss.mt2f',
    'full-battle-random-campaign-10-ring8-release099-budget10000-loss.mt2f',
    'full-battle-random-campaign-10-ring7-mixed-release097-budget10000-loss.mt2f',
    'full-battle-random-campaign-10-ring7-refined-order-search.mt2f',
    'full-battle-random-campaign-10-ring7-refined-order-replay.mt2f',
    'full-battle-random-campaign-10-ring2-budget10000-search.mt2f',
    'full-battle-random-campaign-10-ring2-budget10000-replay.mt2f',
    'full-battle-random-campaign-10-ring3-budget10000-search.mt2f',
    'full-battle-random-campaign-10-ring3-budget10000-replay.mt2f',
    'full-battle-random-campaign-10-ring4-budget10000-search.mt2f',
    'full-battle-random-campaign-10-ring4-budget10000-replay.mt2f',
    'full-battle-random-campaign-10-ring5-budget10000-search.mt2f',
    'full-battle-random-campaign-10-ring5-budget10000-replay.mt2f',
    'full-battle-random-campaign-09-ring7-refined-order-search.mt2f',
    'full-battle-random-campaign-09-ring7-refined-order-replay.mt2f',
    'player-juice-fracture-observation-calibration.mt2f',
    'player-juice-fracture-replay-calibration.mt2f',
    'player-juice-ritual-observation-calibration.mt2f',
    'player-juice-ritual-replay-calibration.mt2f',
    'player-infusion-trigger-merge-calibration.mt2f',
    'player-infusion-trigger-merge-replay-calibration.mt2f',
    'player-natural-hydra-catalog-deployment-calibration.mt2f',
    'player-natural-hydra-catalog-deployment-replay-calibration.mt2f',
    'player-natural-hydra-graft-equipment-calibration.mt2f',
    'player-natural-hydra-graft-equipment-replay-calibration.mt2f',
    'full-battle-random-campaign-05-ring3-budget10000-replay.mt2f',
    'full-battle-random-campaign-05-ring4-budget10000-search.mt2f',
    'full-battle-random-campaign-05-ring4-budget10000-replay.mt2f',
    'full-battle-random-campaign-05-ring5-budget10000-search.mt2f',
    'full-battle-random-campaign-05-ring5-budget10000-replay.mt2f',
    'full-battle-random-campaign-05-ring6-budget10000-loss.mt2f',
    'full-battle-random-campaign-04-ring5-refined-search.mt2f',
    'full-battle-random-campaign-04-ring5-refined-replay.mt2f',
    'full-battle-random-campaign-04-ring6-budget10000-loss.mt2f',
    'player-feeding-spell-routing-calibration.mt2f',
    'player-feeding-spell-routing-replay-calibration.mt2f',
    'player-authored-consume-copy-calibration.mt2f',
    'player-authored-consume-copy-replay-calibration.mt2f',
    'player-ephemeral-copy-full-hand-calibration.mt2f',
    'player-ephemeral-copy-full-hand-replay-calibration.mt2f',
    'player-ephemeral-copy-hand-space-calibration.mt2f',
    'player-ephemeral-copy-hand-space-replay-calibration.mt2f',
    'player-dead-caster-status-calibration.mt2f',
    'player-dead-caster-status-replay-calibration.mt2f',
    'player-ability-attack-compaction-calibration.mt2f',
    'player-ability-attack-compaction-replay-calibration.mt2f',
    'player-heal-growth-record-calibration.mt2f',
    'player-heal-growth-record-replay-calibration.mt2f',
    'player-overcharge-cleanup-calibration.mt2f',
    'player-overcharge-cleanup-replay-calibration.mt2f',
    'player-heal-combat-removal-calibration.mt2f',
    'player-heal-combat-removal-replay-calibration.mt2f',
    'player-heal-combat-removal-observation-calibration.mt2f',
    'player-despawn-listeners-calibration.mt2f',
    'player-despawn-listeners-replay-calibration.mt2f',
    'player-despawn-frame-removal-calibration.mt2f',
    'player-despawn-frame-removal-replay-calibration.mt2f',
    'player-despawn-frame-removal-observation-calibration.mt2f',
    'player-recursion-target-text-calibration.mt2f',
    'player-recursion-target-text-replay-calibration.mt2f',
    'player-friendly-relic-piercing-calibration.mt2f',
    'player-friendly-relic-piercing-replay-calibration.mt2f',
    'player-corrupt-copyability-calibration.mt2f',
    'player-corrupt-copyability-replay-calibration.mt2f',
    'player-corrupt-before-play-calibration.mt2f',
    'player-corrupt-before-play-replay-calibration.mt2f',
    'player-enemy-command-attack-calibration.mt2f',
    'player-enemy-command-attack-replay-calibration.mt2f',
    'player-relic-attachment-replacement-calibration.mt2f',
    'player-relic-attachment-replacement-replay-calibration.mt2f',
    'player-corrupt-source-alignment-calibration.mt2f',
    'player-corrupt-source-alignment-replay-calibration.mt2f',
    'player-guaranteed-status-preview-calibration.mt2f',
    'player-guaranteed-status-preview-replay-calibration.mt2f',
    'player-guaranteed-status-test-rng-calibration.mt2f',
    'player-guaranteed-status-test-rng-replay-calibration.mt2f',
    'player-boss-preview-test-rng-calibration.mt2f',
    'player-boss-preview-test-rng-replay-calibration.mt2f',
    'player-sweep-boss-preview-stop-calibration.mt2f',
    'player-sweep-boss-preview-stop-replay-calibration.mt2f',
    'player-outer-boss-attachment-birth-calibration.mt2f',
    'player-unsupported-card-ranking-calibration.mt2f',
    'player-prepared-selection-publication-calibration.mt2f',
    'full-battle-player-automatic-view-calibration.mt2f',
    'player-conduit-view-continuation-calibration.mt2f',
    'player-sentient-draw-upgrade-calibration.mt2f',
    'player-sentient-draw-upgrade-replay-calibration.mt2f',
    'player-sentient-draw-upgrade2-calibration.mt2f',
    'player-sentient-draw-upgrade2-replay-calibration.mt2f',
    'player-outer-boss-attachment-birth-replay-calibration.mt2f',
    'player-discard-holdover-return-calibration.mt2f',
    'player-discard-holdover-return-replay-calibration.mt2f',
    'player-undying-copy-deployment-calibration.mt2f',
    'player-undying-copy-deployment-replay-calibration.mt2f',
    'player-undying-copy-effects-calibration.mt2f',
    'player-undying-copy-effects-replay-calibration.mt2f',
    'player-undying-permafrost-copy-deployment-calibration.mt2f',
    'player-undying-permafrost-copy-deployment-replay-calibration.mt2f',
    'player-undying-permafrost-copy-effects-calibration.mt2f',
    'player-undying-permafrost-copy-effects-replay-calibration.mt2f',
    'player-undying-permafrost-copy-discard-calibration.mt2f',
    'player-undying-permafrost-copy-discard-replay-calibration.mt2f',
    'player-slay-corruption-callback-calibration.mt2f',
    'player-slay-corruption-callback-replay-calibration.mt2f',
    'player-generated-discard-guard-calibration.mt2f',
    'player-generated-discard-guard-replay-calibration.mt2f',
    'player-ephemeral-consume-order-calibration.mt2f',
    'player-ephemeral-consume-order-replay-calibration.mt2f',
    'player-status-callback-room-sync-calibration.mt2f',
    'player-status-callback-room-sync-replay-calibration.mt2f',
    'player-unstable-revival-calibration.mt2f',
    'player-unstable-revival-replay-calibration.mt2f',
    'player-attachment-reinstall-calibration.mt2f',
    'player-attachment-reinstall-replay-calibration.mt2f',
    'player-corrupt-restricted-state-calibration.mt2f',
    'player-corrupt-restricted-state-replay-calibration.mt2f',
    'player-corrupt-restricted-return-calibration.mt2f',
    'player-corrupt-restricted-return-replay-calibration.mt2f',
    'player-corrupt-restricted-holdover-calibration.mt2f',
    'player-corrupt-restricted-holdover-replay-calibration.mt2f',
    'player-terminal-enemy-slot-calibration.mt2f',
    'player-terminal-enemy-slot-replay-calibration.mt2f',
    'full-battle-terminal-enemy-slot-calibration.mt2f',
    'player-destroyed-feeder-calibration.mt2f',
    'player-destroyed-feeder-replay-calibration.mt2f',
    'player-destroyed-feeder-observation-calibration.mt2f',
    'player-random-campaign-02-ring2-permafrost-unavailable.mt2f',
    'room-moon-spell-threshold-relic-calibration.mt2f',
    'full-battle-original-moon-card-upgrades-threshold-relic.mt2f',
    'full-battle-player-moon-room-threshold-relic-search.mt2f',
    'full-battle-player-moon-room-threshold-relic-replay.mt2f',
    'player-live-arkion-pyre-unavailable.mt2f',
    'full-battle-player-live-arkion-pyre.mt2f',
    'outer-boss-birth-live-arkion-pyre.mt2f',
    'player-live-arkion-pyre-observed-prefix.mt2f',
    'full-battle-player-early-stop-search.mt2f',
    'room-moon-spell-covenant-01-calibration.mt2f',
    'room-moon-spell-covenant-02-calibration.mt2f',
    'room-moon-spell-covenant-03-calibration.mt2f',
    'full-battle-original-moon-card-upgrades-covenant03.mt2f',
    'heavenfall-focus-native-prefix.mt2f',
    'heavenfall-death-focus-native-prefix.mt2f',
    'full-battle-player-moon-room-covenant03-search.mt2f',
    'full-battle-player-moon-room-covenant03-replay.mt2f',
    'room-moon-spell-status-relic-calibration.mt2f',
    'full-battle-player-moon-room-status-relic-search.mt2f',
    'full-battle-player-moon-room-status-relic-replay.mt2f',
    'full-battle-original-moon-card-upgrades-status-relic.mt2f',
    'room-moon-spell-hand-power-relic-calibration.mt2f',
    'full-battle-player-moon-room-hand-power-relic-search.mt2f',
    'full-battle-player-moon-room-hand-power-relic-replay.mt2f',
    'full-battle-original-moon-card-upgrades-hand-power-relic.mt2f',
    'room-moon-spell-phase-energy-relic-calibration.mt2f',
    'full-battle-player-moon-room-phase-energy-relic-search.mt2f',
    'full-battle-player-moon-room-phase-energy-relic-replay.mt2f',
    'full-battle-original-moon-card-upgrades-phase-energy-relic.mt2f',
    'full-battle-player-moon-room-phase-heal-relic-search.mt2f',
    'full-battle-player-moon-room-phase-heal-relic-replay.mt2f',
    'full-battle-player-native-covenant-campaign-03-ring6-default-loss.mt2f',
    'full-battle-angel-duo-natural-covenant03.mt2f',
    'outer-boss-birth-calibration-angel-duo-natural-covenant03.mt2f',
    'full-battle-player-native-covenant-campaign-03-ring5-default-search.mt2f',
    'full-battle-player-native-covenant-campaign-03-ring5-default-replay.mt2f',
    'player-harvester-birth-calibration.mt2f',
    'full-battle-harvester-birth-calibration.mt2f',
    'full-battle-player-native-covenant-campaign-03-ring4-default-search.mt2f',
    'full-battle-player-native-covenant-campaign-03-ring4-default-replay.mt2f',
    'full-battle-player-advisor-capture-search.mt2f',
    'advisor-atomic-budget-failure.mt2f',
    'advisor-ordinary-budget-failure.mt2f',
    'full-battle-fleshweaver-mass-baseline.mt2f',
    'outer-boss-birth-calibration-fleshweaver-mass.mt2f',
    'full-battle-fleshweaver-corruption-baseline.mt2f',
    'outer-boss-birth-calibration-fleshweaver-corruption.mt2f',
    'full-battle-fleshweaver-spikes-baseline.mt2f',
    'outer-boss-birth-calibration-fleshweaver-spikes.mt2f',
    'full-battle-fleshweaver-mass-native-spells.mt2f',
    'full-battle-fleshweaver-corruption-native-spells.mt2f',
    'full-battle-fleshweaver-spikes-native-spells.mt2f',
    'full-battle-player-fleshweaver-mass-search.mt2f',
    'full-battle-player-fleshweaver-mass-replay.mt2f',
    'full-battle-player-fleshweaver-corruption-search.mt2f',
    'full-battle-player-fleshweaver-corruption-replay.mt2f',
    'full-battle-player-fleshweaver-spikes-search.mt2f',
    'full-battle-player-fleshweaver-spikes-replay.mt2f',
    'full-battle-player-floor-browse-search.mt2f', 'full-battle-player-floor-browse-replay.mt2f',
    'full-battle-player-whole-battle-search.mt2f', 'full-battle-player-whole-battle-replay.mt2f',
    'full-battle-player-capture-reuse-search.mt2f', 'full-battle-player-capture-reuse-replay.mt2f',
    'full-battle-player-performance-finish-search.mt2f', 'full-battle-player-performance-finish-replay.mt2f',
    'full-battle-player-cross-turn-scroll-search.mt2f', 'full-battle-player-cross-turn-scroll-replay.mt2f',
    'player-live-conduit-floor-scroll.mt2f',
    'player-live-moon-continuation.mt2f',
    'player-live-forsaken-trial.mt2f',
    'player-live-empty-deployment-advice.mt2f',
    'full-battle-original-opening-forsaken-covenant10.mt2f',
    'full-battle-original-moon-card-upgrades-live-relics-covenant10.mt2f',
    'full-battle-player-ordinary-continuation-search.mt2f', 'full-battle-player-ordinary-continuation-replay.mt2f',
    'player-room-attachment-birth-observed.mt2f',
    'player-endless-spell-routing-calibration.mt2f', 'player-endless-spell-routing-replay-calibration.mt2f',
    'full-battle-player-native-covenant-campaign-03-ring2-default-search.mt2f',
    'full-battle-player-native-covenant-campaign-03-ring2-default-replay.mt2f',
    'full-battle-player-native-covenant-campaign-03-ring3-default-search.mt2f',
    'full-battle-player-native-covenant-campaign-03-ring3-default-replay.mt2f',
    'full-battle-stewards-quarters-birth-order.mt2f',
    'full-battle-player-natural-ring7-ordinary-search.mt2f', 'full-battle-player-natural-ring7-ordinary-replay.mt2f',
    'full-battle-player-state-key-search.mt2f', 'full-battle-player-state-key-replay.mt2f',
    'full-battle-player-natural-ring7-short-budget-search.mt2f', 'full-battle-player-natural-ring7-short-budget-replay.mt2f',
    'full-battle-player-boss-fragile-short-budget-search.mt2f', 'full-battle-player-boss-fragile-short-budget-replay.mt2f',
    'full-battle-player-boss-fragile-default-search.mt2f', 'full-battle-player-boss-fragile-default-replay.mt2f',
    'full-battle-player-arkion-push-search.mt2f',
    'full-battle-player-arkion-push-replay.mt2f',
    'full-battle-player-cael-lifesteal-search.mt2f',
    'full-battle-player-cael-lifesteal-replay.mt2f',
    'full-battle-player-cael-damage-shield-search.mt2f',
    'full-battle-player-cael-damage-shield-replay.mt2f',
    'full-battle-player-arkion-rage-search.mt2f',
    'full-battle-player-arkion-rage-replay.mt2f',
    'full-battle-arkion-rage-baseline.mt2f',
    'full-battle-arkion-rage-natural-covenant03.mt2f',
    'outer-boss-birth-calibration-arkion-rage-natural-covenant03.mt2f',
    'outer-boss-birth-calibration-arkion-rage.mt2f',
    'full-battle-arkion-rage-native-spells.mt2f',
    'full-battle-arkion-push-baseline.mt2f',
    'outer-boss-birth-calibration-arkion-push.mt2f',
    'full-battle-arkion-push-native-spells.mt2f',
    'full-battle-player-arkion-pyre-search.mt2f',
    'full-battle-player-arkion-pyre-replay.mt2f',
    'full-battle-player-cael-atkhp-search.mt2f',
    'full-battle-player-cael-atkhp-replay.mt2f',
    'full-battle-arkion-pyre.mt2f',
    'outer-boss-birth-calibration-arkion-pyre.mt2f',
    'full-battle-player-angel-duo-revenge-harvest-search.mt2f',
    'full-battle-player-angel-duo-revenge-harvest-replay.mt2f',
    'full-battle-cael-atkhp.mt2f',
    'outer-boss-birth-calibration-cael-atkhp.mt2f',
    'full-battle-cael-damage-shield.mt2f',
    'outer-boss-birth-calibration-cael-damage-shield.mt2f',
    'full-battle-cael-lifesteal.mt2f',
    'outer-boss-birth-calibration-cael-lifesteal.mt2f',
    'full-battle-angel-duo-controlled-harvest.mt2f',
    'angel-duo-harvest-calibration.mt2f',
    'full-battle-player-angel-duo-loop-resolve-search.mt2f',
    'full-battle-player-angel-duo-loop-resolve-replay.mt2f',
    'full-battle-angel-duo-revenge-native-spells.mt2f',
    'full-battle-angel-duo-revenge-baseline.mt2f',
    'outer-boss-birth-calibration-angel-duo-revenge-harvest.mt2f',
    'full-battle-angel-duo-loop-resolve.mt2f',
    'outer-boss-birth-calibration-angel-duo-loop-resolve.mt2f',
    'full-battle-player-advisor-capture-replay.mt2f',
    'full-battle-player-angel-duo-search.mt2f',
    'full-battle-player-angel-duo-replay.mt2f',
    'full-battle-angel-duo-paired-relentless.mt2f',
    'angel-duo-relentless-entry-calibration.mt2f',
    'angel-duo-paired-phase-calibration.mt2f',
    'angel-duo-paired-pyre-calibration.mt2f',
    'full-battle-angel-duo-inner-death.mt2f',
    'angel-duo-inner-death-calibration.mt2f',
    'full-battle-natural-angel-pair-cache-calibration.mt2f',
    'full-battle-natural-angel-pair-cache-replay-calibration.mt2f',
    'full-battle-angel-duo-outer-death.mt2f',
    'angel-duo-outer-death-calibration.mt2f',
    'angel-duo-companion-phase-calibration.mt2f',
    'angel-duo-forced-relentless-calibration.mt2f',
    'full-battle-angel-duo-incant.mt2f',
    'full-battle-native-covenant-02-angel-duo.mt2f',
    'outer-boss-birth-calibration-angel-duo.mt2f',
    'full-battle-player-native-covenant-campaign-02-ring2-search.mt2f',
    'full-battle-player-native-covenant-campaign-02-ring2-replay.mt2f',
    'full-battle-player-native-covenant-outer-boss-corrupter-02-search.mt2f',
    'full-battle-player-native-covenant-outer-boss-corrupter-02-replay.mt2f',
    'full-battle-player-native-covenant-final-boss-02-search.mt2f',
    'full-battle-player-native-covenant-final-boss-02-replay.mt2f',
    'status-trigger-order-calibration.mt2f',
    'full-battle-player-native-covenant-outer-boss-fragile-02-search.mt2f',
    'full-battle-player-native-covenant-outer-boss-fragile-02-replay.mt2f',
    'full-battle-player-native-covenant-outer-boss-corrupter-10-search.mt2f',
    'full-battle-player-native-covenant-outer-boss-corrupter-10-replay.mt2f',
    'full-battle-player-native-covenant-outer-boss-allybuffer-02-search.mt2f',
    'full-battle-player-native-covenant-outer-boss-allybuffer-02-replay.mt2f',
    'full-battle-player-native-covenant-outer-boss-fragile-10-search.mt2f',
    'full-battle-player-native-covenant-outer-boss-fragile-10-replay.mt2f',
    'full-battle-player-native-covenant-startup-09-search.mt2f',
    'full-battle-player-native-covenant-startup-09-replay.mt2f',
    'full-battle-player-native-covenant-outer-boss-allybuffer-10-search.mt2f',
    'full-battle-player-native-covenant-outer-boss-allybuffer-10-replay.mt2f',
    'full-battle-player-native-covenant-startup-07-search.mt2f',
    'full-battle-player-native-covenant-startup-07-replay.mt2f',
    'full-battle-player-native-covenant-startup-08-search.mt2f',
    'full-battle-player-native-covenant-startup-08-replay.mt2f',
    'full-battle-chosen-incant-attack.mt2f',
    'full-battle-player-native-covenant-startup-06-search.mt2f',
    'full-battle-player-native-covenant-startup-06-replay.mt2f',
    'full-battle-player-native-covenant-final-boss-10-search.mt2f',
    'full-battle-player-native-covenant-final-boss-10-replay.mt2f',
    'full-battle-native-covenant-final-boss-02.mt2f',
    'true-final-boss-scheduled-native-covenant-02.mt2f',
    'full-battle-native-covenant-final-boss-10.mt2f',
    'true-final-boss-scheduled-native-covenant-10.mt2f',
    'full-battle-native-covenant-outer-boss-allybuffer-10.mt2f',
    'outer-boss-birth-native-covenant-allybuffer-10.mt2f',
    'full-battle-native-covenant-outer-boss-fragile-10.mt2f',
    'outer-boss-birth-native-covenant-fragile-10.mt2f',
    'full-battle-native-covenant-outer-boss-corrupter-10.mt2f',
    'outer-boss-birth-native-covenant-corrupter-10.mt2f',
    'full-battle-native-covenant-outer-boss-allybuffer-02.mt2f',
    'outer-boss-birth-native-covenant-allybuffer-02.mt2f',
    'full-battle-native-covenant-outer-boss-fragile-02.mt2f',
    'outer-boss-birth-native-covenant-fragile-02.mt2f',
    'full-battle-native-covenant-outer-boss-corrupter-02.mt2f',
    'outer-boss-birth-native-covenant-corrupter-02.mt2f',
    'full-battle-player-native-covenant-startup-10-search.mt2f',
    'full-battle-player-native-covenant-startup-10-replay.mt2f',
    'full-battle-native-covenant-startup-06.mt2f',
    'full-battle-native-covenant-startup-07.mt2f',
    'full-battle-native-covenant-startup-08.mt2f',
    'full-battle-native-covenant-startup-09.mt2f',
    'full-battle-native-covenant-startup-10.mt2f',
    'full-battle-player-native-covenant-startup-05-search.mt2f',
    'full-battle-player-native-covenant-startup-05-replay.mt2f',
    'full-battle-player-native-covenant-startup-04-search.mt2f',
    'full-battle-player-native-covenant-startup-04-replay.mt2f',
    'full-battle-native-covenant-startup-05.mt2f',
    'full-battle-native-covenant-startup-04.mt2f',
    'full-battle-player-native-covenant-startup-03-search.mt2f',
    'full-battle-player-native-covenant-startup-03-replay.mt2f',
    'full-battle-player-native-covenant-startup-02-search.mt2f',
    'full-battle-player-native-covenant-startup-02-replay.mt2f',
    'full-battle-native-covenant-startup-03.mt2f',
    'full-battle-player-covenant-outer-boss-corrupter-00-01-search.mt2f',
    'full-battle-player-covenant-outer-boss-corrupter-00-01-replay.mt2f',
    'full-battle-native-covenant-startup-02.mt2f',
    'full-battle-player-covenant-outer-boss-corrupter-01-search.mt2f',
    'full-battle-player-covenant-outer-boss-corrupter-01-replay.mt2f',
    'full-battle-player-native-covenant-startup-search.mt2f',
    'full-battle-player-native-covenant-startup-replay.mt2f',
    'full-battle-native-covenant-startup-01.mt2f',
    'full-battle-player-covenant-outer-boss-fragile-01-search.mt2f',
    'full-battle-player-covenant-outer-boss-fragile-01-replay.mt2f',
    'full-battle-player-covenant-outer-boss-fragile-00-01-search.mt2f',
    'full-battle-player-covenant-outer-boss-fragile-00-01-replay.mt2f',
    'full-battle-player-covenant-outer-boss-allybuffer-01-search.mt2f',
    'full-battle-player-covenant-outer-boss-allybuffer-01-replay.mt2f',
    'full-battle-player-covenant-outer-boss-allybuffer-00-01-search.mt2f',
    'full-battle-player-covenant-outer-boss-allybuffer-00-01-replay.mt2f',
    'full-battle-covenant-outer-boss-allybuffer-01.mt2f',
    'outer-boss-birth-covenant-allybuffer-01.mt2f',
    'full-battle-covenant-outer-boss-allybuffer-00-01.mt2f',
    'outer-boss-birth-covenant-allybuffer-00-01.mt2f',
    'full-battle-covenant-outer-boss-fragile-01.mt2f',
    'outer-boss-birth-covenant-fragile-01.mt2f',
    'full-battle-covenant-outer-boss-fragile-00-01.mt2f',
    'outer-boss-birth-covenant-fragile-00-01.mt2f',
    'full-battle-covenant-outer-boss-corrupter-01.mt2f',
    'outer-boss-birth-covenant-corrupter-01.mt2f',
    'full-battle-covenant-outer-boss-corrupter-00-01.mt2f',
    'outer-boss-birth-covenant-corrupter-00-01.mt2f',
    'full-battle-covenant-enemy-stats-01.mt2f',
    'full-battle-player-covenant-enemy-stats-search.mt2f',
    'full-battle-player-covenant-enemy-stats-replay.mt2f',
    'full-battle-player-covenant-enemy-stats-ftue-search.mt2f',
    'full-battle-player-covenant-enemy-stats-ftue-replay.mt2f',
    'full-battle-covenant-enemy-stats-00-01.mt2f',
    'true-final-boss-birth-calibration.mt2f',
    'true-final-boss-spawn-calibration.mt2f',
    'true-final-boss-spawn-calibration-covenant00.mt2f',
    'true-final-boss-spawn-calibration-covenant01.mt2f',
    'true-final-boss-spawn-calibration-covenant04.mt2f',
    'true-final-boss-spawn-calibration-covenant08.mt2f',
    'true-final-boss-spawn-calibration-covenants.mt2f',
    'true-final-boss-spawn-calibration-covenants-3.mt2f',
    'true-final-boss-spawn-calibration-covenants-5.mt2f',
    'true-final-boss-spawn-calibration-covenants-6.mt2f',
    'true-final-boss-spawn-calibration-covenants-7.mt2f',
    'true-final-boss-spawn-calibration-covenants-9.mt2f',
    'true-final-boss-spawn-calibration-covenants-10.mt2f',
    'true-final-boss-spawn-calibration-covenants-11.mt2f',
    'true-final-boss-spawn-calibration-covenants-12.mt2f',
    'true-final-boss-spawn-calibration-covenants-13.mt2f',
    'true-final-boss-spawn-calibration-covenants-14.mt2f',
    'true-final-boss-covenant-battle-combined.mt2f',
    'true-final-boss-covenant-battle-04.mt2f',
    'true-final-boss-covenant-battle-00.mt2f',
    'true-final-boss-covenant-battle-01.mt2f',
    'true-final-boss-covenant-battle-08.mt2f',
    'true-final-boss-scheduled-spawn-calibration.mt2f',
    'true-final-boss-continuation-calibration.mt2f',
    'true-final-boss-passive-calibration.mt2f',
    'true-final-boss-unit-turn-calibration.mt2f',
    'true-final-boss-post-combat-calibration.mt2f',
    'true-final-boss-preview-calibration.mt2f',
    'true-final-boss-full-battle-calibration.mt2f',
    'true-final-boss-allied-battle-calibration.mt2f',
    'true-final-boss-multi-allied-calibration.mt2f',
    'true-final-boss-dormant-calibration.mt2f',
    'true-final-boss-dormant-trample-calibration.mt2f',
    'true-final-boss-relentless-calibration.mt2f',
    'true-final-boss-relentless-calibration-covenant04.mt2f',
    'true-final-boss-relentless-calibration-covenants.mt2f',
    'true-final-boss-lethal-calibration.mt2f',
    'true-final-boss-lethal-preview-calibration.mt2f',
    'true-final-boss-lethal-calibration-covenant04.mt2f',
    'true-final-boss-lethal-preview-calibration-covenants.mt2f',
    'true-final-boss-paid-spell-dominion.mt2f',
    'true-final-boss-paid-spell-covenants-dominion.mt2f',
    'true-final-boss-paid-spell-covenants-savagery.mt2f',
    'true-final-boss-paid-spell-covenants-entropybot.mt2f',
    'true-final-boss-paid-spell-covenants-entropymid.mt2f',
    'true-final-boss-paid-spell-covenants-entropytop.mt2f',
    'true-final-boss-commanded-attack.mt2f',
    'true-final-boss-pyre-command.mt2f',
    'true-final-boss-triggered-command.mt2f',
    'true-final-boss-triggered-pyre-command.mt2f',
    'true-final-boss-triggered-ordinary-pyre.mt2f',
    'true-final-boss-ordinary-ally-avenger-health.mt2f',
    'true-final-boss-ordinary-ally-avenger-damage.mt2f',
    'true-final-boss-ordinary-ally-avenger-both.mt2f',
    'full-battle-player-true-final-boss-avenger-upgraded-search.mt2f',
    'full-battle-player-true-final-boss-avenger-upgraded-replay.mt2f',
    'true-final-boss-allied-paid-spell-dominion.mt2f',
    'true-final-boss-allied-paid-spell-savagery.mt2f',
    'true-final-boss-allied-paid-spell-entropybot.mt2f',
    'true-final-boss-allied-paid-spell-entropymid.mt2f',
    'true-final-boss-allied-paid-spell-entropytop.mt2f',
    'true-final-boss-triggered-paid-spell-dominion.mt2f',
    'true-final-boss-triggered-paid-spell-savagery.mt2f',
    'true-final-boss-triggered-paid-spell-entropybot.mt2f',
    'true-final-boss-triggered-paid-spell-entropymid.mt2f',
    'true-final-boss-triggered-paid-spell-entropytop.mt2f',
    'true-final-boss-protected-spell-dominion.mt2f',
    'true-final-boss-protected-spell-savagery.mt2f',
    'true-final-boss-protected-spell-entropybot.mt2f',
    'true-final-boss-protected-spell-entropymid.mt2f',
    'true-final-boss-protected-spell-entropytop.mt2f',
    'true-final-boss-paid-spell-savagery.mt2f',
    'true-final-boss-paid-spell-entropybot.mt2f',
    'true-final-boss-paid-spell-entropymid.mt2f',
    'true-final-boss-paid-spell-entropytop.mt2f',
    'true-final-boss-ally-upgrade-health.mt2f',
    'true-final-boss-ally-upgrade-damage.mt2f',
    'true-final-boss-ordinary-ally-steward.mt2f',
    'true-final-boss-ordinary-ally-warrior.mt2f',
    'true-final-boss-ordinary-ally-animus.mt2f',
    'true-final-boss-ordinary-ally-warrior-health.mt2f',
    'true-final-boss-ordinary-ally-animus-damage.mt2f',
    'true-final-boss-ordinary-ally-warrior-both.mt2f',
    'true-final-boss-ordinary-ally-avenger.mt2f',
    'full-battle-player-true-final-boss-avenger-search.mt2f', 'full-battle-player-true-final-boss-avenger-replay.mt2f',
    'full-battle-player-true-final-boss-ordinary-search.mt2f',
    'full-battle-player-true-final-boss-ordinary-replay.mt2f',
    'full-battle-player-true-final-boss-ordinary-upgraded-search.mt2f',
    'full-battle-player-true-final-boss-ordinary-upgraded-replay.mt2f',
    'true-final-boss-linked-health-calibration.mt2f',
    'true-final-boss-life-link-calibration.mt2f',
    'true-final-boss-health-updates-calibration.mt2f',
    'true-final-boss-live-world-observation.mt2f',
    'true-final-boss-damage-calibration.mt2f',
    'true-final-boss-all-target-damage-calibration.mt2f',
    'true-final-boss-spell-damage-calibration.mt2f',
    'true-final-boss-pyrebane-targets-calibration.mt2f',
    'true-final-boss-controller-calibration.mt2f',
    'true-final-boss-phase-calibration.mt2f',
    'true-final-boss-exhausted-phase-calibration.mt2f',
    'true-final-boss-death-dominion.mt2f',
    'true-final-boss-death-savagery.mt2f',
    'true-final-boss-death-entropybot.mt2f',
    'true-final-boss-death-entropymid.mt2f',
    'true-final-boss-death-entropytop.mt2f',
    'true-final-boss-terminal-damage-dominion.mt2f',
    'true-final-boss-terminal-damage-savagery.mt2f',
    'true-final-boss-terminal-damage-entropybot.mt2f',
    'true-final-boss-terminal-damage-entropymid.mt2f',
    'true-final-boss-terminal-damage-entropytop.mt2f',
    'true-final-boss-terminal-spell-dominion.mt2f',
    'true-final-boss-terminal-spell-savagery.mt2f',
    'true-final-boss-terminal-spell-entropybot.mt2f',
    'true-final-boss-terminal-spell-entropymid.mt2f',
    'true-final-boss-terminal-spell-entropytop.mt2f',
    'full-battle-terminal-copy-on-play.mt2f', 'full-battle-terminal-copy-on-play-replay.mt2f',
    'full-battle-terminal-copy-on-play-consume.mt2f', 'full-battle-terminal-copy-on-play-consume-replay.mt2f',
    'full-battle-terminal-purge-copy.mt2f', 'full-battle-terminal-purge-copy-replay.mt2f',
    'full-battle-terminal-purge-copy-consume.mt2f', 'full-battle-terminal-purge-copy-consume-replay.mt2f',
    'player-terminal-copy-target-calibration.mt2f',
    'player-fragile-budget-boundary.mt2f',
    'full-battle-player-terminal-copy-consume-search.mt2f', 'full-battle-player-terminal-copy-consume-replay.mt2f',
    'full-battle-player-terminal-purge-copy-consume-search.mt2f', 'full-battle-player-terminal-purge-copy-consume-replay.mt2f',
    'full-battle-player-terminal-copy-search.mt2f', 'full-battle-player-terminal-copy-replay.mt2f',
    'full-battle-player-terminal-purge-copy-search.mt2f', 'full-battle-player-terminal-purge-copy-replay.mt2f',
    'player-moon-room-turn-calibration.mt2f', 'room-moon-spell-upgrade-calibration.mt2f',
    'room-moon-spell-advance-calibration.mt2f',
    'room-moon-spell-cost-relic-calibration.mt2f',
    'room-moon-spell-attack-relic-calibration.mt2f',
    'room-moon-spell-healing-relic-calibration.mt2f',
    'room-moon-spell-phase-heal-relic-calibration.mt2f',
    'full-battle-original-moon-card-upgrades-phase-heal-relic.mt2f',
    'full-battle-original-moon-card-upgrades-healing-relic.mt2f',
    'full-battle-player-boss-fragile-adaptive-budget-loss.mt2f',
    'full-battle-original-moon-card-upgrades-attack-relic.mt2f', 'full-battle-original-moon-card-upgrades-attack-relic-replay.mt2f',
    'full-battle-original-moon-card-upgrades-cost-relic.mt2f', 'full-battle-original-moon-card-upgrades-cost-relic-replay.mt2f',
    'full-battle-original-moon-card-upgrades-lunacy.mt2f', 'full-battle-original-moon-card-upgrades-lunacy-replay.mt2f',
    'full-battle-original-moon-card-upgrades-replay.mt2f',
    'full-battle-original-moon-card-upgrades-room-mask.mt2f', 'full-battle-original-moon-card-upgrades-room-mask-replay.mt2f',
    'full-battle-eclipse-chamber-mixed-conduit.mt2f', 'full-battle-eclipse-chamber-mixed-conduit-replay.mt2f',
    'full-battle-player-mixed-conduit-search.mt2f', 'full-battle-player-mixed-conduit-replay.mt2f',
    'full-battle-player-moon-room-spells-search.mt2f', 'full-battle-player-moon-room-spells-replay.mt2f',
    'full-battle-player-moon-room-lunacy-search.mt2f', 'full-battle-player-moon-room-lunacy-replay.mt2f',
    'full-battle-player-moon-room-cost-relic-search.mt2f', 'full-battle-player-moon-room-cost-relic-replay.mt2f',
    'full-battle-player-moon-room-attack-relic-search.mt2f', 'full-battle-player-moon-room-attack-relic-replay.mt2f',
    'full-battle-player-moon-room-healing-relic-search.mt2f', 'full-battle-player-moon-room-healing-relic-replay.mt2f',
    'titanite-combinations-calibration.mt2f',
    'titanite-weakness-calibration.mt2f',
    'boss-preview-sweep-calibration.mt2f',
    'full-battle-player-event-graft-search.mt2f', 'full-battle-player-event-graft-replay.mt2f',
    'full-battle-copy-on-play.mt2f', 'full-battle-natural-ring7-copy-on-play.mt2f',
    'full-battle-natural-ring7-tactical-win.mt2f',
    'full-battle-player-natural-ring7-search.mt2f', 'full-battle-player-natural-ring7-replay.mt2f',
    'full-battle-player-natural-ring7-clean-search.mt2f', 'full-battle-player-natural-ring7-clean-replay.mt2f',
    'full-battle-player-natural-ring7-default-search.mt2f', 'full-battle-player-natural-ring7-default-replay.mt2f',
    'full-battle-player-campaign-trial-gold-search.mt2f', 'full-battle-player-campaign-trial-gold-replay.mt2f',
    'full-battle-player-boss-corrupter-search.mt2f', 'full-battle-player-boss-corrupter-replay.mt2f',
    'full-battle-natural-fragile-no-cards.mt2f', 'full-battle-natural-fragile-status.mt2f',
    'full-battle-natural-fragile-titanite.mt2f',
    'full-battle-player-boss-fragile-bounded-search.mt2f', 'full-battle-player-boss-fragile-bounded-replay.mt2f',
    'full-battle-player-boss-fragile-search.mt2f', 'full-battle-player-boss-fragile-replay.mt2f',
    'full-battle-player-boss-corrupter-bounded-search.mt2f', 'full-battle-player-boss-corrupter-bounded-replay.mt2f',
    'full-battle-player-true-final-boss-bounded-search.mt2f', 'full-battle-player-true-final-boss-bounded-replay.mt2f',
    'full-battle-player-true-final-boss-covenant04-search.mt2f', 'full-battle-player-true-final-boss-covenant04-replay.mt2f',
    'full-battle-player-true-final-boss-covenants-search.mt2f', 'full-battle-player-true-final-boss-covenants-replay.mt2f',
    'full-battle-player-true-final-boss-covenant00-search.mt2f', 'full-battle-player-true-final-boss-covenant00-replay.mt2f',
    'full-battle-player-true-final-boss-covenant01-search.mt2f', 'full-battle-player-true-final-boss-covenant01-replay.mt2f',
    'full-battle-player-true-final-boss-covenant08-search.mt2f', 'full-battle-player-true-final-boss-covenant08-replay.mt2f',
    'full-battle-player-boss-trial-allybuffer-bounded-search.mt2f', 'full-battle-player-boss-trial-allybuffer-bounded-replay.mt2f',
    'full-battle-player-boss-trial-fragile-bounded-search.mt2f', 'full-battle-player-boss-trial-fragile-bounded-replay.mt2f',
    'full-battle-player-boss-trial-corrupter-bounded-search.mt2f', 'full-battle-player-boss-trial-corrupter-bounded-replay.mt2f',
    'full-battle-outer-boss-corrupter-casting.mt2f',
    'outer-boss-corrupter-birth-calibration.mt2f',
    'outer-boss-corrupter-phase-calibration.mt2f',
    'boss-action-corrupter-controller-calibration.mt2f',
    'full-battle-outer-boss-corrupter-casting-replay.mt2f',
    'outer-boss-corrupter-relentless-turn-calibration.mt2f',
    'outer-boss-corrupter-relentless-phase-calibration.mt2f',
    'full-battle-outer-boss-trial-corrupter-casting.mt2f', 'full-battle-outer-boss-trial-corrupter-replay.mt2f',
    'outer-boss-trial-corrupter-birth-calibration.mt2f', 'outer-boss-trial-corrupter-phase-calibration.mt2f',
    'boss-action-trial-corrupter-controller-calibration.mt2f',
    'outer-boss-trial-corrupter-relentless-turn-calibration.mt2f', 'outer-boss-trial-corrupter-relentless-phase-calibration.mt2f',
    'full-battle-outer-boss-trial-fragile-casting.mt2f', 'full-battle-outer-boss-trial-fragile-replay.mt2f',
    'outer-boss-trial-fragile-birth-calibration.mt2f', 'outer-boss-trial-fragile-phase-calibration.mt2f',
    'boss-action-trial-fragile-controller-calibration.mt2f',
    'outer-boss-trial-fragile-relentless-turn-calibration.mt2f', 'outer-boss-trial-fragile-relentless-phase-calibration.mt2f',
    'full-battle-outer-boss-trial-allybuffer-casting.mt2f', 'full-battle-outer-boss-trial-allybuffer-replay.mt2f',
    'outer-boss-trial-allybuffer-birth-calibration.mt2f', 'outer-boss-trial-allybuffer-phase-calibration.mt2f',
    'boss-action-trial-allybuffer-controller-calibration.mt2f',
    'outer-boss-trial-allybuffer-relentless-turn-calibration.mt2f', 'outer-boss-trial-allybuffer-relentless-phase-calibration.mt2f',
    'full-battle-retired-aura-card-kill.mt2f', 'full-battle-train-heal-per-ember.mt2f',
    'full-battle-train-heal-per-ember-x.mt2f',
    'full-battle-player-boss-rolling-search.mt2f', 'full-battle-player-boss-rolling-replay.mt2f',
    'full-battle-outer-boss-fragile-no-cards.mt2f', 'outer-boss-fragile-birth-calibration.mt2f',
    'outer-boss-fragile-phase-calibration.mt2f', 'boss-action-fragile-controller-calibration.mt2f',
    'outer-boss-fragile-relentless-phase-calibration.mt2f', 'outer-boss-fragile-pyre-calibration.mt2f',
    'outer-boss-fragile-pyre-turn-calibration.mt2f',
    'full-battle-player-room-moon-movement-search.mt2f', 'full-battle-player-room-moon-movement-replay.mt2f',
    'room-moon-callback-calibration.mt2f', 'room-moon-callback-calibration-shade.mt2f', 'room-moon-movement-calibration.mt2f',
    'room-moon-enemy-movement-calibration.mt2f',
    'full-battle-eclipse-chamber-enemy-movement.mt2f',
    'full-battle-eclipse-chamber-enemy-movement-replay.mt2f',
    'full-battle-eclipse-chamber-enemy-callbacks.mt2f',
    'full-battle-eclipse-chamber-enemy-callbacks-replay.mt2f',
    'full-battle-eclipse-chamber-enemy-conduit.mt2f', 'full-battle-eclipse-chamber-enemy-conduit-replay.mt2f',
    'full-battle-player-room-moon-enemy-search.mt2f', 'full-battle-player-room-moon-enemy-replay.mt2f',
    'full-battle-player-room-moon-enemy-conduit-search.mt2f', 'full-battle-player-room-moon-enemy-conduit-replay.mt2f',
    'full-battle-player-room-moon-search.mt2f', 'full-battle-player-room-moon-replay.mt2f', 'full-battle-player-conduit-search.mt2f', 'full-battle-player-conduit-replay.mt2f',
    'conduit-movement-calibration.mt2f', 'conduit-enemy-movement-calibration.mt2f',
    'full-battle-player-boss-search.mt2f', 'full-battle-player-boss-replay.mt2f',
    'full-battle-player-moon-search.mt2f', 'full-battle-player-moon-replay.mt2f', 'permanent-trait-calibration.mt2f', 'holdover-variants-calibration.mt2f', 'outer-boss-pyre-victory-calibration.mt2f', 'outer-boss-pyre-victory-turn-calibration.mt2f',
    'outer-boss-natural-relentless-phase-calibration.mt2f', 'full-battle-progressed-ring8-gold-frozen-win.mt2f', 'full-battle-progressed-ring8-gold-no-cards.mt2f',
    'full-battle-progressed-ring8-gold-policy.mt2f', 'full-battle-progressed-ring8-gold-search.mt2f', 'full-battle-progressed-ring8-gold-replay.mt2f',
    'outer-boss-pyre-turn-calibration.mt2f', 'outer-boss-pyre-calibration.mt2f', 'outer-boss-instantiation-calibration.mt2f', 'outer-boss-birth-calibration.mt2f', 'outer-boss-occupied-birth-calibration.mt2f', 'outer-boss-turn-calibration.mt2f', 'outer-boss-relentless-phase-calibration.mt2f', 'outer-boss-occupied-phase-calibration.mt2f', 'outer-boss-phase-calibration.mt2f', 'boss-action-controller-calibration.mt2f', 'boss-action-target-calibration.mt2f', 'retired-unit-destruction-calibration.mt2f', 'planner-deployment-calibration.mt2f', 'planner-score-plateau-calibration.mt2f', 'corruption-status-calibration.mt2f', 'undying-boss-preview-calibration.mt2f', 'intrinsic-opening-draw-calibration.mt2f',
    'full-battle-progressed-ring8-no-cards.mt2f', 'full-battle-progressed-ring8-policy.mt2f',
    'full-battle-steward-once.mt2f', 'full-battle-no-cards.mt2f', 'full-battle-units-and-junk.mt2f',
    'full-battle-planner-starter-search.mt2f', 'full-battle-planner-starter-replay.mt2f',
    'full-battle-planner-relics-search.mt2f', 'full-battle-planner-relics-replay.mt2f',
    'full-battle-planner-ability-search.mt2f', 'full-battle-planner-ability-replay.mt2f',
    'full-battle-planner-moon-search.mt2f', 'full-battle-planner-moon-replay.mt2f',
    'full-battle-planner-history-search.mt2f', 'full-battle-planner-history-replay.mt2f',
    'full-battle-advisor-424242-search.mt2f', 'full-battle-advisor-424242-replay.mt2f',
    'full-battle-advisor-424243-search.mt2f', 'full-battle-advisor-424243-replay.mt2f',
    'full-battle-advisor-424244-search.mt2f', 'full-battle-advisor-424244-replay.mt2f',
    'full-battle-rolling-advisor-ring1-search.mt2f', 'full-battle-rolling-advisor-ring1-replay.mt2f', 'full-battle-rolling-advisor-ring1-policy.mt2f',
    'full-battle-rolling-advisor-ring2-search.mt2f', 'full-battle-rolling-advisor-ring2-replay.mt2f', 'full-battle-rolling-advisor-ring2-policy.mt2f',
    'full-battle-rolling-advisor-ring5-search.mt2f', 'full-battle-rolling-advisor-ring5-replay.mt2f', 'full-battle-rolling-advisor-ring5-policy.mt2f',
    'full-battle-rolling-advisor-ring7-search.mt2f', 'full-battle-rolling-advisor-ring7-replay.mt2f', 'full-battle-rolling-advisor-ring7-policy.mt2f',
    'full-battle-progressed-ring5-search.mt2f', 'full-battle-progressed-ring5-replay.mt2f', 'full-battle-progressed-ring5-policy.mt2f',
    'full-battle-progressed-ring7-search.mt2f', 'full-battle-progressed-ring7-replay.mt2f', 'full-battle-progressed-ring7-policy.mt2f',
    'full-battle-progressed-ring7-holdover-policy.mt2f', 'full-battle-progressed-ring7-holdover-win.mt2f',
    'full-battle-progressed-ring7-holdover-exact-replay.mt2f',
    'full-battle-original-isolated-frozen-totem.mt2f', 'full-battle-original-isolated-royal-whelp.mt2f',
    'full-battle-original-isolated-consumer-of-crowns.mt2f', 'full-battle-original-isolated-steelsinger.mt2f',
    'full-battle-unplayed-card-pyre-damage.mt2f',
    'full-battle-unplayed-card-effects.mt2f', 'full-battle-unplayed-card-effects-release149.mt2f',
    'full-battle-train-heal.mt2f', 'full-battle-status-upgrade.mt2f',
    'full-battle-overheal.mt2f', 'full-battle-corruption.mt2f', 'full-battle-corruption-unit-trigger.mt2f',
    'full-battle-corruption-carver-trigger.mt2f', 'full-battle-corruption-keeper-trigger.mt2f',
    'full-battle-corruption-relic-trigger.mt2f', 'full-battle-permanent-corruption.mt2f',
    'full-battle-soul-siphon.mt2f', 'full-battle-moon-phase-card.mt2f', 'full-battle-moon-phase-advance.mt2f',
    'full-battle-moon-phase-callbacks.mt2f',
    'full-battle-conditional-no-player-status.mt2f',
    'full-battle-conditional-pyre-damage.mt2f',
    'full-battle-conditional-pyre-lethal.mt2f',
    'full-battle-self-attack-status.mt2f',
    'full-battle-units-spells-and-junk.mt2f', 'full-battle-relic-card-modifiers.mt2f', 'full-battle-original-relic-batch.mt2f',
    'full-battle-covenant-character-filter.mt2f',
    'full-battle-titan-skin.mt2f',
    'full-battle-all-units-multistrike-mutator.mt2f',
    'full-battle-original-unit-relics.mt2f',
    'full-battle-original-trigger-relics.mt2f', 'full-battle-original-trigger-relic-deaths.mt2f',
    'full-battle-original-fused-pyreshard.mt2f',
    'full-battle-original-part-factory.mt2f',
    'full-battle-original-big-damage-many-morsels.mt2f',
    'full-battle-original-early-discard-energy.mt2f',
    'full-battle-original-cheaper-spell-cost.mt2f',
    'full-battle-original-first-card-cost.mt2f',
    'full-battle-original-rebalance-valor.mt2f',
    'full-battle-original-nefarious-overflow.mt2f', 'full-battle-original-uncontrolled-group.mt2f',
    'full-battle-original-copy-first-spell-played.mt2f',
    'full-battle-original-copy-first-consume-played.mt2f',
    'full-battle-original-copy-consumed-card.mt2f',
    'full-battle-original-monster-spell-targeting.mt2f',
    'full-battle-original-monster-spell-targeting-expanded.mt2f',
    'full-battle-original-damage-on-floor-transition.mt2f',
    'full-battle-original-pyregel-on-ascend.mt2f',
    'full-battle-original-funguy-deck-scaling.mt2f',
    'full-battle-original-card-draw-on-heal.mt2f',
    'full-battle-original-broken-rail.mt2f',
    'full-battle-original-rage-on-burnout-stack-loss.mt2f',
    'full-battle-original-pyre-revenge.mt2f',
    'full-battle-original-extract-for-ember-and-echos.mt2f',
    'full-battle-original-status-relics.mt2f', 'full-battle-original-burnout-stacks.mt2f',
    'full-battle-original-sting-relics.mt2f',
    'full-battle-original-deployable-relic.mt2f',
    'full-battle-original-magnitude-relics.mt2f',
    'full-battle-original-lifecycle-relics.mt2f',
    'full-battle-original-decay-relics.mt2f',
    'full-battle-original-resource-relics.mt2f', 'full-battle-original-forge-points-relic.mt2f', 'full-battle-pyre-resource-refresh.mt2f',
    'full-battle-original-floor-capacity-relics.mt2f',
    'full-battle-original-endless-return.mt2f',
    'full-battle-original-card-play-magic-power.mt2f',
    'full-battle-original-two-charges-on-card-consumed.mt2f',
    'full-battle-original-chance-for-spell-weakness.mt2f',
    'full-battle-original-chance-to-cleanse.mt2f',
    'full-battle-original-extra-feed-trigger.mt2f',
    'full-battle-original-hephs-blueprints.mt2f',
    'full-battle-original-summon-with-avarice.mt2f',
    'full-battle-original-card-draw-on-card-play.mt2f',
    'full-battle-original-carriage-jack.mt2f',
    'full-battle-original-first-room-attachment.mt2f',
    'full-battle-original-start-room-attachment.mt2f', 'full-battle-original-start-room-attachment-floor2.mt2f',
    'full-battle-original-start-room-attachment-floor3.mt2f',
    'full-battle-original-room-entry-status-relics.mt2f',
    'full-battle-original-moon-phase-energy-relic.mt2f',
    'full-battle-original-moon-phase-heal-relic.mt2f',
    'full-battle-moon-heal-on-heal.mt2f',
    'full-battle-original-moon-phase-status-relic.mt2f',
    'full-battle-original-card-attack-relic.mt2f',
    'full-battle-original-upgrade-count-relics.mt2f',
    'full-battle-original-healing-power-relic.mt2f',
    'full-battle-original-start-combat-energy-relic.mt2f',
    'full-battle-original-start-combat-hoard-relic.mt2f',
    'full-battle-original-forge-conservation-relic.mt2f',
    'full-battle-original-opening-corruption-relics.mt2f',
    'full-battle-original-spell-play-threshold-relics.mt2f',
    'full-battle-original-shroom-decay-relic.mt2f',
    'full-battle-original-conduit-relics.mt2f', 'full-battle-original-conduit-relics-physical.mt2f',
    'full-battle-original-equipment-growth-relics.mt2f',
    'full-battle-original-health-choice-relic.mt2f',
    'full-battle-original-after-draw-generation-relics.mt2f',
    'full-battle-original-add-snack-on-kill.mt2f',
    'full-battle-original-final-ending-artifact.mt2f', 'full-battle-original-balatro-event-yorick.mt2f',
    'full-battle-original-balatro-event-cavendish.mt2f',
    'full-battle-original-garfield-box-random-card.mt2f',
    'full-battle-original-unit-resource-relics.mt2f',
    'full-battle-original-opening-draw-relics.mt2f',
    'full-battle-original-turn-draw-relics.mt2f',
    'full-battle-original-relic-count-attack.mt2f', 'full-battle-original-more-spikes.mt2f',
    'full-battle-original-stealth-slay.mt2f',
    'full-battle-original-unit-phase-upgrade-relics.mt2f',
    'full-battle-original-add-stats-to-solo-units.mt2f',
    'full-battle-original-front-room-health-upgrade.mt2f',
    'full-battle-original-champion-start-turn-heal.mt2f',
    'full-battle-original-nims-oath.mt2f',
    'full-battle-original-judges-scales.mt2f',
    'full-battle-original-no-upgrade-cost.mt2f',
    'full-battle-original-first-imp-bonus.mt2f',
    'full-battle-original-moon-phase-rooms-gain-magic-power.mt2f',
    'full-battle-original-mothers-remnant3.mt2f',
    'full-battle-original-balatro-event-triboulet.mt2f',
    'full-battle-original-cooldown-on-damage-dh.mt2f',
    'full-battle-original-deck-turn-draw.mt2f',
    'full-battle-original-card-play-damage.mt2f',
    'full-battle-original-consume-damage.mt2f',
    'full-battle-original-eaten-damage.mt2f',
    'full-battle-original-more-capacity.mt2f',
    'full-battle-original-burnout-death-damage.mt2f',
    'full-battle-original-heal-damage.mt2f',
    'full-battle-original-kill-attack.mt2f',
    'full-battle-original-chance-to-silence.mt2f',
    'full-battle-original-freeze-reduce-cost.mt2f',
    'full-battle-original-random-freeze.mt2f',
    'full-battle-original-random-hand-upgrade.mt2f',
    'full-battle-original-start-turn-propagation.mt2f',
    'full-battle-original-mooncycle-fae.mt2f',
    'full-battle-original-random-draw-cost.mt2f',
    'full-battle-original-suppress-exhaust.mt2f',
    'full-battle-original-expensive-card-upgrade.mt2f',
    'full-battle-original-wolos-bloodletter.mt2f',
    'full-battle-original-card-type-sequence.mt2f',
    'full-battle-original-whelp-cooldown.mt2f',
    'full-battle-original-spawn-retreat.mt2f',
    'full-battle-original-gros-michel.mt2f',
    'full-battle-original-bone-dog.mt2f',
    'full-battle-original-decay-death.mt2f',
    'full-battle-original-opening-morsels.mt2f',
    'full-battle-card-backed-feeding.mt2f',
    'full-battle-forced-feeding.mt2f',
    'full-battle-soldier-of-fortune.mt2f',
    'full-battle-restoration-detonation.mt2f',
    'full-battle-run-card-generation.mt2f',
    'full-battle-armament-drive.mt2f',
    'full-battle-preserve.mt2f',
    'full-battle-mass-production.mt2f',
    'full-battle-stardust-invocation.mt2f',
    'full-battle-adaptive-mutation.mt2f',
    'full-battle-mixalot.mt2f',
    'full-battle-small-box.mt2f',
    'full-battle-magnetic-relay.mt2f',
    'full-battle-transcend-imp.mt2f',
    'full-battle-fused-weaponry.mt2f',
    'full-battle-eclipse-chamber.mt2f', 'full-battle-eclipse-chamber-callbacks.mt2f', 'full-battle-eclipse-chamber-shade-callbacks.mt2f', 'full-battle-eclipse-chamber-movement.mt2f',
    'full-battle-original-room-cards.mt2f',
    'full-battle-original-forge-cards.mt2f',
    'full-battle-original-status-cards.mt2f',
    'full-battle-original-deck-spell.mt2f',
    'full-battle-goldmance.mt2f',
    'full-battle-engine-upgrade.mt2f',
    'full-battle-stewards-quarters.mt2f',
    'full-battle-zone-of-silence.mt2f',
    'full-battle-memorial.mt2f',
    'full-battle-planetarium.mt2f',
    'full-battle-grow-room.mt2f',
    'full-battle-mushy-room.mt2f',
    'full-battle-original-piercing-cards.mt2f',
    'full-battle-inferno-room.mt2f',
    'full-battle-original-consume-cards.mt2f',
    'full-battle-original-strong-magic-cards.mt2f',
    'full-battle-original-mine-collapse.mt2f',
    'full-battle-original-imp-olate.mt2f',
    'full-battle-original-imp-olate-zero.mt2f',
    'full-battle-original-pyreshot.mt2f',
    'full-battle-original-retch.mt2f',
    'full-battle-original-x-damage-cards.mt2f',
    'full-battle-original-unpurgable-cards.mt2f',
    'full-battle-original-hand-costs.mt2f',
    'full-battle-original-limited-range.mt2f',
    'full-battle-original-monster-hand-costs.mt2f',
    'full-battle-original-unplayable-cards.mt2f',
    'full-battle-original-permafrost-cards.mt2f',
    'full-battle-original-overflow-cards.mt2f',
    'full-battle-original-corrupt-restricted.mt2f',
    'full-battle-original-perfect-insanity.mt2f',
    'full-battle-original-starter-reform.mt2f',
    'full-battle-original-status-stack-damage.mt2f',
    'full-battle-original-specific-card-damage.mt2f',
    'full-battle-original-scaled-energy.mt2f',
    'full-battle-original-status-removal-scaling.mt2f',
    'full-battle-original-effect-applications.mt2f',
    'full-battle-original-pile-draw.mt2f',
    'full-battle-original-card-draw-potion.mt2f',
    'full-battle-original-accelerated-incubation.mt2f',
    'full-battle-original-eager-conscript.mt2f',
    'full-battle-original-copy-morsel-twice.mt2f',
    'full-battle-original-lazarus-league-spike.mt2f',
    'full-battle-original-tome-of-contracts.mt2f',
    'full-battle-original-pile-recursion.mt2f',
    'full-battle-original-scaled-buff.mt2f',
    'full-battle-original-pile-status.mt2f',
    'full-battle-original-draw-upgrade-scaling.mt2f',
    'full-battle-original-consumed-upgrade.mt2f',
    'full-battle-original-status-compositions.mt2f',
    'full-battle-original-grafted-equipment.mt2f',
    'full-battle-original-self-attack.mt2f',
    'full-battle-original-healing-cleanse.mt2f',
    'full-battle-original-on-kill.mt2f',
    'full-battle-original-on-kill-generation.mt2f',
    'full-battle-original-morsel-eaten.mt2f',
    'full-battle-original-unit-buffs.mt2f',
    'full-battle-original-kill-copy.mt2f',
    'full-battle-original-kill-hoard.mt2f',
    'full-battle-original-kill-forge.mt2f',
    'full-battle-original-kill-gold.mt2f',
    'full-battle-original-friendly-statuses.mt2f',
    'full-battle-original-basic-damage.mt2f',
    'full-battle-original-resource-spells.mt2f',
    'full-battle-original-hostile-statuses.mt2f',
    'full-battle-original-scaled-damage.mt2f',
    'full-battle-original-movement-spells.mt2f',
    'full-battle-original-rearrange-spells.mt2f',
    'full-battle-original-support-spells.mt2f',
    'full-battle-original-equipment-cards.mt2f',
    'full-battle-original-status-units.mt2f',
    'full-battle-original-base-champions.mt2f',
    'full-battle-original-reactive-units.mt2f',
    'full-battle-original-attack-overrides.mt2f',
    'full-battle-original-fuel.mt2f',
    'full-battle-original-support-units.mt2f',
    'full-battle-original-divine-dancer.mt2f',
    'full-battle-original-isolated-unit-batch.mt2f',
    'full-battle-original-isolated-coldstrike-siren.mt2f',
    'full-battle-original-isolated-coldsting-siren.mt2f',
    'full-battle-original-isolated-eel-gorgon.mt2f',
    'full-battle-original-isolated-entombed-explosive.mt2f',
    'full-battle-original-isolated-overworked-assistant.mt2f',
    'full-battle-original-isolated-crucible-collector.mt2f',
    'full-battle-original-isolated-crucible-warden.mt2f',
    'full-battle-original-isolated-makeshift-golem.mt2f',
    'full-battle-original-isolated-deathcapped-prophet.mt2f',
    'full-battle-original-isolated-parrafin-enforcer.mt2f',
    'full-battle-original-isolated-sycophant.mt2f',
    'full-battle-original-isolated-votivary.mt2f',
    'full-battle-original-isolated-bounty-stalker.mt2f',
    'full-battle-original-isolated-plague-doctor.mt2f',
    'full-battle-original-isolated-hulking-mass.mt2f',
    'full-battle-original-isolated-reforming-ooze.mt2f',
    'full-battle-original-astrologer.mt2f',
    'full-battle-original-snotty-whelp.mt2f',
    'full-battle-original-bloated-whelp.mt2f',
    'full-battle-original-fae-meddler.mt2f',
    'full-battle-original-molten-encasement.mt2f',
    'full-battle-original-isolated-shattered-shell.mt2f',
    'full-battle-original-isolated-weldling.mt2f',
    'full-battle-original-isolated-hostguard.mt2f',
    'full-battle-original-isolated-rubble-raiser.mt2f',
    'full-battle-original-isolated-nightingale.mt2f',
    'full-battle-original-isolated-juggernaut.mt2f',
    'full-battle-original-isolated-hoard-acolyte.mt2f',
    'full-battle-original-isolated-hothead.mt2f',
    'full-battle-original-isolated-steadfast-crusader.mt2f',
    'full-battle-original-isolated-truffles.mt2f',
    'full-battle-original-isolated-pyreblooded.mt2f',
    'full-battle-original-isolated-kinhost-carapace.mt2f',
    'full-battle-original-isolated-meathook.mt2f',
    'full-battle-original-isolated-railgun.mt2f',
    'full-battle-original-isolated-silent-sentinel.mt2f',
    'full-battle-original-isolated-spikedriver-factory.mt2f',
    'full-battle-original-isolated-scrivener.mt2f',
    'full-battle-original-isolated-banished-champ2.mt2f',
    'full-battle-original-isolated-pyreborne-champ1.mt2f',
    'full-battle-original-isolated-death-eater.mt2f',
    'full-battle-original-isolated-luna-coven-champ1.mt2f',
    'full-battle-original-isolated-luna-coven-champ2.mt2f',
    'full-battle-original-isolated-sporelauncher.mt2f',
    'full-battle-original-isolated-conductor-steward.mt2f',
    'full-battle-original-isolated-enchantrelle.mt2f',
    'full-battle-original-isolated-feeder-damage-enemies.mt2f',
    'full-battle-original-isolated-lady-of-the-reformed.mt2f',
    'full-battle-original-isolated-feeder-permanent-attack-boost.mt2f',
    'full-battle-original-isolated-zealot.mt2f',
    'full-battle-original-isolated-parrafin-thug.mt2f',
    'full-battle-original-isolated-monstrosity.mt2f',
    'full-battle-original-isolated-wickless-death-trigger-batch.mt2f',
    'full-battle-original-isolated-death-summon-batch.mt2f',
    'full-battle-original-isolated-conduit-moon-batch.mt2f',
    'full-battle-original-isolated-enchant-batch.mt2f',
    'full-battle-original-isolated-fae-moon-pixie.mt2f',
    'full-battle-original-isolated-equipment-phase-batch.mt2f',
    'full-battle-original-isolated-equipment-combat-batch.mt2f',
    'full-battle-original-isolated-stasis-crystal.mt2f',
    'full-battle-original-isolated-mitosis-cap.mt2f',
    'full-battle-original-isolated-loamcoat.mt2f',
    'full-battle-original-isolated-morselmaker.mt2f',
    'full-battle-original-isolated-cluster-colonel.mt2f',
    'full-battle-original-isolated-glareminder.mt2f',
    'full-battle-original-isolated-lunar-priestess.mt2f',
    'full-battle-original-isolated-fae-meddler.mt2f',
    'full-battle-original-isolated-forgotten-trade.mt2f',
    'full-battle-original-isolated-windblade.mt2f',
    'full-battle-original-isolated-shield-of-wyngh.mt2f',
    'full-battle-original-isolated-generation-spells.mt2f',
    'full-battle-original-isolated-damage-capacity-spells.mt2f',
    'full-battle-original-isolated-replicator-ray.mt2f',
    'full-battle-original-isolated-intent-on-death.mt2f',
    'full-battle-original-isolated-cannibalize.mt2f',
    'full-battle-original-isolated-scrap-for-parts.mt2f',
    'full-battle-original-isolated-hallowed-halls.mt2f',
    'full-battle-original-isolated-hallowed-halls-capacity.mt2f',
    'full-battle-original-isolated-frostpiercer.mt2f',
    'full-battle-original-isolated-sacrificial-resurrection.mt2f',
    'full-battle-original-isolated-exhausted-first-of-kin.mt2f',
    'full-battle-original-isolated-exhausted-morel-mistress.mt2f',
    'full-battle-original-isolated-void-arms-living.mt2f',
    'full-battle-original-isolated-void-arms-death.mt2f',
    'full-battle-original-isolated-self-attack.mt2f',
    'full-battle-original-isolated-bog-hatching.mt2f',
    'full-battle-original-isolated-dante.mt2f',
    'full-battle-original-isolated-balmabello.mt2f',
    'full-battle-original-isolated-pyre-steward.mt2f',
    'full-battle-original-isolated-bone-dog.mt2f',
    'full-battle-original-isolated-shadesplitter.mt2f',
    'full-battle-original-isolated-revenge-of-the-damned.mt2f',
    'full-battle-original-isolated-filtered-hand-upgrades.mt2f',
    'full-battle-original-isolated-deaths-dancer.mt2f',
    'full-battle-original-isolated-deaths-dancer-shift.mt2f',
    'full-battle-original-isolated-kill-damage-growth.mt2f',
    'full-battle-original-isolated-boilerplate-forged.mt2f',
    'full-battle-original-isolated-boilerplate-zero.mt2f',
    'full-battle-original-isolated-inhale-birth.mt2f',
    'full-battle-original-isolated-inhale-existing.mt2f',
    'full-battle-original-isolated-inhale-friendly-death.mt2f',
    'full-battle-original-isolated-ephemeral-routing.mt2f',
    'full-battle-original-isolated-bounding-corrupt.mt2f',
    'full-battle-original-isolated-returned-corrupt.mt2f',
    'full-battle-original-isolated-forge-resource-callbacks.mt2f',
    'full-battle-original-isolated-horde-and-morsel-targets.mt2f',
    'full-battle-original-isolated-funguy-existing-horde.mt2f',
    'full-battle-original-isolated-discard-copy-plain.mt2f',
    'full-battle-original-isolated-discard-copy-ephemeral.mt2f',
    'full-battle-original-isolated-discard-copy-stardust.mt2f',
    'full-battle-original-isolated-unit-room-resources.mt2f',
    'full-battle-original-isolated-vinemother.mt2f',
    'full-battle-original-isolated-greed-dragon.mt2f',
    'full-battle-original-isolated-greed-dragon-zero.mt2f',
    'full-battle-original-isolated-shadeguard.mt2f',
    'full-battle-original-isolated-mooncaller.mt2f',
    'full-battle-original-isolated-crucible-steward.mt2f',
    'full-battle-original-isolated-triggered-recursion.mt2f',
    'full-battle-original-isolated-triggered-recursion-empty.mt2f',
    'full-battle-original-isolated-room-trigger-rage.mt2f',
    'full-battle-original-isolated-room-trigger-valor.mt2f',
    'full-battle-original-isolated-room-trigger-healing.mt2f',
    'full-battle-original-isolated-room-trigger-spikes.mt2f',
    'full-battle-original-isolated-room-trigger-energy.mt2f',
    'full-battle-original-isolated-room-trigger-gold.mt2f',
    'full-battle-original-isolated-room-trigger-spawn-upgrade.mt2f',
    'full-battle-original-isolated-room-trigger-replacement.mt2f',
    'full-battle-original-isolated-weapons-cache.mt2f',
    'full-battle-original-isolated-weapons-cache-empty.mt2f',
    'full-battle-original-isolated-imprisoned-zephyr.mt2f',
    'full-battle-original-isolated-hellfurnace.mt2f',
    'full-battle-original-isolated-hellfurnace-replacement.mt2f',
    'full-battle-original-isolated-total-recall.mt2f',
    'full-battle-original-isolated-total-recall-empty.mt2f',
    'full-battle-original-isolated-hall-of-mirrors.mt2f',
    'full-battle-original-isolated-hall-of-mirrors-replacement.mt2f',
    'full-battle-original-isolated-copy-morsels-unit.mt2f',
    'full-battle-original-isolated-ethereal-seelie.mt2f',
    'full-battle-original-isolated-unit-grafts.mt2f',
    'full-battle-original-isolated-infectory.mt2f',
    'full-battle-original-isolated-siren-song.mt2f',
    'full-battle-original-isolated-equipment-consumption.mt2f',
    'full-battle-original-isolated-food-spawn.mt2f',
    'full-battle-original-isolated-status-synergy.mt2f',
    'full-battle-original-isolated-treasure-discard.mt2f',
    'full-battle-original-isolated-room-skills.mt2f',
    'full-battle-original-isolated-room-movement-skills.mt2f',
    'full-battle-original-isolated-sentry-turret.mt2f',
    'full-battle-original-isolated-squad-leader.mt2f',
    'full-battle-original-isolated-squad-leader-owner-death.mt2f',
    'full-battle-original-isolated-squad-leader-last-troop.mt2f',
    'full-battle-original-isolated-jack-in-the-box.mt2f',
    'full-battle-original-isolated-jack-in-the-box-empty.mt2f',
    'full-battle-original-isolated-echo-vault.mt2f',
    'full-battle-original-bog-fly-flight-ability.mt2f',
    'full-battle-original-glugsider-ability.mt2f',
    'full-battle-original-extract-draw-ability.mt2f',
    'full-battle-original-fetch-ability.mt2f',
    'full-battle-original-isolated-infusion-definitions.mt2f',
    'full-battle-original-savior2-ability.mt2f',
    'full-battle-original-savior3-ability.mt2f',
    'full-battle-original-courage-ability.mt2f',
    'full-battle-original-chomp-sacrifice-ability.mt2f',
    'full-battle-original-chomp-food-ability.mt2f',
    'full-battle-original-time-freeze-ability.mt2f',
    'full-battle-original-lunar-echo-ability.mt2f',
    'full-battle-original-spikedriver-factory-choice0-ability.mt2f',
    'full-battle-original-spikedriver-factory-choice1-ability.mt2f',
    'full-battle-original-isolated-enchanted-chamber.mt2f',
    'full-battle-original-isolated-enchanted-chamber-replacement.mt2f',
    'full-battle-original-isolated-heal-cost.mt2f',
    'full-battle-original-isolated-heal-cost-other-floor.mt2f',
    'full-battle-original-isolated-room-rage.mt2f',
    'full-battle-original-isolated-room-spikes.mt2f',
    'full-battle-original-isolated-room-valor.mt2f',
    'full-battle-original-isolated-room-rage-self-attack.mt2f',
    'full-battle-original-unplayable-persistent-routing.mt2f',
    'full-battle-original-null-scourges.mt2f',
    'full-battle-original-scaled-moon-phase.mt2f',
    'full-battle-original-positive-status.mt2f',
    'full-battle-original-spell-compositions.mt2f',
    'full-battle-original-discard-draw.mt2f',
    'full-battle-original-discard-draw-routing.mt2f',
    'full-battle-original-moon-card-upgrades.mt2f',
    'full-battle-original-room-status-damage.mt2f',
    'full-battle-original-corruption-damage.mt2f',
    'full-battle-original-x-status-cards.mt2f',
    'full-battle-original-x-banished-spike.mt2f',
    'full-battle-original-x-luna-spike.mt2f',
    'full-battle-original-x-stygian-spike.mt2f',
    'full-battle-original-x-wurmkin-spike.mt2f',
    'full-battle-shroud-spike-feeding.mt2f',
    'full-battle-original-shroud-spike.mt2f',
    'full-battle-original-opening-deck-spawn.mt2f',
    'full-battle-original-perkeo.mt2f',
    'full-battle-original-corrupted-angels.mt2f',
    'full-battle-corrupted-angels-cost-first.mt2f',
    'full-battle-corrupted-angels-cost-last.mt2f',
    'full-battle-original-propagate-on-status-effect-applied.mt2f',
    'full-battle-original-pyre-health-card-cost.mt2f',
    'full-battle-original-unit-upgrade-trigger-relic.mt2f',
    'full-battle-relic-card-status-upgrades.mt2f', 'full-battle-relic-card-negative-unhealed.mt2f',
    'full-battle-generation-relic-card-status-upgrades.mt2f',
    'full-battle-relic-card-piercing-upgrades.mt2f', 'full-battle-relic-card-self-purge-upgrades.mt2f',
    'full-battle-relic-card-exhaust-upgrades.mt2f',
    'full-battle-relic-card-ephemeral-upgrades.mt2f',
    'full-battle-relic-extra-tough-tombs.mt2f',
    'full-battle-relic-card-ability-upgrades.mt2f',
    'full-battle-relic-card-trigger-upgrades.mt2f',
    'full-battle-relic-card-cast-trigger-upgrades.mt2f',
    'full-battle-relic-card-cast-trigger-unit-upgrades.mt2f',
    'full-battle-relic-card-replacement-upgrades.mt2f',
    'full-battle-conditional-relic-card-upgrades.mt2f', 'full-battle-statistics.mt2f', 'full-battle-numeric-upgrades.mt2f',
    'full-battle-dynamic-upgrades.mt2f', 'full-battle-sacrifice-effects.mt2f', 'full-battle-card-effect-kill.mt2f', 'full-battle-card-effect-kill-subtype.mt2f', 'full-battle-null-forwarding-cleave.mt2f', 'full-battle-null-unit-triggers.mt2f', 'full-battle-null-targetless-scourges.mt2f', 'full-battle-original-trample-basher.mt2f', 'full-battle-original-spell-shield.mt2f', 'full-battle-choose-discard.mt2f', 'full-battle-dragon-hoard.mt2f', 'full-battle-remove-all-status-effects.mt2f', 'full-battle-card-recursion.mt2f', 'full-battle-card-recursion-choice.mt2f', 'full-battle-card-recursion-choose-to-hand.mt2f', 'full-battle-card-recursion-filter-choice.mt2f', 'full-battle-card-recursion-discard-choice.mt2f', 'full-battle-card-recursion-upgrade.mt2f', 'full-battle-card-recursion-upgrade-hand.mt2f', 'full-battle-card-recursion-upgrade-choice.mt2f', 'full-battle-card-recursion-upgrade-filter-choice.mt2f', 'full-battle-card-recursion-upgrade-replacement-choice.mt2f', 'full-battle-sacrifice-upgrades.mt2f',
    'full-battle-set-health.mt2f',
    'full-battle-transfer-attack.mt2f',
    'full-battle-transfer-attack-physical.mt2f',
    'full-battle-transfer-attack-ability.mt2f',
    'full-battle-brain-transfer.mt2f',
    'full-battle-frost-channeling.mt2f',
    'full-battle-mix.mt2f', 'full-battle-mix-alternate.mt2f', 'full-battle-mix-01.mt2f', 'full-battle-mix-10.mt2f',
    'full-battle-mix-unstable.mt2f', 'full-battle-mix-unstable-explosion.mt2f',
    'full-battle-original-tinkermaster-mix.mt2f', 'full-battle-original-progress-pacemaker-mix.mt2f',
    'full-battle-original-card-type-cost-relic.mt2f',
    'full-battle-original-purge-to-exhaust.mt2f',
    'full-battle-original-conserve-ember.mt2f',
    'full-battle-card-type-cost-discount-first.mt2f', 'full-battle-card-type-cost-upgrade-first.mt2f',
    'full-battle-damage-by-units-killed.mt2f',
    'full-battle-damage-by-units-killed-physical.mt2f',
    'full-battle-hand-upgrades.mt2f', 'full-battle-targeted-hand-upgrades.mt2f', 'full-battle-permanent-consume-hand-upgrades.mt2f',
    'full-battle-permanent-consume-over-temporary.mt2f',
    'full-battle-choose-discard-others.mt2f',
    'full-battle-choose-discard-others-policy.mt2f',
    'full-battle-choose-discard-refine.mt2f',
    'full-battle-choose-discard-persistent.mt2f',
    'full-battle-original-smelt-ember.mt2f', 'full-battle-original-smelt-ember-zero.mt2f',
    'full-battle-original-smelt-ember-bulk.mt2f',
    'full-battle-healing.mt2f', 'full-battle-healing-triggers.mt2f', 'full-battle-room-spells.mt2f',
    'full-battle-damage-per-hp.mt2f', 'full-battle-damage-per-hp-last-attacker.mt2f',
    'full-battle-damage-per-hp-ability.mt2f', 'full-battle-damage-overflow.mt2f',
    'full-battle-terminal-spells.mt2f',
    'full-battle-post-kill-spells.mt2f',
    'full-battle-random-spells.mt2f',
    'full-battle-random-status.mt2f',
    'full-battle-cross-room-spells.mt2f', 'full-battle-cross-room-targets.mt2f',
    'full-battle-attack-buffs.mt2f',
    'full-battle-max-health-spells.mt2f', 'full-battle-max-health-lethal.mt2f',
    'full-battle-numeric-ranges-ui-isolated.mt2f', 'full-battle-numeric-ranges-lethal-ui-isolated.mt2f',
    'full-battle-target-filters.mt2f',
    'full-battle-drawing-ui-isolated.mt2f',
    'full-battle-typed-draw.mt2f',
    'full-battle-hand-removal.mt2f', 'full-battle-hand-removal-lethal.mt2f', 'full-battle-random-discard.mt2f',
    'full-battle-generation.mt2f', 'full-battle-generation-permanent-consume.mt2f', 'full-battle-generation-lethal.mt2f',
    'full-battle-shared-piles.mt2f', 'full-battle-shared-piles-lethal.mt2f',
    'full-battle-damage-scaling.mt2f',
    'full-battle-dynamic-statistics.mt2f',
    'full-battle-status-scaling.mt2f',
    'full-battle-unit-upgrade-scaling.mt2f',
    'full-battle-tower-corruption-callbacks.mt2f',
    'full-battle-null-unit-ability.mt2f',
    'full-battle-removed-status-upgrade-scaling.mt2f',
    'full-battle-unit-trigger-upgrades.mt2f',
    'full-battle-spawn-triggers.mt2f', 'full-battle-spawn-triggers-lethal.mt2f',
    'full-battle-unit-turn-begin.mt2f',
    'full-battle-team-turn-begin.mt2f',
    'full-battle-pre-hand-discard.mt2f', 'full-battle-pre-hand-discard-lethal.mt2f',
    'full-battle-clone-upgrade-refresh.mt2f',
    'full-battle-pre-combat.mt2f',
    'full-battle-statistic-cache.mt2f',
    'full-battle-triggered-healing.mt2f',
    'full-battle-post-combat-healing.mt2f',
    'full-battle-triggered-damage.mt2f',
    'full-battle-damage-death-queue.mt2f',
    'full-battle-terminal-death-damage.mt2f',
    'full-battle-hit-kill.mt2f',
    'full-battle-dying-upgrades.mt2f',
    'full-battle-attack-triggers.mt2f',
    'full-battle-triggered-status.mt2f',
    'full-battle-triggered-status-instant.mt2f',
    'full-battle-status-registry.mt2f',
    'full-battle-native-binary.mt2f',
    'full-battle-status-callbacks.mt2f',
    'full-battle-status-callback-actions.mt2f',
    'full-battle-purify.mt2f',
    'full-battle-purify-queues.mt2f',
    'full-battle-incant.mt2f',
    'full-battle-incant-thresholds.mt2f',
    'full-battle-ability-incant.mt2f', 'full-battle-room-selection-preview.mt2f', 'full-battle-incant-relic.mt2f',
    'full-battle-incant-relics-combined.mt2f',
    'full-battle-spawn-status-relics.mt2f',
    'full-battle-spawn-status-relics-clones.mt2f',
    'full-battle-spawn-status-relics-clones-ungated-capture.mt2f',
    'full-battle-spawn-status-relics-mixed-cardless-order.mt2f',
    'full-battle-original-stealth-waxers.mt2f',
    'full-battle-original-team-spawn-status.mt2f',
    'full-battle-spawn-status-relics-tuned-gear.mt2f',
    'full-battle-spawn-status-relics-boss-mute.mt2f',
    'full-battle-spawn-status-relics-pyre.mt2f',
    'full-battle-spawn-status-relics-egg-hatch.mt2f',
    'full-battle-egg-transformation.mt2f',
    'full-battle-original-kinhost-vessel.mt2f',
    'full-battle-original-kinhost-vessel-delayed.mt2f',
    'full-battle-original-mageblade-magnitude.mt2f',
    'full-battle-cardless-relic-upgrades.mt2f',
    'full-battle-energy-effects.mt2f',
    'full-battle-energy-effects-lethal.mt2f',
    'full-battle-x-cost.mt2f',
    'full-battle-x-cost-lethal.mt2f',
    'full-battle-bonus-draw.mt2f',
    'full-battle-bonus-draw-lethal.mt2f',
    'full-battle-room-capacity.mt2f',
    'full-battle-room-capacity-lethal.mt2f',
    'full-battle-room-capacity-attachment.mt2f',
    'full-battle-direct-unit-upgrades.mt2f',
    'full-battle-equipment.mt2f',
    'full-battle-equipment-exhausted.mt2f',
    'full-battle-equipment-overflow.mt2f',
    'full-battle-equipment-triggers.mt2f',
    'full-battle-equipment-buff.mt2f',
    'full-battle-trigger-mutation.mt2f', 'full-battle-detached-bonus-draw.mt2f',
    'full-battle-conditional-triggers.mt2f',
    'full-battle-trigger-repeats.mt2f',
    'full-battle-companion-boss.mt2f', 'full-battle-companion-boss-actions.mt2f',
    'full-battle-sentry.mt2f', 'full-battle-sentry-lethal.mt2f',
    'full-battle-ability-cooldown.mt2f', 'full-battle-ability-cache.mt2f',
    'full-battle-ability-activation.mt2f', 'full-battle-ability-activation-x.mt2f',
    'full-battle-ability-activation-pyre-focus.mt2f',
    'full-battle-ability-activation-lethal.mt2f',
    'full-battle-unit-ability-teleport.mt2f',
    'player-teleport-adjacent-order-calibration.mt2f',
    'player-teleport-adjacent-order-replay-calibration.mt2f',
    'full-battle-spawn-or-status.mt2f',
    'full-battle-ability-lifecycle.mt2f',
    'full-battle-ability-effects.mt2f',
    'full-battle-equipment-abilities.mt2f',
    'full-battle-horde-statuses.mt2f',
    'full-battle-horde-merge.mt2f',
    'full-battle-harvest-triggers.mt2f',
    'full-battle-horde-removal.mt2f',
    'full-battle-horde-death.mt2f',
    'full-battle-horde-upgrades.mt2f',
    'full-battle-dying-horde-upgrades.mt2f',
    'full-battle-rally-triggers.mt2f',
    'full-battle-rally-lethal.mt2f',
    'full-battle-unit-identities.mt2f',
    'full-battle-unit-identities-lethal.mt2f',
    'full-battle-mageblade.mt2f',
    'full-battle-multi-summon.mt2f',
    'full-battle-multi-summon-zero.mt2f',
    'full-battle-multi-summon-upgrade.mt2f',
    'full-battle-multi-summon-upgrade-unique.mt2f',
    'full-battle-multi-summon-upgrade-restricted.mt2f',
    'full-battle-multi-summon-fresh.mt2f',
    'full-battle-multi-summon-fresh-deaths.mt2f',
    'full-battle-multi-summon-additional.mt2f',
    'full-battle-multi-summon-additional-fresh.mt2f',
    'full-battle-multi-summon-pool-additional.mt2f',
    'full-battle-multi-summon-pool-additional-fresh.mt2f',
    'full-battle-multi-summon-pool-singleton-additional.mt2f',
    'full-battle-multi-summon-pool.mt2f',
    'full-battle-multi-summon-pool-no-primary.mt2f',
    'full-battle-multi-summon-pool-missing-fresh.mt2f',
    'full-battle-multi-summon-pool-additional-missing-fresh.mt2f',
    'full-battle-multi-summon-pool-no-primary-missing-fresh.mt2f',
    'full-battle-multi-summon-pool-no-primary-missing-fresh-deaths.mt2f',
    'full-battle-spawn-points.mt2f',
    'full-battle-physical-spawn-points.mt2f',
    'full-battle-physical-spawn-points-no-cards.mt2f',
    'full-battle-triggered-summons.mt2f', 'full-battle-triggered-summons-fresh.mt2f',
    'full-battle-triggered-summons-equipment.mt2f', 'full-battle-triggered-summons-equipment-fresh.mt2f',
    'full-battle-triggered-summons-equipment-death.mt2f', 'full-battle-triggered-summons-equipment-death-fresh.mt2f',
    'full-battle-triggered-summons-equipment-owned.mt2f', 'full-battle-triggered-summons-equipment-owned-fresh.mt2f',
    'full-battle-triggered-summons-equipment-owned-death.mt2f', 'full-battle-triggered-summons-equipment-owned-death-fresh.mt2f',
    'full-battle-triggered-summons-equipment-owned-revival.mt2f', 'full-battle-triggered-summons-equipment-owned-revival-fresh.mt2f',
    'full-battle-triggered-summons-death.mt2f', 'full-battle-triggered-summons-death-fresh.mt2f',
    'full-battle-revival.mt2f', 'full-battle-bump.mt2f', 'full-battle-companion-bump.mt2f', 'full-battle-outer-boss-bump.mt2f', 'full-battle-unit-clone.mt2f', 'full-battle-unit-copy.mt2f',
    'full-battle-hero-copy.mt2f',
    'full-battle-spawn-enchant.mt2f',
    'full-battle-persistent-enchantment.mt2f',
    'full-battle-persistent-enchantment-deaths.mt2f',
    'full-battle-persistent-enchantment-revivals.mt2f',
    'full-battle-persistent-enchantment-random.mt2f',
    'full-battle-persistent-enchantment-random-deaths.mt2f',
    'full-battle-persistent-enchantment-random-revivals.mt2f',
    'full-battle-persistent-enchantment-upgrades.mt2f',
    'full-battle-persistent-enchantment-random-upgrades.mt2f',
    'full-battle-persistent-enchantment-summons.mt2f',
    'full-battle-persistent-enchantment-summons-fresh.mt2f',
    'full-battle-persistent-enchantment-random-summons.mt2f',
    'full-battle-persistent-enchantment-random-summons-fresh.mt2f',
    'full-battle-branch-card-masks.mt2f', 'full-battle-branch-card-masks-hand-upgrades.mt2f',
    'full-battle-branch-card-masks-generation.mt2f', 'full-battle-branch-card-masks-clone-upgrade-refresh.mt2f',
    'full-battle-branch-card-masks-bonus-draw.mt2f', 'full-battle-branch-card-masks-equipment.mt2f',
    'card-modifier-calibration.mt2f', 'rng-calibration.mt2f', 'gold-reward-calibration.mt2f',
    'campaign-relic-calibration.mt2f',
    'campaign-draft-calibration.mt2f',
    'campaign-post-battle-calibration.mt2f',
    'campaign-battle-resource-calibration.mt2f',
    'campaign-purge-gold-calibration.mt2f',
    'campaign-deck-copy-calibration.mt2f',
    'campaign-draft-enhancer-count-calibration.mt2f',
    'null-relic-equivalence-calibration.mt2f',
    'standby-routing-calibration.mt2f', 'ui-rng-isolation-calibration.mt2f',
    'statistic-query-calibration.mt2f', 'statistic-overflow-calibration.mt2f',
    'statistic-zero-increment-calibration.mt2f', 'horde-stat-calibration.mt2f', 'enchantment-lifecycle-calibration.mt2f',
    'enchantment-combat-calibration.mt2f', 'enchantment-world-calibration.mt2f', 'enchantment-source-order-calibration.mt2f',
    'character-removal-calibration.mt2f',
    'enchantment-summon-calibration.mt2f', 'enchantment-summon-fresh-calibration.mt2f',
    'enchantment-summon-random-calibration.mt2f', 'enchantment-summon-random-fresh-calibration.mt2f',
    'preview-reference-calibration.mt2f',
    'preview-birth-calibration.mt2f', 'preview-birth-fresh-calibration.mt2f',
    'preview-birth-random-calibration.mt2f', 'preview-birth-random-fresh-calibration.mt2f',
    'physical-plane-calibration.mt2f', 'card-upgrade-mask-calibration.mt2f',
    'card-modifier-overflow-calibration.mt2f', 'card-trait-composition-calibration.mt2f',
    'card-status-composition-calibration.mt2f', 'card-owned-mask-calibration.mt2f',
    'card-upgrade-lifecycle-calibration.mt2f', 'relic-card-upgrade-calibration.mt2f', 'moon-card-upgrade-calibration.mt2f',
    'full-battle-original-deep-offering.mt2f', 'full-battle-original-molded.mt2f',
    'full-battle-original-broken-memories.mt2f', 'full-battle-original-wicked-blaze.mt2f',
    'full-battle-original-offering-token.mt2f', 'full-battle-original-moon-ritual.mt2f',
    'full-battle-original-self-purge-energy-gain.mt2f', 'full-battle-original-smelted-treasure.mt2f',
    'full-battle-original-pyre-gro.mt2f', 'full-battle-original-deadly-variant.mt2f',
    'full-battle-original-drain.mt2f', 'full-battle-original-starter-frostbite-discard.mt2f',
    'full-battle-original-feeding-frenzy.mt2f', 'full-battle-original-titans-gratitude.mt2f',
    'full-battle-original-minds-of-their-own.mt2f', 'full-battle-original-metallurgy.mt2f',
    'full-battle-original-fertile-ground.mt2f', 'full-battle-original-cover-of-night.mt2f',
    'full-battle-original-waxen-spike.mt2f', 'full-battle-original-imp-pressive.mt2f',
    'full-battle-original-tome-of-evergrowth.mt2f', 'full-battle-original-tome-of-the-lost-path.mt2f',
    'full-battle-original-rapid-raise.mt2f', 'full-battle-original-spawning-pods.mt2f',
    'full-battle-original-make-it-rain.mt2f', 'full-battle-original-deathroar.mt2f',
    'full-battle-original-secret-ingredient.mt2f', 'full-battle-original-green-recruit.mt2f',
    'full-battle-original-green-recruit-settled.mt2f',
    'full-battle-original-primordium.mt2f', 'full-battle-original-cheery-deathcap.mt2f',
    'full-battle-original-enthusiastic-fan.mt2f')
$selectedFixtureNames = $fixtureNames
if ($Fixture.Count -gt 0) {
    $selectedFixtureNames = @($Fixture | ForEach-Object { Split-Path -Leaf $_ })
    $unknown = @($selectedFixtureNames | Where-Object { $_ -notin $fixtureNames })
    if ($unknown.Count -gt 0) { throw "Unknown curated fixture: $($unknown -join ', ')" }
    if (@($selectedFixtureNames | Select-Object -Unique).Count -ne $selectedFixtureNames.Count) {
        throw 'Duplicate fixture selections are not allowed.'
    }
}
$fixtures = @($selectedFixtureNames | ForEach-Object { Get-TestFixturePath $_ })
& (Join-Path $PSScriptRoot 'Check-FixtureCatalog.ps1')
if ($Fixture.Count -gt 0) {
    Write-Host "FIXTURE-INTEGRITY PASS: $($fixtures.Count) selected of $($fixtureNames.Count) curated archives match the SHA-256 manifest."
} else {
    Write-Host "FIXTURE-INTEGRITY PASS: $($fixtures.Count) binary archives match the curated inventory and SHA-256 manifest."
}
# Explicit selections for this composition have complete dedicated gates.
# Avoid starting all unrelated pure checks before a component-only request.
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-inactive-fragile-inferno-new-moon-entry-calibration.mt2f', 'player-inactive-fragile-inferno-new-moon-entry-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Inactive Fragile Inferno new-moon entry checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --inactive-fragile-inferno-new-moon-entry-only (Get-TestFixturePath 'player-inactive-fragile-inferno-new-moon-entry-calibration.mt2f') (Get-TestFixturePath 'player-inactive-fragile-inferno-new-moon-entry-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Inactive Fragile Inferno new-moon entry native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-inactive-fragile-inferno-entry-calibration.mt2f', 'player-inactive-fragile-inferno-entry-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Inactive Fragile Inferno entry checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --inactive-fragile-inferno-entry-only (Get-TestFixturePath 'player-inactive-fragile-inferno-entry-calibration.mt2f') (Get-TestFixturePath 'player-inactive-fragile-inferno-entry-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Inactive Fragile Inferno entry native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-inactive-fullmoon-inferno-exit-calibration.mt2f', 'player-inactive-fullmoon-inferno-exit-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Inactive full-moon Inferno exit checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --inactive-fullmoon-inferno-exit-only (Get-TestFixturePath 'player-inactive-fullmoon-inferno-exit-calibration.mt2f') (Get-TestFixturePath 'player-inactive-fullmoon-inferno-exit-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Inactive full-moon Inferno exit native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-inactive-heavy-inferno-exit-calibration.mt2f', 'player-inactive-heavy-inferno-exit-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Inactive heavy Inferno exit checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --inactive-heavy-inferno-exit-only (Get-TestFixturePath 'player-inactive-heavy-inferno-exit-calibration.mt2f') (Get-TestFixturePath 'player-inactive-heavy-inferno-exit-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Inactive heavy Inferno exit native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-inactive-fragile-inferno-exit-calibration.mt2f', 'player-inactive-fragile-inferno-exit-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Inactive Fragile Inferno exit checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --inactive-fragile-inferno-exit-only (Get-TestFixturePath 'player-inactive-fragile-inferno-exit-calibration.mt2f') (Get-TestFixturePath 'player-inactive-fragile-inferno-exit-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Inactive Fragile Inferno exit native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-pending-physical-harvest-calibration.mt2f', 'player-pending-physical-harvest-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Pending physical harvest checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --pending-physical-harvest-only (Get-TestFixturePath 'player-pending-physical-harvest-calibration.mt2f') (Get-TestFixturePath 'player-pending-physical-harvest-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Pending physical harvest native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-inactive-multistrike-zealot-inferno-exit-calibration.mt2f', 'player-inactive-multistrike-zealot-inferno-exit-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Multistrike Zealot exit checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --inactive-multistrike-zealot-inferno-exit-only (Get-TestFixturePath 'player-inactive-multistrike-zealot-inferno-exit-calibration.mt2f') (Get-TestFixturePath 'player-inactive-multistrike-zealot-inferno-exit-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Multistrike Zealot exit native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-inactive-buffed-zealot-inferno-exit-calibration.mt2f', 'player-inactive-buffed-zealot-inferno-exit-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Buffed Zealot exit checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --inactive-buffed-zealot-inferno-exit-only (Get-TestFixturePath 'player-inactive-buffed-zealot-inferno-exit-calibration.mt2f') (Get-TestFixturePath 'player-inactive-buffed-zealot-inferno-exit-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Buffed Zealot exit native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-retired-titanskin-troop-calibration.mt2f', 'player-retired-titanskin-troop-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Retired Titanskin troop checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --retired-titanskin-troop-only (Get-TestFixturePath 'player-retired-titanskin-troop-calibration.mt2f') (Get-TestFixturePath 'player-retired-titanskin-troop-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Retired Titanskin troop native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-inactive-pyre-damage-inferno-exit-calibration.mt2f', 'player-inactive-pyre-damage-inferno-exit-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Inactive pyre-damage Inferno exit checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --inactive-pyre-damage-inferno-exit-only (Get-TestFixturePath 'player-inactive-pyre-damage-inferno-exit-calibration.mt2f') (Get-TestFixturePath 'player-inactive-pyre-damage-inferno-exit-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Inactive pyre-damage Inferno exit native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-third-troop-two-live-preview-calibration.mt2f', 'player-third-troop-two-live-preview-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Two-live preview checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --third-troop-two-live-preview-only (Get-TestFixturePath 'player-third-troop-two-live-preview-calibration.mt2f') (Get-TestFixturePath 'player-third-troop-two-live-preview-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Two-live preview native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-fight-club-injured-recruit-calibration.mt2f', 'player-fight-club-injured-recruit-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Fight Club injured recruit checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --fight-club-injured-recruit-only (Get-TestFixturePath 'player-fight-club-injured-recruit-calibration.mt2f') (Get-TestFixturePath 'player-fight-club-injured-recruit-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Fight Club injured recruit native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-fight-club-injured-companion-calibration.mt2f', 'player-fight-club-injured-companion-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Fight Club injured companion checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --fight-club-injured-companion-only (Get-TestFixturePath 'player-fight-club-injured-companion-calibration.mt2f') (Get-TestFixturePath 'player-fight-club-injured-companion-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Fight Club injured companion native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-untouchable-troop-aura-counterexample.mt2f', 'player-untouchable-troop-aura-observation.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Untouchable troop-aura checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --untouchable-troop-aura-native (Get-TestFixturePath 'player-untouchable-troop-aura-counterexample.mt2f') (Get-TestFixturePath 'player-untouchable-troop-aura-observation.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Untouchable troop-aura guard check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-triple-horde-retired-combat-calibration.mt2f', 'player-triple-horde-retired-combat-replay-calibration.mt2f', 'player-triple-horde-retired-combat-phase.mt2f', 'player-triple-horde-retired-combat-replay-phase.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Triple horde retired combat checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --triple-horde-retired-combat-only (Get-TestFixturePath 'player-triple-horde-phase-boundary-calibration.mt2f') (Get-TestFixturePath 'player-triple-horde-phase-boundary-experiment.mt2f') (Get-TestFixturePath 'player-triple-horde-phase-boundary-stage.mt2f') (Get-TestFixturePath 'player-triple-horde-retired-combat-calibration.mt2f') (Get-TestFixturePath 'player-triple-horde-retired-combat-replay-calibration.mt2f') (Get-TestFixturePath 'player-triple-horde-retired-combat-phase.mt2f') (Get-TestFixturePath 'player-triple-horde-retired-combat-replay-phase.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Triple horde retired combat native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-triple-horde-identity-map-calibration.mt2f', 'player-triple-horde-identity-map-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Horde identity map checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --horde-identity-map-only (Get-TestFixturePath 'player-triple-horde-phase-boundary-calibration.mt2f') (Get-TestFixturePath 'player-triple-horde-identity-map-calibration.mt2f') (Get-TestFixturePath 'player-triple-horde-identity-map-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Horde identity map native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-triple-horde-dual-trace-calibration.mt2f', 'player-triple-horde-dual-trace-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Dual trace horde checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --dual-trace-horde-only (Get-TestFixturePath 'player-triple-horde-phase-boundary-calibration.mt2f') (Get-TestFixturePath 'player-triple-horde-dual-trace-calibration.mt2f') (Get-TestFixturePath 'player-triple-horde-dual-trace-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Dual trace horde native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-triple-horde-phase-boundary-calibration.mt2f', 'player-triple-horde-phase-boundary-experiment.mt2f', 'player-triple-horde-phase-boundary-stage.mt2f', 'player-triple-horde-phase-boundary-timeline.mt2f', 'player-triple-horde-phase-boundary-timeline-replay.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Triple horde phase boundary checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --triple-horde-phase-boundary-only (Get-TestFixturePath 'player-triple-horde-phase-boundary-calibration.mt2f') (Get-TestFixturePath 'player-triple-horde-phase-boundary-experiment.mt2f') (Get-TestFixturePath 'player-triple-horde-phase-boundary-stage.mt2f') (Get-TestFixturePath 'player-triple-horde-phase-boundary-timeline.mt2f') (Get-TestFixturePath 'player-triple-horde-phase-boundary-timeline-replay.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Triple horde phase boundary native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-fight-club-missing-horde-preview-calibration.mt2f', 'player-fight-club-missing-horde-preview-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Fight Club missing horde preview checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --fight-club-missing-horde-preview-only (Get-TestFixturePath 'player-fight-club-missing-horde-preview-calibration.mt2f') (Get-TestFixturePath 'player-fight-club-missing-horde-preview-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Fight Club missing horde preview native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-fight-club-fourth-write-calibration.mt2f', 'player-fight-club-fourth-write-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Fight Club fourth write checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --fight-club-fourth-write-only (Get-TestFixturePath 'player-fight-club-fourth-write-calibration.mt2f') (Get-TestFixturePath 'player-fight-club-fourth-write-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Fight Club fourth write native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-three-companions-missing-preview-calibration.mt2f', 'player-three-companions-missing-preview-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Three companions missing preview checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --three-companions-missing-preview-only (Get-TestFixturePath 'player-three-companions-missing-preview-calibration.mt2f') (Get-TestFixturePath 'player-three-companions-missing-preview-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Three companions missing preview native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-four-horde-goldy-preview-calibration.mt2f', 'player-four-horde-goldy-preview-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Four horde Goldy preview checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --four-horde-goldy-preview-only (Get-TestFixturePath 'player-four-horde-goldy-preview-calibration.mt2f') (Get-TestFixturePath 'player-four-horde-goldy-preview-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Four horde Goldy preview native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-four-horde-combat-birth-calibration.mt2f', 'player-four-horde-combat-birth-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Four horde combat birth checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --four-horde-combat-birth-only (Get-TestFixturePath 'player-four-horde-combat-birth-calibration.mt2f') (Get-TestFixturePath 'player-four-horde-combat-birth-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Four horde combat birth native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-four-horde-retired-enchanter-calibration.mt2f', 'player-four-horde-retired-enchanter-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Four horde retired enchanter checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --four-horde-retired-enchanter-only (Get-TestFixturePath 'player-four-horde-retired-enchanter-calibration.mt2f') (Get-TestFixturePath 'player-four-horde-retired-enchanter-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Four horde retired enchanter native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-twin-sycophant-seven-preview-calibration.mt2f', 'player-twin-sycophant-seven-preview-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Twin sycophant seven preview checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --twin-sycophant-seven-preview-only (Get-TestFixturePath 'player-twin-sycophant-seven-preview-calibration.mt2f') (Get-TestFixturePath 'player-twin-sycophant-seven-preview-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Twin sycophant seven preview native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-steward-whelp-preview-calibration.mt2f', 'player-steward-whelp-preview-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Steward whelp preview checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --steward-whelp-preview-only (Get-TestFixturePath 'player-steward-whelp-preview-calibration.mt2f') (Get-TestFixturePath 'player-steward-whelp-preview-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Steward whelp preview native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-fight-club-upgraded-recruit-boss-calibration.mt2f', 'player-fight-club-upgraded-recruit-boss-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Fight Club upgraded recruit boss checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --fight-club-upgraded-recruit-boss-only (Get-TestFixturePath 'player-fight-club-upgraded-recruit-boss-calibration.mt2f') (Get-TestFixturePath 'player-fight-club-upgraded-recruit-boss-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Fight Club upgraded recruit boss native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-fight-club-wounded-companions-boss-calibration.mt2f', 'player-fight-club-wounded-companions-boss-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Fight Club wounded companions boss checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --fight-club-wounded-companions-boss-only (Get-TestFixturePath 'player-fight-club-wounded-companions-boss-calibration.mt2f') (Get-TestFixturePath 'player-fight-club-wounded-companions-boss-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Fight Club wounded companions boss native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-fight-club-wounded-steward-boss-calibration.mt2f', 'player-fight-club-wounded-steward-boss-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Fight Club wounded steward boss checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --fight-club-wounded-steward-boss-only (Get-TestFixturePath 'player-fight-club-wounded-steward-boss-calibration.mt2f') (Get-TestFixturePath 'player-fight-club-wounded-steward-boss-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Fight Club wounded steward boss native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-fight-club-steward-recruit-boss-calibration.mt2f', 'player-fight-club-steward-recruit-boss-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Fight Club steward recruit boss checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --fight-club-steward-recruit-boss-only (Get-TestFixturePath 'player-fight-club-steward-recruit-boss-calibration.mt2f') (Get-TestFixturePath 'player-fight-club-steward-recruit-boss-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Fight Club steward recruit boss native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-fight-club-dying-preview-calibration.mt2f', 'player-fight-club-dying-preview-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Fight Club dying preview checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --fight-club-dying-preview-only (Get-TestFixturePath 'player-fight-club-dying-preview-calibration.mt2f') (Get-TestFixturePath 'player-fight-club-dying-preview-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Fight Club dying preview native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-troop-cache-only-calibration.mt2f', 'player-troop-cache-only-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Troop cache-only checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --troop-cache-only (Get-TestFixturePath 'player-troop-cache-only-calibration.mt2f') (Get-TestFixturePath 'player-troop-cache-only-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Troop cache-only native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-twin-sycophant-preview-calibration.mt2f', 'player-twin-sycophant-preview-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Twin sycophant preview checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --twin-sycophant-preview-only (Get-TestFixturePath 'player-twin-sycophant-preview-calibration.mt2f') (Get-TestFixturePath 'player-twin-sycophant-preview-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Twin sycophant preview native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-armor-champion-preview-calibration.mt2f', 'player-armor-champion-preview-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Armor champion preview checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --armor-champion-preview-only (Get-TestFixturePath 'player-armor-champion-preview-calibration.mt2f') (Get-TestFixturePath 'player-armor-champion-preview-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Armor champion preview native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-club-wounded-companions-preview-calibration.mt2f', 'player-club-wounded-companions-preview-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Club wounded companions preview checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --club-wounded-companions-preview-only (Get-TestFixturePath 'player-club-wounded-companions-preview-calibration.mt2f') (Get-TestFixturePath 'player-club-wounded-companions-preview-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Club wounded companions preview native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-club-upgraded-steward-preview-calibration.mt2f', 'player-club-upgraded-steward-preview-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Club upgraded steward preview checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --club-upgraded-steward-preview-only (Get-TestFixturePath 'player-club-upgraded-steward-preview-calibration.mt2f') (Get-TestFixturePath 'player-club-upgraded-steward-preview-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Club upgraded steward preview native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-club-steward-preview-calibration.mt2f', 'player-club-steward-preview-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Club steward preview checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --club-steward-preview-only (Get-TestFixturePath 'player-club-steward-preview-calibration.mt2f') (Get-TestFixturePath 'player-club-steward-preview-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Club steward preview native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-club-nine-horde-calibration.mt2f', 'player-club-nine-horde-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Club nine horde checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --club-nine-horde-only (Get-TestFixturePath 'player-club-nine-horde-calibration.mt2f') (Get-TestFixturePath 'player-club-nine-horde-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Club nine horde native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-six-horde-fight-club-five-writes-calibration.mt2f', 'player-six-horde-fight-club-five-writes-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Five-write Fight Club checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --six-horde-fightclub-five-writes-only (Get-TestFixturePath 'player-six-horde-fight-club-five-writes-calibration.mt2f') (Get-TestFixturePath 'player-six-horde-fight-club-five-writes-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Five-write Fight Club native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-six-horde-fight-club-companions-calibration.mt2f', 'player-six-horde-fight-club-companions-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Six horde Fight Club companions checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --six-horde-fight-club-companions-only (Get-TestFixturePath 'player-six-horde-fight-club-companions-calibration.mt2f') (Get-TestFixturePath 'player-six-horde-fight-club-companions-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Six horde Fight Club companions native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-six-horde-companions-preview-calibration.mt2f', 'player-six-horde-companions-preview-replay-calibration.mt2f', 'player-six-horde-companions-health-calibration.mt2f', 'player-six-horde-companions-health-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Six horde companions preview checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --six-horde-companions-preview-only (Get-TestFixturePath 'player-six-horde-companions-preview-calibration.mt2f') (Get-TestFixturePath 'player-six-horde-companions-preview-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Six horde companions preview native check failed.' }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --six-horde-companions-preview-only (Get-TestFixturePath 'player-six-horde-companions-health-calibration.mt2f') (Get-TestFixturePath 'player-six-horde-companions-health-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Six horde companions health native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-fight-club-lava-boss-preview-calibration.mt2f', 'player-fight-club-lava-boss-preview-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Fight Club Lava boss preview checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --fight-club-lava-boss-preview-only (Get-TestFixturePath 'player-fight-club-lava-boss-preview-calibration.mt2f') (Get-TestFixturePath 'player-fight-club-lava-boss-preview-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Fight Club Lava boss preview native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-retired-puffball-five-writes-calibration.mt2f', 'player-retired-puffball-five-writes-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Retired puffball five writes checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --six-horde-retired-puffball-five-writes-only (Get-TestFixturePath 'player-retired-puffball-five-writes-calibration.mt2f') (Get-TestFixturePath 'player-retired-puffball-five-writes-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Retired puffball five writes native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-six-horde-retired-puffball-calibration.mt2f', 'player-six-horde-retired-puffball-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Retired puffball checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --six-horde-retired-puffball-only (Get-TestFixturePath 'player-six-horde-retired-puffball-calibration.mt2f') (Get-TestFixturePath 'player-six-horde-retired-puffball-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Retired puffball native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-six-horde-puffball-preview-calibration.mt2f', 'player-six-horde-puffball-preview-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Six horde puffball preview checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --six-horde-puffball-preview-only (Get-TestFixturePath 'player-six-horde-puffball-preview-calibration.mt2f') (Get-TestFixturePath 'player-six-horde-puffball-preview-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Six horde puffball preview native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-lava-fan-preview-calibration.mt2f', 'player-lava-fan-preview-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Lava fan preview checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --lava-fan-preview-only (Get-TestFixturePath 'player-lava-fan-preview-calibration.mt2f') (Get-TestFixturePath 'player-lava-fan-preview-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Lava fan preview native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-lava-six-writes-calibration.mt2f', 'player-lava-six-writes-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Lava six writes checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --lava-six-writes-only (Get-TestFixturePath 'player-lava-six-writes-calibration.mt2f') (Get-TestFixturePath 'player-lava-six-writes-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Lava six writes native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-lava-retired-recruits-preview-calibration.mt2f', 'player-lava-retired-recruits-preview-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Lava retired recruits preview checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --lava-retired-recruits-preview-only (Get-TestFixturePath 'player-lava-retired-recruits-preview-calibration.mt2f') (Get-TestFixturePath 'player-lava-retired-recruits-preview-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Lava retired recruits preview native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-lava-steward-live-horde-calibration.mt2f', 'player-lava-steward-live-horde-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Lava steward live horde checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --lava-steward-live-horde-only (Get-TestFixturePath 'player-lava-steward-live-horde-calibration.mt2f') (Get-TestFixturePath 'player-lava-steward-live-horde-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Lava steward live horde native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-lava-steward-horde-birth-calibration.mt2f', 'player-lava-steward-horde-birth-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Lava steward horde birth checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --lava-steward-horde-birth-only (Get-TestFixturePath 'player-lava-steward-horde-birth-calibration.mt2f') (Get-TestFixturePath 'player-lava-steward-horde-birth-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Lava steward horde birth native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-lava-steward-five-writes-calibration.mt2f', 'player-lava-steward-five-writes-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Lava steward five writes checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --lava-steward-five-writes-only (Get-TestFixturePath 'player-lava-steward-five-writes-calibration.mt2f') (Get-TestFixturePath 'player-lava-steward-five-writes-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Lava steward five writes native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-lava-steward-preview-calibration.mt2f', 'player-lava-steward-preview-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Lava steward preview checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --lava-steward-preview-only (Get-TestFixturePath 'player-lava-steward-preview-calibration.mt2f') (Get-TestFixturePath 'player-lava-steward-preview-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Lava steward preview native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-triple-horde-eight-writes-calibration.mt2f', 'player-triple-horde-eight-writes-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Triple horde eight writes checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --triple-horde-eight-writes-only (Get-TestFixturePath 'player-triple-horde-eight-writes-calibration.mt2f') (Get-TestFixturePath 'player-triple-horde-eight-writes-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Triple horde eight writes native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-triple-horde-retired-companions-calibration.mt2f', 'player-triple-horde-retired-companions-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Triple horde retired companions checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --triple-horde-retired-companions-only (Get-TestFixturePath 'player-triple-horde-retired-companions-calibration.mt2f') (Get-TestFixturePath 'player-triple-horde-retired-companions-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Triple horde retired companions native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-four-horde-retired-companions-calibration.mt2f', 'player-four-horde-retired-companions-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Four horde retired companions checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --four-horde-retired-companions-only (Get-TestFixturePath 'player-four-horde-retired-companions-calibration.mt2f') (Get-TestFixturePath 'player-four-horde-retired-companions-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Four horde retired companions native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-fight-club-whelp-death-calibration.mt2f', 'player-fight-club-whelp-death-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Fight Club whelp death checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --fight-club-whelp-death-only (Get-TestFixturePath 'player-fight-club-whelp-death-calibration.mt2f') (Get-TestFixturePath 'player-fight-club-whelp-death-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Fight Club whelp death native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-triple-horde-lava-target-death-calibration.mt2f', 'player-triple-horde-lava-target-death-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Triple horde Lava target death checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --triple-horde-lava-target-death-only (Get-TestFixturePath 'player-triple-horde-lava-target-death-calibration.mt2f') (Get-TestFixturePath 'player-triple-horde-lava-target-death-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Triple horde Lava target death native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-triple-horde-target-death-calibration.mt2f', 'player-triple-horde-target-death-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Triple horde target death checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --triple-horde-target-death-only (Get-TestFixturePath 'player-triple-horde-target-death-calibration.mt2f') (Get-TestFixturePath 'player-triple-horde-target-death-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Triple horde target death native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-retained-preview-physical-shift-calibration.mt2f', 'player-retained-preview-physical-shift-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Retained preview physical shift checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --retained-preview-physical-shift-only (Get-TestFixturePath 'player-retained-preview-physical-shift-calibration.mt2f') (Get-TestFixturePath 'player-retained-preview-physical-shift-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Retained preview physical shift native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-single-horde-fightclub-lava-calibration.mt2f', 'player-single-horde-fightclub-lava-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Single Fight Club Lava horde checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --single-horde-fightclub-lava-only (Get-TestFixturePath 'player-single-horde-fightclub-lava-calibration.mt2f') (Get-TestFixturePath 'player-single-horde-fightclub-lava-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Single Fight Club Lava horde native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-single-horde-sycophant-attack24-calibration.mt2f', 'player-single-horde-sycophant-attack24-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Single horde attack24 checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --single-horde-sycophant-attack24-only (Get-TestFixturePath 'player-single-horde-sycophant-attack24-calibration.mt2f') (Get-TestFixturePath 'player-single-horde-sycophant-attack24-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Single horde attack24 native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-single-horde-sycophant-calibration.mt2f', 'player-single-horde-sycophant-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Single horde sycophant checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --single-horde-sycophant-only (Get-TestFixturePath 'player-single-horde-sycophant-calibration.mt2f') (Get-TestFixturePath 'player-single-horde-sycophant-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Single horde sycophant native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-lava-fightclub-preview-calibration.mt2f', 'player-lava-fightclub-preview-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Lava Fight Club preview checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --lava-fightclub-preview-only (Get-TestFixturePath 'player-lava-fightclub-preview-calibration.mt2f') (Get-TestFixturePath 'player-lava-fightclub-preview-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Lava Fight Club preview native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-lava-sycophant-preview-calibration.mt2f', 'player-lava-sycophant-preview-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Lava sycophant preview checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --lava-sycophant-preview-only (Get-TestFixturePath 'player-lava-sycophant-preview-calibration.mt2f') (Get-TestFixturePath 'player-lava-sycophant-preview-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Lava sycophant preview native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-wounded-horde-steward-preview-calibration.mt2f', 'player-wounded-horde-steward-preview-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Wounded horde steward preview checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --wounded-horde-steward-preview-only (Get-TestFixturePath 'player-wounded-horde-steward-preview-calibration.mt2f') (Get-TestFixturePath 'player-wounded-horde-steward-preview-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Wounded horde steward preview native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-wounded-horde-preview-identity-calibration.mt2f', 'player-wounded-horde-preview-identity-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Wounded horde preview checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --wounded-horde-preview-identity-only (Get-TestFixturePath 'player-wounded-horde-preview-identity-calibration.mt2f') (Get-TestFixturePath 'player-wounded-horde-preview-identity-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Wounded horde preview native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-third-troop-fightclub-retired-preview-calibration.mt2f', 'player-third-troop-fightclub-retired-preview-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Fight Club retired preview checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --third-troop-fightclub-retired-preview-only (Get-TestFixturePath 'player-third-troop-fightclub-retired-preview-calibration.mt2f') (Get-TestFixturePath 'player-third-troop-fightclub-retired-preview-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Fight Club retired preview native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-generated-moon-mixed-retired-calibration.mt2f', 'player-generated-moon-mixed-retired-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Mixed retired generated moon checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --generated-moon-mixed-retired-only (Get-TestFixturePath 'player-generated-moon-mixed-retired-calibration.mt2f') (Get-TestFixturePath 'player-generated-moon-mixed-retired-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Mixed retired generated moon native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-generated-moon-ten-retired-calibration.mt2f', 'player-generated-moon-ten-retired-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Ten retired generated moon checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --generated-moon-ten-retired-only (Get-TestFixturePath 'player-generated-moon-ten-retired-calibration.mt2f') (Get-TestFixturePath 'player-generated-moon-ten-retired-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Ten retired generated moon native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-generated-moon-retired-enemies-calibration.mt2f', 'player-generated-moon-retired-enemies-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Retired generated moon checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --generated-moon-retired-enemies-only (Get-TestFixturePath 'player-generated-moon-retired-enemies-calibration.mt2f') (Get-TestFixturePath 'player-generated-moon-retired-enemies-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Retired generated moon native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-third-troop-two-retired-mixed-preview-calibration.mt2f', 'player-third-troop-two-retired-mixed-preview-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Two retired mixed preview checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --third-troop-two-retired-mixed-preview-only (Get-TestFixturePath 'player-third-troop-two-retired-mixed-preview-calibration.mt2f') (Get-TestFixturePath 'player-third-troop-two-retired-mixed-preview-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Two retired mixed preview native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-inactive-once-buffed-multistrike-zealot-alternative-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Alternative multistrike moon checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --inactive-once-buffed-multistrike-zealot-alternative-only (Get-TestFixturePath 'player-inactive-once-buffed-multistrike-zealot-alternative-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Alternative multistrike moon native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-two-plain-companions-preview-calibration.mt2f', 'player-two-plain-companions-preview-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Two plain companions preview checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --two-plain-companions-preview-only (Get-TestFixturePath 'player-two-plain-companions-preview-calibration.mt2f') (Get-TestFixturePath 'player-two-plain-companions-preview-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Two plain companions preview native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-fight-club-small-steward-missing-preview-calibration.mt2f', 'player-fight-club-small-steward-missing-preview-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Fight Club small steward missing-target preview checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --steward-companion-missing-target-preview-only (Get-TestFixturePath 'player-fight-club-small-steward-missing-preview-calibration.mt2f') (Get-TestFixturePath 'player-fight-club-small-steward-missing-preview-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Fight Club small steward missing-target preview native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-steward-companion-seven-writes-preview-calibration.mt2f', 'player-steward-companion-seven-writes-preview-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Steward companion seven-writes preview checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --steward-companion-seven-writes-preview-only (Get-TestFixturePath 'player-steward-companion-seven-writes-preview-calibration.mt2f') (Get-TestFixturePath 'player-steward-companion-seven-writes-preview-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Steward companion seven-writes preview native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-steward-companion-three-horde-preview-calibration.mt2f', 'player-steward-companion-three-horde-preview-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Steward companion three-horde preview checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --steward-companion-three-horde-preview-only (Get-TestFixturePath 'player-steward-companion-three-horde-preview-calibration.mt2f') (Get-TestFixturePath 'player-steward-companion-three-horde-preview-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Steward companion three-horde preview native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-steward-companion-six-horde-preview-calibration.mt2f', 'player-steward-companion-six-horde-preview-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Steward companion six-horde preview checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --steward-companion-six-horde-preview-only (Get-TestFixturePath 'player-steward-companion-six-horde-preview-calibration.mt2f') (Get-TestFixturePath 'player-steward-companion-six-horde-preview-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Steward companion six-horde preview native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-fight-club-wounded-steward-preview-calibration.mt2f', 'player-fight-club-wounded-steward-preview-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Fight Club wounded steward preview checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --fight-club-wounded-steward-preview-only (Get-TestFixturePath 'player-fight-club-wounded-steward-preview-calibration.mt2f') (Get-TestFixturePath 'player-fight-club-wounded-steward-preview-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Fight Club wounded steward preview native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-steward-fan-five-triple-preview-calibration.mt2f', 'player-steward-fan-five-triple-preview-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Steward fan five triple preview checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --steward-fan-five-triple-preview-only (Get-TestFixturePath 'player-steward-fan-five-triple-preview-calibration.mt2f') (Get-TestFixturePath 'player-steward-fan-five-triple-preview-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Steward fan five triple preview native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-steward-fan-six-horde-preview-calibration.mt2f', 'player-steward-fan-six-horde-preview-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Steward fan six-horde preview checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --steward-fan-six-horde-preview-only (Get-TestFixturePath 'player-steward-fan-six-horde-preview-calibration.mt2f') (Get-TestFixturePath 'player-steward-fan-six-horde-preview-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Steward fan six-horde preview native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-steward-whelp-preview-id-calibration.mt2f', 'player-steward-whelp-preview-id-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Steward whelp preview identity checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --steward-whelp-preview-id-only (Get-TestFixturePath 'player-steward-whelp-preview-id-calibration.mt2f') (Get-TestFixturePath 'player-steward-whelp-preview-id-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Steward whelp preview identity native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-retired-six-horde-preview-calibration.mt2f', 'player-retired-six-horde-preview-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Retired six-horde preview checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --retired-six-horde-preview-only (Get-TestFixturePath 'player-retired-six-horde-preview-calibration.mt2f') (Get-TestFixturePath 'player-retired-six-horde-preview-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Retired six-horde preview native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-inactive-twelve-buffed-multistrike-zealot-inferno-exit-calibration.mt2f', 'player-inactive-twelve-buffed-multistrike-zealot-inferno-exit-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Twelve-buffed multistrike moon checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --inactive-twelve-buffed-multistrike-zealot-inferno-exit-only (Get-TestFixturePath 'player-inactive-twelve-buffed-multistrike-zealot-inferno-exit-calibration.mt2f') (Get-TestFixturePath 'player-inactive-twelve-buffed-multistrike-zealot-inferno-exit-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Twelve-buffed multistrike moon native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-inactive-thrice-buffed-multistrike-zealot-inferno-exit-calibration.mt2f', 'player-inactive-thrice-buffed-multistrike-zealot-inferno-exit-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Thrice-buffed multistrike moon checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --inactive-thrice-buffed-multistrike-zealot-inferno-exit-only (Get-TestFixturePath 'player-inactive-thrice-buffed-multistrike-zealot-inferno-exit-calibration.mt2f') (Get-TestFixturePath 'player-inactive-thrice-buffed-multistrike-zealot-inferno-exit-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Thrice-buffed multistrike moon native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-inactive-once-buffed-multistrike-zealot-inferno-exit-calibration.mt2f', 'player-inactive-once-buffed-multistrike-zealot-inferno-exit-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Once-buffed multistrike moon checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --inactive-once-buffed-multistrike-zealot-inferno-exit-only (Get-TestFixturePath 'player-inactive-once-buffed-multistrike-zealot-inferno-exit-calibration.mt2f') (Get-TestFixturePath 'player-inactive-once-buffed-multistrike-zealot-inferno-exit-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Once-buffed multistrike moon native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-third-troop-retired-four-horde-preview-calibration.mt2f', 'player-third-troop-retired-four-horde-preview-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Retired four-horde preview checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --third-troop-retired-four-horde-preview-only (Get-TestFixturePath 'player-third-troop-retired-four-horde-preview-calibration.mt2f') (Get-TestFixturePath 'player-third-troop-retired-four-horde-preview-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Retired four-horde preview native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-six-horde-short-owner-calibration.mt2f', 'player-six-horde-short-owner-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Six-horde short owner checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --six-horde-short-owner-only (Get-TestFixturePath 'player-six-horde-short-owner-calibration.mt2f') (Get-TestFixturePath 'player-six-horde-short-owner-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Six-horde short owner native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-bone-dog-preview-leak-calibration.mt2f', 'player-bone-dog-preview-leak-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Bone Dog preview leak checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --bone-dog-preview-leak-only (Get-TestFixturePath 'player-bone-dog-preview-leak-calibration.mt2f') (Get-TestFixturePath 'player-bone-dog-preview-leak-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Bone Dog preview leak native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-four-horde-two-writes-calibration.mt2f', 'player-four-horde-two-writes-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Four-horde two writes checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --four-horde-two-writes-only (Get-TestFixturePath 'player-four-horde-two-writes-calibration.mt2f') (Get-TestFixturePath 'player-four-horde-two-writes-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Four-horde two writes native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-four-horde-eight-writes-calibration.mt2f', 'player-four-horde-eight-writes-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Four-horde eight writes checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --four-horde-eight-writes-only (Get-TestFixturePath 'player-four-horde-eight-writes-calibration.mt2f') (Get-TestFixturePath 'player-four-horde-eight-writes-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Four-horde eight writes native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-third-troop-four-horde-preview-calibration.mt2f', 'player-third-troop-four-horde-preview-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Four-horde preview checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --third-troop-four-horde-preview-only (Get-TestFixturePath 'player-third-troop-four-horde-preview-calibration.mt2f') (Get-TestFixturePath 'player-third-troop-four-horde-preview-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Four-horde preview native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-third-troop-whelp-combat-calibration.mt2f', 'player-third-troop-whelp-combat-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Whelp combat checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --third-troop-whelp-combat-only (Get-TestFixturePath 'player-third-troop-whelp-combat-calibration.mt2f') (Get-TestFixturePath 'player-third-troop-whelp-combat-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Whelp combat native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-third-troop-recruit-combat-calibration.mt2f', 'player-third-troop-recruit-combat-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Recruit combat checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --third-troop-recruit-combat-only (Get-TestFixturePath 'player-third-troop-recruit-combat-calibration.mt2f') (Get-TestFixturePath 'player-third-troop-recruit-combat-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Recruit combat native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-third-troop-sycophant-preview-calibration.mt2f', 'player-third-troop-sycophant-preview-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Sycophant preview checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --third-troop-sycophant-preview-only (Get-TestFixturePath 'player-third-troop-sycophant-preview-calibration.mt2f') (Get-TestFixturePath 'player-third-troop-sycophant-preview-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Sycophant preview native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-third-troop-triple-two-live-combat-calibration.mt2f', 'player-third-troop-triple-two-live-combat-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Triple two-live combat checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --third-troop-triple-two-live-combat-only (Get-TestFixturePath 'player-third-troop-triple-two-live-combat-calibration.mt2f') (Get-TestFixturePath 'player-third-troop-triple-two-live-combat-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Triple two-live combat native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-third-troop-original-mixed-combat-calibration.mt2f', 'player-third-troop-original-mixed-combat-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Original mixed combat checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --third-troop-original-mixed-combat-only (Get-TestFixturePath 'player-third-troop-original-mixed-combat-calibration.mt2f') (Get-TestFixturePath 'player-third-troop-original-mixed-combat-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Original mixed combat native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-third-troop-fightclub-mixed-calibration.mt2f', 'player-third-troop-fightclub-mixed-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'FightClub mixed troop checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --third-troop-fightclub-mixed-only (Get-TestFixturePath 'player-third-troop-fightclub-mixed-calibration.mt2f') (Get-TestFixturePath 'player-third-troop-fightclub-mixed-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'FightClub mixed troop native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-third-troop-sycophant-two-live-calibration.mt2f', 'player-third-troop-sycophant-two-live-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Sycophant two-live checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --third-troop-sycophant-two-live-only (Get-TestFixturePath 'player-third-troop-sycophant-two-live-calibration.mt2f') (Get-TestFixturePath 'player-third-troop-sycophant-two-live-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Sycophant two-live native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-preview-existing-horde-identity-calibration.mt2f', 'player-preview-existing-horde-identity-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Existing horde preview identity checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --preview-existing-horde-identity-only (Get-TestFixturePath 'player-preview-existing-horde-identity-calibration.mt2f') (Get-TestFixturePath 'player-preview-existing-horde-identity-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Existing horde preview identity native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-preview-two-live-identity-calibration.mt2f', 'player-preview-two-live-identity-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Preview two-live identity checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --preview-two-live-identity-only (Get-TestFixturePath 'player-preview-two-live-identity-calibration.mt2f') (Get-TestFixturePath 'player-preview-two-live-identity-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Preview two-live identity native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-third-troop-fragile-two-live-calibration.mt2f', 'player-third-troop-fragile-two-live-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Two live Fragile troop checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --third-troop-fragile-two-live-only (Get-TestFixturePath 'player-third-troop-fragile-two-live-calibration.mt2f') (Get-TestFixturePath 'player-third-troop-fragile-two-live-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Two live Fragile troop native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-inactive-heavy-inferno-new-moon-entry-calibration.mt2f', 'player-inactive-heavy-inferno-new-moon-entry-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Inactive heavy Inferno new moon entry checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --inactive-heavy-inferno-new-moon-entry-only (Get-TestFixturePath 'player-inactive-heavy-inferno-new-moon-entry-calibration.mt2f') (Get-TestFixturePath 'player-inactive-heavy-inferno-new-moon-entry-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Inactive heavy Inferno new moon entry native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-inactive-heavy-inferno-entry-calibration.mt2f', 'player-inactive-heavy-inferno-entry-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Inactive heavy Inferno entry checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --inactive-heavy-inferno-entry-only (Get-TestFixturePath 'player-inactive-heavy-inferno-entry-calibration.mt2f') (Get-TestFixturePath 'player-inactive-heavy-inferno-entry-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Inactive heavy Inferno entry native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-third-troop-fragile-mixed-preview-calibration.mt2f', 'player-third-troop-fragile-mixed-preview-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Mixed Fragile preview checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --third-troop-fragile-mixed-preview-only (Get-TestFixturePath 'player-third-troop-fragile-mixed-preview-calibration.mt2f') (Get-TestFixturePath 'player-third-troop-fragile-mixed-preview-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Mixed Fragile preview native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-third-troop-fragile-upper-combat-calibration.mt2f', 'player-third-troop-fragile-upper-combat-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Upper Fragile combat checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --third-troop-fragile-upper-combat-only (Get-TestFixturePath 'player-third-troop-fragile-upper-combat-calibration.mt2f') (Get-TestFixturePath 'player-third-troop-fragile-upper-combat-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Upper Fragile combat native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-third-troop-fragile-upper-preview-calibration.mt2f', 'player-third-troop-fragile-upper-preview-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Upper Fragile preview checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --third-troop-fragile-upper-preview-only (Get-TestFixturePath 'player-third-troop-fragile-upper-preview-calibration.mt2f') (Get-TestFixturePath 'player-third-troop-fragile-upper-preview-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Upper Fragile preview native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-third-troop-fragile-preview-calibration.mt2f', 'player-third-troop-fragile-preview-rng-calibration.mt2f', 'player-third-troop-fragile-preview-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Third troop Fragile preview checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --third-troop-fragile-preview-only (Get-TestFixturePath 'player-third-troop-fragile-preview-calibration.mt2f') (Get-TestFixturePath 'player-third-troop-fragile-preview-rng-calibration.mt2f') (Get-TestFixturePath 'player-third-troop-fragile-preview-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Third troop Fragile preview native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-third-troop-fragile-buff-calibration.mt2f', 'player-third-troop-fragile-buff-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Third troop Fragile checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --third-troop-fragile-buff-only (Get-TestFixturePath 'player-third-troop-fragile-buff-calibration.mt2f') (Get-TestFixturePath 'player-third-troop-fragile-buff-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Third troop Fragile native check failed.' }
    return
}
$inactiveBossMoonNames = @('player-inactive-fragile-moon-birth-calibration.mt2f', 'player-inactive-fragile-moon-birth-replay-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $inactiveBossMoonNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Inactive outer-boss moon checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --inactive-outer-boss-moon-birth-only (Get-TestFixturePath 'player-inactive-fragile-moon-birth-calibration.mt2f') (Get-TestFixturePath 'player-inactive-fragile-moon-birth-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Inactive outer-boss moon native prefix failed.' }
    return
}
$powerstoneJuiceNames = @('player-pyregel-powerstone-juice-calibration.mt2f', 'player-pyregel-powerstone-juice-replay-calibration.mt2f', 'player-pyregel-powerstone-juice-cast-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $powerstoneJuiceNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Powerstone Juice checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --pyregel-powerstone-juice-only (Get-TestFixturePath 'player-pyregel-powerstone-juice-calibration.mt2f') (Get-TestFixturePath 'player-pyregel-powerstone-juice-replay-calibration.mt2f') (Get-TestFixturePath 'player-pyregel-powerstone-juice-cast-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Powerstone Juice native component failed.' }
    return
}
$duskRecruitPreviewNames = @('player-dusk-mantle-recruit-boss-preview-calibration.mt2f', 'player-dusk-mantle-recruit-boss-preview-replay-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $duskRecruitPreviewNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Dusk Mantle recruit checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --dusk-mantle-recruit-boss-preview-only (Get-TestFixturePath 'player-dusk-mantle-recruit-boss-preview-calibration.mt2f') (Get-TestFixturePath 'player-dusk-mantle-recruit-boss-preview-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Dusk Mantle recruit native component failed.' }
    return
}
$lowerOpeningNames = @('parallel-lower-opening-model-counterexample.mt2f', 'parallel-lower-opening-route-model-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $lowerOpeningNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Lower-floor opening checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --parallel-lower-opening-only (Get-TestFixturePath 'parallel-lower-opening-model-counterexample.mt2f') (Get-TestFixturePath 'parallel-lower-opening-route-model-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Lower-floor opening portfolio failed.' }
    return
}
$thirdPreviewNames = @('player-underlegion-third-troop-preview-calibration.mt2f', 'player-underlegion-third-troop-preview-replay-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $thirdPreviewNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Third-tier champion preview checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --underlegion-third-troop-preview-only (Get-TestFixturePath 'player-underlegion-third-troop-preview-calibration.mt2f') (Get-TestFixturePath 'player-underlegion-third-troop-preview-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Third-tier champion preview native component failed.' }
    return
}
$firstMoonNames = @('player-generated-moon-first-transition-calibration.mt2f', 'player-generated-moon-first-transition-replay-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $firstMoonNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'First tracked moon transition checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --generated-moon-first-transition-only (Get-TestFixturePath 'player-generated-moon-first-transition-calibration.mt2f') (Get-TestFixturePath 'player-generated-moon-first-transition-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'First tracked moon transition native component failed.' }
    return
}
$generatedMoonNames = @('player-generated-moon-tracking-calibration.mt2f', 'player-generated-moon-tracking-replay-calibration.mt2f', 'player-inactive-moon-room-attachment-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $generatedMoonNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Generated moon tracking checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --generated-moon-tracking-only (Get-TestFixturePath 'player-generated-moon-tracking-calibration.mt2f') (Get-TestFixturePath 'player-generated-moon-tracking-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Generated moon tracking native component failed.' }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --inactive-moon-attachment-only (Get-TestFixturePath 'player-inactive-moon-room-attachment-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Inactive room attachment native prefix failed.' }
    return
}
$sample17RefinedNames = @('full-battle-random-campaign-17-ring3-release178-offline-search.mt2f', 'full-battle-random-campaign-17-ring3-release178-offline-replay.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $sample17RefinedNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Refined campaign checker build failed.' }
    }
    $refinedArguments = @('--random-refined-campaign-pair-only') + @($sample17RefinedNames | ForEach-Object { Get-TestFixturePath $_ }) + @('278140591','3','2','Hellhorned','Banished','34')
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @refinedArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused refined campaign pair failed.' }
    return
}
$untouchableFrontNames = @('player-untouchable-front-relic-counterexample-calibration.mt2f', 'player-untouchable-front-relic-calibration.mt2f', 'player-untouchable-front-relic-replay-calibration.mt2f')
$stasisShatterNames = @('player-stasis-shatter-counterexample-calibration.mt2f', 'player-stasis-shatter-calibration.mt2f', 'player-stasis-shatter-replay-calibration.mt2f')
$tripleJuiceNames = @('player-triple-juice-spike-refusal-calibration.mt2f', 'player-triple-juice-spike-calibration.mt2f', 'player-triple-juice-spike-replay-calibration.mt2f')
$doubleJuiceNames = @('player-double-juice-spike-refusal-calibration.mt2f', 'player-double-juice-spike-calibration.mt2f', 'player-double-juice-spike-replay-calibration.mt2f')
$doubleCurseNames = @('player-double-juice-curse-calibration.mt2f', 'player-double-juice-curse-replay-calibration.mt2f')
$roomProvenanceNames = @('player-room-upgrade-provenance-counterexample-calibration.mt2f', 'player-room-upgrade-provenance-calibration.mt2f')
$unboxMoonNames = @('player-unbox-moon-relic-calibration.mt2f', 'player-unbox-moon-relic-replay-calibration.mt2f')
$inherentJuiceNames = @('player-inherent-juice-spike-refusal-calibration.mt2f', 'player-inherent-juice-spike-calibration.mt2f', 'player-inherent-juice-spike-replay-calibration.mt2f')
$moreMassMoonNames = @('player-moremass-moon-birth-calibration.mt2f', 'player-moremass-moon-birth-replay-calibration.mt2f')
$unplacedMoreMassNames = @('player-unplaced-moremass-moon-counterexample-calibration.mt2f', 'player-unplaced-moremass-moon-replay-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $unplacedMoreMassNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Unplaced MoreMass moon checker build failed.' }
    }
    $unplacedMoreMassArguments = @('--unplaced-moremass-moon-only') + @($unplacedMoreMassNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @unplacedMoreMassArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused unplaced MoreMass moon checks failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $moreMassMoonNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'MoreMass moon birth checker build failed.' }
    }
    $moreMassMoonArguments = @('--moremass-moon-birth-only') + @($moreMassMoonNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @moreMassMoonArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused MoreMass moon birth checks failed.' }
    return
}
$lazarusConduitNames = @('player-lazarus-juice-conduit-calibration.mt2f', 'player-lazarus-juice-conduit-replay-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $lazarusConduitNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Lazarus Juice Conduit checker build failed.' }
    }
    $lazarusConduitArguments = @('--lazarus-juice-conduit-only') + @($lazarusConduitNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @lazarusConduitArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused Lazarus Juice Conduit checks failed.' }
    return
}
$summonRejectionNames = @('player-summon-capacity-rejection-calibration.mt2f', 'player-summon-capacity-rejection-replay-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $summonRejectionNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Summon capacity rejection checker build failed.' }
    }
    $summonRejectionArguments = @('--summon-capacity-rejection-only') + @($summonRejectionNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @summonRejectionArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused summon capacity rejection checks failed.' }
    return
}
$abilityMixNames = @('player-unit-ability-mix-choices-calibration.mt2f', 'player-unit-ability-mix-choices-replay-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $abilityMixNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Unit ability Mix checker build failed.' }
    }
    $abilityMixArguments = @('--unit-ability-mix-choices-only') + @($abilityMixNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @abilityMixArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused unit ability Mix checks failed.' }
    return
}
$echoMiddleNames = @('player-echo-juice-middle-preview-calibration.mt2f', 'player-echo-juice-middle-preview-replay-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $echoMiddleNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Middle-floor Echo checker build failed.' }
    }
    $echoMiddleArguments = @('--echo-juice-lower-preview-only') + @($echoMiddleNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @echoMiddleArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused middle-floor Echo checks failed.' }
    return
}
$echoJustCauseNames = @('player-echo-just-cause-copy-calibration.mt2f', 'player-echo-just-cause-endturn-calibration.mt2f', 'player-echo-just-cause-endturn-replay-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $echoJustCauseNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Echo Just Cause checker build failed.' }
    }
    $echoJustCauseArguments = @('--echo-just-cause-only') + @($echoJustCauseNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @echoJustCauseArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused Echo Just Cause checks failed.' }
    return
}
$lazarusJuiceNames = @('player-lazarus-juice-spike-deployment-refusal-calibration.mt2f', 'player-lazarus-juice-spike-cast-calibration.mt2f', 'player-lazarus-juice-spike-cast-replay-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $lazarusJuiceNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Lazarus Juice Spike checker build failed.' }
    }
    $lazarusJuiceArguments = @('--lazarus-juice-spike-only') + @($lazarusJuiceNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @lazarusJuiceArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused Lazarus Juice Spike checks failed.' }
    return
}
$echoJuiceNames = @('player-echo-juice-spike-copy-calibration.mt2f', 'player-echo-juice-spike-endturn-calibration.mt2f', 'player-echo-juice-spike-endturn-replay-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $echoJuiceNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Echo Juice Spike checker build failed.' }
    }
    $echoJuiceArguments = @('--echo-juice-spike-only') + @($echoJuiceNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @echoJuiceArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused Echo Juice Spike checks failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -ne 'player-ascend-deferred-destruction-counterexample-calibration.mt2f' }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Ascend destruction checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --ascend-destruction-boundary-only `
        (Get-TestFixturePath 'player-ascend-deferred-destruction-counterexample-calibration.mt2f') `
        (Get-TestFixturePath 'full-battle-random-campaign-20-ring3-release215-budget10000-search.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Focused Ascend destruction checks failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -ne 'player-multistrike-equipment-target-calibration.mt2f' }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Multistrike equipment target checker build failed.' }
    }
    $checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
    dotnet $checker --multistrike-equipment-target-only (Get-TestFixturePath 'full-battle-random-campaign-20-ring6-release223-budget10000-loss.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Multistrike equipment target counterexample failed.' }
    dotnet $checker --multistrike-equipment-alternative-only (Get-TestFixturePath 'player-multistrike-equipment-target-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Native Multistrike equipment alternative failed.' }
    return
}
$relentlessCompanionBumpNames = @('player-relentless-companion-bump-counterexample-calibration.mt2f', 'player-relentless-companion-bump-calibration.mt2f', 'player-relentless-companion-bump-replay-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $relentlessCompanionBumpNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Relentless companion Bump checker build failed.' }
    }
    $checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
    dotnet $checker --relentless-companion-bump-counterexample-only (Get-TestFixturePath $relentlessCompanionBumpNames[0])
    if ($LASTEXITCODE -ne 0) { throw 'Relentless companion Bump counterexample failed.' }
    dotnet $checker --relentless-companion-bump-only (Get-TestFixturePath $relentlessCompanionBumpNames[1]) (Get-TestFixturePath $relentlessCompanionBumpNames[2])
    if ($LASTEXITCODE -ne 0) { throw 'Relentless companion Bump native pair failed.' }
    return
}
$previewDeathShiftNames = @('player-preview-death-spawn-shift-calibration.mt2f', 'player-preview-death-spawn-shift-replay-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $previewDeathShiftNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Preview death-spawn shift checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --preview-death-spawn-shift-only `
        (Get-TestFixturePath $previewDeathShiftNames[0]) (Get-TestFixturePath $previewDeathShiftNames[1])
    if ($LASTEXITCODE -ne 0) { throw 'Focused preview death-spawn shift native pair failed.' }
    return
}
$ephemeralEquipmentNames = @('player-ephemeral-equipment-counterexample-calibration.mt2f', 'player-ephemeral-equipment-calibration.mt2f', 'player-ephemeral-equipment-replay-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $ephemeralEquipmentNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Ephemeral equipment checker build failed.' }
    }
    $checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
    dotnet $checker --ephemeral-equipment-counterexample-only (Get-TestFixturePath $ephemeralEquipmentNames[0])
    if ($LASTEXITCODE -ne 0) { throw 'Ephemeral equipment counterexample check failed.' }
    dotnet $checker --ephemeral-equipment-routing-only (Get-TestFixturePath $ephemeralEquipmentNames[1]) (Get-TestFixturePath $ephemeralEquipmentNames[2])
    if ($LASTEXITCODE -ne 0) { throw 'Focused Ephemeral equipment native pair failed.' }
    return
}
$greenRecruitEndTurnNames = @('player-green-recruit-endturn-identity-counterexample-calibration.mt2f', 'player-green-recruit-endturn-identity-calibration.mt2f', 'player-green-recruit-endturn-identity-replay-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $greenRecruitEndTurnNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Green Recruit EndTurn checker build failed.' }
    }
    $checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
    dotnet $checker --green-recruit-endturn-counterexample-only (Get-TestFixturePath $greenRecruitEndTurnNames[0])
    if ($LASTEXITCODE -ne 0) { throw 'Green Recruit EndTurn counterexample check failed.' }
    dotnet $checker --green-recruit-endturn-identity-only (Get-TestFixturePath $greenRecruitEndTurnNames[1]) (Get-TestFixturePath $greenRecruitEndTurnNames[2])
    if ($LASTEXITCODE -ne 0) { throw 'Focused Green Recruit EndTurn pair failed.' }
    return
}
$greenRecruitIdentityNames = @('player-green-recruit-preview-identity-counterexample-calibration.mt2f', 'player-green-recruit-preview-identity-calibration.mt2f', 'player-green-recruit-preview-identity-replay-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $greenRecruitIdentityNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Green Recruit preview identity checker build failed.' }
    }
    $checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
    dotnet $checker --green-recruit-preview-counterexample-only (Get-TestFixturePath $greenRecruitIdentityNames[0])
    if ($LASTEXITCODE -ne 0) { throw 'Green Recruit identity counterexample check failed.' }
    dotnet $checker --green-recruit-preview-identity-only (Get-TestFixturePath $greenRecruitIdentityNames[1]) (Get-TestFixturePath $greenRecruitIdentityNames[2])
    if ($LASTEXITCODE -ne 0) { throw 'Focused Green Recruit identity pair failed.' }
    return
}
$harvestAngelMoonNames = @('player-harvest-angel-moon-birth-calibration.mt2f', 'harvest-angel-moon-creation-calibration.mt2f', 'player-harvest-angel-moon-birth-replay-calibration.mt2f', 'harvest-angel-moon-creation-replay-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $harvestAngelMoonNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Harvest angel moon birth checker build failed.' }
    }
    $harvestAngelMoonArguments = @('--harvest-angel-moon-birth-only') + @($harvestAngelMoonNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @harvestAngelMoonArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused harvest angel moon birth checks failed.' }
    return
}
$echoPactNames = @('player-echo-remnant-pact-copy-calibration.mt2f', 'player-echo-remnant-pact-endturn-calibration.mt2f', 'player-echo-remnant-pact-endturn-replay-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $echoPactNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Echo Remnant Pact checker build failed.' }
    }
    $echoPactArguments = @('--echo-remnant-pact-only') + @($echoPactNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @echoPactArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused Echo Remnant Pact checks failed.' }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --endless-upgrade-target-only (Get-TestFixturePath 'player-echo-remnant-pact-copy-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Focused Endless upgrade target checks failed.' }
    return
}
$echoWaxenRelicsNames = @('player-echo-waxen-relics-copy-calibration.mt2f', 'player-echo-waxen-relics-endturn-calibration.mt2f', 'player-echo-waxen-relics-endturn-replay-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $echoWaxenRelicsNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Echo Waxen relic checker build failed.' }
    }
    $echoWaxenRelicsArguments = @('--echo-waxen-relics-only') + @($echoWaxenRelicsNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @echoWaxenRelicsArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused Echo Waxen relic checks failed.' }
    return
}
$echoWaxenNames = @('player-echo-waxen-spike-copy-calibration.mt2f', 'player-echo-waxen-spike-endturn-calibration.mt2f', 'player-echo-waxen-spike-endturn-replay-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $echoWaxenNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Echo Waxen Spike checker build failed.' }
    }
    $echoWaxenArguments = @('--echo-waxen-spike-only') + @($echoWaxenNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @echoWaxenArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused Echo Waxen Spike checks failed.' }
    return
}
$inherentCurseNames = @('player-inherent-juice-curse-calibration.mt2f', 'player-inherent-juice-curse-replay-calibration.mt2f')
$combatStatisticsNames = @('player-combat-victory-statistics-counterexample-calibration.mt2f', 'player-combat-victory-statistics-calibration.mt2f', 'player-combat-victory-cache-retention-counterexample-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $combatStatisticsNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Combat victory statistic checker build failed.' }
    }
    $combatStatisticsArguments = @('--combat-victory-statistics-only') + @($combatStatisticsNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @combatStatisticsArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused combat victory statistics failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $inherentCurseNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Inherent Juice curse checker build failed.' }
    }
    $inherentCurseArguments = @('--inherent-juice-curse-only') + @($inherentCurseNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @inherentCurseArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused inherent Juice curse failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $inherentJuiceNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Inherent Juice Spike checker build failed.' }
    }
    $inherentArguments = @('--inherent-juice-spike-only') + @($inherentJuiceNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @inherentArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused inherent Juice Spike pair failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $unboxMoonNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Unboxing with moon relic checker build failed.' }
    }
    $unboxArguments = @('--unbox-moon-relic-only') + @($unboxMoonNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @unboxArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused unboxing/moon relic pair failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $roomProvenanceNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Room upgrade provenance checker build failed.' }
    }
    $provenanceArguments = @('--room-upgrade-provenance-only') + @($roomProvenanceNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @provenanceArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused native room upgrade provenance check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $doubleCurseNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Double Juice curse checker build failed.' }
    }
    $curseArguments = @('--double-juice-curse-only') + @($doubleCurseNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @curseArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused double Juice curse pair failed.' }
    return
}
$fragileMoonNames = @('player-fragile-moon-birth-calibration.mt2f', 'fragile-moon-creation-calibration.mt2f', 'player-fragile-moon-birth-replay-calibration.mt2f', 'fragile-moon-creation-replay-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $fragileMoonNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Fragile moon checker build failed.' }
    }
    $fragileArguments = @('--fragile-moon-birth-only') + @($fragileMoonNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @fragileArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused Fragile moon pair failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $doubleJuiceNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Double Juice checker build failed.' }
    }
    $doubleArguments = @('--double-juice-spike-only') + @($doubleJuiceNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @doubleArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused double Juice pair failed.' }
    return
}
$tripleCurseNames = @('player-triple-juice-curse-calibration.mt2f', 'player-triple-juice-curse-replay-calibration.mt2f')
$tripleCurseCastNames = @('player-triple-juice-curse-cast-calibration.mt2f', 'player-triple-juice-curse-cast-replay-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $tripleCurseCastNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Post-curse cast checker build failed.' }
    }
    $castArguments = @('--triple-juice-curse-cast-only') + @($tripleCurseCastNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @castArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused post-curse cast pair failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $tripleCurseNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Triple Juice curse checker build failed.' }
    }
    $curseArguments = @('--triple-juice-curse-only') + @($tripleCurseNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @curseArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused triple Juice curse pair failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -ne 'player-triple-juice-search-failure-calibration.mt2f' }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Triple Juice search checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --triple-juice-search-only (Get-TestFixturePath 'player-triple-juice-search-failure-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Focused triple Juice search failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $tripleJuiceNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Triple Juice checker build failed.' }
    }
    $tripleArguments = @('--triple-juice-spike-only') + @($tripleJuiceNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @tripleArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused triple Juice pair failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $stasisShatterNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Stasis shatter checker build failed.' }
    }
    $shatterArguments = @('--stasis-shatter-only') + @($stasisShatterNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @shatterArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused Stasis shatter pair failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $untouchableFrontNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Untouchable front relic checker build failed.' }
    }
    $frontArguments = @('--untouchable-front-relic-only') + @($untouchableFrontNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @frontArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused Untouchable front relic pair failed.' }
    return
}
$untouchableRoomNames = @('player-untouchable-room-counterexample-calibration.mt2f', 'player-untouchable-room-calibration.mt2f', 'player-untouchable-room-replay-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $untouchableRoomNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Untouchable room checker build failed.' }
    }
    $untouchableArguments = @('--untouchable-room-attachment-only') + @($untouchableRoomNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @untouchableArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused Untouchable room pair failed.' }
    return
}
$moonSilenceNames = @('player-moon-silence-reset-counterexample-calibration.mt2f', 'player-moon-silence-reset-calibration.mt2f', 'player-moon-silence-reset-replay-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $moonSilenceNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Moon Silence checker build failed.' }
    }
    $silenceArguments = @('--moon-silence-reset-only') + @($moonSilenceNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @silenceArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused moon Silence pair failed.' }
    return
}
$automaticFocusNames = @('player-auto-focus-no-preview-calibration.mt2f', 'player-auto-focus-no-preview-prefix-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $automaticFocusNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Automatic focus checker build failed.' }
    }
    $focusArguments = @('--automatic-focus-preview-only') + @($automaticFocusNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @focusArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused automatic focus check failed.' }
    return
}
$abilityPresentationNames = @('player-ability-presentation-mutation-calibration.mt2f', 'player-ability-cache-display-replay-calibration.mt2f', 'player-ability-cache-winning-continuation-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $abilityPresentationNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Ability presentation checker build failed.' }
    }
    $presentationArguments = @('--ability-presentation-only') + @($abilityPresentationNames[0..1] | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @presentationArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused ability presentation counterexample failed.' }
    $cacheArguments = @('--ability-cache-continuation-only') + @($abilityPresentationNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @cacheArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused native cache continuation failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-discard-equipment-turn-calibration.mt2f', 'player-discard-equipment-turn-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Discard equipment turn checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --discard-equipment-turn-only `
        (Get-TestFixturePath 'player-discard-equipment-turn-calibration.mt2f') `
        (Get-TestFixturePath 'player-discard-equipment-turn-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Focused discard equipment turn check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -ne 'player-champion-aura-preview-retirement-calibration.mt2f' }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Champion aura preview retirement checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --champion-aura-preview-retirement-only `
        (Get-TestFixturePath 'player-champion-aura-preview-retirement-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Focused champion aura preview retirement check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-troop-birth-world-calibration.mt2f', 'player-troop-birth-world-replay-calibration.mt2f', 'player-athane-summon-calibration.mt2f', 'player-athane-summon-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Enemy birth checker build failed.' }
    }
    foreach ($group in @(@('troop-birth-world','player-troop-birth-world'), @('athane-summon','player-athane-summon'))) {
        if (@($selectedFixtureNames | Where-Object { $_.StartsWith($group[1] + '-') }).Count -eq 0) { continue }
        dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') "--$($group[0])-only" `
            (Get-TestFixturePath "$($group[1])-calibration.mt2f") `
            (Get-TestFixturePath "$($group[1])-replay-calibration.mt2f")
        if ($LASTEXITCODE -ne 0) { throw "Focused $($group[0]) check failed." }
    }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-pyregel-juice-calibration.mt2f', 'player-pyregel-juice-replay-calibration.mt2f', 'player-pyregel-juice-cast-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Pyregel Juice checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --pyregel-juice-only `
        (Get-TestFixturePath 'player-pyregel-juice-calibration.mt2f') `
        (Get-TestFixturePath 'player-pyregel-juice-replay-calibration.mt2f') `
        (Get-TestFixturePath 'player-pyregel-juice-cast-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Focused pyregel Juice check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-champion-enemy-death-preview-calibration.mt2f', 'player-champion-enemy-death-preview-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Champion enemy-death preview checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --champion-enemy-death-preview-only `
        (Get-TestFixturePath 'player-champion-enemy-death-preview-calibration.mt2f') `
        (Get-TestFixturePath 'player-champion-enemy-death-preview-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Focused champion enemy-death preview check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-treasure-despawn-movement-calibration.mt2f', 'player-treasure-despawn-movement-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Treasure despawn movement checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --treasure-despawn-movement-only `
        (Get-TestFixturePath 'player-treasure-despawn-movement-calibration.mt2f') `
        (Get-TestFixturePath 'player-treasure-despawn-movement-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Focused treasure despawn movement check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-titanskin-hit-calibration.mt2f', 'player-titanskin-hit-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Titanskin hit checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --titan-skin-hit-only `
        (Get-TestFixturePath 'player-titanskin-hit-calibration.mt2f') `
        (Get-TestFixturePath 'player-titanskin-hit-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Focused Titanskin hit check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-mirrored-meddler-moon-calibration.mt2f', 'player-mirrored-meddler-moon-replay-calibration.mt2f', 'player-mirrored-meddler-moon-coherent-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Mirrored Meddler checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --mirrored-meddler-moon-only `
        (Get-TestFixturePath 'player-mirrored-meddler-moon-calibration.mt2f') `
        (Get-TestFixturePath 'player-mirrored-meddler-moon-replay-calibration.mt2f') `
        (Get-TestFixturePath 'player-mirrored-meddler-moon-coherent-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Focused mirrored Meddler check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-starstruck-retired-target-calibration.mt2f', 'player-starstruck-retired-target-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Retired Starstruck target checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --starstruck-retired-target-only `
        (Get-TestFixturePath 'player-starstruck-retired-target-calibration.mt2f') `
        (Get-TestFixturePath 'player-starstruck-retired-target-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Focused retired Starstruck target check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-mirrored-existing-horde-calibration.mt2f', 'player-mirrored-existing-horde-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Mirrored existing Horde checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --mirrored-existing-horde-only `
        (Get-TestFixturePath 'player-mirrored-existing-horde-calibration.mt2f') `
        (Get-TestFixturePath 'player-mirrored-existing-horde-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Focused mirrored existing Horde check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-mirrored-troop-aura-birth-calibration.mt2f', 'player-mirrored-troop-aura-birth-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Mirrored troop aura birth checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --mirrored-troop-aura-birth-only `
        (Get-TestFixturePath 'player-mirrored-troop-aura-birth-calibration.mt2f') `
        (Get-TestFixturePath 'player-mirrored-troop-aura-birth-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Focused mirrored troop aura birth check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-mirrored-horde-birth-calibration.mt2f', 'player-mirrored-horde-birth-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Mirrored Horde birth checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --mirrored-horde-birth-only `
        (Get-TestFixturePath 'player-mirrored-horde-birth-calibration.mt2f') `
        (Get-TestFixturePath 'player-mirrored-horde-birth-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Focused mirrored Horde birth check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-cael-moon-null-relic-calibration.mt2f', 'player-cael-moon-null-relic-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Cael Moon null relic checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --cael-moon-null-relic-only `
        (Get-TestFixturePath 'player-cael-moon-null-relic-calibration.mt2f') `
        (Get-TestFixturePath 'player-cael-moon-null-relic-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Focused Cael Moon null relic check failed.' }
    return
}
$caelLifestealMoonNames = @('player-cael-lifesteal-moon-birth-calibration.mt2f', 'cael-lifesteal-moon-creation-calibration.mt2f',
    'player-cael-lifesteal-moon-birth-replay-calibration.mt2f', 'cael-lifesteal-moon-creation-replay-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $caelLifestealMoonNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Cael Lifesteal moon birth checker build failed.' }
    }
    $birthArguments = @('--cael-lifesteal-moon-birth-only') + @($caelLifestealMoonNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @birthArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused Cael Lifesteal moon birth check failed.' }
    return
}
$caelMoonNames = @('player-cael-moon-birth-calibration.mt2f', 'cael-moon-creation-calibration.mt2f',
    'player-cael-moon-birth-replay-calibration.mt2f', 'cael-moon-creation-replay-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $caelMoonNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Cael moon birth checker build failed.' }
    }
    $birthArguments = @('--cael-moon-birth-only') + @($caelMoonNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @birthArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused Cael moon birth pair failed.' }
    return
}
$fleshweaverMerchantNames = @('player-fleshweaver-merchant-birth-calibration.mt2f', 'fleshweaver-merchant-creation-calibration.mt2f',
    'player-fleshweaver-merchant-birth-replay-calibration.mt2f', 'fleshweaver-merchant-creation-replay-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $fleshweaverMerchantNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Fleshweaver merchant birth checker build failed.' }
    }
    $birthArguments = @('--fleshweaver-merchant-birth-only') + @($fleshweaverMerchantNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @birthArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused Fleshweaver merchant birth pair failed.' }
    return
}
$arkionMoonNames = @('player-arkion-moon-birth-calibration.mt2f', 'arkion-moon-creation-calibration.mt2f',
    'player-arkion-moon-birth-replay-calibration.mt2f', 'arkion-moon-creation-replay-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $arkionMoonNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Arkion moon birth checker build failed.' }
    }
    $moonArguments = @('--arkion-moon-birth-only') + @($arkionMoonNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @moonArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused Arkion moon birth pair failed.' }
    return
}
$outerMoonNames = @('player-outer-boss-moon-calibration.mt2f', 'outer-boss-moon-creation-calibration.mt2f',
    'player-outer-boss-moon-replay-calibration.mt2f', 'outer-boss-moon-creation-replay-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $outerMoonNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Outer-boss moon birth checker build failed.' }
    }
    $moonArguments = @('--outer-boss-moon-birth-only') + @($outerMoonNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @moonArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused outer-boss moon birth pair failed.' }
    return
}
$angelMoonNames = @('player-angel-duo-moon-birth-calibration.mt2f', 'angel-duo-moon-creation-calibration.mt2f',
    'player-angel-duo-moon-birth-replay-calibration.mt2f', 'angel-duo-moon-creation-replay-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $angelMoonNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Angel duo moon birth checker build failed.' }
    }
    $moonArguments = @('--angel-duo-moon-birth-only') + @($angelMoonNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @moonArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused angel duo moon birth pair failed.' }
    return
}
$retiredFrameNames = @('player-retired-actor-frame-calibration.mt2f', 'player-retired-actor-frame-replay-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $retiredFrameNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Retired actor frame checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --retired-actor-frame-only `
        (Get-TestFixturePath $retiredFrameNames[0]) `
        (Get-TestFixturePath 'full-battle-original-isolated-squad-leader-owner-death.mt2f') `
        (Get-TestFixturePath $retiredFrameNames[1])
    if ($LASTEXITCODE -ne 0) { throw 'Focused retired actor frame replay failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object {
    $_ -notin @('player-rank-three-room-slay-execution-calibration.mt2f', 'player-rank-three-room-slay-execution-replay-calibration.mt2f')
}).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Room slay execution checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --rank-three-room-slay-execution-only `
        (Get-TestFixturePath 'player-rank-three-room-slay-execution-calibration.mt2f') `
        (Get-TestFixturePath 'player-rank-three-room-slay-execution-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Independent native room slay execution pair failed.' }
    return
}
if ($Fixture.Count -eq 1 -and $selectedFixtureNames[0] -eq 'full-battle-random-campaign-14-ring6-release165-refined-loss.mt2f') {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Complete native battle checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --complete-native-battle-only $fixtures[0]
    if ($LASTEXITCODE -ne 0) { throw 'Focused complete native battle failed.' }
    return
}
if ($Fixture.Count -eq 1 -and $selectedFixtureNames[0] -eq 'full-battle-random-campaign-14-ring6-release157-refined-loss.mt2f') {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Conduit retirement full battle checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --conduit-retirement-battle-only $fixtures[0]
    if ($LASTEXITCODE -ne 0) { throw 'Focused Conduit retirement full battle failed.' }
    return
}
$remoteFrameNames = @('player-remote-preview-frame-calibration.mt2f',
    'player-remote-preview-frame-counterexample-calibration.mt2f', 'player-remote-preview-frame-replay-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $remoteFrameNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Remote preview frame checker build failed.' }
    }
    $remoteFrameArguments = @('--remote-preview-frame-only') + @($remoteFrameNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @remoteFrameArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused remote preview frame pair failed.' }
    return
}
$troopDeadCacheNames = @('player-troop-dead-cache-birth-calibration.mt2f', 'player-troop-dead-cache-birth-replay-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $troopDeadCacheNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Troop dead-cache birth checker build failed.' }
    }
    $deadCacheArguments = @('--troop-dead-cache-birth-only') + @($troopDeadCacheNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @deadCacheArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused troop dead-cache birth pair failed.' }
    return
}
$troopWriteNames = @('player-troop-write-world-calibration.mt2f', 'player-troop-write-world-replay-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $troopWriteNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Troop write world checker build failed.' }
    }
    $writeArguments = @('--troop-write-world-only') + @($troopWriteNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @writeArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused troop write world pair failed.' }
    return
}
$troopLastHasteNames = @('player-troop-last-haste-calibration.mt2f', 'player-troop-last-haste-replay-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $troopLastHasteNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Troop last Haste checker build failed.' }
    }
    $lastHasteArguments = @('--troop-last-haste-only') + @($troopLastHasteNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @lastHasteArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused troop last Haste pair failed.' }
    return
}
$troopCombatNames = @('player-troop-combat-birth-calibration.mt2f',
    'player-troop-combat-birth-counterexample-calibration.mt2f', 'player-troop-combat-birth-replay-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $troopCombatNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Troop combat birth checker build failed.' }
    }
    $combatArguments = @('--troop-combat-birth-only') + @($troopCombatNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @combatArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused troop combat birth pair failed.' }
    return
}
$troopRetiredNames = @('player-troop-retired-source-calibration.mt2f', 'player-troop-retired-source-replay-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $troopRetiredNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Troop retired source checker build failed.' }
    }
    $retiredArguments = @('--troop-retired-source-only') + @($troopRetiredNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @retiredArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused troop retired source pair failed.' }
    return
}
$troopBirthNames = @('player-troop-birth-addition-calibration.mt2f', 'player-troop-birth-addition-replay-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $troopBirthNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Troop birth addition checker build failed.' }
    }
    $birthArguments = @('--troop-birth-addition-only') + @($troopBirthNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @birthArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused troop birth addition pair failed.' }
    return
}
$troopPreviewNames = @('player-troop-preview-addition-calibration.mt2f', 'player-troop-preview-addition-replay-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $troopPreviewNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Troop preview addition checker build failed.' }
    }
    $troopArguments = @('--troop-preview-addition-only') + @($troopPreviewNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @troopArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused troop preview addition pair failed.' }
    return
}
$conduitRetirementNames = @('player-conduit-retirement-calibration.mt2f',
    'player-conduit-retirement-counterexample-calibration.mt2f', 'player-conduit-retirement-replay-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $conduitRetirementNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Conduit retirement checker build failed.' }
    }
    $retirementArguments = @('--conduit-retirement-only') + @($conduitRetirementNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @retirementArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused Conduit retirement pair failed.' }
    return
}
$generatedConduitNames = @('player-generated-conduit-copy-calibration.mt2f',
    'player-generated-conduit-copy-counterexample-calibration.mt2f', 'player-generated-conduit-copy-replay-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $generatedConduitNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Generated Conduit copy checker build failed.' }
    }
    $copyArguments = @('--generated-conduit-copy-only') + @($generatedConduitNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @copyArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused generated Conduit copy pair failed.' }
    return
}
if ($Fixture.Count -eq 1 -and $selectedFixtureNames[0] -eq 'player-terminal-coroutine-planes-counterexample-calibration.mt2f') {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Terminal coroutine capture checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --terminal-coroutine-planes-only $fixtures[0] `
        (Get-TestFixturePath 'full-battle-random-campaign-14-ring2-release143-budget10000-search.mt2f') `
        (Get-TestFixturePath 'full-battle-random-campaign-14-ring2-release143-budget10000-replay.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Focused terminal coroutine capture check failed.' }
    return
}
if ($Fixture.Count -eq 1 -and $selectedFixtureNames[0] -eq 'player-resumed-moon-capture-counterexample.mt2f') {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Resumed moon capture checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --resumed-moon-capture-only $fixtures[0] `
        (Get-TestFixturePath 'full-battle-random-campaign-14-ring1-release138-budget10000-search.mt2f') `
        (Get-TestFixturePath 'full-battle-random-campaign-14-ring1-release138-budget10000-replay.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Focused resumed moon capture check failed.' }
    return
}
$uninjuredBossNames = @('uninjured-pending-boss-model-counterexample-calibration.mt2f', 'uninjured-pending-boss-model-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $uninjuredBossNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Uninjured pending boss checker build failed.' }
    }
    $uninjuredBossArguments = @('--uninjured-pending-boss-only') + @($uninjuredBossNames | ForEach-Object { Get-TestFixturePath $_ })
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') @uninjuredBossArguments
    if ($LASTEXITCODE -ne 0) { throw 'Focused uninjured pending boss order check failed.' }
    return
}
if ($Fixture.Count -eq 1 -and $selectedFixtureNames[0] -eq 'pending-boss-loss-order-model-calibration.mt2f') {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Pending boss loss checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --pending-boss-loss-order-only `
        (Get-TestFixturePath 'full-battle-random-campaign-13-ring7-release137-budget10000-loss.mt2f') `
        (Get-TestFixturePath 'full-battle-random-campaign-13-ring7-release137-refined-loss.mt2f') $fixtures[0]
    if ($LASTEXITCODE -ne 0) { throw 'Focused pending boss loss order check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-consumed-curse-cleanse-calibration.mt2f', 'player-consumed-curse-cleanse-replay-calibration.mt2f', 'player-consumed-curse-cleanse-terminal-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Consumed curse Cleanse checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --consumed-curse-cleanse-only `
        (Get-TestFixturePath 'player-consumed-curse-cleanse-calibration.mt2f') `
        (Get-TestFixturePath 'player-consumed-curse-cleanse-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Focused consumed curse Cleanse native pair failed.' }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --consumed-curse-cleanse-terminal-only `
        (Get-TestFixturePath 'player-consumed-curse-cleanse-terminal-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Focused consumed curse Cleanse terminal check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-moon-advance-consume-calibration.mt2f', 'player-moon-advance-consume-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Moon advance/Consume checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --moon-advance-consume-only `
        (Get-TestFixturePath 'player-moon-advance-consume-calibration.mt2f') `
        (Get-TestFixturePath 'player-moon-advance-consume-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Focused Moon advance/Consume native pair failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-starstruck-conduit-calibration.mt2f', 'player-starstruck-conduit-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Starstruck Conduit checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --starstruck-conduit-only `
        (Get-TestFixturePath 'player-starstruck-conduit-calibration.mt2f') `
        (Get-TestFixturePath 'player-starstruck-conduit-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Focused Starstruck Conduit check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-starstruck-reduce-cost-calibration.mt2f', 'player-starstruck-reduce-cost-replay-calibration.mt2f', 'player-starstruck-reduce-cost-cast-calibration.mt2f', 'player-starstruck-reduce-cost-cast-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Starstruck ReduceCost checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --starstruck-reduce-cost-only `
        (Get-TestFixturePath 'player-starstruck-reduce-cost-calibration.mt2f') `
        (Get-TestFixturePath 'player-starstruck-reduce-cost-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Focused Starstruck ReduceCost native pair failed.' }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --starstruck-reduce-cost-cast-only `
        (Get-TestFixturePath 'player-starstruck-reduce-cost-cast-calibration.mt2f') `
        (Get-TestFixturePath 'player-starstruck-reduce-cost-cast-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Focused Starstruck ReduceCost actual cast pair failed.' }
    return
}
if ($Fixture.Count -eq 1 -and $selectedFixtureNames[0] -eq 'player-reanimate-terminal-point-calibration.mt2f') {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Reanimate terminal point checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --reanimate-terminal-point-only $fixtures[0]
    if ($LASTEXITCODE -ne 0) { throw 'Focused Reanimate terminal point check failed.' }
    return
}
if ($Fixture.Count -eq 1 -and $selectedFixtureNames[0] -eq 'player-green-recruit-preview-retirement-calibration.mt2f') {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Green Recruit preview retirement checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --green-recruit-preview-retirement-only $fixtures[0]
    if ($LASTEXITCODE -ne 0) { throw 'Focused Green Recruit preview retirement check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-unit-copy-birth-catalog-calibration.mt2f', 'player-unit-copy-birth-catalog-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Unit-copy birth catalog checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --unit-copy-birth-catalog-only `
        (Get-TestFixturePath 'player-unit-copy-birth-catalog-calibration.mt2f') `
        (Get-TestFixturePath 'player-unit-copy-birth-catalog-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Focused unit-copy birth catalog native pair failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-triple-juice-starstruck-calibration.mt2f', 'player-triple-juice-starstruck-replay-calibration.mt2f', 'player-triple-juice-starstruck-cast-calibration.mt2f', 'player-triple-juice-starstruck-cast-replay-calibration.mt2f', 'player-triple-juice-starstruck-curse-calibration.mt2f', 'player-triple-juice-starstruck-curse-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Triple-Juice Starstruck checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --triple-juice-starstruck-only `
        (Get-TestFixturePath 'player-triple-juice-starstruck-calibration.mt2f') `
        (Get-TestFixturePath 'player-triple-juice-starstruck-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Focused triple-Juice Starstruck native deployment pair failed.' }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --triple-juice-starstruck-cast-only `
        (Get-TestFixturePath 'player-triple-juice-starstruck-cast-calibration.mt2f') `
        (Get-TestFixturePath 'player-triple-juice-starstruck-cast-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Focused triple-Juice Starstruck native cast pair failed.' }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --triple-juice-starstruck-curse-only `
        (Get-TestFixturePath 'player-triple-juice-starstruck-curse-calibration.mt2f') `
        (Get-TestFixturePath 'player-triple-juice-starstruck-curse-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Focused triple-Juice Starstruck native curse pair failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-moon-trial-gold-relic-calibration.mt2f', 'player-moon-trial-gold-relic-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Moon trial-gold checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --moon-trial-gold-relic-only `
        (Get-TestFixturePath 'player-moon-trial-gold-relic-calibration.mt2f') `
        (Get-TestFixturePath 'player-moon-trial-gold-relic-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Focused Moon trial-gold native pair failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-terminal-pyre-capture-calibration.mt2f', 'player-terminal-pyre-capture-replay-calibration.mt2f', 'player-terminal-pyre-capture-raw-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Terminal Pyre capture checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --terminal-pyre-capture-only `
        (Get-TestFixturePath 'player-terminal-pyre-capture-calibration.mt2f') `
        (Get-TestFixturePath 'player-terminal-pyre-capture-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Terminal Pyre capture check failed.' }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --inferno-pre-combat-positions-only `
        (Get-TestFixturePath 'player-terminal-pyre-capture-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Inferno pre-combat positions check failed.' }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --inferno-placement-policy-only `
        (Get-TestFixturePath 'player-terminal-pyre-capture-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Inferno placement policy check failed.' }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --native-room-train-only `
        (Get-TestFixturePath 'player-terminal-pyre-capture-raw-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Fixed native room/train replay failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-underlegion-third-troop-upgrade-calibration.mt2f', 'player-underlegion-third-troop-upgrade-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Third-tier Underlegion upgrade checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --underlegion-third-troop-upgrade-only `
        (Get-TestFixturePath 'player-underlegion-third-troop-upgrade-calibration.mt2f') `
        (Get-TestFixturePath 'player-underlegion-third-troop-upgrade-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Third-tier Underlegion upgrade check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-underlegion-second-troop-upgrade-calibration.mt2f', 'player-underlegion-second-troop-upgrade-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Second-tier Underlegion checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --underlegion-second-troop-upgrade-only `
        (Get-TestFixturePath 'player-underlegion-second-troop-upgrade-calibration.mt2f') `
        (Get-TestFixturePath 'player-underlegion-second-troop-upgrade-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Focused second-tier Underlegion birth check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-underlegion-troop-upgrade-calibration.mt2f', 'player-underlegion-troop-upgrade-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Underlegion troop upgrade checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --underlegion-troop-upgrade-only `
        (Get-TestFixturePath 'player-underlegion-troop-upgrade-calibration.mt2f') `
        (Get-TestFixturePath 'player-underlegion-troop-upgrade-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Focused Underlegion troop upgrade native pair failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin @('player-targeted-damage-calibration.mt2f', 'player-targeted-damage-replay-calibration.mt2f') }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Targeted damage checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --targeted-damage-only `
        (Get-TestFixturePath 'full-battle-random-campaign-22-ring6-release235-budget10000-loss.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Focused targeted damage counterexample check failed.' }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --targeted-damage-alternative-only `
        (Get-TestFixturePath 'player-targeted-damage-calibration.mt2f') `
        (Get-TestFixturePath 'player-targeted-damage-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Focused targeted damage native pair failed.' }
    return
}
if ($Fixture.Count -eq 1 -and $selectedFixtureNames[0] -eq 'parallel-opening-portfolio-model-calibration.mt2f') {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Opening portfolio checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --parallel-opening-portfolio-only `
        $fixtures[0] (Get-TestFixturePath 'parallel-horizon-route-model-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Focused opening portfolio check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notlike 'parallel-horizon-*-model-calibration.mt2f' }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Parallel horizon checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --parallel-horizon-only `
        (Get-TestFixturePath 'parallel-horizon-shallow-model-calibration.mt2f') `
        (Get-TestFixturePath 'parallel-horizon-route-model-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Focused parallel horizon check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notlike 'parallel-planner-merge-model-*.mt2f' }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Parallel merge checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --parallel-planner-merge-only `
        (Get-TestFixturePath 'parallel-planner-merge-model-calibration.mt2f') `
        (Get-TestFixturePath 'parallel-planner-merge-model-counterexample.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Focused parallel merge check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -ne 'full-battle-terminal-enemy-slot-calibration.mt2f' }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Terminal stage checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --terminal-enemy-slot-stages-only $fixtures[0]
    if ($LASTEXITCODE -ne 0) { throw 'Terminal room/train stage acceptance failed.' }
    return
}
if ($Fixture.Count -eq 1 -and $selectedFixtureNames[0] -eq 'full-battle-native-covenant-03-ring7-release110-budget10000-loss.mt2f') {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Natural Covenant default-loss checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --random-campaign-source-only $fixtures[0] 424242 3 6 Banished Pyreborne 10000
    if ($LASTEXITCODE -ne 0) { throw 'Natural Covenant default-loss check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object {
    $_ -notin @('full-battle-native-covenant-03-ring6-refined-search.mt2f', 'full-battle-native-covenant-03-ring6-refined-replay.mt2f')
}).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Natural Covenant refined-pair checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --random-refined-campaign-pair-only (Get-TestFixturePath 'full-battle-native-covenant-03-ring6-refined-search.mt2f') (Get-TestFixturePath 'full-battle-native-covenant-03-ring6-refined-replay.mt2f') 424242 3 5 Banished Pyreborne 35
    if ($LASTEXITCODE -ne 0) { throw 'Natural Covenant refined-pair check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object {
    $_ -notin @('full-battle-random-campaign-11-ring7-refined-search.mt2f', 'full-battle-random-campaign-11-ring7-refined-replay.mt2f')
}).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Random campaign Ring7 refined-pair checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --random-refined-campaign-pair-only (Get-TestFixturePath 'full-battle-random-campaign-11-ring7-refined-search.mt2f') (Get-TestFixturePath 'full-battle-random-campaign-11-ring7-refined-replay.mt2f') 73751504 0 6 Stygian Awoken 36
    if ($LASTEXITCODE -ne 0) { throw 'Random campaign Ring7 refined-pair check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object {
    $_ -notin @('player-effect-test-rng-counterexample.mt2f', 'player-effect-test-rng-observed.mt2f')
}).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Effect query RNG checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --effect-test-rng-counterexample-only (Get-TestFixturePath 'player-effect-test-rng-counterexample.mt2f') (Get-TestFixturePath 'player-effect-test-rng-observed.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Effect query RNG counterexample check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object {
    $_ -notin @('player-hand-highlight-test-rng-counterexample.mt2f', 'player-hand-highlight-test-rng-observed.mt2f')
}).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Hand highlight RNG checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --hand-highlight-rng-counterexample-only (Get-TestFixturePath 'player-hand-highlight-test-rng-counterexample.mt2f') (Get-TestFixturePath 'player-hand-highlight-test-rng-observed.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Hand highlight RNG counterexample check failed.' }
    return
}
if ($Fixture.Count -eq 1 -and $selectedFixtureNames[0] -eq 'player-unit-ability-duality-counterexample.mt2f') {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Unit ability Duality checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --unit-ability-duality-counterexample-only (Get-TestFixturePath $selectedFixtureNames[0])
    if ($LASTEXITCODE -ne 0) { throw 'Unit ability Duality counterexample check failed.' }
    return
}
if ($Fixture.Count -eq 1 -and $selectedFixtureNames[0] -in @('player-terminal-companion-slot-counterexample.mt2f', 'player-terminal-companion-slot-observation.mt2f')) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Terminal companion slot checker build failed.' }
    }
    $mode = if ($selectedFixtureNames[0] -eq 'player-terminal-companion-slot-observation.mt2f') { '--terminal-companion-slot-observation-only' } else { '--terminal-companion-slot-only' }
    $nativeFiles = @((Get-TestFixturePath $selectedFixtureNames[0]))
    if ($mode -eq '--terminal-companion-slot-only') { $nativeFiles += Get-TestFixturePath 'player-terminal-companion-slot-replay.mt2f' }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') $mode @nativeFiles
    if ($LASTEXITCODE -ne 0) { throw 'Terminal companion slot check failed.' }
    return
}
if ($Fixture.Count -eq 1 -and $selectedFixtureNames[0] -eq 'player-terminal-companion-roster-counterexample.mt2f') {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Terminal companion checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --terminal-companion-roster-only (Get-TestFixturePath $selectedFixtureNames[0])
    if ($LASTEXITCODE -ne 0) { throw 'Terminal companion counterexample check failed.' }
    return
}
if ($Fixture.Count -eq 1 -and $selectedFixtureNames[0] -eq 'player-room-pre-combat-world-root.mt2f') {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Room pre-combat world checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --room-pre-combat-world-root-only (Get-TestFixturePath $selectedFixtureNames[0])
    if ($LASTEXITCODE -ne 0) { throw 'Room pre-combat shared-world root checks failed.' }
    return
}
if ($Fixture.Count -eq 1 -and $selectedFixtureNames[0] -in @('player-green-recruit-boss-preview-calibration.mt2f', 'player-green-recruit-boss-preview-replay-calibration.mt2f')) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Green Recruit boss-preview checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --green-recruit-boss-preview-only `
        (Get-TestFixturePath 'player-green-recruit-boss-preview-calibration.mt2f') `
        (Get-TestFixturePath 'player-green-recruit-boss-preview-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Green Recruit boss-preview check failed.' }
    return
}
if ($Fixture.Count -eq 1 -and $selectedFixtureNames[0] -in @('player-inferno-removal-queue-calibration.mt2f', 'player-inferno-removal-queue-replay-calibration.mt2f')) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Inferno removal queue checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --inferno-removal-queue-only `
        (Get-TestFixturePath 'player-inferno-removal-queue-calibration.mt2f') `
        (Get-TestFixturePath 'player-inferno-removal-queue-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Inferno removal queue check failed.' }
    return
}
if ($Fixture.Count -eq 1 -and $selectedFixtureNames[0] -in @('player-champion-meat-shield-boss-preview-calibration.mt2f', 'player-champion-meat-shield-boss-preview-replay-calibration.mt2f')) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Champion MeatShield boss-preview checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --champion-meat-shield-boss-preview-only `
        (Get-TestFixturePath 'player-champion-meat-shield-boss-preview-calibration.mt2f') `
        (Get-TestFixturePath 'player-champion-meat-shield-boss-preview-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Champion MeatShield boss-preview check failed.' }
    return
}
if ($Fixture.Count -eq 1 -and $selectedFixtureNames[0] -in @('player-champion-meat-shield-preview-calibration.mt2f', 'player-champion-meat-shield-preview-replay-calibration.mt2f')) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Champion MeatShield preview checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --champion-meat-shield-preview-only `
        (Get-TestFixturePath 'player-champion-meat-shield-preview-calibration.mt2f') `
        (Get-TestFixturePath 'player-champion-meat-shield-preview-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Champion MeatShield preview check failed.' }
    return
}
if ($Fixture.Count -eq 1 -and $selectedFixtureNames[0] -in @('full-battle-player-harvest-listener-snapshot-calibration.mt2f', 'full-battle-player-harvest-listener-snapshot-replay-calibration.mt2f')) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Harvest listener checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --harvest-listener-snapshot-only `
        (Get-TestFixturePath 'full-battle-player-harvest-listener-snapshot-calibration.mt2f') `
        (Get-TestFixturePath 'full-battle-player-harvest-listener-snapshot-replay-calibration.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Harvest listener native check failed.' }
    return
}
if ($Fixture.Count -eq 1 -and $selectedFixtureNames[0] -eq 'player-retaliation-revival-calibration.mt2f') {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Retaliation revival checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --retaliation-revival-only (Get-TestFixturePath $selectedFixtureNames[0])
    if ($LASTEXITCODE -ne 0) { throw 'Retaliation revival native check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and $selectedFixtureNames.Count -eq 1 -and
    $selectedFixtureNames[0] -in @('player-unsupported-card-ranking-calibration.mt2f', 'player-prepared-selection-publication-calibration.mt2f')) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Unsupported ranking checker build failed.' }
    }
    $mode = if ($selectedFixtureNames[0] -eq 'player-unsupported-card-ranking-calibration.mt2f') {
        '--unsupported-card-ranking-only'
    } else { '--prepared-selection-publication-only' }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') $mode $fixtures[0]
    if ($LASTEXITCODE -ne 0) { throw 'Unsupported card ranking check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and $selectedFixtureNames.Count -eq 1 -and
    $selectedFixtureNames[0] -eq 'player-conduit-view-continuation-calibration.mt2f') {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Conduit view checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --conduit-view-continuation-only $fixtures[0]
    if ($LASTEXITCODE -ne 0) { throw 'Conduit view continuation check failed.' }
    return
}
if ($Fixture.Count -gt 0 -and $selectedFixtureNames.Count -eq 1 -and
    $selectedFixtureNames[0] -eq 'full-battle-player-automatic-view-calibration.mt2f') {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Automatic view checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --automatic-view-continuation-only `
        (Get-TestFixturePath 'player-prepared-selection-publication-calibration.mt2f') $fixtures[0]
    if ($LASTEXITCODE -ne 0) { throw 'Automatic view continuation check failed.' }
    return
}
$branchPairGates = @{
    'player-sentient-draw-upgrade-calibration.mt2f' = '--sentient-draw-upgrade-only'
    'player-sentient-draw-upgrade2-calibration.mt2f' = '--sentient-draw-upgrade-only'
    'player-effect-test-rng-calibration.mt2f' = '--hand-highlight-rng-pair-only'
    'player-hand-highlight-test-rng-calibration.mt2f' = '--hand-highlight-rng-pair-only'
    'player-unit-ability-duality-calibration.mt2f' = '--unit-ability-duality-pair-only'
    'player-authored-consume-copy-calibration.mt2f' = '--authored-consume-copy-pair-only'
    'player-ephemeral-copy-full-hand-calibration.mt2f' = '--authored-consume-copy-pair-only'
    'player-ephemeral-copy-hand-space-calibration.mt2f' = '--authored-consume-copy-pair-only'
    'player-dead-caster-status-calibration.mt2f' = '--dead-caster-status-pair-only'
    'player-ability-attack-compaction-calibration.mt2f' = '--ability-attack-compaction-pair-only'
    'player-heal-growth-record-calibration.mt2f' = '--heal-growth-record-pair-only'
    'player-overcharge-cleanup-calibration.mt2f' = '--overcharge-cleanup-pair-only'
    'player-heal-combat-removal-calibration.mt2f' = '--heal-combat-removal-pair-only'
    'player-despawn-listeners-calibration.mt2f' = '--despawn-listeners-pair-only'
    'player-despawn-frame-removal-calibration.mt2f' = '--despawn-frame-removal-pair-only'
    'player-recursion-target-text-calibration.mt2f' = '--recursion-target-text-pair-only'
    'player-friendly-relic-piercing-calibration.mt2f' = '--friendly-relic-piercing-pair-only'
    'player-corrupt-copyability-calibration.mt2f' = '--corrupt-copyability-pair-only'
    'player-corrupt-before-play-calibration.mt2f' = '--corrupt-before-play-pair-only'
    'player-enemy-command-attack-calibration.mt2f' = '--enemy-command-attack-pair-only'
    'player-relic-attachment-replacement-calibration.mt2f' = '--relic-attachment-replacement-pair-only'
    'player-corrupt-source-alignment-calibration.mt2f' = '--corrupt-source-alignment-pair-only'
    'player-guaranteed-status-preview-calibration.mt2f' = '--guaranteed-status-preview-pair-only'
    'player-guaranteed-status-test-rng-calibration.mt2f' = '--guaranteed-status-test-rng-pair-only'
    'player-boss-preview-test-rng-calibration.mt2f' = '--boss-preview-test-rng-pair-only'
    'player-sweep-boss-preview-stop-calibration.mt2f' = '--sweep-boss-preview-stop-native-only'
    'player-outer-boss-attachment-birth-calibration.mt2f' = '--outer-boss-attachment-birth-only'
    'player-discard-holdover-return-calibration.mt2f' = '--discard-holdover-return-pair-only'
    'player-undying-copy-deployment-calibration.mt2f' = '--undying-copy-deployment-pair-only'
    'player-undying-copy-effects-calibration.mt2f' = '--undying-copy-effects-pair-only'
    'player-undying-permafrost-copy-deployment-calibration.mt2f' = '--undying-copy-deployment-pair-only'
    'player-undying-permafrost-copy-effects-calibration.mt2f' = '--undying-copy-effects-pair-only'
    'player-undying-permafrost-copy-discard-calibration.mt2f' = '--undying-copy-effects-pair-only'
    'player-slay-corruption-callback-calibration.mt2f' = '--slay-corruption-callback-pair-only'
    'full-battle-natural-angel-pair-cache-calibration.mt2f' = '--natural-angel-pair-cache-only'
    'player-natural-hydra-catalog-deployment-calibration.mt2f' = '--natural-hydra-catalog-pair-only'
    'player-natural-hydra-graft-equipment-calibration.mt2f' = '--natural-hydra-graft-pair-only'
    'player-infusion-trigger-merge-calibration.mt2f' = '--infusion-trigger-merge-pair-only'
    'player-juice-ritual-calibration.mt2f' = '--juice-ritual-pair-only'
    'player-juice-magic-calibration.mt2f' = '--juice-magic-pair-only'
    'player-juice-tome-calibration.mt2f' = '--juice-tome-pair-only'
    'player-juice-fracture-calibration.mt2f' = '--juice-fracture-pair-only'
    'player-juice-justcause-calibration.mt2f' = '--juice-justcause-pair-only'
    'player-corrupt-equipment-calibration.mt2f' = '--corrupt-equipment-only'
    'player-floor-transition-piercing-calibration.mt2f' = '--floor-transition-piercing-only'
    'player-deathwish-enchantment-registry-calibration.mt2f' = '--enchantment-preview-death-only'
    'player-boss-aura-preview-pending-calibration.mt2f' = '--enchantment-boss-preview-only'
    'player-consume-equipment-order-calibration.mt2f' = '--consume-equipment-order-only'
    'player-champion-troop-titanskin-calibration.mt2f' = '--champion-troop-titanskin-only'
    'player-champion-troop-titanskin-many-sources-calibration.mt2f' = '--champion-troop-titanskin-only'
    'player-enemy-statistics-listeners-calibration.mt2f' = '--enemy-statistics-listeners-only'
    'player-pyre-ascension-queue-calibration.mt2f' = '--pyre-ascension-queue-only'
    'player-corrupt-rage-room-calibration.mt2f' = '--corrupt-room-attachment-only'
    'player-standby-etch-paired-boss-calibration.mt2f' = '--first-of-kin-exhaust-only'
    'player-standby-etch-final-boss-calibration.mt2f' = '--first-of-kin-exhaust-only'
    'player-preview-birth-binding-calibration.mt2f' = '--preview-birth-binding-only'
    'player-preview-birth-stage-calibration.mt2f' = '--preview-birth-stage-only'
    'player-pyre-attachment-calibration.mt2f' = '--outer-boss-pyre-attachment-only'
    'player-companion-world-roster-calibration.mt2f' = '--companion-world-roster-only'
    'player-unbox-relic-calibration.mt2f' = '--unbox-relic-only'
    'player-corruption-attack-upgrade-calibration.mt2f' = '--card-corruption-attack-upgrade-only'
    'player-retained-sentry-target-calibration.mt2f' = '--retained-sentry-target-only'
    'player-slay-enchantment-order-calibration.mt2f' = '--slay-enchantment-order-only'
    'player-spell-shield-target-calibration.mt2f' = '--spell-shield-target-only'
    'player-boxed-copy-trait-calibration.mt2f' = '--boxed-copy-trait-only'
    'player-corrupt-diadem-calibration.mt2f' = '--corrupt-diadem-only'
    'player-corrupt-bloodsuit-calibration.mt2f' = '--corrupt-bloodsuit-only'
    'player-recursion-permafrost-calibration.mt2f' = '--recursion-draw-callbacks-only'
    'player-triggered-corruption-calibration.mt2f' = '--triggered-corruption-only'
    'player-triggered-corruption-relic-calibration.mt2f' = '--triggered-corruption-relic-only'
    'player-draft-upgrade-combat-calibration.mt2f' = '--draft-upgrade-combat-only'
    'player-post-battle-reward-combat-calibration.mt2f' = '--post-battle-reward-combat-pair-only'
    'player-justcause-monster-spell-calibration.mt2f' = '--justcause-monster-spell-only'
    'player-justcause-retain-copy-calibration.mt2f' = '--justcause-retain-copy-only'
    'player-justcause-consume-copy-calibration.mt2f' = '--justcause-consume-copy-only'
    'player-intrinsic-infusion-calibration.mt2f' = '--intrinsic-infusion-only'
    'player-intrinsic-infusion-zero-calibration.mt2f' = '--intrinsic-infusion-only'
    'player-challenge-recast-calibration.mt2f' = '--challenge-recast-only'
    'player-echoes-past-recast-calibration.mt2f' = '--echoes-past-recast-only'
    'player-echoes-past-recast-zero-calibration.mt2f' = '--echoes-past-recast-only'
    'player-wurmkin-spike-copy-deployment-calibration.mt2f' = '--wurmkin-spike-copy-deployment-only'
    'player-wurmkin-spike-copy-calibration.mt2f' = '--wurmkin-spike-copy-only'
    'player-moonbeam-copy-deployment-calibration.mt2f' = '--moonbeam-copy-deployment-only'
    'player-moonbeam-copy-lifecycle-calibration.mt2f' = '--moonbeam-copy-lifecycle-only'
    'player-troop-enchantment-noop-calibration.mt2f' = '--troop-enchantment-noop-only'
    'player-retained-player-preview-calibration.mt2f' = '--retained-player-preview-only'
    'player-spent-monster-spell-relic-calibration.mt2f' = '--spent-monster-spell-relic-only'
    'player-unobserved-preview-birth-calibration.mt2f' = '--unobserved-preview-birth-only'
    'player-room-death-preview-identity-calibration.mt2f' = '--room-death-preview-identity-only'
    'player-moon-conduit-birth-order-calibration.mt2f' = '--moon-conduit-birth-order-only'
    'player-moon-null-boss-relic-calibration.mt2f' = '--moon-null-boss-relic-only'
    'player-moon-boss-ember-calibration.mt2f' = '--boss-ember-moon-only'
    'player-titan-curse-preview-calibration.mt2f' = '--titan-curse-preview-only'
    'player-room-slay-status-calibration.mt2f' = '--room-slay-status-only'
    'player-rank-three-room-slay-calibration.mt2f' = '--rank-three-room-slay-only'
    'player-room-death-retained-cleanup-calibration.mt2f' = '--room-death-retained-cleanup-only'
    'player-zero-energy-x-policy-calibration.mt2f' = '--zero-energy-x-policy-only'
    'player-x-room-status-policy-calibration.mt2f' = '--x-room-status-policy-only'
    'player-liquid-courage-recast-calibration.mt2f' = '--liquid-courage-recast-only'
    'player-monster-spell-no-teammates-calibration.mt2f' = '--monster-spell-no-teammates-only'
    'player-fracture-recast-calibration.mt2f' = '--fracture-recast-only'
    'player-teleport-adjacent-order-calibration.mt2f' = '--teleport-adjacent-order-only'
    'player-unowned-relic-copy-calibration.mt2f' = '--unowned-relic-copy-pair-only'
    'player-revived-sacrifice-attribution-calibration.mt2f' = '--revived-sacrifice-attribution-pair-only'
    'player-consume-kill-copy-calibration.mt2f' = '--consume-kill-copy-pair-only'
    'player-sacrifice-preview-calibration.mt2f' = '--sacrifice-preview-pair-only'
    'player-pyre-max-health-calibration.mt2f' = '--pyre-max-health-pair-only'
    'player-card-type-sequence-reset-calibration.mt2f' = '--card-type-sequence-reset-pair-only'
    'player-pyre-payment-world-calibration.mt2f' = '--pyre-payment-world-pair-only'
    'player-removed-ability-self-calibration.mt2f' = '--removed-ability-self-only'
    'player-generated-discard-guard-calibration.mt2f' = '--generated-discard-guard-pair-only'
    'player-ephemeral-consume-order-calibration.mt2f' = '--ephemeral-consume-order-pair-only'
    'player-status-callback-room-sync-calibration.mt2f' = '--status-callback-room-sync-pair-only'
    'player-unstable-revival-calibration.mt2f' = '--unstable-revival-pair-only'
    'player-attachment-reinstall-calibration.mt2f' = '--attachment-reinstall-pair-only'
    'player-corrupt-restricted-state-calibration.mt2f' = '--corrupt-restricted-state-pair-only'
    'player-corrupt-restricted-return-calibration.mt2f' = '--corrupt-restricted-state-pair-only'
    'player-corrupt-restricted-holdover-calibration.mt2f' = '--corrupt-restricted-state-pair-only'
    'player-terminal-enemy-slot-calibration.mt2f' = '--terminal-enemy-slot-pair-only'
    'player-destroyed-feeder-calibration.mt2f' = '--destroyed-feeder-pair-only'
    'player-relic-damage-tower-calibration.mt2f' = '--relic-damage-tower-roster-only'
    'player-last-attack-statistic-refresh-calibration.mt2f' = '--last-attack-statistic-refresh-only'
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object {
    -not $branchPairGates.ContainsKey(($_ -replace '-(?:replay|observation)-calibration\.mt2f$', '-calibration.mt2f'))
}).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Native branch checker build failed.' }
    }
    $checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
    foreach ($sourceName in @($selectedFixtureNames | ForEach-Object {
        $_ -replace '-(?:replay|observation)-calibration\.mt2f$', '-calibration.mt2f'
    } | Select-Object -Unique)) {
        $sourcePath = Get-TestFixturePath $sourceName
        $replayPath = Get-TestFixturePath ($sourceName -replace '-calibration\.mt2f$', '-replay-calibration.mt2f')
        $pairArguments = @($branchPairGates[$sourceName], $sourcePath, $replayPath)
        if ($sourceName -in @('player-heal-combat-removal-calibration.mt2f', 'player-despawn-frame-removal-calibration.mt2f',
            'player-destroyed-feeder-calibration.mt2f', 'player-juice-ritual-calibration.mt2f', 'player-juice-magic-calibration.mt2f', 'player-juice-tome-calibration.mt2f', 'player-juice-fracture-calibration.mt2f', 'player-juice-justcause-calibration.mt2f',
            'player-consume-kill-copy-calibration.mt2f')) {
            $pairArguments += Get-TestFixturePath ($sourceName -replace '-calibration\.mt2f$', '-observation-calibration.mt2f')
        }
        dotnet $checker @pairArguments
        if ($LASTEXITCODE -ne 0) { throw 'Focused native branch pair failed.' }
        if ($sourceName -eq 'player-dead-caster-status-calibration.mt2f') {
            dotnet $checker --tactical-ability-target-only $sourcePath
            if ($LASTEXITCODE -ne 0) { throw 'Focused tactical ability target check failed.' }
        }
        if ($sourceName -eq 'player-juice-magic-calibration.mt2f') {
            dotnet $checker --room-status-policy-only (Get-TestFixturePath 'player-juice-magic-observation-calibration.mt2f')
            if ($LASTEXITCODE -ne 0) { throw 'Focused room-status target check failed.' }
        }
    }
    return
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notlike 'full-battle-random-campaign-*' -and
    $_ -notlike 'full-battle-metagame-campaign-*' -and
    $_ -ne 'player-random-campaign-02-ring2-permafrost-unavailable.mt2f' -and
    $_ -ne 'player-random-campaign-02-ring3-upgrade-observed-prefix.mt2f' -and
    $_ -ne 'player-random-campaign-02-ring3-relentless-preview-observed.mt2f' -and
    $_ -ne 'player-random-campaign-02-ring3-fixed-policy-native-win.mt2f' -and
    $_ -ne 'player-random-campaign-02-ring3-permafrost-copy-observed-prefix.mt2f' }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Random campaign checker build failed.' }
    }
    $checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
    $checkedSources = @{}
    foreach ($path in $fixtures) {
        $sourceName = if ($path.EndsWith('-settled-loss-replay.mt2f', [StringComparison]::Ordinal)) {
            (Split-Path -Leaf $path) -replace '-replay\.mt2f$', '.mt2f'
        } else { (Split-Path -Leaf $path) -replace '-replay\.mt2f$', '-search.mt2f' }
        if ($sourceName -eq 'player-random-campaign-02-ring2-permafrost-unavailable.mt2f') {
            $sourceName = 'full-battle-random-campaign-02-ring2-search.mt2f'
        }
        if ($checkedSources.ContainsKey($sourceName)) { continue }
        dotnet $checker --random-campaign-curated-only (Get-TestFixturePath $sourceName)
        if ($LASTEXITCODE -ne 0) { throw 'Focused random campaign acceptance failed.' }
        $checkedSources[$sourceName] = $true
    }
    return
}
$moonThresholdGates = @{
    'room-moon-spell-threshold-relic-calibration.mt2f' = '--moon-spell-threshold-component-only'
    'full-battle-original-moon-card-upgrades-threshold-relic.mt2f' = '--original-moon-card-upgrades-only'
    'full-battle-player-moon-room-threshold-relic-search.mt2f' = '--player-moon-room-threshold-relic-pair-only'
    'full-battle-player-moon-room-threshold-relic-replay.mt2f' = '--player-moon-room-threshold-relic-pair-only'
}
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { -not $moonThresholdGates.ContainsKey($_) }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Moon threshold checker build failed.' }
    }
    $checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
    $thresholdPairChecked = $false
    foreach ($path in $fixtures) {
        $gate = $moonThresholdGates[(Split-Path -Leaf $path)]
        if ($gate -eq '--player-moon-room-threshold-relic-pair-only') {
            if ($thresholdPairChecked) { continue }
            dotnet $checker $gate (Get-TestFixturePath 'full-battle-player-moon-room-threshold-relic-search.mt2f') `
                (Get-TestFixturePath 'full-battle-player-moon-room-threshold-relic-replay.mt2f')
            $thresholdPairChecked = $true
        } else { dotnet $checker $gate $path }
        if ($LASTEXITCODE -ne 0) { throw 'Focused moon threshold acceptance failed.' }
    }
    return
}
$playerEggCatalogNames = @('player-egg-catalog-missing-observation.mt2f',
    'player-egg-catalog-calibration.mt2f', 'player-egg-catalog-replay-calibration.mt2f')
if ($Fixture.Count -gt 0 -and @($selectedFixtureNames | Where-Object { $_ -notin $playerEggCatalogNames }).Count -eq 0) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Player egg catalog checker build failed.' }
    }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --player-egg-catalog-only `
        (Get-TestFixturePath $playerEggCatalogNames[0]) (Get-TestFixturePath $playerEggCatalogNames[1]) `
        (Get-TestFixturePath $playerEggCatalogNames[2])
    if ($LASTEXITCODE -ne 0) { throw 'Focused player egg catalog calibration failed.' }
    return
}
if ($Workers -gt 1) {
    if (-not $SkipBuild) {
        dotnet build (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release
        if ($LASTEXITCODE -ne 0) { throw 'Independent model checker build failed.' }
    }
    $binaryRoot = Join-Path $workspace 'src\ModelChecks\bin\Release\net8.0'
    $binaryHashes = [ordered]@{}
    foreach ($binary in @('ModelChecks.dll', 'Model.dll', 'FixtureArchive.dll')) {
        $binaryHashes[$binary] = (Get-FileHash -LiteralPath (Join-Path $binaryRoot $binary) -Algorithm SHA256).Hash
    }
    $runDirectory = Join-Path $workspace ('.probe-runs\model-checks-' +
        (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
    $null = New-Item -ItemType Directory -Path $runDirectory
    $workerCount = [Math]::Min($Workers, $selectedFixtureNames.Count)
    $workerStates = [Collections.Generic.List[object]]::new()
    $startedUtc = [DateTime]::UtcNow.ToString('o')
    $summaryPath = Join-Path $runDirectory 'summary.json'
    $runError = $null
    $cleanupErrors = [Collections.Generic.List[string]]::new()
    function Save-ParallelSummary([string]$Status) {
        $incomplete = @($workerStates | Where-Object Status -ne 'Passed')
        [ordered]@{
            Status = $Status
            StartedUtc = $startedUtc
            UpdatedUtc = [DateTime]::UtcNow.ToString('o')
            RequestedWorkers = $Workers
            FixtureCount = $selectedFixtureNames.Count
            BinaryHashes = $binaryHashes
            Error = $runError
            CleanupErrors = @($cleanupErrors)
            Workers = @($workerStates | Select-Object Number, Status, ProcessId, ExitCode,
                Fixtures, InputPath, OutputPath, ErrorPath, ResultPath)
            # A failed shard may have partial passes. Rerun its complete selection
            # because general checks precede its focused checks.
            RerunFixtures = @($incomplete | ForEach-Object Fixtures)
        } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $summaryPath -Encoding utf8
    }
    function Complete-ParallelWorker($Worker) {
        $Worker.Process.WaitForExit()
        $Worker.ExitCode = $Worker.Process.ExitCode
        $Worker.Status = 'Failed'
        if (Test-Path -LiteralPath $Worker.ResultPath) {
            $result = Get-Content -LiteralPath $Worker.ResultPath -Raw | ConvertFrom-Json
            if ($Worker.ExitCode -eq 0 -and $result.ExitCode -eq 0 -and
                (@($result.Fixtures) -join "`n") -ceq ($Worker.Fixtures -join "`n")) {
                $Worker.Status = 'Passed'
            }
        }
    }
    for ($workerIndex = 0; $workerIndex -lt $workerCount; $workerIndex++) {
        $workerFixtures = @(for ($fixtureIndex = $workerIndex; $fixtureIndex -lt $selectedFixtureNames.Count; $fixtureIndex += $workerCount) {
            $selectedFixtureNames[$fixtureIndex]
        })
        $prefix = Join-Path $runDirectory ('worker-{0:D2}' -f ($workerIndex + 1))
        $worker = [pscustomobject]@{
            Number = $workerIndex + 1; Status = 'Pending'; ProcessId = $null; ExitCode = $null
            Fixtures = $workerFixtures; InputPath = "$prefix-input.json"
            OutputPath = "$prefix.stdout.log"; ErrorPath = "$prefix.stderr.log"
            ResultPath = "$prefix-result.json"; Process = $null
        }
        [ordered]@{ ScriptPath = $PSCommandPath; Fixtures = $workerFixtures; ResultPath = $worker.ResultPath } |
            ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $worker.InputPath -Encoding utf8
        $workerStates.Add($worker)
    }
    Save-ParallelSummary 'Running'
    Write-Host "MODEL-CHECKS-PARALLEL START: $workerCount workers, $($fixtures.Count) fixtures; $runDirectory"
    try {
        foreach ($worker in $workerStates) {
            # EncodedCommand avoids native command-line quoting of fixture arrays
            # and workspace paths. Only this literal JSON path enters the script.
            $inputLiteral = "'" + $worker.InputPath.Replace("'", "''") + "'"
            $workerCommand = @'
$ErrorActionPreference = 'Stop'
$inputData = Get-Content -LiteralPath __INPUT_PATH__ -Raw | ConvertFrom-Json
$started = [DateTime]::UtcNow.ToString('o')
$exitCode = 1
$errorText = $null
try {
    & $inputData.ScriptPath -Fixture ([string[]]$inputData.Fixtures) -SkipBuild
    $exitCode = 0
} catch {
    $errorText = $_.ToString()
    [Console]::Error.WriteLine($_.ToString())
    [Console]::Error.WriteLine($_.ScriptStackTrace)
} finally {
    [ordered]@{
        ExitCode = $exitCode; StartedUtc = $started; CompletedUtc = [DateTime]::UtcNow.ToString('o')
        Fixtures = @($inputData.Fixtures); Error = $errorText
    } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $inputData.ResultPath -Encoding utf8
}
exit $exitCode
'@
            $workerCommand = $workerCommand.Replace('__INPUT_PATH__', $inputLiteral)
            $encodedCommand = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($workerCommand))
            $process = Start-Process -FilePath (Join-Path $PSHOME 'pwsh.exe') -WorkingDirectory $workspace `
                -ArgumentList @('-NoProfile', '-NonInteractive', '-OutputFormat', 'Text', '-EncodedCommand', $encodedCommand) `
                -WindowStyle Hidden -RedirectStandardOutput $worker.OutputPath `
                -RedirectStandardError $worker.ErrorPath -PassThru
            $worker.Process = $process
            $worker.ProcessId = $process.Id
            $worker.Status = 'Running'
            Save-ParallelSummary 'Running'
        }
        $progressTimer = [Diagnostics.Stopwatch]::StartNew()
        while (@($workerStates | Where-Object Status -eq 'Running').Count -gt 0) {
            foreach ($worker in @($workerStates | Where-Object Status -eq 'Running')) {
                if (-not $worker.Process.HasExited) { continue }
                Complete-ParallelWorker $worker
                Write-Host "MODEL-CHECKS-WORKER $($worker.Number): $($worker.Status), exit=$($worker.ExitCode), fixtures=$($worker.Fixtures.Count)"
                Save-ParallelSummary 'Running'
            }
            if ($progressTimer.Elapsed.TotalSeconds -ge 15) {
                $passed = @($workerStates | Where-Object Status -eq 'Passed')
                $passedCount = @($passed | ForEach-Object Fixtures).Count
                Write-Host "MODEL-CHECKS-PARALLEL PROGRESS: $($passed.Count)/$workerCount workers passed, $passedCount/$($fixtures.Count) fixtures complete; $summaryPath"
                $progressTimer.Restart()
            }
            if (@($workerStates | Where-Object Status -eq 'Running').Count -gt 0) { Start-Sleep -Milliseconds 250 }
        }
        try {
            foreach ($binary in $binaryHashes.Keys) {
                if ((Get-FileHash -LiteralPath (Join-Path $binaryRoot $binary) -Algorithm SHA256).Hash -ne $binaryHashes[$binary]) {
                    throw "Checker binary changed during parallel regression: $binary"
                }
            }
        } catch {
            # Missing or unreadable binaries invalidate the run just like a hash
            # mismatch; every shard must then remain in the rerun selection.
            foreach ($worker in $workerStates) { $worker.Status = 'Invalidated' }
            throw
        }
        $failed = @($workerStates | Where-Object Status -ne 'Passed')
        if ($failed.Count -gt 0) {
            throw "Parallel model checks failed in workers $($failed.Number -join ', '); see logs and RerunFixtures in $summaryPath"
        }
    } catch {
        $runError = $_.ToString()
        throw
    } finally {
        # On interruption or launch failure, terminate only children started by
        # this invocation; completed worker exit records remain on disk.
        foreach ($worker in $workerStates) {
            if ($null -ne $worker.Process) {
                try {
                    if (-not $worker.Process.HasExited) {
                        $worker.Process.Kill($true)
                        $worker.Process.WaitForExit()
                        $worker.Status = 'Interrupted'
                        $worker.ExitCode = $worker.Process.ExitCode
                    } elseif ($worker.Status -in @('Pending', 'Running')) {
                        Complete-ParallelWorker $worker
                    }
                } catch {
                    $worker.Status = 'CleanupFailed'
                    $cleanupErrors.Add("Worker $($worker.Number) cleanup: $($_.ToString())")
                } finally {
                    try {
                        $worker.Process.Dispose()
                    } catch {
                        $worker.Status = 'CleanupFailed'
                        $cleanupErrors.Add("Worker $($worker.Number) dispose: $($_.ToString())")
                    }
                }
            }
        }
        if ($cleanupErrors.Count -gt 0 -and $null -eq $runError) {
            $runError = $cleanupErrors -join [Environment]::NewLine
        }
        try {
            if ($null -ne $runError -or @($workerStates | Where-Object Status -ne 'Passed').Count -gt 0) {
                Save-ParallelSummary 'Failed'
            } else {
                Save-ParallelSummary 'Passed'
            }
        } catch {
            # Keep the original failure or interruption and still report a
            # secondary summary-write error without masking it.
            $cleanupErrors.Add("Final summary: $($_.ToString())")
        }
        foreach ($cleanupError in $cleanupErrors) { Write-Warning $cleanupError -WarningAction Continue }
    }
    if ($cleanupErrors.Count -gt 0) { throw ($cleanupErrors -join [Environment]::NewLine) }
    Write-Host "MODEL-CHECKS-PARALLEL PASS: $($fixtures.Count) fixtures, $workerCount workers, all terminal exit codes zero; $summaryPath"
    return
}
$focusedBumpFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -in @('full-battle-companion-bump.mt2f', 'full-battle-outer-boss-bump.mt2f',
        'full-battle-original-damage-on-floor-transition.mt2f', 'full-battle-original-pyregel-on-ascend.mt2f')
})
$focusedTeamSpawnStatusFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-team-spawn-status.mt2f'
})
# These fixtures have dedicated native/replay checks below.
$generalFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -notin @('full-battle-companion-bump.mt2f', 'full-battle-outer-boss-bump.mt2f',
        'full-battle-original-damage-on-floor-transition.mt2f', 'full-battle-original-pyregel-on-ascend.mt2f',
        'full-battle-original-team-spawn-status.mt2f', 'full-battle-egg-transformation.mt2f',
        'full-battle-corruption-unit-trigger.mt2f',
        'full-battle-original-kinhost-vessel.mt2f',
        'full-battle-original-kinhost-vessel-delayed.mt2f',
        'full-battle-relic-card-cast-trigger-upgrades.mt2f',
        'full-battle-relic-card-cast-trigger-unit-upgrades.mt2f',
        'full-battle-original-cheaper-spell-cost.mt2f',
        'full-battle-original-first-card-cost.mt2f',
        'full-battle-progressed-ring7-holdover-policy.mt2f',
        'full-battle-progressed-ring7-holdover-win.mt2f',
        'full-battle-progressed-ring7-holdover-exact-replay.mt2f',
        'full-battle-original-rebalance-valor.mt2f',
        'full-battle-original-first-imp-bonus.mt2f',
        'full-battle-original-moon-phase-rooms-gain-magic-power.mt2f',
        'full-battle-original-uncontrolled-group.mt2f')
})
$buildArguments = @()
if ($SkipBuild) { $buildArguments += '--no-build' }
if ($generalFixtures.Count -gt 0) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release @buildArguments -- @generalFixtures
} else {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release @buildArguments
}
if ($LASTEXITCODE -ne 0) { throw 'Independent model checks failed.' }

dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --relic-first-card-cost-pure
if ($LASTEXITCODE -ne 0) { throw 'First-card cost pure checks failed.' }
dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --original-rebalance-valor-pure
if ($LASTEXITCODE -ne 0) { throw 'RebalanceValor pure checks failed.' }

foreach ($fixturePath in $focusedTeamSpawnStatusFixtures) {
    foreach ($mode in @('--room-combat-only', '--native-actions-only', '--unit-clone-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Team-wide spawn status checks failed ($mode): $fixturePath" }
    }
}

$focusedSpawnStatusFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -in @('full-battle-spawn-status-relics-boss-mute.mt2f',
        'full-battle-spawn-status-relics-pyre.mt2f', 'full-battle-spawn-status-relics-egg-hatch.mt2f')
})
foreach ($fixturePath in $focusedSpawnStatusFixtures) {
    foreach ($mode in @('--relic-spawn-status-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Spawn-status relic checks failed ($mode): $fixturePath" }
    }
}

foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-egg-transformation.mt2f' })) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --egg-transformation-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Egg transformation checks failed: $fixturePath" }
}

foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-kinhost-vessel.mt2f' })) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --original-egg-card-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Original Egg card checks failed: $fixturePath" }
}

foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-kinhost-vessel-delayed.mt2f' })) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --original-egg-card-delayed-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Delayed original Egg card checks failed: $fixturePath" }
}

$relicCardCastTriggerFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -in @('full-battle-relic-card-cast-trigger-upgrades.mt2f',
        'full-battle-relic-card-cast-trigger-unit-upgrades.mt2f')
})
foreach ($fixturePath in $relicCardCastTriggerFixtures) {
    foreach ($mode in @('--relic-card-modifiers-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Relic card cast-trigger checks failed ($mode): $fixturePath" }
    }
}

$cardDrawOnCardPlayFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-card-draw-on-card-play.mt2f'
})
foreach ($fixturePath in $cardDrawOnCardPlayFixtures) {
    foreach ($mode in @('--original-card-draw-on-card-play-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Card-draw-on-play relic checks failed ($mode): $fixturePath" }
    }
}

$firstRoomAttachmentFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-first-room-attachment.mt2f'
})
foreach ($fixturePath in $firstRoomAttachmentFixtures) {
    foreach ($mode in @('--original-first-room-attachment-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "First-room attachment relic checks failed ($mode): $fixturePath" }
    }
}

$startRoomAttachmentFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -in @('full-battle-original-start-room-attachment.mt2f', 'full-battle-original-start-room-attachment-floor2.mt2f', 'full-battle-original-start-room-attachment-floor3.mt2f')
})
foreach ($fixturePath in $startRoomAttachmentFixtures) {
    foreach ($mode in @('--original-start-room-attachment-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Start-room attachment relic checks failed ($mode): $fixturePath" }
    }
}

$unplayedCardTriggerFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-unplayed-card-pyre-damage.mt2f'
})
foreach ($fixturePath in $unplayedCardTriggerFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --unplayed-card-trigger-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Unplayed-card trigger checks failed: $fixturePath" }
}

$unplayedCardEffectsFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -in @('full-battle-unplayed-card-effects.mt2f', 'full-battle-unplayed-card-effects-release149.mt2f')
})
foreach ($fixturePath in $unplayedCardEffectsFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --unplayed-card-effects-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Unplayed-card effect checks failed: $fixturePath" }
}

$removeAllStatusFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-remove-all-status-effects.mt2f'
})
foreach ($fixturePath in $removeAllStatusFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --remove-all-status-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Remove-all-status checks failed: $fixturePath" }
}

$trainHealFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-train-heal.mt2f'
})
foreach ($fixturePath in $trainHealFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --train-heal-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Train-heal checks failed: $fixturePath" }
}

$overhealFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-overheal.mt2f'
})
foreach ($fixturePath in $overhealFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --overheal-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Overheal checks failed: $fixturePath" }
}

$corruptionFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-corruption.mt2f'
})
foreach ($fixturePath in $corruptionFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --room-corruption-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Room-corruption checks failed: $fixturePath" }
}

$corruptionUnitFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-corruption-unit-trigger.mt2f'
})
foreach ($fixturePath in $corruptionUnitFixtures) {
    foreach ($mode in @('--room-combat-only', '--corruption-unit-trigger-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Corruption unit-trigger checks failed ($mode): $fixturePath" }
    }
}

$corruptionCarverFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-corruption-carver-trigger.mt2f'
})
foreach ($fixturePath in $corruptionCarverFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --corruption-carver-trigger-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Corruption Carver trigger checks failed: $fixturePath" }
}

$corruptionKeeperFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-corruption-keeper-trigger.mt2f'
})
foreach ($fixturePath in $corruptionKeeperFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --corruption-keeper-trigger-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Corruption Keeper trigger checks failed: $fixturePath" }
}

$corruptionRelicFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-corruption-relic-trigger.mt2f'
})
foreach ($fixturePath in $corruptionRelicFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --corruption-relic-trigger-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Corruption relic trigger checks failed: $fixturePath" }
}

$permanentCorruptionFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-permanent-corruption.mt2f'
})
foreach ($fixturePath in $permanentCorruptionFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --permanent-corruption-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Permanent-corruption checks failed: $fixturePath" }
}

$moonPhaseCardFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-moon-phase-card.mt2f'
})
foreach ($fixturePath in $moonPhaseCardFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --moon-phase-transition-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Moon-phase transition checks failed: $fixturePath" }
}

$moonPhaseAdvanceFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-moon-phase-advance.mt2f'
})
foreach ($fixturePath in $moonPhaseAdvanceFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --moon-phase-advance-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Moon-phase advance checks failed: $fixturePath" }
}

$moonPhaseCallbackFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-moon-phase-callbacks.mt2f'
})
foreach ($fixturePath in $moonPhaseCallbackFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --moon-phase-callbacks-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Moon-phase callback checks failed: $fixturePath" }
}

$conditionalNoPlayerFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-conditional-no-player-status.mt2f'
})
foreach ($fixturePath in $conditionalNoPlayerFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --no-player-units-status-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Conditional no-player status checks failed: $fixturePath" }
}

$conditionalPyreDamageFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-conditional-pyre-damage.mt2f'
})
foreach ($fixturePath in $conditionalPyreDamageFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --conditional-pyre-damage-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Conditional Pyre damage checks failed: $fixturePath" }
}

foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-conditional-pyre-lethal.mt2f' })) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --conditional-pyre-lethal-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Conditional Pyre lethal checks failed: $fixturePath" }
}

$selfAttackStatusFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-self-attack-status.mt2f'
})
foreach ($fixturePath in $selfAttackStatusFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --self-attack-status-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Self-attack status checks failed: $fixturePath" }
}

$scorchFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-soul-siphon.mt2f'
})
foreach ($fixturePath in $scorchFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --scorch-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Scorch checks failed: $fixturePath" }
}

$statusUpgradeFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-status-upgrade.mt2f'
})
foreach ($fixturePath in $statusUpgradeFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --status-upgrade-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Status-upgrade checks failed: $fixturePath" }
}

$recursionChoiceFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -in @('full-battle-card-recursion-choice.mt2f', 'full-battle-card-recursion-choose-to-hand.mt2f', 'full-battle-card-recursion-filter-choice.mt2f', 'full-battle-card-recursion-discard-choice.mt2f')
})
foreach ($fixturePath in $recursionChoiceFixtures) {
    $selectionMode = switch (Split-Path -Leaf $fixturePath) {
        'full-battle-card-recursion-choose-to-hand.mt2f' { '--card-recursion-hand-choice-only' }
        'full-battle-card-recursion-filter-choice.mt2f' { '--card-recursion-filter-choice-only' }
        'full-battle-card-recursion-discard-choice.mt2f' { '--card-recursion-discard-only' }
        default { '--card-recursion-choice-only' }
    }
    foreach ($mode in @($selectionMode, '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Chosen card recursion checks failed ($mode): $fixturePath" }
    }
}

$recursionUpgradeFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -in @('full-battle-card-recursion-upgrade.mt2f', 'full-battle-card-recursion-upgrade-hand.mt2f', 'full-battle-card-recursion-upgrade-choice.mt2f', 'full-battle-card-recursion-upgrade-filter-choice.mt2f', 'full-battle-card-recursion-upgrade-replacement-choice.mt2f')
})
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-choose-discard.mt2f' })) {
    foreach ($mode in @('--choose-discard-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Choose-discard checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -in @(
    'full-battle-original-tinkermaster-mix.mt2f', 'full-battle-original-progress-pacemaker-mix.mt2f') })) {
    foreach ($mode in @('--original-mix-relic-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Mix relic checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-upgrade-count-relics.mt2f' })) {
    foreach ($mode in @('--original-upgrade-count-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original upgrade-count relic checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-forge-conservation-relic.mt2f' })) {
    foreach ($mode in @('--original-forge-conservation-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original forge conservation checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-shroom-decay-relic.mt2f' })) {
    foreach ($mode in @('--original-combat-trigger-relics-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Shroom Decay checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -in @('full-battle-original-conduit-relics.mt2f', 'full-battle-original-conduit-relics-physical.mt2f') })) {
    foreach ($mode in @('--original-conduit-relics-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Conduit relic checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-equipment-growth-relics.mt2f' })) {
    foreach ($mode in @('--original-equipment-growth-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original equipment growth relic checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-health-choice-relic.mt2f' })) {
    foreach ($mode in @('--original-health-choice-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original health-choice relic checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-unit-phase-upgrade-relics.mt2f' })) {
    foreach ($mode in @('--original-unit-phase-upgrades-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original unit phase upgrade checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-add-stats-to-solo-units.mt2f' })) {
    foreach ($mode in @('--original-solo-unit-upgrade-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original solo-unit upgrade checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-front-room-health-upgrade.mt2f' })) {
    foreach ($mode in @('--original-front-room-health-upgrade-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original front-room health upgrade checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-champion-start-turn-heal.mt2f' })) {
    foreach ($mode in @('--original-champion-start-turn-heal-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original champion start-turn heal checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-nims-oath.mt2f' })) {
    foreach ($mode in @('--original-nims-oath-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Nims Oath checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-judges-scales.mt2f' })) {
    foreach ($mode in @('--original-judges-scales-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Judges Scales checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-first-imp-bonus.mt2f' })) {
    foreach ($mode in @('--original-first-imp-bonus-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original FirstImpBonus checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-moon-phase-rooms-gain-magic-power.mt2f' })) {
    foreach ($mode in @('--original-moon-phase-hand-upgrade-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original moon-phase hand-upgrade checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-no-upgrade-cost.mt2f' })) {
    foreach ($mode in @('--original-no-upgrade-card-cost-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original no-upgrade card-cost checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-pyre-health-card-cost.mt2f' })) {
    foreach ($mode in @('--original-pyre-health-card-cost-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Pyre health card-cost checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-relic-count-attack.mt2f' })) {
    foreach ($mode in @('--original-relic-count-attack-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original relic-count attack checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-more-spikes.mt2f' })) {
    foreach ($mode in @('--original-more-spikes-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original MoreSpikes checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-stealth-slay.mt2f' })) {
    foreach ($mode in @('--original-stealth-slay-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original stealth-on-slay checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-turn-draw-relics.mt2f' })) {
    foreach ($mode in @('--original-turn-draw-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original turn draw checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-opening-draw-relics.mt2f' })) {
    foreach ($mode in @('--original-opening-draw-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original opening draw checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-unit-resource-relics.mt2f' })) {
    foreach ($mode in @('--original-unit-resources-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original unit resource checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-after-draw-generation-relics.mt2f' })) {
    foreach ($mode in @('--original-after-draw-generation-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original after-draw generation checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-add-snack-on-kill.mt2f' })) {
    foreach ($mode in @('--original-add-snack-on-kill-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original AddSnackOnKill checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-final-ending-artifact.mt2f' })) {
    foreach ($mode in @('--original-final-ending-artifact-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original FinalEndingArtifact checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-balatro-event-yorick.mt2f' })) {
    foreach ($mode in @('--original-balatro-event-yorick-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original BalatroEventYorick checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-balatro-event-cavendish.mt2f' })) {
    foreach ($mode in @('--original-balatro-event-cavendish-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original BalatroEventCavendish checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-two-charges-on-card-consumed.mt2f' })) {
    foreach ($mode in @('--original-two-charges-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original 2ChargesOnCardConsumed checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-chance-for-spell-weakness.mt2f' })) {
    foreach ($mode in @('--original-chance-for-spell-weakness-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original ChanceForSpellWeakness checks failed ($mode): $fixturePath" }
    }
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --original-chance-for-spell-weakness-model-only
    if ($LASTEXITCODE -ne 0) { throw "Original ChanceForSpellWeakness focused model checks failed: $fixturePath" }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-chance-to-cleanse.mt2f' })) {
    foreach ($mode in @('--original-chance-to-cleanse-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original ChanceToCleanse checks failed ($mode): $fixturePath" }
    }
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --original-chance-to-cleanse-model-only
    if ($LASTEXITCODE -ne 0) { throw "Original ChanceToCleanse focused model checks failed: $fixturePath" }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-extra-feed-trigger.mt2f' })) {
    foreach ($mode in @('--original-extra-feed-trigger-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original ExtraFeedTrigger checks failed ($mode): $fixturePath" }
    }
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --original-extra-feed-trigger-model-only
    if ($LASTEXITCODE -ne 0) { throw "Original ExtraFeedTrigger focused model checks failed: $fixturePath" }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-hephs-blueprints.mt2f' })) {
    foreach ($mode in @('--original-hephs-blueprints-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original HephsBlueprints checks failed ($mode): $fixturePath" }
    }
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --original-hephs-blueprints-model-only
    if ($LASTEXITCODE -ne 0) { throw "Original HephsBlueprints focused model checks failed: $fixturePath" }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-summon-with-avarice.mt2f' })) {
    foreach ($mode in @('--original-summon-with-avarice-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original SummonWithAvarice checks failed ($mode): $fixturePath" }
    }
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --original-summon-with-avarice-model-only
    if ($LASTEXITCODE -ne 0) { throw "Original SummonWithAvarice focused model checks failed: $fixturePath" }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-garfield-box-random-card.mt2f' })) {
    foreach ($mode in @('--original-garfield-box-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original GarfieldBox checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-part-factory.mt2f' })) {
    foreach ($mode in @('--original-part-factory-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original PartFactory checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-big-damage-many-morsels.mt2f' })) {
    foreach ($mode in @('--original-big-damage-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "BigDamageFromManyMorsels checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-early-discard-energy.mt2f' })) {
    foreach ($mode in @('--original-early-discard-energy-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "EmberOnEarlyDiscard checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-cheaper-spell-cost.mt2f' })) {
    foreach ($mode in @('--original-cheaper-spell-cost-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "ReduceSpellCostOfCheaper checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-first-card-cost.mt2f' })) {
    foreach ($mode in @('--original-first-card-cost-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "ReduceFirstSpellCostPerDeckSize checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-rebalance-valor.mt2f' })) {
    foreach ($mode in @('--original-rebalance-valor-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "RebalanceValor checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-nefarious-overflow.mt2f' })) {
    foreach ($mode in @('--original-nefarious-overflow-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "NefariousOverflow checks failed ($mode): $fixturePath" }
    }
}
dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --original-uncontrolled-group-pure
if ($LASTEXITCODE -ne 0) { throw 'Original UncontrolledGroup pure checks failed.' }
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-uncontrolled-group.mt2f' })) {
    foreach ($mode in @('--original-uncontrolled-group-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original UncontrolledGroup checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-copy-first-spell-played.mt2f' })) {
    foreach ($mode in @('--original-copy-first-spell-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "CopyFirstSpellPlayed checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-copy-first-consume-played.mt2f' })) {
    foreach ($mode in @('--original-copy-first-consume-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "CopyFirstConsumePlayed checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-copy-consumed-card.mt2f' })) {
    foreach ($mode in @('--original-copy-consumed-card-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "CopyConsumedCard checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -like 'full-battle-original-monster-spell-targeting*.mt2f' })) {
    foreach ($mode in @('--original-monster-spell-targeting-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "SingleToTeamMonsterSpellTargeting checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-funguy-deck-scaling.mt2f' })) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --original-funguy-deck-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Original Funguy deck-scaling checks failed: $fixturePath" }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-card-draw-on-heal.mt2f' })) {
    foreach ($mode in @('--original-card-draw-on-heal-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original CardDrawOnHeal checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-broken-rail.mt2f' })) {
    foreach ($mode in @('--original-broken-rail-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original BrokenRail checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-rage-on-burnout-stack-loss.mt2f' })) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --original-rage-on-burnout-stack-loss-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Original RageOnBurnoutStackLoss checks failed: $fixturePath" }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-pyre-revenge.mt2f' })) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --original-pyre-revenge-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Original PyreRevengeAddPyregel checks failed: $fixturePath" }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-extract-for-ember-and-echos.mt2f' })) {
    foreach ($mode in @('--original-extract-for-ember-and-echos-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original ExtractForEmberAndEchos checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-spell-play-threshold-relics.mt2f' })) {
    foreach ($mode in @('--original-spell-play-thresholds-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original spell threshold checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-opening-corruption-relics.mt2f' })) {
    foreach ($mode in @('--original-opening-corruption-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original opening corruption checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -in @(
    'full-battle-original-healing-power-relic.mt2f', 'full-battle-original-start-combat-energy-relic.mt2f',
    'full-battle-original-start-combat-hoard-relic.mt2f') })) {
    $focusedMode = if ((Split-Path -Leaf $fixturePath) -eq 'full-battle-original-healing-power-relic.mt2f') {
        '--original-healing-power-only'
    } elseif ((Split-Path -Leaf $fixturePath) -eq 'full-battle-original-start-combat-hoard-relic.mt2f') {
        '--original-start-combat-hoard-only'
    } else { '--original-start-combat-energy-only' }
    foreach ($mode in @($focusedMode, '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original healing/start-combat relic checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-card-type-cost-relic.mt2f' })) {
    foreach ($mode in @('--original-card-type-cost-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original card-type cost relic checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-purge-to-exhaust.mt2f' })) {
    foreach ($mode in @('--original-purge-to-exhaust-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original PurgeToExhaust checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-conserve-ember.mt2f' })) {
    foreach ($mode in @('--original-conserve-ember-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original ConserveEmber checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -in @(
    'full-battle-card-type-cost-discount-first.mt2f', 'full-battle-card-type-cost-upgrade-first.mt2f') })) {
    foreach ($mode in @('--card-type-cost-order-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Card-type cost upgrade-order checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -in @(
    'full-battle-original-smelt-ember.mt2f', 'full-battle-original-smelt-ember-zero.mt2f',
    'full-battle-original-smelt-ember-bulk.mt2f') })) {
    foreach ($mode in @('--original-smelt-ember-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original smelt Ember checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-choose-discard-others.mt2f' })) {
    foreach ($mode in @('--choose-discard-others-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Choose-discard others checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-choose-discard-others-policy.mt2f' })) {
    foreach ($mode in @('--choose-discard-others-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Choose-discard others policy checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-choose-discard-refine.mt2f' })) {
    foreach ($mode in @('--choose-discard-others-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Choose-discard Refine checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-choose-discard-persistent.mt2f' })) {
    foreach ($mode in @('--choose-discard-others-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Choose-discard Persistent checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-set-health.mt2f' })) {
    foreach ($mode in @('--native-actions-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "SetHealth checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -in @('full-battle-mix.mt2f', 'full-battle-mix-alternate.mt2f',
        'full-battle-mix-01.mt2f', 'full-battle-mix-10.mt2f', 'full-battle-mix-unstable.mt2f',
        'full-battle-mix-unstable-explosion.mt2f')
})) {
    foreach ($mode in @('--mix-catalog-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Mix checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-mix-unstable-explosion.mt2f' })) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --unstable-explosion-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Unstable explosion witness failed: $fixturePath" }
}
foreach ($fixturePath in @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -in @('full-battle-damage-by-units-killed.mt2f',
        'full-battle-damage-by-units-killed-physical.mt2f')
})) {
    foreach ($mode in @('--damage-by-units-killed-only', '--native-actions-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Damage-by-units-killed checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in $recursionUpgradeFixtures) {
    foreach ($mode in @('--card-recursion-upgrade-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Card recursion upgrade checks failed ($mode): $fixturePath" }
    }
}

$covenantFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-covenant-character-filter.mt2f' })
foreach ($fixturePath in $covenantFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --covenant-character-filter-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Covenant character-filter checks failed: $fixturePath" }
}

$bumpFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -in @('full-battle-bump.mt2f', 'full-battle-companion-bump.mt2f', 'full-battle-outer-boss-bump.mt2f',
        'full-battle-original-damage-on-floor-transition.mt2f', 'full-battle-original-pyregel-on-ascend.mt2f')
})
foreach ($fixturePath in $bumpFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --bump-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Bump scenario checks failed: $fixturePath" }
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --native-actions-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Native action checks failed: $fixturePath" }
}
foreach ($fixturePath in $focusedBumpFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --room-combat-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Room combat checks failed: $fixturePath" }
}
$originalFloorTransitionFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-damage-on-floor-transition.mt2f'
})
foreach ($fixturePath in $originalFloorTransitionFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --battle-search-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Original floor-transition damage relic battle search failed: $fixturePath" }
}

$originalPyregelFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-pyregel-on-ascend.mt2f'
})
foreach ($fixturePath in $originalPyregelFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --pyregel-on-ascend-pure
    if ($LASTEXITCODE -ne 0) { throw 'Original PyregelOnAscend_DH pure model checks failed.' }
    foreach ($mode in @('--pyregel-on-ascend-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original PyregelOnAscend_DH checks failed ($mode): $fixturePath" }
    }
}

$originalMoonPhaseHealFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -in @('full-battle-original-moon-phase-heal-relic.mt2f', 'full-battle-moon-heal-on-heal.mt2f')
})
foreach ($fixturePath in $originalMoonPhaseHealFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --battle-search-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Original moon-phase healing relic search checks failed: $fixturePath" }
}
$originalMoonPhaseStatusFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-moon-phase-status-relic.mt2f'
})
foreach ($fixturePath in $originalMoonPhaseStatusFixtures) {
    foreach ($mode in @('--original-moon-phase-status-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original moon-phase status relic checks failed ($mode): $fixturePath" }
    }
}
$originalPropagateStatusFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-propagate-on-status-effect-applied.mt2f'
})
foreach ($fixturePath in $originalPropagateStatusFixtures) {
    foreach ($mode in @('--original-propagate-on-status-effect-applied-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original status-propagation relic checks failed ($mode): $fixturePath" }
    }
}
$originalMothersRemnant3Fixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-mothers-remnant3.mt2f'
})
foreach ($fixturePath in $originalMothersRemnant3Fixtures) {
    foreach ($mode in @('--original-mothers-remnant3-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original MothersRemnant3 checks failed ($mode): $fixturePath" }
    }
}
$originalTribouletFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-balatro-event-triboulet.mt2f'
})
foreach ($fixturePath in $originalTribouletFixtures) {
    foreach ($mode in @('--original-triboulet-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Triboulet checks failed ($mode): $fixturePath" }
    }
}
$originalCooldownOnDamageFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-cooldown-on-damage-dh.mt2f'
})
foreach ($fixturePath in $originalCooldownOnDamageFixtures) {
    foreach ($mode in @('--original-cooldown-on-damage-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original CooldownOnDamage checks failed ($mode): $fixturePath" }
    }
}
$originalDeckTurnDrawFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-deck-turn-draw.mt2f'
})
foreach ($fixturePath in $originalDeckTurnDrawFixtures) {
    foreach ($mode in @('--original-deck-turn-draw-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original DrawCardPerDeckSize checks failed ($mode): $fixturePath" }
    }
}
$originalCardPlayDamageFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-card-play-damage.mt2f'
})
foreach ($fixturePath in $originalCardPlayDamageFixtures) {
    foreach ($mode in @('--original-card-play-damage-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original card-play damage checks failed ($mode): $fixturePath" }
    }
}
$originalConsumeDamageFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-consume-damage.mt2f'
})
foreach ($fixturePath in $originalConsumeDamageFixtures) {
    foreach ($mode in @('--original-consume-damage-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Consume damage checks failed ($mode): $fixturePath" }
    }
}
$originalEatenDamageFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-eaten-damage.mt2f'
})
foreach ($fixturePath in $originalEatenDamageFixtures) {
    foreach ($mode in @('--original-eaten-damage-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original OnEaten damage checks failed ($mode): $fixturePath" }
    }
}
$originalMoreCapacityFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-more-capacity.mt2f'
})
foreach ($fixturePath in $originalMoreCapacityFixtures) {
    foreach ($mode in @('--original-more-capacity-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original MoreCapacity checks failed ($mode): $fixturePath" }
    }
}
$originalBurnoutDamageFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-burnout-death-damage.mt2f'
})
foreach ($fixturePath in $originalBurnoutDamageFixtures) {
    foreach ($mode in @('--original-burnout-damage-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original burnout/death damage checks failed ($mode): $fixturePath" }
    }
}
$originalHealDamageFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-heal-damage.mt2f'
})
foreach ($fixturePath in $originalHealDamageFixtures) {
    foreach ($mode in @('--original-heal-damage-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original heal damage checks failed ($mode): $fixturePath" }
    }
}
$originalKillAttackFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-kill-attack.mt2f'
})
foreach ($fixturePath in $originalKillAttackFixtures) {
    foreach ($mode in @('--original-kill-attack-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original kill-attack checks failed ($mode): $fixturePath" }
    }
}
$originalSpawnSilenceFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-chance-to-silence.mt2f'
})
foreach ($fixturePath in $originalSpawnSilenceFixtures) {
    foreach ($mode in @('--original-spawn-silence-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original ChanceToSilence checks failed ($mode): $fixturePath" }
    }
}
$originalFreezeCostFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-freeze-reduce-cost.mt2f'
})
foreach ($fixturePath in $originalFreezeCostFixtures) {
    foreach ($mode in @('--original-freeze-cost-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original FreezeReduceCost checks failed ($mode): $fixturePath" }
    }
}
$originalRandomFreezeFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-random-freeze.mt2f'
})
foreach ($fixturePath in $originalRandomFreezeFixtures) {
    foreach ($mode in @('--original-random-freeze-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original FreezeRandomEndOfTurn checks failed ($mode): $fixturePath" }
    }
}
$originalRandomHandUpgradeFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-random-hand-upgrade.mt2f'
})
foreach ($fixturePath in $originalRandomHandUpgradeFixtures) {
    foreach ($mode in @('--original-random-hand-upgrade-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original ReduceRandomCardCostOnUnitPlay checks failed ($mode): $fixturePath" }
    }
}
$originalStartTurnPropagationFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-start-turn-propagation.mt2f'
})
foreach ($fixturePath in $originalStartTurnPropagationFixtures) {
    foreach ($mode in @('--original-start-turn-propagation-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original StartOfTurnPropagate checks failed ($mode): $fixturePath" }
    }
}
$originalMooncycleFaeFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-mooncycle-fae.mt2f'
})
foreach ($fixturePath in $originalMooncycleFaeFixtures) {
    foreach ($mode in @('--original-mooncycle-fae-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original MooncycleFaeAddMagicPower checks failed ($mode): $fixturePath" }
    }
}
$originalRandomDrawCostFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-random-draw-cost.mt2f'
})
foreach ($fixturePath in $originalRandomDrawCostFixtures) {
    foreach ($mode in @('--original-random-draw-cost-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original RandomCardCostOnDraw checks failed ($mode): $fixturePath" }
    }
}
$originalSuppressExhaustFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-suppress-exhaust.mt2f'
})
foreach ($fixturePath in $originalSuppressExhaustFixtures) {
    foreach ($mode in @('--original-suppress-exhaust-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original ChanceToNotConsume checks failed ($mode): $fixturePath" }
    }
}
$originalExpensiveCardUpgradeFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-expensive-card-upgrade.mt2f'
})
foreach ($fixturePath in $originalExpensiveCardUpgradeFixtures) {
    foreach ($mode in @('--original-expensive-card-upgrade-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original expensive-card Permafrost checks failed ($mode): $fixturePath" }
    }
}
$originalWolosBloodletterFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-wolos-bloodletter.mt2f'
})
foreach ($fixturePath in $originalWolosBloodletterFixtures) {
    foreach ($mode in @('--original-wolos-bloodletter-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Wolos Bloodletter checks failed ($mode): $fixturePath" }
    }
}
$originalCardTypeSequenceFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-card-type-sequence.mt2f'
})
foreach ($fixturePath in $originalCardTypeSequenceFixtures) {
    foreach ($mode in @('--original-card-type-sequence-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original three-card-type sequence checks failed ($mode): $fixturePath" }
    }
}
$originalWhelpCooldownFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-whelp-cooldown.mt2f'
})
foreach ($fixturePath in $originalWhelpCooldownFixtures) {
    foreach ($mode in @('--original-whelp-cooldown-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Whelp cooldown checks failed ($mode): $fixturePath" }
    }
}
$originalSpawnRetreatFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-spawn-retreat.mt2f'
})
foreach ($fixturePath in $originalSpawnRetreatFixtures) {
    foreach ($mode in @('--original-spawn-retreat-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original spawn retreat checks failed ($mode): $fixturePath" }
    }
}
$originalPerkeoFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-perkeo.mt2f' })
$originalCorruptedAngelsFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -in @('full-battle-original-corrupted-angels.mt2f', 'full-battle-corrupted-angels-cost-first.mt2f', 'full-battle-corrupted-angels-cost-last.mt2f') })
foreach ($fixturePath in $originalCorruptedAngelsFixtures) {
    foreach ($mode in @('--original-corrupted-angels-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Corrupted Angels checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-ability-activation-pyre-focus.mt2f' })) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- '--unit-ability-native-only' $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Native Pyre attack focus checks failed: $fixturePath" }
}
$extendedMechanicFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -in @('full-battle-tower-corruption-callbacks.mt2f', 'full-battle-null-unit-ability.mt2f', 'full-battle-removed-status-upgrade-scaling.mt2f')
})
foreach ($fixturePath in $extendedMechanicFixtures) {
    $mechanicMode = switch (Split-Path -Leaf $fixturePath) {
        'full-battle-tower-corruption-callbacks.mt2f' { '--corruption-keeper-trigger-only' }
        'full-battle-null-unit-ability.mt2f' { '--unit-ability-native-only' }
        'full-battle-removed-status-upgrade-scaling.mt2f' { '--unit-upgrade-scaling-only' }
    }
    foreach ($mode in @($mechanicMode, '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Extended mechanic checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in $originalPerkeoFixtures) {
    foreach ($mode in @('--original-perkeo-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Perkeo checks failed ($mode): $fixturePath" }
    }
}
$originalOpeningDeckSpawnFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-opening-deck-spawn.mt2f'
})
foreach ($fixturePath in $originalOpeningDeckSpawnFixtures) {
    foreach ($mode in @('--original-opening-deck-spawn-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original opening deck-spawn checks failed ($mode): $fixturePath" }
    }
}
$originalOpeningMorselsFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-opening-morsels.mt2f'
})
foreach ($fixturePath in $originalOpeningMorselsFixtures) {
    foreach ($mode in @('--original-opening-morsels-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original opening Morsels checks failed ($mode): $fixturePath" }
    }
}
$feedingFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-card-backed-feeding.mt2f' })
foreach ($fixturePath in $feedingFixtures) {
    foreach ($mode in @('--card-backed-feeding-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Card-backed feeding checks failed ($mode): $fixturePath" }
    }
}
$forcedFeedingFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -in @('full-battle-forced-feeding.mt2f', 'full-battle-shroud-spike-feeding.mt2f', 'full-battle-original-shroud-spike.mt2f') })
foreach ($fixturePath in $forcedFeedingFixtures) {
    $modes = if ((Split-Path -Leaf $fixturePath) -eq 'full-battle-original-shroud-spike.mt2f') {
        @('--original-shroud-spike-only', '--battle-search-only')
    } else {
        @('--forced-feeding-only', '--battle-search-only')
    }
    foreach ($mode in $modes) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Forced feeding checks failed ($mode): $fixturePath" }
    }
}
$soldierFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-soldier-of-fortune.mt2f' })
foreach ($fixturePath in $soldierFixtures) {
    foreach ($mode in @('--soldier-of-fortune-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Soldier of Fortune checks failed ($mode): $fixturePath" }
    }
}
$originalMoonCardUpgradeFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-moon-card-upgrades.mt2f' })
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'player-live-empty-deployment-advice.mt2f' })) {
    dotnet run --project (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-build -- --deployment-advice-budget-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Deployment advice budget checks failed: $fixturePath" }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'player-live-forsaken-trial.mt2f' })) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --live-forsaken-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Live Forsaken opening checks failed: $fixturePath" }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-opening-forsaken-covenant10.mt2f' })) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --original-opening-forsaken-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Native Forsaken opening checks failed: $fixturePath" }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'player-live-moon-continuation.mt2f' })) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --live-moon-continuation-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Live moon continuation checks failed: $fixturePath" }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-moon-card-upgrades-live-relics-covenant10.mt2f' })) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --live-moon-composition-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Native live moon composition checks failed: $fixturePath" }
}
foreach ($fixturePath in $originalMoonCardUpgradeFixtures) {
    foreach ($mode in @('--original-moon-card-upgrades-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original moon card upgrades checks failed ($mode): $fixturePath" }
    }
}
$originalDiscardDrawFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -in @(
    'full-battle-original-discard-draw.mt2f', 'full-battle-original-discard-draw-routing.mt2f') })
foreach ($fixturePath in $originalDiscardDrawFixtures) {
    foreach ($mode in @('--original-discard-draw-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original discard draw checks failed ($mode): $fixturePath" }
    }
}
$originalSpellCompositionsFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-spell-compositions.mt2f' })
foreach ($fixturePath in $originalSpellCompositionsFixtures) {
    foreach ($mode in @('--original-spell-compositions-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original spell compositions checks failed ($mode): $fixturePath" }
    }
}
$originalPositiveStatusFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-positive-status.mt2f' })
foreach ($fixturePath in $originalPositiveStatusFixtures) {
    foreach ($mode in @('--original-positive-status-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original positive status checks failed ($mode): $fixturePath" }
    }
}
$originalScaledMoonPhaseFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-scaled-moon-phase.mt2f' })
foreach ($fixturePath in $originalScaledMoonPhaseFixtures) {
    foreach ($mode in @('--original-scaled-moon-phase-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original scaled moon phase checks failed ($mode): $fixturePath" }
    }
}
$originalDrawUpgradeScalingFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-draw-upgrade-scaling.mt2f' })
foreach ($fixturePath in $originalDrawUpgradeScalingFixtures) {
    foreach ($mode in @('--original-draw-upgrade-scaling-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original draw upgrade scaling checks failed ($mode): $fixturePath" }
    }
}
$originalConsumedUpgradeFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-consumed-upgrade.mt2f' })
foreach ($fixturePath in $originalConsumedUpgradeFixtures) {
    foreach ($mode in @('--original-consumed-upgrade-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original consumed upgrade checks failed ($mode): $fixturePath" }
    }
}
$originalStatusCompositionFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-status-compositions.mt2f' })
foreach ($fixturePath in $originalStatusCompositionFixtures) {
    foreach ($mode in @('--original-status-compositions-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original status composition checks failed ($mode): $fixturePath" }
    }
}
$originalFuelFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-fuel.mt2f' })
foreach ($fixturePath in $originalFuelFixtures) {
    foreach ($mode in @('--original-fuel-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original fuel checks failed ($mode): $fixturePath" }
    }
}
$originalSupportUnitFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-support-units.mt2f' })
foreach ($fixturePath in $originalSupportUnitFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original support unit checks failed ($mode): $fixturePath" }
    }
}
$originalDivineDancerFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-divine-dancer.mt2f' })
foreach ($fixturePath in $originalDivineDancerFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Divine Dancer checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedUnitBatchFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-unit-batch.mt2f' })
foreach ($fixturePath in $originalIsolatedUnitBatchFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original isolated unit batch checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedColdstrikeSirenFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-coldstrike-siren.mt2f' })
foreach ($fixturePath in $originalIsolatedColdstrikeSirenFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original isolated Coldstrike Siren checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedColdstingSirenFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-coldsting-siren.mt2f' })
foreach ($fixturePath in $originalIsolatedColdstingSirenFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original isolated Coldsting Siren checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedEelGorgonFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-eel-gorgon.mt2f' })
foreach ($fixturePath in $originalIsolatedEelGorgonFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original isolated Eel Gorgon checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedEntombedExplosiveFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-entombed-explosive.mt2f' })
foreach ($fixturePath in $originalIsolatedEntombedExplosiveFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original isolated Entombed Explosive checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedOverworkedAssistantFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-overworked-assistant.mt2f' })
foreach ($fixturePath in $originalIsolatedOverworkedAssistantFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original isolated Overworked Assistant checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedCrucibleCollectorFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-crucible-collector.mt2f' })
foreach ($fixturePath in $originalIsolatedCrucibleCollectorFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original isolated Crucible Collector checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedCrucibleWardenFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-crucible-warden.mt2f' })
foreach ($fixturePath in $originalIsolatedCrucibleWardenFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original isolated Crucible Warden checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedMakeshiftGolemFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-makeshift-golem.mt2f' })
foreach ($fixturePath in $originalIsolatedMakeshiftGolemFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original isolated Makeshift Golem checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedDeathcappedProphetFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-deathcapped-prophet.mt2f' })
foreach ($fixturePath in $originalIsolatedDeathcappedProphetFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original isolated Deathcapped Prophet checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedParrafinEnforcerFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-parrafin-enforcer.mt2f' })
foreach ($fixturePath in $originalIsolatedParrafinEnforcerFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original isolated Parrafin Enforcer checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedSycophantFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-sycophant.mt2f' })
foreach ($fixturePath in $originalIsolatedSycophantFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original isolated Sycophant checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedVotivaryFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-votivary.mt2f' })
foreach ($fixturePath in $originalIsolatedVotivaryFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original isolated Votivary checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedBountyStalkerFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-bounty-stalker.mt2f' })
foreach ($fixturePath in $originalIsolatedBountyStalkerFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original isolated Bounty Stalker checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedPlagueDoctorFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-plague-doctor.mt2f' })
foreach ($fixturePath in $originalIsolatedPlagueDoctorFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original isolated Plague Doctor checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedHulkingMassFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-hulking-mass.mt2f' })
foreach ($fixturePath in $originalIsolatedHulkingMassFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original isolated Hulking Mass checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedReformingOozeFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-reforming-ooze.mt2f' })
foreach ($fixturePath in $originalIsolatedReformingOozeFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original isolated Reforming Ooze checks failed ($mode): $fixturePath" }
    }
}
$originalAstrologerFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-astrologer.mt2f' })
foreach ($fixturePath in $originalAstrologerFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Astrologer checks failed ($mode): $fixturePath" }
    }
}
$originalSnottyWhelpFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-snotty-whelp.mt2f' })
foreach ($fixturePath in $originalSnottyWhelpFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Snotty Whelp checks failed ($mode): $fixturePath" }
    }
}
$originalBloatedWhelpFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-bloated-whelp.mt2f' })
foreach ($fixturePath in $originalBloatedWhelpFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Bloated Whelp checks failed ($mode): $fixturePath" }
    }
}
$originalFaeMeddlerFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-fae-meddler.mt2f' })
foreach ($fixturePath in $originalFaeMeddlerFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Fae Meddler checks failed ($mode): $fixturePath" }
    }
}
$originalMoltenEncasementFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-molten-encasement.mt2f' })
foreach ($fixturePath in $originalMoltenEncasementFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Molten Encasement checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedShatteredShellFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-shattered-shell.mt2f' })
foreach ($fixturePath in $originalIsolatedShatteredShellFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Shattered Shell checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedWeldlingFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-weldling.mt2f' })
foreach ($fixturePath in $originalIsolatedWeldlingFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Weldling checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedHostguardFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-hostguard.mt2f' })
foreach ($fixturePath in $originalIsolatedHostguardFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Hostguard checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedRubbleRaiserFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-rubble-raiser.mt2f' })
foreach ($fixturePath in $originalIsolatedRubbleRaiserFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Rubble Raiser checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedNightingaleFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-nightingale.mt2f' })
foreach ($fixturePath in $originalIsolatedNightingaleFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Nightingale checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedJuggernautFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-juggernaut.mt2f' })
foreach ($fixturePath in $originalIsolatedJuggernautFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Juggernaut checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedHoardAcolyteFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-hoard-acolyte.mt2f' })
foreach ($fixturePath in $originalIsolatedHoardAcolyteFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Hoard Acolyte checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedHotHeadFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-hothead.mt2f' })
foreach ($fixturePath in $originalIsolatedHotHeadFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original HotHead checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedSteadfastCrusaderFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-steadfast-crusader.mt2f' })
foreach ($fixturePath in $originalIsolatedSteadfastCrusaderFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Steadfast Crusader checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedTrufflesFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-truffles.mt2f' })
foreach ($fixturePath in $originalIsolatedTrufflesFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Truffles checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedPyrebloodedFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-pyreblooded.mt2f' })
foreach ($fixturePath in $originalIsolatedPyrebloodedFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Pyreblooded checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedKinhostCarapaceFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-kinhost-carapace.mt2f' })
foreach ($fixturePath in $originalIsolatedKinhostCarapaceFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Kinhost Carapace checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedMeathookFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-meathook.mt2f' })
foreach ($fixturePath in $originalIsolatedMeathookFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Meathook checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedRailgunFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-railgun.mt2f' })
foreach ($fixturePath in $originalIsolatedRailgunFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Railgun checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedSilentSentinelFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-silent-sentinel.mt2f' })
foreach ($fixturePath in $originalIsolatedSilentSentinelFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Silent Sentinel checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedSpikedriverFactoryFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-spikedriver-factory.mt2f' })
foreach ($fixturePath in $originalIsolatedSpikedriverFactoryFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Spikedriver Factory checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedScrivenerFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-scrivener.mt2f' })
foreach ($fixturePath in $originalIsolatedScrivenerFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Scrivener checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedBanishedChamp2Fixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-banished-champ2.mt2f' })
foreach ($fixturePath in $originalIsolatedBanishedChamp2Fixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Banished Champion 2 checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedPyreborneChamp1Fixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-pyreborne-champ1.mt2f' })
foreach ($fixturePath in $originalIsolatedPyreborneChamp1Fixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Pyreborne Champion 1 checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedDeathEaterFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-death-eater.mt2f' })
foreach ($fixturePath in $originalIsolatedDeathEaterFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Death Eater checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedLunaCovenChamp1Fixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-luna-coven-champ1.mt2f' })
foreach ($fixturePath in $originalIsolatedLunaCovenChamp1Fixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Luna Coven Champion 1 checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedLunaCovenChamp2Fixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-luna-coven-champ2.mt2f' })
foreach ($fixturePath in $originalIsolatedLunaCovenChamp2Fixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Luna Coven Champion 2 checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedSporelauncherFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-sporelauncher.mt2f' })
foreach ($fixturePath in $originalIsolatedSporelauncherFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Sporelauncher checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedConductorStewardFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-conductor-steward.mt2f' })
foreach ($fixturePath in $originalIsolatedConductorStewardFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Conductor Steward checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedEnchantrelleFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-enchantrelle.mt2f' })
foreach ($fixturePath in $originalIsolatedEnchantrelleFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Enchantrelle checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedFeederDamageEnemiesFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-feeder-damage-enemies.mt2f' })
foreach ($fixturePath in $originalIsolatedFeederDamageEnemiesFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Feeder Damage Enemies checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedLadyOfTheReformedFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-lady-of-the-reformed.mt2f' })
foreach ($fixturePath in $originalIsolatedLadyOfTheReformedFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Lady of the Reformed checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedFeederPermanentAttackBoostFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-feeder-permanent-attack-boost.mt2f' })
foreach ($fixturePath in $originalIsolatedFeederPermanentAttackBoostFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Feeder Permanent Attack Boost checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedZealotFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-zealot.mt2f' })
foreach ($fixturePath in $originalIsolatedZealotFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Zealot checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedParrafinThugFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-parrafin-thug.mt2f' })
foreach ($fixturePath in $originalIsolatedParrafinThugFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Parrafin Thug checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedMonstrosityFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-monstrosity.mt2f' })
foreach ($fixturePath in $originalIsolatedMonstrosityFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Monstrosity checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedWicklessDeathFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-wickless-death-trigger-batch.mt2f' })
foreach ($fixturePath in $originalIsolatedWicklessDeathFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Wickless death-trigger batch checks failed ($mode): $fixturePath" }
    }
}
$originalIsolatedDeathSummonFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -in @('full-battle-original-isolated-death-summon-batch.mt2f', 'full-battle-original-isolated-conduit-moon-batch.mt2f', 'full-battle-original-isolated-enchant-batch.mt2f', 'full-battle-original-isolated-fae-moon-pixie.mt2f', 'full-battle-original-isolated-morselmaker.mt2f', 'full-battle-original-isolated-cluster-colonel.mt2f', 'full-battle-original-isolated-glareminder.mt2f', 'full-battle-original-isolated-lunar-priestess.mt2f') })
foreach ($fixturePath in $originalIsolatedDeathSummonFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original death-summon batch checks failed ($mode): $fixturePath" }
    }
}
$originalFaeMeddlerFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-isolated-fae-meddler.mt2f' })
$originalForgeCallbackFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -in @(
    'full-battle-original-isolated-forge-resource-callbacks.mt2f', 'full-battle-original-isolated-unit-room-resources.mt2f',
    'full-battle-original-isolated-vinemother.mt2f', 'full-battle-original-isolated-greed-dragon.mt2f',
    'full-battle-original-isolated-shadeguard.mt2f', 'full-battle-original-isolated-mooncaller.mt2f',
    'full-battle-original-isolated-triggered-recursion.mt2f') })
foreach ($fixturePath in $originalForgeCallbackFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Forge callback checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in $originalFaeMeddlerFixtures) {
    foreach ($mode in @('--original-support-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Fae Meddler checks failed ($mode): $fixturePath" }
    }
}
$originalAttackOverrideFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-attack-overrides.mt2f' })
foreach ($fixturePath in $originalAttackOverrideFixtures) {
    foreach ($mode in @('--original-attack-overrides-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original attack override checks failed ($mode): $fixturePath" }
    }
}
$originalReactiveUnitFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-reactive-units.mt2f' })
foreach ($fixturePath in $originalReactiveUnitFixtures) {
    foreach ($mode in @('--original-reactive-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original reactive unit checks failed ($mode): $fixturePath" }
    }
}
$originalBaseChampionFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-base-champions.mt2f' })
foreach ($fixturePath in $originalBaseChampionFixtures) {
    foreach ($mode in @('--original-base-champions-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original base champion checks failed ($mode): $fixturePath" }
    }
}
$originalStatusUnitFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-status-units.mt2f' })
foreach ($fixturePath in $originalStatusUnitFixtures) {
    foreach ($mode in @('--original-status-units-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original status unit checks failed ($mode): $fixturePath" }
    }
}
$originalEquipmentCardFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -in @('full-battle-original-equipment-cards.mt2f', 'full-battle-original-isolated-equipment-phase-batch.mt2f', 'full-battle-original-isolated-equipment-combat-batch.mt2f', 'full-battle-original-isolated-stasis-crystal.mt2f', 'full-battle-original-isolated-mitosis-cap.mt2f', 'full-battle-original-isolated-loamcoat.mt2f', 'full-battle-original-isolated-forgotten-trade.mt2f', 'full-battle-original-isolated-windblade.mt2f', 'full-battle-original-isolated-shield-of-wyngh.mt2f', 'full-battle-original-isolated-frostpiercer.mt2f') })
foreach ($fixturePath in $originalEquipmentCardFixtures) {
    foreach ($mode in @('--original-equipment-cards-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original equipment card checks failed ($mode): $fixturePath" }
    }
}
$originalNullScourgeFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-null-scourges.mt2f' })
foreach ($fixturePath in $originalNullScourgeFixtures) {
    foreach ($mode in @('--original-null-scourges-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original null Scourge checks failed ($mode): $fixturePath" }
    }
}
$originalSupportSpellFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -in @('full-battle-original-support-spells.mt2f', 'full-battle-original-isolated-generation-spells.mt2f', 'full-battle-original-isolated-damage-capacity-spells.mt2f', 'full-battle-original-isolated-replicator-ray.mt2f') })
foreach ($fixturePath in $originalSupportSpellFixtures) {
    foreach ($mode in @('--original-support-spells-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original support spell checks failed ($mode): $fixturePath" }
    }
}
$remainingOriginalSpellFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -in @(
    'full-battle-original-isolated-intent-on-death.mt2f', 'full-battle-original-isolated-cannibalize.mt2f',
    'full-battle-original-isolated-scrap-for-parts.mt2f', 'full-battle-original-isolated-hallowed-halls.mt2f',
    'full-battle-original-isolated-hallowed-halls-capacity.mt2f', 'full-battle-original-isolated-sacrificial-resurrection.mt2f',
    'full-battle-original-isolated-exhausted-first-of-kin.mt2f', 'full-battle-original-isolated-exhausted-morel-mistress.mt2f',
    'full-battle-original-isolated-void-arms-living.mt2f', 'full-battle-original-isolated-void-arms-death.mt2f',
    'full-battle-original-isolated-self-attack.mt2f', 'full-battle-original-isolated-bog-hatching.mt2f',
    'full-battle-original-isolated-dante.mt2f', 'full-battle-original-isolated-balmabello.mt2f',
    'full-battle-original-isolated-pyre-steward.mt2f', 'full-battle-original-isolated-bone-dog.mt2f',
    'full-battle-original-isolated-shadesplitter.mt2f', 'full-battle-original-isolated-revenge-of-the-damned.mt2f',
    'full-battle-original-isolated-filtered-hand-upgrades.mt2f', 'full-battle-original-isolated-deaths-dancer.mt2f',
    'full-battle-original-isolated-deaths-dancer-shift.mt2f', 'full-battle-original-isolated-kill-damage-growth.mt2f',
    'full-battle-original-isolated-boilerplate-forged.mt2f', 'full-battle-original-isolated-boilerplate-zero.mt2f',
    'full-battle-original-isolated-inhale-birth.mt2f', 'full-battle-original-isolated-inhale-existing.mt2f',
    'full-battle-original-isolated-inhale-friendly-death.mt2f', 'full-battle-original-isolated-ephemeral-routing.mt2f',
    'full-battle-original-isolated-bounding-corrupt.mt2f', 'full-battle-original-isolated-returned-corrupt.mt2f',
    'full-battle-original-isolated-horde-and-morsel-targets.mt2f', 'full-battle-original-isolated-funguy-existing-horde.mt2f',
    'full-battle-original-isolated-discard-copy-plain.mt2f', 'full-battle-original-isolated-discard-copy-ephemeral.mt2f',
    'full-battle-original-isolated-discard-copy-stardust.mt2f', 'full-battle-original-isolated-heal-cost.mt2f',
    'full-battle-original-isolated-heal-cost-other-floor.mt2f', 'full-battle-original-isolated-room-rage.mt2f',
    'full-battle-original-isolated-room-spikes.mt2f', 'full-battle-original-isolated-room-valor.mt2f',
    'full-battle-original-isolated-room-rage-self-attack.mt2f', 'full-battle-original-isolated-greed-dragon-zero.mt2f',
    'full-battle-original-isolated-crucible-steward.mt2f', 'full-battle-original-isolated-triggered-recursion-empty.mt2f',
    'full-battle-original-isolated-room-trigger-rage.mt2f', 'full-battle-original-isolated-room-trigger-valor.mt2f',
    'full-battle-original-isolated-room-trigger-healing.mt2f', 'full-battle-original-isolated-room-trigger-spikes.mt2f',
    'full-battle-original-isolated-room-trigger-energy.mt2f', 'full-battle-original-isolated-room-trigger-gold.mt2f',
    'full-battle-original-isolated-room-trigger-spawn-upgrade.mt2f', 'full-battle-original-isolated-room-trigger-replacement.mt2f',
    'full-battle-original-isolated-weapons-cache.mt2f', 'full-battle-original-isolated-weapons-cache-empty.mt2f',
    'full-battle-original-isolated-imprisoned-zephyr.mt2f',
    'full-battle-original-isolated-hellfurnace.mt2f', 'full-battle-original-isolated-hellfurnace-replacement.mt2f',
    'full-battle-original-isolated-total-recall.mt2f', 'full-battle-original-isolated-total-recall-empty.mt2f',
    'full-battle-original-isolated-hall-of-mirrors.mt2f', 'full-battle-original-isolated-hall-of-mirrors-replacement.mt2f',
    'full-battle-original-isolated-copy-morsels-unit.mt2f',
    'full-battle-original-isolated-enchanted-chamber.mt2f', 'full-battle-original-isolated-enchanted-chamber-replacement.mt2f',
    'full-battle-original-isolated-ethereal-seelie.mt2f',
    'full-battle-original-isolated-unit-grafts.mt2f',
    'full-battle-original-isolated-infectory.mt2f',
    'full-battle-original-isolated-siren-song.mt2f',
    'full-battle-original-isolated-equipment-consumption.mt2f',
    'full-battle-original-isolated-food-spawn.mt2f',
    'full-battle-original-isolated-status-synergy.mt2f',
    'full-battle-original-isolated-treasure-discard.mt2f',
    'full-battle-original-isolated-room-skills.mt2f',
    'full-battle-original-isolated-room-movement-skills.mt2f',
    'full-battle-original-isolated-sentry-turret.mt2f',
    'full-battle-original-isolated-squad-leader.mt2f',
    'full-battle-original-isolated-squad-leader-owner-death.mt2f',
    'full-battle-original-isolated-squad-leader-last-troop.mt2f',
    'full-battle-original-isolated-jack-in-the-box.mt2f',
    'full-battle-original-isolated-jack-in-the-box-empty.mt2f',
    'full-battle-original-isolated-echo-vault.mt2f',
    'full-battle-original-bog-fly-flight-ability.mt2f',
    'full-battle-original-glugsider-ability.mt2f',
    'full-battle-original-extract-draw-ability.mt2f',
    'full-battle-original-fetch-ability.mt2f',
    'full-battle-original-isolated-infusion-definitions.mt2f',
    'full-battle-original-savior2-ability.mt2f',
    'full-battle-original-savior3-ability.mt2f',
    'full-battle-original-courage-ability.mt2f',
    'full-battle-original-chomp-sacrifice-ability.mt2f',
    'full-battle-original-chomp-food-ability.mt2f',
    'full-battle-original-time-freeze-ability.mt2f',
    'full-battle-original-lunar-echo-ability.mt2f',
    'full-battle-original-spikedriver-factory-choice0-ability.mt2f',
    'full-battle-original-spikedriver-factory-choice1-ability.mt2f') })
foreach ($fixturePath in $remainingOriginalSpellFixtures) {
    foreach ($mode in @('--original-support-spells-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Remaining original spell checks failed ($mode): $fixturePath" }
    }
}
$originalRearrangeSpellFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-rearrange-spells.mt2f' })
foreach ($fixturePath in $originalRearrangeSpellFixtures) {
    foreach ($mode in @('--original-rearrange-spells-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original rearrange spell checks failed ($mode): $fixturePath" }
    }
}
$originalMovementSpellFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-movement-spells.mt2f' })
foreach ($fixturePath in $originalMovementSpellFixtures) {
    foreach ($mode in @('--original-movement-spells-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original movement spell checks failed ($mode): $fixturePath" }
    }
}
$originalScaledDamageFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-scaled-damage.mt2f' })
foreach ($fixturePath in $originalScaledDamageFixtures) {
    foreach ($mode in @('--original-scaled-damage-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original scaled damage checks failed ($mode): $fixturePath" }
    }
}
$originalHostileStatusFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-hostile-statuses.mt2f' })
foreach ($fixturePath in $originalHostileStatusFixtures) {
    foreach ($mode in @('--original-hostile-statuses-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original hostile status checks failed ($mode): $fixturePath" }
    }
}
$originalResourceSpellFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-resource-spells.mt2f' })
foreach ($fixturePath in $originalResourceSpellFixtures) {
    foreach ($mode in @('--original-resource-spells-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original resource spell checks failed ($mode): $fixturePath" }
    }
}
$originalBasicDamageFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-basic-damage.mt2f' })
foreach ($fixturePath in $originalBasicDamageFixtures) {
    foreach ($mode in @('--original-basic-damage-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original basic damage checks failed ($mode): $fixturePath" }
    }
}
$originalFriendlyStatusFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-friendly-statuses.mt2f' })
foreach ($fixturePath in $originalFriendlyStatusFixtures) {
    foreach ($mode in @('--original-friendly-statuses-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original friendly status checks failed ($mode): $fixturePath" }
    }
}
$originalKillResourceFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -in @(
    'full-battle-original-kill-hoard.mt2f', 'full-battle-original-kill-forge.mt2f', 'full-battle-original-kill-gold.mt2f') })
foreach ($fixturePath in $originalKillResourceFixtures) {
    foreach ($mode in @('--original-kill-resources-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original kill resource checks failed ($mode): $fixturePath" }
    }
}
$originalKillCopyFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-kill-copy.mt2f' })
foreach ($fixturePath in $originalKillCopyFixtures) {
    foreach ($mode in @('--original-kill-copy-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Surgical Strike checks failed ($mode): $fixturePath" }
    }
}
$originalUnitBuffsFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-unit-buffs.mt2f' })
foreach ($fixturePath in $originalUnitBuffsFixtures) {
    foreach ($mode in @('--original-unit-buffs-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original unit buff checks failed ($mode): $fixturePath" }
    }
}
$originalMorselEatenFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-morsel-eaten.mt2f' })
foreach ($fixturePath in $originalMorselEatenFixtures) {
    foreach ($mode in @('--original-morsel-eaten-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Morsel feeding checks failed ($mode): $fixturePath" }
    }
}
$originalOnKillGenerationFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-on-kill-generation.mt2f' })
foreach ($fixturePath in $originalOnKillGenerationFixtures) {
    foreach ($mode in @('--original-on-kill-generation-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original card OnKill generation checks failed ($mode): $fixturePath" }
    }
}
$originalOnKillFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-on-kill.mt2f' })
foreach ($fixturePath in $originalOnKillFixtures) {
    foreach ($mode in @('--original-on-kill-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original card OnKill checks failed ($mode): $fixturePath" }
    }
}
$originalHealingCleanseFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-healing-cleanse.mt2f' })
foreach ($fixturePath in $originalHealingCleanseFixtures) {
    foreach ($mode in @('--original-healing-cleanse-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original healing/cleanse checks failed ($mode): $fixturePath" }
    }
}
$originalSelfAttackFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-self-attack.mt2f' })
foreach ($fixturePath in $originalSelfAttackFixtures) {
    foreach ($mode in @('--original-self-attack-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original self-attack checks failed ($mode): $fixturePath" }
    }
}
$originalGraftedEquipmentFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-grafted-equipment.mt2f' })
foreach ($fixturePath in $originalGraftedEquipmentFixtures) {
    foreach ($mode in @('--original-grafted-equipment-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original grafted equipment checks failed ($mode): $fixturePath" }
    }
}
$originalPileStatusFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-pile-status.mt2f' })
foreach ($fixturePath in $originalPileStatusFixtures) {
    foreach ($mode in @('--original-pile-status-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original pile status checks failed ($mode): $fixturePath" }
    }
}
$originalScaledBuffFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-scaled-buff.mt2f' })
foreach ($fixturePath in $originalScaledBuffFixtures) {
    foreach ($mode in @('--original-scaled-buff-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original scaled buff checks failed ($mode): $fixturePath" }
    }
}
$originalPileRecursionFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-pile-recursion.mt2f' })
foreach ($fixturePath in $originalPileRecursionFixtures) {
    foreach ($mode in @('--original-pile-recursion-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original pile recursion checks failed ($mode): $fixturePath" }
    }
}
$originalPileDrawFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-pile-draw.mt2f' })
foreach ($fixturePath in $originalPileDrawFixtures) {
    foreach ($mode in @('--original-pile-draw-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original pile draw checks failed ($mode): $fixturePath" }
    }
}
$originalCardDrawPotionFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-card-draw-potion.mt2f' })
foreach ($fixturePath in $originalCardDrawPotionFixtures) {
    foreach ($mode in @('--original-card-draw-potion-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Card Draw Potion checks failed ($mode): $fixturePath" }
    }
}
$originalAcceleratedIncubationFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-accelerated-incubation.mt2f' })
foreach ($fixturePath in $originalAcceleratedIncubationFixtures) {
    foreach ($mode in @('--original-accelerated-incubation-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Accelerated Incubation checks failed ($mode): $fixturePath" }
    }
}
$originalTomeOfContractsFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-tome-of-contracts.mt2f' })
foreach ($fixturePath in $originalTomeOfContractsFixtures) {
    foreach ($mode in @('--original-tome-of-contracts-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Tome of Contracts checks failed ($mode): $fixturePath" }
    }
}
$originalEagerConscriptFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-eager-conscript.mt2f' })
foreach ($fixturePath in $originalEagerConscriptFixtures) {
    foreach ($mode in @('--original-eager-conscript-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Eager Conscript checks failed ($mode): $fixturePath" }
    }
}
$originalCopyMorselTwiceFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-copy-morsel-twice.mt2f' })
foreach ($fixturePath in $originalCopyMorselTwiceFixtures) {
    foreach ($mode in @('--original-copy-morsel-twice-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Copy Morsel Twice checks failed ($mode): $fixturePath" }
    }
}
$originalLazarusLeagueSpikeFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-lazarus-league-spike.mt2f' })
foreach ($fixturePath in $originalLazarusLeagueSpikeFixtures) {
    foreach ($mode in @('--original-lazarus-league-spike-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Lazarus League Spike checks failed ($mode): $fixturePath" }
    }
}
$originalEffectApplicationsFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-effect-applications.mt2f' })
foreach ($fixturePath in $originalEffectApplicationsFixtures) {
    foreach ($mode in @('--original-effect-applications-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original effect applications checks failed ($mode): $fixturePath" }
    }
}
$originalStatusRemovalScalingFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-status-removal-scaling.mt2f' })
foreach ($fixturePath in $originalStatusRemovalScalingFixtures) {
    foreach ($mode in @('--original-status-removal-scaling-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original status-removal scaling checks failed ($mode): $fixturePath" }
    }
}
$originalScaledEnergyFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-scaled-energy.mt2f' })
foreach ($fixturePath in $originalScaledEnergyFixtures) {
    foreach ($mode in @('--original-scaled-energy-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original scaled energy checks failed ($mode): $fixturePath" }
    }
}
$originalSpecificCardDamageFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-specific-card-damage.mt2f' })
foreach ($fixturePath in $originalSpecificCardDamageFixtures) {
    foreach ($mode in @('--original-specific-card-damage-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original specific-card damage checks failed ($mode): $fixturePath" }
    }
}
$originalStatusStackDamageFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-status-stack-damage.mt2f' })
foreach ($fixturePath in $originalStatusStackDamageFixtures) {
    foreach ($mode in @('--original-status-stack-damage-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original status-stack damage checks failed ($mode): $fixturePath" }
    }
}
$originalCorruptRestrictedFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-corrupt-restricted.mt2f' })
foreach ($fixturePath in $originalCorruptRestrictedFixtures) {
    foreach ($mode in @('--original-corrupt-restricted-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Corrupt Restricted checks failed ($mode): $fixturePath" }
    }
}
$originalPerfectInsanityFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-perfect-insanity.mt2f' })
foreach ($fixturePath in $originalPerfectInsanityFixtures) {
    foreach ($mode in @('--original-perfect-insanity-only', '--native-actions-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Perfect Insanity checks failed ($mode): $fixturePath" }
    }
}
$originalStarterReformFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-starter-reform.mt2f' })
foreach ($fixturePath in $originalStarterReformFixtures) {
    foreach ($mode in @('--original-starter-reform-only', '--native-actions-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Starter Reform checks failed ($mode): $fixturePath" }
    }
}
$originalRapidRaiseFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-rapid-raise.mt2f' })
foreach ($fixturePath in $originalRapidRaiseFixtures) {
    foreach ($mode in @('--original-rapid-raise-only', '--native-actions-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Rapid Raise checks failed ($mode): $fixturePath" }
    }
}
$originalSpawningPodsFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-spawning-pods.mt2f' })
foreach ($fixturePath in $originalSpawningPodsFixtures) {
    foreach ($mode in @('--original-spawning-pods-only', '--native-actions-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Spawning Pods checks failed ($mode): $fixturePath" }
    }
}
$originalMindsOfTheirOwnFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-minds-of-their-own.mt2f' })
foreach ($fixturePath in $originalMindsOfTheirOwnFixtures) {
    foreach ($mode in @('--original-minds-of-their-own-only', '--native-actions-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Minds of Their Own checks failed ($mode): $fixturePath" }
    }
}
$originalMetallurgyFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-metallurgy.mt2f' })
foreach ($fixturePath in $originalMetallurgyFixtures) {
    foreach ($mode in @('--original-metallurgy-only', '--native-actions-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Metallurgy checks failed ($mode): $fixturePath" }
    }
}
$originalFertileGroundFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-fertile-ground.mt2f' })
foreach ($fixturePath in $originalFertileGroundFixtures) {
    foreach ($mode in @('--original-fertile-ground-only', '--native-actions-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Fertile Ground checks failed ($mode): $fixturePath" }
    }
}
$originalCoverOfNightFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-cover-of-night.mt2f' })
foreach ($fixturePath in $originalCoverOfNightFixtures) {
    foreach ($mode in @('--original-cover-of-night-only', '--native-actions-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Cover of Night checks failed ($mode): $fixturePath" }
    }
}
$originalWaxenSpikeFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-waxen-spike.mt2f' })
foreach ($fixturePath in $originalWaxenSpikeFixtures) {
    foreach ($mode in @('--original-waxen-spike-only', '--native-actions-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Waxen Spike checks failed ($mode): $fixturePath" }
    }
}
$originalImpPressiveFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-imp-pressive.mt2f' })
foreach ($fixturePath in $originalImpPressiveFixtures) {
    foreach ($mode in @('--original-imp-pressive-only', '--native-actions-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original ImpPressive checks failed ($mode): $fixturePath" }
    }
}
$originalTomeOfEvergrowthFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-tome-of-evergrowth.mt2f' })
foreach ($fixturePath in $originalTomeOfEvergrowthFixtures) {
    foreach ($mode in @('--original-tome-of-evergrowth-only', '--native-actions-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Tome of Evergrowth checks failed ($mode): $fixturePath" }
    }
}
$originalTomeOfTheLostPathFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-tome-of-the-lost-path.mt2f' })
foreach ($fixturePath in $originalTomeOfTheLostPathFixtures) {
    foreach ($mode in @('--original-tome-of-the-lost-path-only', '--native-actions-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Tome of the Lost Path checks failed ($mode): $fixturePath" }
    }
}
$originalMakeItRainFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-make-it-rain.mt2f' })
foreach ($fixturePath in $originalMakeItRainFixtures) {
    foreach ($mode in @('--original-make-it-rain-only', '--native-actions-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Make It Rain checks failed ($mode): $fixturePath" }
    }
}
$originalDeathroarFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-deathroar.mt2f' })
foreach ($fixturePath in $originalDeathroarFixtures) {
    foreach ($mode in @('--original-deathroar-only', '--native-actions-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Deathroar checks failed ($mode): $fixturePath" }
    }
}
$originalSecretIngredientFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-secret-ingredient.mt2f' })
foreach ($fixturePath in $originalSecretIngredientFixtures) {
    foreach ($mode in @('--original-secret-ingredient-only', '--native-actions-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Secret Ingredient checks failed ($mode): $fixturePath" }
    }
}
$originalGreenRecruitFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -in @('full-battle-original-green-recruit.mt2f', 'full-battle-original-green-recruit-settled.mt2f') })
foreach ($fixturePath in $originalGreenRecruitFixtures) {
    if ((Split-Path -Leaf $fixturePath) -eq 'full-battle-original-green-recruit.mt2f') {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --green-recruit-room-preview-only $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Green Recruit room preview checks failed: $fixturePath" }
    }
    foreach ($mode in @('--original-green-recruit-only', '--native-actions-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Green Recruit checks failed ($mode): $fixturePath" }
    }
}
$originalPrimordiumFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-primordium.mt2f' })
foreach ($fixturePath in $originalPrimordiumFixtures) {
    foreach ($mode in @('--original-primordium-model-only', '--original-primordium-only', '--native-actions-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Primordium checks failed ($mode): $fixturePath" }
    }
}
$originalCheeryDeathcapFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-cheery-deathcap.mt2f' })
foreach ($fixturePath in $originalCheeryDeathcapFixtures) {
    foreach ($mode in @('--original-cheery-deathcap-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Cheery Deathcap checks failed ($mode): $fixturePath" }
    }
}
$originalEnthusiasticFanFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-enthusiastic-fan.mt2f' })
foreach ($fixturePath in $originalEnthusiasticFanFixtures) {
    foreach ($mode in @('--original-enthusiastic-fan-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Enthusiastic Fan checks failed ($mode): $fixturePath" }
    }
}
$originalOverflowFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-overflow-cards.mt2f' })
foreach ($fixturePath in $originalOverflowFixtures) {
    foreach ($mode in @('--original-overflow-cards-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original overflow checks failed ($mode): $fixturePath" }
    }
}
$originalPermafrostFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-permafrost-cards.mt2f' })
foreach ($fixturePath in $originalPermafrostFixtures) {
    foreach ($mode in @('--original-permafrost-cards-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Permafrost checks failed ($mode): $fixturePath" }
    }
}
$holdoverFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -in @(
    'full-battle-progressed-ring7-holdover-policy.mt2f', 'full-battle-progressed-ring7-holdover-win.mt2f',
    'full-battle-progressed-ring7-holdover-exact-replay.mt2f') })
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'holdover-variants-calibration.mt2f' })) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --holdover-variants-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Native Holdover variant checks failed: $fixturePath" }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'permanent-trait-calibration.mt2f' })) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --permanent-traits-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Native permanent trait checks failed: $fixturePath" }
}
foreach ($fixturePath in $holdoverFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --holdover-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Native Holdover checks failed: $fixturePath" }
    if ((Split-Path -Leaf $fixturePath) -in @('full-battle-progressed-ring7-holdover-win.mt2f',
        'full-battle-progressed-ring7-holdover-exact-replay.mt2f')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --starting-duality-only $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Native starting Duality checks failed: $fixturePath" }
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --rolling-advice-only $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Native Holdover frozen forecast checks failed: $fixturePath" }
    }
    if ((Split-Path -Leaf $fixturePath) -eq 'full-battle-progressed-ring7-holdover-win.mt2f') {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --exact-battle-only $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Exact solver certificate checks failed: $fixturePath" }
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --belief-battle-only $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Unknown draw policy checks failed: $fixturePath" }
    }
}
$originalUnplayableFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -in @(
    'full-battle-original-unplayable-cards.mt2f', 'full-battle-original-unplayable-persistent-routing.mt2f') })
foreach ($fixturePath in $originalUnplayableFixtures) {
    foreach ($mode in @('--original-unplayable-cards-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Unplayable checks failed ($mode): $fixturePath" }
    }
}
$originalMonsterHandCostFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-monster-hand-costs.mt2f' })
foreach ($fixturePath in $originalMonsterHandCostFixtures) {
    foreach ($mode in @('--original-monster-hand-costs-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Monster hand cost checks failed ($mode): $fixturePath" }
    }
}
$originalLimitedRangeFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-limited-range.mt2f' })
foreach ($fixturePath in $originalLimitedRangeFixtures) {
    foreach ($mode in @('--original-limited-range-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Limited Range checks failed ($mode): $fixturePath" }
    }
}
$originalHandCostFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-hand-costs.mt2f' })
foreach ($fixturePath in $originalHandCostFixtures) {
    foreach ($mode in @('--original-hand-costs-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original hand cost checks failed ($mode): $fixturePath" }
    }
}
$originalUnpurgableFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-unpurgable-cards.mt2f' })
foreach ($fixturePath in $originalUnpurgableFixtures) {
    foreach ($mode in @('--original-unpurgable-cards-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Unpurgable checks failed ($mode): $fixturePath" }
    }
}
$originalRoomStatusFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-room-status-damage.mt2f' })
foreach ($fixturePath in $originalRoomStatusFixtures) {
    foreach ($mode in @('--original-room-status-damage-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original room-status damage checks failed ($mode): $fixturePath" }
    }
}
$originalCorruptionFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-corruption-damage.mt2f' })
foreach ($fixturePath in $originalCorruptionFixtures) {
    foreach ($mode in @('--original-corruption-damage-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original corruption-damage checks failed ($mode): $fixturePath" }
    }
}
$originalXStatusFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-x-status-cards.mt2f' })
foreach ($fixturePath in $originalXStatusFixtures) {
    foreach ($mode in @('--original-x-status-cards-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original X-status checks failed ($mode): $fixturePath" }
    }
}
$originalXBanishedFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-x-banished-spike.mt2f' })
foreach ($fixturePath in $originalXBanishedFixtures) {
    foreach ($mode in @('--original-x-banished-spike-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Spike of the Banished checks failed ($mode): $fixturePath" }
    }
}
$originalXLunaFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-x-luna-spike.mt2f' })
foreach ($fixturePath in $originalXLunaFixtures) {
    foreach ($mode in @('--original-x-luna-spike-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Spike of the Luna Coven checks failed ($mode): $fixturePath" }
    }
}
$originalXStygianFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-x-stygian-spike.mt2f' })
foreach ($fixturePath in $originalXStygianFixtures) {
    foreach ($mode in @('--original-x-stygian-spike-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Spike of the Stygian checks failed ($mode): $fixturePath" }
    }
}
$originalXWurmkinFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-x-wurmkin-spike.mt2f' })
foreach ($fixturePath in $originalXWurmkinFixtures) {
    foreach ($mode in @('--original-x-wurmkin-spike-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Wurmkin Spike checks failed ($mode): $fixturePath" }
    }
}
$originalXDamageFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-x-damage-cards.mt2f' })
foreach ($fixturePath in $originalXDamageFixtures) {
    foreach ($mode in @('--original-x-damage-cards-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original X-damage checks failed ($mode): $fixturePath" }
    }
}
$originalMineCollapseFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-mine-collapse.mt2f' })
foreach ($fixturePath in $originalMineCollapseFixtures) {
    foreach ($mode in @('--original-mine-collapse-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Mine Collapse checks failed ($mode): $fixturePath" }
    }
}
$originalImpOlateFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-imp-olate.mt2f' })
foreach ($fixturePath in $originalImpOlateFixtures) {
    foreach ($mode in @('--original-imp-olate-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Imp Olate checks failed ($mode): $fixturePath" }
    }
}
$originalImpOlateZeroFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-imp-olate-zero.mt2f' })
foreach ($fixturePath in $originalImpOlateZeroFixtures) {
    foreach ($mode in @('--original-imp-olate-zero-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Imp Olate zero-Imp checks failed ($mode): $fixturePath" }
    }
}
$originalPyreshotFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-pyreshot.mt2f' })
foreach ($fixturePath in $originalPyreshotFixtures) {
    foreach ($mode in @('--attack-with-unit-pure', '--original-pyreshot-only', '--battle-search-only')) {
        if ($mode -eq '--attack-with-unit-pure') {
            dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode
        } else {
            dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        }
        if ($LASTEXITCODE -ne 0) { throw "Original Pyreshot checks failed ($mode): $fixturePath" }
    }
}
$originalRetchFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-retch.mt2f' })
foreach ($fixturePath in $originalRetchFixtures) {
    foreach ($mode in @('--original-retch-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Retch checks failed ($mode): $fixturePath" }
    }
}
$originalStrongMagicFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-strong-magic-cards.mt2f' })
foreach ($fixturePath in $originalStrongMagicFixtures) {
    foreach ($mode in @('--original-strong-magic-cards-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original strong-magic checks failed ($mode): $fixturePath" }
    }
}
$originalConsumeFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-consume-cards.mt2f' })
foreach ($fixturePath in $originalConsumeFixtures) {
    foreach ($mode in @('--original-consume-cards-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Consume checks failed ($mode): $fixturePath" }
    }
}
$infernoRoomFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-inferno-room.mt2f' })
foreach ($fixturePath in $infernoRoomFixtures) {
    foreach ($mode in @('--inferno-room-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Inferno Room checks failed ($mode): $fixturePath" }
    }
}
$originalPiercingFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-piercing-cards.mt2f' })
foreach ($fixturePath in $originalPiercingFixtures) {
    foreach ($mode in @('--original-piercing-cards-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original piercing card checks failed ($mode): $fixturePath" }
    }
}
$mushyRoomFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-mushy-room.mt2f' })
foreach ($fixturePath in $mushyRoomFixtures) {
    foreach ($mode in @('--mushy-room-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Mushy Room checks failed ($mode): $fixturePath" }
    }
}
$growRoomFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-grow-room.mt2f' })
foreach ($fixturePath in $growRoomFixtures) {
    foreach ($mode in @('--grow-room-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Grow Room checks failed ($mode): $fixturePath" }
    }
}
$planetariumFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-planetarium.mt2f' })
foreach ($fixturePath in $planetariumFixtures) {
    foreach ($mode in @('--planetarium-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Planetarium checks failed ($mode): $fixturePath" }
    }
}
$memorialFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-memorial.mt2f' })
foreach ($fixturePath in $memorialFixtures) {
    foreach ($mode in @('--memorial-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Memorial checks failed ($mode): $fixturePath" }
    }
}
$silenceRoomFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-zone-of-silence.mt2f' })
foreach ($fixturePath in $silenceRoomFixtures) {
    foreach ($mode in @('--zone-of-silence-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Zone of Silence checks failed ($mode): $fixturePath" }
    }
}
$quartersFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-stewards-quarters.mt2f' })
foreach ($fixturePath in $quartersFixtures) {
    foreach ($mode in @('--stewards-quarters-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Stewards Quarters checks failed ($mode): $fixturePath" }
    }
}
$engineUpgradeFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-engine-upgrade.mt2f' })
foreach ($fixturePath in $engineUpgradeFixtures) {
    foreach ($mode in @('--engine-upgrade-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Engine Upgrade checks failed ($mode): $fixturePath" }
    }
}
$goldmanceFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-goldmance.mt2f' })
foreach ($fixturePath in $goldmanceFixtures) {
    foreach ($mode in @('--goldmance-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Goldmance checks failed ($mode): $fixturePath" }
    }
}
$originalDeckSpellFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-deck-spell.mt2f' })
foreach ($fixturePath in $originalDeckSpellFixtures) {
    foreach ($mode in @('--original-deck-spell-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original deck-spell checks failed ($mode): $fixturePath" }
    }
}
$originalStatusCardsFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-status-cards.mt2f' })
foreach ($fixturePath in $originalStatusCardsFixtures) {
    foreach ($mode in @('--original-status-cards-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original status-card checks failed ($mode): $fixturePath" }
    }
}
$originalForgeCardsFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-forge-cards.mt2f' })
foreach ($fixturePath in $originalForgeCardsFixtures) {
    foreach ($mode in @('--original-forge-cards-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original forge-card checks failed ($mode): $fixturePath" }
    }
}
$originalRoomCardsFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-original-room-cards.mt2f' })
foreach ($fixturePath in $originalRoomCardsFixtures) {
    foreach ($mode in @('--original-room-cards-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original room-card checks failed ($mode): $fixturePath" }
    }
}
$eclipseFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -in @('full-battle-eclipse-chamber.mt2f', 'full-battle-eclipse-chamber-callbacks.mt2f', 'full-battle-eclipse-chamber-shade-callbacks.mt2f', 'full-battle-eclipse-chamber-movement.mt2f') })
foreach ($fixturePath in $eclipseFixtures) {
    $eclipseMode = if ((Split-Path -Leaf $fixturePath) -like '*movement.mt2f') { '--eclipse-movement-only' }
        elseif ((Split-Path -Leaf $fixturePath) -like '*callbacks.mt2f') { '--eclipse-callbacks-only' } else { '--eclipse-chamber-only' }
    foreach ($mode in @($eclipseMode, '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Eclipse Chamber checks failed ($mode): $fixturePath" }
    }
}
$transcendImpFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-transcend-imp.mt2f' })
foreach ($fixturePath in $transcendImpFixtures) {
    foreach ($mode in @('--transcend-imp-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Transcend Imp checks failed ($mode): $fixturePath" }
    }
}
$magneticRelayFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-magnetic-relay.mt2f' })
foreach ($fixturePath in $magneticRelayFixtures) {
    foreach ($mode in @('--magnetic-relay-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Magnetic Relay checks failed ($mode): $fixturePath" }
    }
}
$equipmentMergeFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-fused-weaponry.mt2f' })
foreach ($fixturePath in $equipmentMergeFixtures) {
    foreach ($mode in @('--fused-weaponry-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Fused Weaponry checks failed ($mode): $fixturePath" }
    }
}
$smallBoxFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-small-box.mt2f' })
foreach ($fixturePath in $smallBoxFixtures) {
    foreach ($mode in @('--small-box-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Small Box checks failed ($mode): $fixturePath" }
    }
}
$mixalotFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-mixalot.mt2f' })
foreach ($fixturePath in $mixalotFixtures) {
    foreach ($mode in @('--mixalot-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Mixalot checks failed ($mode): $fixturePath" }
    }
}
$adaptiveFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-adaptive-mutation.mt2f' })
foreach ($fixturePath in $adaptiveFixtures) {
    foreach ($mode in @('--adaptive-mutation-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Adaptive Mutation checks failed ($mode): $fixturePath" }
    }
}
$duplicateFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -in @('full-battle-mass-production.mt2f', 'full-battle-stardust-invocation.mt2f') })
foreach ($fixturePath in $duplicateFixtures) {
    foreach ($mode in @('--card-duplicate-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Hand duplication checks failed ($mode): $fixturePath" }
    }
}
$preserveFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-preserve.mt2f' })
foreach ($fixturePath in $preserveFixtures) {
    foreach ($mode in @('--preserve-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Preserve checks failed ($mode): $fixturePath" }
    }
}
$armamentFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-armament-drive.mt2f' })
foreach ($fixturePath in $armamentFixtures) {
    foreach ($mode in @('--armament-drive-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Armament Drive checks failed ($mode): $fixturePath" }
    }
}
$runGenerationFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-run-card-generation.mt2f' })
foreach ($fixturePath in $runGenerationFixtures) {
    foreach ($mode in @('--run-card-generation-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Run-card generation checks failed ($mode): $fixturePath" }
    }
}
$restorationFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-restoration-detonation.mt2f' })
foreach ($fixturePath in $restorationFixtures) {
    foreach ($mode in @('--restoration-detonation-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Restoration Detonation checks failed ($mode): $fixturePath" }
    }
}
$originalDecayDeathFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-decay-death.mt2f'
})
foreach ($fixturePath in $originalDecayDeathFixtures) {
    foreach ($mode in @('--original-decay-death-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Decay-death checks failed ($mode): $fixturePath" }
    }
}
$originalBoneDogFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-bone-dog.mt2f'
})
foreach ($fixturePath in $originalBoneDogFixtures) {
    foreach ($mode in @('--original-bone-dog-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Bone Dog checks failed ($mode): $fixturePath" }
    }
}
$originalGrosMichelFixtures = @($fixtures | Where-Object {
    (Split-Path -Leaf $_) -eq 'full-battle-original-gros-michel.mt2f'
})
foreach ($fixturePath in $originalGrosMichelFixtures) {
    foreach ($mode in @('--original-gros-michel-only', '--battle-search-only')) {
        dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- $mode $fixturePath
        if ($LASTEXITCODE -ne 0) { throw "Original Gros Michel checks failed ($mode): $fixturePath" }
    }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'player-untouchable-troop-aura-counterexample.mt2f' })) {
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --untouchable-troop-aura-native $fixturePath (Get-TestFixturePath 'player-untouchable-troop-aura-observation.mt2f')
    if ($LASTEXITCODE -ne 0) { throw 'Untouchable troop-aura guard check failed.' }
}
$plannerFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -like 'full-battle-planner-*.mt2f' -or (Split-Path -Leaf $_) -like 'full-battle-advisor-*.mt2f' })
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'player-live-arkion-pyre-observed-prefix.mt2f' })) {
    dotnet run --project (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-build -- --live-arkion-pyre-observed-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Observed Arkion Pyre native inputs failed: $fixturePath" }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'player-live-arkion-pyre-unavailable.mt2f' })) {
    $nativePath = Get-TestFixturePath 'full-battle-player-live-arkion-pyre.mt2f'
    dotnet run --project (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-build -- --live-arkion-pyre-only $fixturePath $nativePath
    if ($LASTEXITCODE -ne 0) { throw "Live Arkion Pyre advice checks failed: $fixturePath" }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-player-live-arkion-pyre.mt2f' })) {
    $birthPath = Get-TestFixturePath 'outer-boss-birth-live-arkion-pyre.mt2f'
    dotnet run --project (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-build -- --live-arkion-pyre-native-only $fixturePath $birthPath
    if ($LASTEXITCODE -ne 0) { throw "Native Arkion Pyre advice checks failed: $fixturePath" }
}
foreach ($fixturePath in @($fixtures | Where-Object { (Split-Path -Leaf $_) -eq 'full-battle-player-early-stop-search.mt2f' })) {
    dotnet run --project (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-build -- --early-stop-native-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Native early stopping checks failed: $fixturePath" }
}
foreach ($fixturePath in $plannerFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --planner-replay-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Native planner checks failed: $fixturePath" }
}
$advisorFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -like 'full-battle-advisor-*-search.mt2f' })
foreach ($fixturePath in $advisorFixtures) {
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --native-advice-only $fixturePath
    if ($LASTEXITCODE -ne 0) { throw "Native live advisor checks failed: $fixturePath" }
}
$progressedFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -like 'full-battle-progressed-*-search.mt2f' })
foreach ($fixturePath in $progressedFixtures) {
    $replayPath = $fixturePath -replace '-search\.mt2f$', '-replay.mt2f'
    $policyPath = $fixturePath -replace '-search\.mt2f$', '-policy.mt2f'
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --rolling-advice-victory-pair-only $fixturePath $replayPath $policyPath
    if ($LASTEXITCODE -ne 0) { throw "Progressed native victory checks failed: $fixturePath" }
}
$rollingFixtures = @($fixtures | Where-Object { (Split-Path -Leaf $_) -like 'full-battle-rolling-advisor-*-search.mt2f' })
foreach ($fixturePath in $rollingFixtures) {
    $replayPath = $fixturePath -replace '-search\.mt2f$', '-replay.mt2f'
    $policyPath = $fixturePath -replace '-search\.mt2f$', '-policy.mt2f'
    dotnet run --project (Join-Path $workspace 'src\ModelChecks\ModelChecks.csproj') -c Release --no-build -- --rolling-advice-pair-only $fixturePath $replayPath $policyPath
    if ($LASTEXITCODE -ne 0) { throw "Native rolling advisor checks failed: $fixturePath" }
}
