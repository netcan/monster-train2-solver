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
