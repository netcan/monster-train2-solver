#requires -Version 7.4
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $InputProfile,
    [string] $SourceProfile = '',
    [string] $ReplayProfile = '',
    [ValidateRange(1, 2)][int] $NativeWorker = 2,
    [ValidateRange(60, 3600)][int] $TimeoutSeconds = 1800,
    [switch] $SkipBuild
)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
$InputProfile = (Resolve-Path -LiteralPath $InputProfile).Path
$savePath = Join-Path $InputProfile 'saves/save-singlePlayer.json'
$inputHash = '07884DC51C1354EE4668E277C87E9B733CD8156D49625D7F41848B5D3DA5D449'
if ((Get-FileHash -LiteralPath $savePath -Algorithm SHA256).Hash -cne $inputHash) {
    throw 'Short-budget Fragile pairs require the immutable seed424244 seven-win checkpoint.'
}
if ($ReplayProfile -and -not $SourceProfile) { throw 'Reusing a replay requires its source profile.' }
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE -ne 0) { throw 'Short-budget Fragile checker build failed.' }
}
if (-not $SourceProfile) {
    $result = $null
    & (Join-Path $PSScriptRoot 'Run-FullBattleProbe.ps1') -Policy units-spells-and-junk -InputProfile $InputProfile -BattleDistance 7 -Seed 424244 `
        -PlayerAdvisor -RollingAdvice -BranchCardMasks -PhysicalSpawnPoints -SettleDeathDissolves -SettleCardAnimations `
        -PlannerHorizon 3 -PlannerTimeLimitMs 3000 -PlannerDeploymentRoom 0 -NativeWorker $NativeWorker `
        -TimeoutSeconds $TimeoutSeconds -SkipBuild:$SkipBuild | ForEach-Object {
            if ($_.ToString().StartsWith('{')) { $result = $_ | ConvertFrom-Json } else { Write-Output $_ }
        }
    if ($null -eq $result) { throw 'Missing short-budget Fragile source result.' }
    $SourceProfile = $result.Profile
}
$SourceProfile = (Resolve-Path -LiteralPath $SourceProfile).Path
$source = Join-Path $SourceProfile 'full-battle.mt2f'
$accepted = Get-Content -LiteralPath (Join-Path $SourceProfile 'native-accepted-summary.json') -Raw | ConvertFrom-Json
$loaded = Get-Content -LiteralPath (Join-Path $SourceProfile 'native-checkpoint-input.json') -Raw | ConvertFrom-Json
if (-not $accepted.NativePassed -or -not $accepted.OriginalFilesUnchanged -or $accepted.ExitCode -ne 0 -or
    $accepted.CaptureFailures -ne 0 -or $accepted.Mismatches -ne 0 -or $accepted.Unsupported -ne 0 -or $accepted.Pending -ne 0 -or
    (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash -ine $accepted.TraceSha256 -or $loaded.SHA256 -ine $inputHash) {
    throw 'Short-budget Fragile native source acceptance or input hash failed.'
}
Add-Type -Path (Join-Path $workspace 'src/FixtureArchive/bin/Release/net8.0/FixtureArchive.dll')
$document = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read($source)
try {
    $trace = $document.RootElement
    $overlay = $trace.GetProperty('PlayerAdvisor').GetProperty('Overlay')
    if (-not $trace.GetProperty('NativeWon').GetBoolean() -or
        $overlay.GetProperty('Horizon').GetInt32() -ne 3 -or $overlay.GetProperty('TimeLimitMs').GetInt32() -ne 3000 -or
        $overlay.GetProperty('PreferredDeploymentRoom').GetInt32() -ne 0 -or $overlay.GetProperty('TacticalSeed').GetBoolean()) {
        throw 'Source must already be a native three-turn/3000ms Fragile victory with bottom preference and no extra tactical seed.'
    }
} finally { $document.Dispose() }
if (-not $ReplayProfile) {
    $result = $null
    & (Join-Path $PSScriptRoot 'Run-CampaignProgress.ps1') -InputProfile $InputProfile -BattleReplay $source -StopBeforeRing 9 `
        -Seed 424244 -DeploymentRoom 0 -NativeWorker $NativeWorker -TimeoutSeconds $TimeoutSeconds -SkipBuild | ForEach-Object {
            if ($_.ToString().StartsWith('{')) { $result = $_ | ConvertFrom-Json } else { Write-Output $_ }
        }
    if ($null -eq $result) { throw 'Missing short-budget Fragile campaign replay result.' }
    $ReplayProfile = $result.Profile
}
$ReplayProfile = (Resolve-Path -LiteralPath $ReplayProfile).Path
$campaign = Get-Content -LiteralPath (Join-Path $ReplayProfile 'campaign-summary.json') -Raw | ConvertFrom-Json
$continued = Get-Content -LiteralPath (Join-Path $ReplayProfile 'campaign-input.json') -Raw | ConvertFrom-Json
$replay = Join-Path $ReplayProfile 'full-battle.mt2f'
if (-not $campaign.NativePassed -or -not $campaign.OriginalFilesUnchanged -or $campaign.ExitCode -ne 0 -or
    -not $campaign.BattleReplayPassed -or -not $campaign.PlayerReplay -or -not $campaign.NativeCampaignWon -or
    $campaign.NativeVictoryType -cne 'Standard') { throw 'Fragile campaign replay or original Standard settlement failed.' }
if ($continued.InputSaveSHA256 -ine $inputHash -or
    $continued.BattleReplaySHA256 -ine (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash -or
    $campaign.FinalSaveSHA256 -ine (Get-FileHash -LiteralPath (Join-Path $ReplayProfile 'saves/save-singlePlayer.json') -Algorithm SHA256).Hash) {
    throw 'Fragile campaign replay input, frozen source or settled save changed.'
}
dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --player-boss-fragile-short-budget-pair-only $source $replay
if ($LASTEXITCODE -ne 0) { throw 'Independent short-budget Fragile native pair failed.' }
if ((Get-FileHash -LiteralPath $savePath -Algorithm SHA256).Hash -cne $inputHash) { throw 'Original Fragile checkpoint changed.' }
$pair = [pscustomobject]@{
    Schema = 1; InputProfile = $InputProfile; InputSaveSHA256 = $inputHash
    SourceProfile = $SourceProfile; ReplayProfile = $ReplayProfile
    SourceSha256 = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant()
    ReplaySha256 = (Get-FileHash -LiteralPath $replay -Algorithm SHA256).Hash.ToLowerInvariant()
    NativeCampaignWon = $true; NativeVictoryType = 'Standard'; Passed = $true
}
$pair | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $SourceProfile 'player-pair-accepted.json') -Encoding utf8
$pair | ConvertTo-Json -Compress
