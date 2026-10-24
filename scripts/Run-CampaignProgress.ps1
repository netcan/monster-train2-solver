#requires -Version 7.4
[CmdletBinding()]
param(
    [ValidateRange(1, 9)] [int] $StopBeforeRing = 5,
    [int] $Seed = 424242,
    [string] $PrimaryClan = '',
    [string] $SecondaryClan = '',
    [ValidateRange(0, 10)] [int] $StartingCovenantLevel = 0,
    [string] $InputProfile = '',
    [string] $BattleReplay = '',
    [string[]] $ExcludeUpgradeTrait = @(),
    [switch] $SkipMerchantPurchases,
    [ValidateSet('First', 'Last')] [string] $StoryChoiceOrder = 'First',
    [ValidateRange(0, 2)] [int] $DeploymentRoom = 2,
    [switch] $OuterBossTurns,
    [switch] $OuterBossBirth,
    [switch] $StoryProgressionOnly,
    [ValidateRange(1, 2)] [int] $NativeWorker = 1,
    [ValidateRange(60, 7200)] [int] $TimeoutSeconds = 1800,
    [switch] $SkipBuild
)
$ErrorActionPreference = 'Stop'
if (($PrimaryClan -or $SecondaryClan -or $PSBoundParameters.ContainsKey('StartingCovenantLevel')) -and $InputProfile) {
    throw 'Clan and Covenant selection require a fresh native campaign.'
}
if ($PrimaryClan -and $SecondaryClan -and $PrimaryClan -ieq $SecondaryClan) { throw 'Starting clans must differ.' }
if ($OuterBossBirth -and -not $OuterBossTurns) { throw '-OuterBossBirth requires -OuterBossTurns.' }
if ($StoryProgressionOnly -and (-not $InputProfile -or $BattleReplay)) {
    throw '-StoryProgressionOnly requires an InputProfile and cannot execute BattleReplay.'
}
$workspace = Split-Path $PSScriptRoot -Parent
$profilesRoot = [IO.Path]::GetFullPath((Join-Path $workspace '.probe-runs')).TrimEnd('\')
$gameRoot = Join-Path $workspace $(if ($NativeWorker -eq 1) { '.sandbox-game' } else { '.sandbox-game-worker2' })
$executable = Join-Path $gameRoot 'MonsterTrain2.exe'
if (Get-CimInstance Win32_Process -Filter "Name='MonsterTrain2.exe'" | Where-Object ExecutablePath -EQ $executable) {
    throw "Worker $NativeWorker is already running."
}
$source = Join-Path $workspace '.probe-baseline'
if ($InputProfile) {
    $source = (Resolve-Path -LiteralPath $InputProfile).Path.TrimEnd('\')
    if ((Split-Path $source -Parent) -ine $profilesRoot) { throw 'InputProfile must be a direct child of .probe-runs.' }
    if (-not (Test-Path -LiteralPath (Join-Path $source 'saves/save-singlePlayer.json'))) { throw 'InputProfile has no native save.' }
}
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/Probe/Probe.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE -ne 0) { throw 'Campaign driver build failed.' }
}
$playerReplay = $false
$playerSettings = $null
if ($BattleReplay) {
    if (-not $InputProfile) { throw 'BattleReplay requires a native InputProfile.' }
    $BattleReplay = (Resolve-Path -LiteralPath $BattleReplay).Path
    $inputSave = Get-Content -LiteralPath (Join-Path $source 'saves/save-singlePlayer.json') -Raw | ConvertFrom-Json
    if ($inputSave.gameSequence -ne 6 -or $inputSave.wasDevCheatUsed -or $StopBeforeRing -ne ([int]$inputSave.runState.distance + 2)) {
        throw 'BattleReplay must continue an unmodified BattleIntro checkpoint to the immediately following ring.'
    }
    if (($OuterBossTurns -or $OuterBossBirth) -and [int]$inputSave.runState.distance -ne 7) {
        throw 'Outer-boss replay probes require the Ring8 battle; omit them when replaying an earlier ring.'
    }
    Add-Type -Path (Join-Path $workspace 'src/FixtureArchive/bin/Release/net8.0/FixtureArchive.dll')
    $sourceDocument = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read($BattleReplay)
    try {
        $sourceRoot = $sourceDocument.RootElement
        $kindValue = [MonsterTrain2Poju.Fixtures.FixtureValue]::new()
        if ($sourceRoot.TryGetProperty('EvidenceKind', [ref]$kindValue) -and
            $kindValue.GetString() -in @('ModelPrediction', 'NativeDeploymentObservation')) {
            throw 'Campaign continuation needs a settled native victory, not a model prediction or deployment observation.'
        }
        if (-not $sourceRoot.GetProperty('NativeWon').GetBoolean() -or -not $sourceRoot.GetProperty('TerminalEffectsSettled').GetBoolean() -or
            @('CaptureFailures', 'Mismatches', 'Unsupported', 'Pending').Where({ $sourceRoot.GetProperty($_).GetInt32() -ne 0 }).Count -gt 0) {
            throw 'Campaign continuation needs a settled native victory with all four failure counters zero.'
        }
        $playerValue = [MonsterTrain2Poju.Fixtures.FixtureValue]::new()
        if ($sourceDocument.RootElement.TryGetProperty('PlayerAdvisor', [ref]$playerValue) -and
            $playerValue.ValueKind -ne [MonsterTrain2Poju.Fixtures.FixtureKind]::Null) {
        if ($playerValue.GetProperty('Assembly').GetString() -cne 'MonsterTrain2Poju.Advisor') {
                throw 'Unknown player assembly in the frozen battle.'
            }
            $playerReplay = $true
            $playerSettings = $playerValue.GetProperty('Overlay').ToObjectGraph()
            # Older accepted traces predate these additive settings. Preserve
            # the original fixed-horizon/single-worker replay in that case.
            if (-not $playerSettings.ContainsKey('WholeBattle')) { $playerSettings['WholeBattle'] = $false }
            if (-not $playerSettings.ContainsKey('MaxSteps')) { $playerSettings['MaxSteps'] = 256 }
            if (-not $playerSettings.ContainsKey('SearchWorkers')) { $playerSettings['SearchWorkers'] = 1 }
        }
    } finally { $sourceDocument.Dispose() }
}
if ($playerReplay -and -not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/Advisor/Advisor.csproj') -c Release --no-restore -p:WarningLevel=0 -p:BuildProjectReferences=false -v:q
    if ($LASTEXITCODE -ne 0) { throw 'Campaign player assembly build failed.' }
}
$originalRoot = Join-Path ([Environment]::GetFolderPath('UserProfile')) 'AppData/LocalLow/Shiny Shoe/MonsterTrain2'
function Get-OriginalHashes {
    @(Get-ChildItem -LiteralPath $originalRoot -File -Recurse | Where-Object {
        $_.Name -match '^(save-|metagameSave|preferences|prefs)' -or $_.Directory.Name -eq 'saves'
    } | Sort-Object FullName | ForEach-Object { $_.FullName + '=' + (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash })
}
$originalBefore = Get-OriginalHashes
$profilePrefix = if ($StoryProgressionOnly) { 'campaign-story' } else { 'campaign-ring' + $StopBeforeRing }
$profile = Join-Path $profilesRoot ($profilePrefix + '-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
$null = New-Item -ItemType Directory -Path $profile
foreach ($item in Get-ChildItem -LiteralPath $source) {
    if (-not $InputProfile) { Copy-Item -LiteralPath $item.FullName -Destination $profile -Recurse; continue }
    if ($item.Name -in @('saves', 'metagameSave.json', 'preferences.json', 'preferences-local.json', 'prefs.json') -or
        ($item.PSIsContainer -and $item.Name -in @('metagame', 'preferences'))) {
        Copy-Item -LiteralPath $item.FullName -Destination $profile -Recurse
    }
}
# Preserve a chain of native inputs without copying large battle traces or logs.
if (Test-Path -LiteralPath (Join-Path $source 'campaign-audit.jsonl')) {
    Copy-Item -LiteralPath (Join-Path $source 'campaign-audit.jsonl') -Destination (Join-Path $profile 'campaign-parent-audit.jsonl')
}
$savePath = Join-Path $profile 'saves/save-singlePlayer.json'
[pscustomobject]@{ InputProfile = $source; InputSaveSHA256 = (Get-FileHash -LiteralPath $savePath -Algorithm SHA256).Hash;
    StopBeforeRing = $StopBeforeRing; Seed = $Seed; NativeWorker = $NativeWorker; ExcludeUpgradeTrait = $ExcludeUpgradeTrait;
    PrimaryClan = $PrimaryClan; SecondaryClan = $SecondaryClan; StartingCovenantLevel = $StartingCovenantLevel;
    SkipMerchantPurchases = [bool]$SkipMerchantPurchases;
    StoryChoiceOrder = $StoryChoiceOrder;
    DeploymentRoom = $DeploymentRoom;
    OuterBossTurns = [bool]$OuterBossTurns; OuterBossBirth = [bool]$OuterBossBirth;
    StoryProgressionOnly = [bool]$StoryProgressionOnly;
    BattleReplay = $BattleReplay; PlayerReplay = $playerReplay;
    BattleReplaySHA256 = $(if ($BattleReplay) { (Get-FileHash -LiteralPath $BattleReplay -Algorithm SHA256).Hash } else { $null }) } |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $profile 'campaign-input.json')
$playerCheckPlugin = $null; $playerConfigPath = $null; $playerConfigBackup = $null
$playerConfigPrepared = $false; $process = $null
try {
foreach ($assembly in @('Probe', 'Model', 'FixtureArchive')) {
    Copy-Item -LiteralPath (Join-Path $workspace "src/Probe/bin/Release/netstandard2.1/$assembly.dll") -Destination (Join-Path $gameRoot 'BepInEx/plugins') -Force
}
if ($playerReplay) {
    $pluginDir = Join-Path $gameRoot 'BepInEx/plugins'
    if (@(Get-ChildItem -LiteralPath $pluginDir -Filter 'MonsterTrain2Poju.Advisor.dll' -File -Recurse).Count -ne 0) {
        throw 'The isolated campaign worker already contains a player advisor.'
    }
    $playerCheckPlugin = Join-Path $pluginDir ('PojuPlayerCheck-' + [Guid]::NewGuid().ToString('N'))
    $null = New-Item -ItemType Directory -Path $playerCheckPlugin
    Copy-Item -LiteralPath (Join-Path $workspace 'src/Advisor/bin/Release/netstandard2.1/MonsterTrain2Poju.Advisor.dll') -Destination $playerCheckPlugin
    $playerConfigPath = Join-Path $gameRoot 'BepInEx/config/netcan.monstertrain2.advisor.cfg'
    $playerConfigBackup = Join-Path $profile 'original-player-advisor.cfg'
    if (Test-Path -LiteralPath $playerConfigPath) { Move-Item -LiteralPath $playerConfigPath -Destination $playerConfigBackup }
    $playerConfigPrepared = $true
    @"
[General]
Enabled = true
PauseWhenHidden = true
[Search]
Horizon = $($playerSettings['Horizon'])
TimeLimitMs = $($playerSettings['TimeLimitMs'])
PreferredFloor = $(1 + [int]$playerSettings['PreferredDeploymentRoom'])
TacticalSeed = $($playerSettings['TacticalSeed'])
WholeBattle = $($playerSettings['WholeBattle'])
MaxSteps = $($playerSettings['MaxSteps'])
SearchWorkers = $($playerSettings['SearchWorkers'])
"@ | Set-Content -LiteralPath $playerConfigPath -Encoding utf8
}
$start = [Diagnostics.ProcessStartInfo]::new($executable)
$start.WorkingDirectory = $gameRoot
$start.UseShellExecute = $false
$start.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
$start.ArgumentList.Add('-logFile')
$start.ArgumentList.Add((Join-Path $profile 'unity-scenario.log'))
foreach ($name in @($start.Environment.Keys)) {
    if ($name.StartsWith('MT2_PROBE_') -or $name.StartsWith('MT2_ADVISOR_')) { $null = $start.Environment.Remove($name) }
}
$settings = @{
    MT2_PROBE_DATA_DIR = $profile; MT2_PROBE_SCENARIO = 'campaign'; MT2_PROBE_SEED = "$Seed"
    MT2_PROBE_PRIMARY_CLAN = $PrimaryClan; MT2_PROBE_SECONDARY_CLAN = $SecondaryClan
    MT2_PROBE_STARTING_COVENANT_LEVEL = "$StartingCovenantLevel"
    MT2_PROBE_CAMPAIGN_STOP_RING = "$StopBeforeRing"; MT2_PROBE_SETTLE_CARD_ANIMATIONS = '1'
    MT2_PROBE_CAMPAIGN_STORY_ONLY = $(if ($StoryProgressionOnly) { '1' } else { '0' })
    MT2_PROBE_CAMPAIGN_EXCLUDED_UPGRADE_TRAITS = ($ExcludeUpgradeTrait -join ';')
    MT2_PROBE_CAMPAIGN_SKIP_MERCHANT_PURCHASES = $(if ($SkipMerchantPurchases) { '1' } else { '0' })
    MT2_PROBE_CAMPAIGN_STORY_CHOICE_ORDER = $StoryChoiceOrder
    MT2_PROBE_CAMPAIGN_DEPLOYMENT_ROOM = [string]$DeploymentRoom
    MT2_PROBE_BRANCH_CARD_MASKS = '1'; MT2_PROBE_PHYSICAL_SPAWNPOINTS = '1'; MT2_PROBE_GAME_SPEED = 'Instant'
    MT2_PROBE_OUTER_BOSS_TURNS = $(if ($OuterBossTurns) { '1' } else { '0' })
    MT2_PROBE_OUTER_BOSS_BIRTH = $(if ($OuterBossBirth) { '1' } else { '0' })
}
if ($InputProfile) { $settings.MT2_PROBE_RESUME_RUN = '1' }
if ($BattleReplay) {
    $settings.MT2_PROBE_SCENARIO = 'native-replay'
    $settings.MT2_PROBE_CAMPAIGN_AFTER_REPLAY = '1'
    $settings.MT2_PROBE_BATTLE_DISTANCE = [string]$inputSave.runState.distance
    $settings.MT2_PROBE_FULL_BATTLE = '1'
    $settings.MT2_PROBE_FULL_BATTLE_POLICY = 'units-spells-and-junk'
    $settings.MT2_PROBE_DIRECT_BRANCH = '1'
    $settings.MT2_PROBE_DEPTH = '100'
    $settings.MT2_PROBE_TARGET_TURN = '0'
    $settings.MT2_PROBE_ROLLING_ADVICE = '1'
    $settings.MT2_PROBE_ROLLING_INPUT = $BattleReplay
    $settings.MT2_PROBE_SETTLE_DEATH_DISSOLVES = '1'
    $settings.MT2_PROBE_BINARY_CAPTURE = '1'
    if ($playerReplay) {
        $settings.MT2_PROBE_PLAYER_ADVISOR = '1'
        # Keep recorded settings consistent with the actual player config copied
        # from the source. Frozen inputs bypass search; they must not relabel a
        # bottom-floor or tactical route with the probe's fallback settings.
        $settings.MT2_PROBE_PLANNER_HORIZON = [string]$playerSettings['Horizon']
        $settings.MT2_PROBE_PLANNER_TIME_LIMIT_MS = [string]$playerSettings['TimeLimitMs']
        $settings.MT2_PROBE_PLANNER_DEPLOYMENT_ROOM = [string]$playerSettings['PreferredDeploymentRoom']
        $settings.MT2_PROBE_PLANNER_TACTICAL_SEED = $(if ($playerSettings['TacticalSeed']) { '1' } else { '0' })
        $settings.MT2_PROBE_PLANNER_WHOLE_BATTLE = $(if ($playerSettings['WholeBattle']) { '1' } else { '0' })
        $settings.MT2_PROBE_PLANNER_MAX_STEPS = [string]$playerSettings['MaxSteps']
        $settings.MT2_PROBE_PLANNER_SEARCH_WORKERS = [string]$playerSettings['SearchWorkers']
    }
}
foreach ($entry in $settings.GetEnumerator()) { $start.Environment[$entry.Key] = $entry.Value }
$process = [Diagnostics.Process]::Start($start)
Write-Host "CAMPAIGN-RUN Profile=$profile ProcessId=$($process.Id)"
$deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
while (-not $process.WaitForExit(1000)) {
    if ([DateTime]::UtcNow -gt $deadline) { $process.Kill(); throw "Campaign timed out: $profile" }
}
$originalAfter = Get-OriginalHashes
$unchanged = (($originalBefore -join "`n") -ceq ($originalAfter -join "`n"))
$auditPath = Join-Path $profile 'campaign-audit.jsonl'
$audit = @(if (Test-Path -LiteralPath $auditPath) { Get-Content -LiteralPath $auditPath | ForEach-Object { $_ | ConvertFrom-Json } })
$replayPassed = -not $BattleReplay
if ($BattleReplay -and (Test-Path -LiteralPath (Join-Path $profile 'full-battle.mt2f'))) {
    Add-Type -Path (Join-Path $workspace 'src/FixtureArchive/bin/Release/net8.0/FixtureArchive.dll')
    $document = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read((Join-Path $profile 'full-battle.mt2f'))
    try { $trace = $document.RootElement.ToObjectGraph() } finally { $document.Dispose() }
    $decisions = @($trace['RollingDecisions'])
    $replayPassed = $trace['NativeWon'] -eq $true -and $trace['TerminalEffectsSettled'] -eq $true -and
        $trace['CaptureFailures'] -eq 0 -and $trace['Mismatches'] -eq 0 -and $trace['Unsupported'] -eq 0 -and $trace['Pending'] -eq 0 -and
        $decisions.Count -gt 0 -and @($decisions | Where-Object { $_['Mode'] -ne 'replay' -or $_['Nodes'] -ne 0 -or
            $_['Difference'] -or $null -eq $_['Actual'] }).Count -eq 0
    if ($playerReplay) {
        $player = $trace['PlayerAdvisor']
        $replayPassed = $replayPassed -and $player['Assembly'] -ceq 'MonsterTrain2Poju.Advisor' -and
            $player['Overlay']['PurityFailures'] -eq 0 -and $player['Overlay']['Failures'] -eq 0 -and
            $player['Overlay']['Published'] -eq 0
    }
}
$passed = $process.ExitCode -eq 0 -and $audit.Count -gt 0 -and $audit[-1].kind -eq 'complete' -and $unchanged -and $replayPassed
$runOutcome = @($audit | Where-Object kind -EQ 'run-outcome')
$summary = [pscustomobject]@{ Profile = $profile; NativePassed = $passed; ExitCode = $process.ExitCode;
    StoryProgressionOnly = [bool]$StoryProgressionOnly;
    BattleReplayPassed = $replayPassed; PlayerReplay = $playerReplay;
    NativeCampaignWon = $runOutcome.Count -eq 1 -and $runOutcome[0].data.won -eq $true;
    NativeVictoryType = $(if ($runOutcome.Count -eq 1) { $runOutcome[0].data.victoryType } else { $null });
    OriginalFilesUnchanged = $unchanged; Events = $audit.Count; BattlesWon = @($audit | Where-Object { $_.kind -eq 'battle-outcome' -and $_.data.won }).Count;
    FinalSaveSHA256 = $(if (Test-Path -LiteralPath $savePath) { (Get-FileHash -LiteralPath $savePath -Algorithm SHA256).Hash } else { $null }) }
$summary | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $profile 'campaign-summary.json')
$summary | ConvertTo-Json -Compress
if (-not $passed) { throw "Native campaign did not complete: $profile" }
} finally {
    if ($process -and -not $process.HasExited) { $process.Kill(); $process.WaitForExit() }
    if ($playerCheckPlugin -and (Test-Path -LiteralPath $playerCheckPlugin)) {
        Move-Item -LiteralPath $playerCheckPlugin -Destination (Join-Path $profile 'player-plugin')
        foreach ($dependency in @('Model.dll', 'FixtureArchive.dll')) {
            Copy-Item -LiteralPath (Join-Path $gameRoot "BepInEx/plugins/$dependency") -Destination (Join-Path $profile 'player-plugin')
        }
    }
    if ($playerConfigPrepared) {
        if (Test-Path -LiteralPath $playerConfigPath) {
            Move-Item -LiteralPath $playerConfigPath -Destination (Join-Path $profile 'checked-player-advisor.cfg')
        }
        if (Test-Path -LiteralPath $playerConfigBackup) { Move-Item -LiteralPath $playerConfigBackup -Destination $playerConfigPath }
    }
}
