#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild)

$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
Push-Location $workspace
try {
    if (-not $SkipBuild) {
        foreach ($project in @('ModelChecks', 'FixtureTools')) {
            dotnet build "src/$project/$project.csproj" -c Release --no-restore -p:WarningLevel=0 -v:q
            if ($LASTEXITCODE -ne 0) { throw "$project build failed." }
        }
    }
    $checks = 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
    $tools = 'src/FixtureTools/bin/Release/net8.0/FixtureTools.dll'
    $original = 'tests/fixtures/campaign/progression/full-battle-progressed-ring7-holdover-win.mt2f'
    $shortened = 'tests/fixtures/campaign/progression/full-battle-progressed-ring7-holdover-exact-replay.mt2f'
    dotnet $checks --holdover-variants-only tests/fixtures/cards/traits/holdover-variants-calibration.mt2f
    if ($LASTEXITCODE -ne 0) { throw 'Native upgrade/clone variant matrix failed.' }
    dotnet $checks --permanent-traits-only tests/fixtures/cards/traits/permanent-trait-calibration.mt2f
    if ($LASTEXITCODE -ne 0) { throw 'Native permanent trait matrix failed.' }
    foreach ($mode in @('--holdover-only', '--starting-duality-only', '--rolling-advice-only')) {
        dotnet $checks $mode $shortened
        if ($LASTEXITCODE -ne 0) { throw "Shortened native victory failed: $mode" }
    }
    foreach ($mode in @('--exact-battle-only', '--belief-battle-only')) {
        dotnet $checks $mode $original
        if ($LASTEXITCODE -ne 0) { throw "Search certificate acceptance failed: $mode" }
    }
    dotnet $checks --exact-battle-multi-turn-only $workspace
    if ($LASTEXITCODE -ne 0) { throw 'Current multi-turn search acceptance failed.' }
    dotnet $checks --atomic-advice-budget-only tests/fixtures/planning/search/advisor-atomic-budget-failure.mt2f
    if ($LASTEXITCODE -ne 0) { throw 'Advice budget acceptance failed.' }
    $current = 'tests/compatibility/game-2.3.1/mechanics'
    $offeringToken = "$current/controlled-isolated-spells"
    foreach ($offeringArchive in @('offering-token.mt2f', 'offering-token-discard-drawn.mt2f')) {
        dotnet $checks --offering-token-frontier-only "$offeringToken/$offeringArchive"
        if ($LASTEXITCODE -ne 0) { throw "Original Offering Token chooser frontier failed: $offeringArchive" }
    }
    dotnet $checks --original-offering-token-draw-discard-only "$offeringToken/offering-token-discard-drawn.mt2f"
    if ($LASTEXITCODE -ne 0) { throw 'Original Offering Token newly drawn discard failed.' }
    dotnet $checks --stardust-heal-draw-frontier-only "$current/stardust-heal-draw/native-stardust-heal-draw.mt2f"
    if ($LASTEXITCODE -ne 0) { throw 'Original Stardust heal-and-draw duplication frontier failed.' }
    $hallowed = "$current/controlled-original-support-units"
    dotnet $checks --hallowed-halls-draft-relic-only "$hallowed/hallowed-halls-allied-draft-relic.mt2f"
    if ($LASTEXITCODE -ne 0) { throw 'Original Hallowed Halls with AlliedClassRelic failed.' }
    dotnet $checks --hallowed-halls-draft-relic-only "$hallowed/hallowed-halls.mt2f" "$current/configured-opening-relics/native-draft-and-deck-owners.mt2f"
    if ($LASTEXITCODE -ne 0) { throw 'Original Hallowed Halls relic callback guard controls failed.' }
    $echoVaultSilver = "$current/controlled-isolated-spells/native-echo-vault-silver.mt2f"
    dotnet $checks --original-support-spells-only $echoVaultSilver
    if ($LASTEXITCODE -ne 0) { throw 'Original Echo Vault with Silver Increased Ember failed.' }
    dotnet $checks --battle-frontier-only $echoVaultSilver
    if ($LASTEXITCODE -ne 0) { throw 'Original Echo Vault with Silver Increased Ember candidate frontier failed.' }
    $railSpike = "$current/controlled-original-resource-spells/awoken-rail-spike-spell-slot-relic.mt2f"
    dotnet $checks --original-discard-draw-only $railSpike
    if ($LASTEXITCODE -ne 0) { throw 'Original Rail Spike with ExtraSpellUpgrades failed.' }
    dotnet $checks --battle-frontier-only $railSpike
    if ($LASTEXITCODE -ne 0) { throw 'Original Rail Spike with ExtraSpellUpgrades candidate frontier failed.' }
    $bossSpellSlot = "$current/true-final-boss-spell-slot"
    dotnet $checks --true-final-boss-spell-slot-native-only "$bossSpellSlot/relentless.mt2f" "$bossSpellSlot/early-entropy.mt2f"
    if ($LASTEXITCODE -ne 0) { throw 'Original True Final Boss with ExtraSpellUpgrades failed.' }
    dotnet $checks --true-final-boss-spell-slot-pair-only "$current/player-advisor-rolling/current-ring9-original-true-final-boss.mt2f" `
        "$current/configured-opening-relics/native-card-play-damage-roster-isolated-rng.mt2f"
    if ($LASTEXITCODE -ne 0) { throw 'True Final Boss spell-slot relic guard controls failed.' }
    $campaignOnly = "$current/campaign-only-combat-relics"
    foreach ($case in @('hallowed-halls', 'awoken-rail-spike', 'hallowed-halls-and-rail-spike')) {
        dotnet $checks --hallowed-halls-draft-relic-only "$campaignOnly/$case.mt2f" `
            "$current/configured-opening-relics/native-draft-and-deck-owners.mt2f"
        if ($LASTEXITCODE -ne 0) { throw "Original combat with both campaign-only relics failed for $case." }
    }
    dotnet $checks --original-support-spells-only "$campaignOnly/echo-vault.mt2f"
    if ($LASTEXITCODE -ne 0) { throw 'Original Echo Vault with both campaign-only relics failed.' }
    dotnet $checks --native-all-unit-subtypes-only "$current/all-unit-subtypes/queries.mt2f"
    if ($LASTEXITCODE -ne 0) { throw 'Original AllUnitsAllSubtypes query receipts failed.' }
    dotnet $checks --native-surgical-strike-subtypes-only "$current/all-unit-subtypes/surgical-strike.mt2f"
    if ($LASTEXITCODE -ne 0) { throw 'Original Surgical Strike subtype suppression failed.' }
    dotnet $checks --native-all-unit-subtypes-battle-only "$current/all-unit-subtypes/native-battle.mt2f"
    if ($LASTEXITCODE -ne 0) { throw 'Original AllUnitsAllSubtypes battle failed.' }
    dotnet $checks --native-all-unit-subtypes-battle-only "$current/all-unit-subtypes/native-surgical-strike-battle.mt2f"
    if ($LASTEXITCODE -ne 0) { throw 'Original AllUnitsAllSubtypes Surgical Strike battle failed.' }
    dotnet $checks --native-all-unit-subtypes-battle-only "$current/all-unit-subtypes/native-hallowed-halls-battle.mt2f"
    if ($LASTEXITCODE -ne 0) { throw 'Original AllUnitsAllSubtypes Hallowed Halls battle failed.' }
    foreach ($shardtailCase in @('native-rank-one', 'native-rank-one-all-subtypes', 'native-higher-ranks')) {
        $shardtailSacrifice = "$current/shardtail-sacrifice/$shardtailCase.mt2f"
        dotnet $checks --damage-by-units-killed-only $shardtailSacrifice
        if ($LASTEXITCODE -ne 0) { throw "Original Shardtail damage checks failed for $shardtailCase." }
        dotnet $checks --solver-stage-case-only $shardtailSacrifice
        if ($LASTEXITCODE -ne 0) { throw "Original Shardtail battle failed for $shardtailCase." }
    }
    $loseMoneyOnHit = "$current/lose-money-on-hit/native-battle.mt2f"
    dotnet $checks --native-lose-money-on-hit-battle-only $loseMoneyOnHit
    if ($LASTEXITCODE -ne 0) { throw 'Original LoseMoneyOnHit battle failed.' }
    foreach ($tier in @('tier2', 'tier3')) {
        $reanimation = "$current/front-reanimation/native-$tier"
        dotnet $checks --native-front-reanimation-only "${reanimation}-settled.mt2f" --whole-turns
        if ($LASTEXITCODE -ne 0) { throw "Original front reanimation opening failed for $tier." }
        dotnet $checks --solver-stage-case-only "${reanimation}-battle.mt2f"
        if ($LASTEXITCODE -ne 0) { throw "Original front reanimation battle failed for $tier." }
    }
    $skullCollectors = "$current/skull-collectors"
    dotnet $checks --native-skull-collectors-only "$skullCollectors/native-first-treasure.mt2f"
    if ($LASTEXITCODE -ne 0) { throw 'Original SkullCollectors opening failed.' }
    dotnet $checks --solver-stage-case-only "$skullCollectors/native-tier1-battle.mt2f"
    if ($LASTEXITCODE -ne 0) { throw 'Original SkullCollectors tier 1 battle failed.' }
    foreach ($tier in @('tier2', 'tier3')) {
        dotnet $checks --native-skull-collectors-only "$skullCollectors/native-$tier-projected.mt2f"
        if ($LASTEXITCODE -ne 0) { throw "Original SkullCollectors opening failed for $tier." }
        dotnet $checks --recorded-chain-diagnose "$skullCollectors/native-$tier-battle.mt2f" --frontiers
        if ($LASTEXITCODE -ne 0) { throw "Original SkullCollectors battle failed for $tier." }
    }
    foreach ($rank in @('i', 'ii', 'iii')) {
        dotnet $checks --native-lazarus-devour-only "$current/lazarus-devour/native-devour-$rank.mt2f"
        if ($LASTEXITCODE -ne 0) { throw "Original Lazarus Devour ability failed at rank $rank." }
    }
    dotnet $checks --native-glutton-onfeed-only "$current/glutton-onfeed/native-glutton-onfeed.mt2f"
    if ($LASTEXITCODE -ne 0) { throw 'Original MonsterGlutton OnFeed failed.' }
    dotnet $checks --native-blighted-totem-only "$current/blighted-totem/native-blighted-totem.mt2f"
    if ($LASTEXITCODE -ne 0) { throw 'Original Blighted Totem CardSpellPlayed failed.' }
    dotnet $checks --parallel-shorter-fallback-only "$current/player-advisor-rolling/unlocked-covenant0-ring5-healing-route-defeat.mt2f"
    if ($LASTEXITCODE -ne 0) { throw 'Parallel whole-battle cutoff acceptance failed.' }
    dotnet $checks --relentless-battle-pair-only "$current/relentless-battle/abandon.mt2f" "$current/relentless-battle/continue.mt2f"
    if ($LASTEXITCODE -ne 0) { throw 'Current combat dialog replay failed.' }
    dotnet $checks --planner-pair-only "$current/relentless-advisor/mixed-policy.mt2f" "$current/relentless-advisor/mixed-policy-replay.mt2f"
    if ($LASTEXITCODE -ne 0) { throw 'Current advisor dialog plan failed to replay.' }
    dotnet $checks --advisor-future-target-only "$current/player-advisor-rolling/current-ring9-advisor-deadline.mt2f"
    if ($LASTEXITCODE -ne 0) { throw 'Verified future-target advice failed.' }
    dotnet $checks --native-advisor-restart-only "$current/player-advisor-rolling/native-restart-restored-deployment.mt2f"
    if ($LASTEXITCODE -ne 0) { throw 'Native restart advice acceptance failed.' }
    dotnet $checks --native-advisor-undo-only "$current/player-advisor-rolling/native-undo-restored-first-turn.mt2f"
    if ($LASTEXITCODE -ne 0) { throw 'Native undo advice acceptance failed.' }
    dotnet $checks --native-advisor-end-turn-only "$current/player-advisor-rolling/native-end-turn-confirmation.mt2f"
    if ($LASTEXITCODE -ne 0) { throw 'Native EndTurn confirmation advice acceptance failed.' }
    dotnet $checks --native-advisor-rejected-deployment-only "$current/player-advisor-rolling/native-rejected-deployment.mt2f"
    if ($LASTEXITCODE -ne 0) { throw 'Native rejected deployment advice acceptance failed.' }
    dotnet $checks --player-advisor-presentation-only "$current/player-advisor-rolling/current-ring1-presentation-after-allocation.mt2f"
    if ($LASTEXITCODE -ne 0) { throw 'Native advisor presentation acceptance failed.' }
    $endless = "$current/endless-enemy-scaling"
    dotnet $checks --original-endless-ring2-second-wave-only "$endless/native-controlled-ring2-second-wave.mt2f"
    if ($LASTEXITCODE -ne 0) { throw 'Native Endless Ring 2 second wave failed.' }
    dotnet $checks --recorded-chain-diagnose "$endless/native-controlled-ring2-defeat.mt2f" --frontiers
    if ($LASTEXITCODE -ne 0) { throw 'Native Endless Ring 2 no-card battle failed.' }
    dotnet $checks --original-endless-ring2-late-boss-only "$endless/native-controlled-ring2-late-boss-settled.mt2f" --full-world
    if ($LASTEXITCODE -ne 0) { throw 'Native Endless Ring 2 late boss and its first Armor turn failed.' }
    dotnet $checks --original-endless-loop2-advisor-first-end-turn-only "$endless/native-controlled-loop2-advisor-first-end-turn.mt2f"
    if ($LASTEXITCODE -ne 0) { throw 'Native Endless Loop 2 advice handoff through the first EndTurn failed.' }
    dotnet $checks --original-lady-ephemeral-attack-only "$current/player-advisor-rolling/native-moon-remnant-sixth-battle-defeat.mt2f"
    if ($LASTEXITCODE -ne 0) { throw 'Original Lady of the House Burnout attack scaling failed.' }
    $persistentHealth = "$current/persistent-monster-health/campaign"
    dotnet $checks --native-persistent-monster-hp-campaign-only "$persistentHealth/campaign-battle-replay.mt2f" `
        "$persistentHealth/persistent-monster-hp-campaign-checkpoint.mt2f" "$persistentHealth/persistent-monster-hp-campaign-reload.mt2f"
    if ($LASTEXITCODE -ne 0) { throw 'Native campaign persistent health acceptance failed.' }
    $devoteArchive = "$current/devote-battle/player-advisor/player-advisor-controlled-helper.mt2f"
    pwsh -NoProfile -File (Join-Path $PSScriptRoot 'Check-AdvisorDevoteArchive.ps1') -Archive $devoteArchive
    if ($LASTEXITCODE -ne 0) { throw 'Native Devote player advice acceptance failed.' }
    dotnet $checks --devote-battle-only $devoteArchive
    if ($LASTEXITCODE -ne 0) { throw 'Controlled native Devote battle replay failed.' }
    dotnet $checks --player-advisor-mix-prompt-only "$current/player-advisor-mix/native-prompt.mt2f"
    if ($LASTEXITCODE -ne 0) { throw 'Original hand-card Mix prompt acceptance failed.' }
    dotnet $checks --advisor-unit-mix-modal-only "$current/controlled-original-support-units/opening-ability-activations.mt2f"
    if ($LASTEXITCODE -ne 0) { throw 'Original unit ability Mix prompt matching failed.' }
    dotnet $checks --player-advisor-unit-mix-prompt-only "$current/player-advisor-mix/native-unit-prompt.mt2f"
    if ($LASTEXITCODE -ne 0) { throw 'Original unit ability Mix prompt acceptance failed.' }
    pwsh -NoProfile -File (Join-Path $PSScriptRoot 'Check-Game231BeforeDrawOrder.ps1') -SkipBuild -PlayerAdvisor
    if ($LASTEXITCODE -ne 0) { throw 'Ordered before-draw player advice acceptance failed.' }
    dotnet $checks --transcend-imp-only "$current/controlled-original-support-units/transcend-imp-original-fledgling-rage.mt2f"
    if ($LASTEXITCODE -ne 0) { throw 'Original Fledgling Imp Rage history replay failed.' }
    dotnet $checks --transcend-imp-only "$current/controlled-original-support-units/transcend-imp-original-welder-front-armor.mt2f"
    if ($LASTEXITCODE -ne 0) { throw 'Original Welder Helper Armor history replay failed.' }
    dotnet $checks --transcend-imp-only "$current/controlled-original-support-units/transcend-imp-original-enemy-room-decay-sap.mt2f"
    if ($LASTEXITCODE -ne 0) { throw 'Original enemy room Decay and Sap history replay failed.' }
    dotnet $checks --transcend-imp-only "$current/controlled-original-support-units/transcend-imp-original-front-room-damage.mt2f"
    if ($LASTEXITCODE -ne 0) { throw 'Original front and room Damage history replay failed.' }
    $output = Join-Path '.probe-runs' ('solver-completion-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
    $null = New-Item -ItemType Directory -Path $output
    $nodeCutoff = Join-Path $output 'node-cutoff.mt2f'
    dotnet $tools solve-exact $original $nodeCutoff 1 30000 1
    if ($LASTEXITCODE -ne 0) { throw 'Exact-search CLI export failed.' }
    dotnet $tools verify-exact $nodeCutoff
    if ($LASTEXITCODE -ne 0) { throw 'Fresh-process exact-search cutoff replay failed.' }
    $relentless = "$current/relentless-battle/abandon.mt2f"
    $exactRelentless = Join-Path $output 'relentless-choices.mt2f'
    dotnet $tools solve-exact $relentless $exactRelentless 200 12000 2
    if ($LASTEXITCODE -ne 0) { throw 'Exact-search CLI failed to replay its Relentless choices.' }
    dotnet $tools verify-exact $exactRelentless
    if ($LASTEXITCODE -ne 0) { throw 'Fresh-process exact-search Relentless replay failed.' }
    Add-Type -Path (Join-Path (Split-Path $tools) 'FixtureArchive.dll')
    $exactDocument = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read((Resolve-Path -LiteralPath $exactRelentless).Path)
    try {
        $exactResult = $exactDocument.RootElement.GetProperty('Result')
        $choiceSteps = @($exactResult.GetProperty('Steps').EnumerateArray() | Where-Object {
            $choices = [MonsterTrain2Poju.Fixtures.FixtureValue]::new()
            $_.GetProperty('IsEndTurn').GetBoolean() -and $_.TryGetProperty('RelentlessDecisions', [ref]$choices) -and
                $choices.ValueKind -eq 'Array' -and $choices.GetArrayLength() -gt 0
        })
        if (-not $exactDocument.RootElement.GetProperty('Verified').GetBoolean() -or
            -not $exactResult.GetProperty('CertifiedHorizonOptimal').GetBoolean() -or $choiceSteps.Count -eq 0) {
            throw 'The verified exact-search export must contain an EndTurn with Relentless choices.'
        }
    } finally { $exactDocument.Dispose() }
    $policy = Join-Path $output 'belief.mt2f'
    dotnet $tools plan-belief $original $policy 8 2 128 30000 7
    if ($LASTEXITCODE -ne 0) { throw 'Probability-policy CLI export failed.' }
    dotnet $tools verify-belief $policy
    if ($LASTEXITCODE -ne 0) { throw 'Fresh-process policy verification failed.' }

    # Byte/hash/registration checks inspect metadata, not simulated regressions.
    & (Join-Path $PSScriptRoot 'Check-FixtureCatalog.ps1')
    $manifest = @(Import-Csv tests/fixtures/manifest.tsv -Delimiter "`t")
    pwsh -NoProfile -File scripts/Get-CardCoverageInventory.ps1 -Check -SkipBuild
    if ($LASTEXITCODE -ne 0) { throw 'Coverage inventory is stale.' }
    & (Join-Path $PSScriptRoot 'Get-CardCoverageInventory.ps1') -Current -SkipBuild
    & (Join-Path $PSScriptRoot 'Get-RelicCoverageInventory.ps1') -Current -SkipBuild
    Write-Output "SOLVER-COMPLETION PASS: focused native/model/CLI checks, $($manifest.Count) fixture hashes/registrations, current card and relic evidence. Predictions: $output"
} finally { Pop-Location }
