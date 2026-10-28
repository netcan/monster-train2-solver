#requires -Version 7.4
param(
    [string]$Fixture = 'tests/fixtures/campaign/random/full-battle-random-campaign-24-ring8-release265-budget10000-settled-loss.mt2f',
    [string]$ToolsRoot = 'src/FixtureTools/bin/Release/net8.0',
    [switch]$SkipBuild
)
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
if (-not $SkipBuild) {
    dotnet build src/FixtureTools/FixtureTools.csproj -c Release -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE -ne 0) { throw 'FixtureTools build failed.' }
}
$tool = Join-Path (Resolve-Path $ToolsRoot) 'FixtureTools.dll'
Add-Type -Path (Join-Path (Resolve-Path $ToolsRoot) 'FixtureArchive.dll')
$source = (Resolve-Path $Fixture).Path
$originalHash = (Get-FileHash $source).Hash
$testRoot = Join-Path ([IO.Path]::GetTempPath()) ('poju-deployment-opening-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testRoot | Out-Null
$baseline = Join-Path $testRoot 'baseline.mt2f'
$actual = Join-Path $testRoot 'actual.mt2f'
$interleaved = Join-Path $testRoot 'interleaved.mt2f'
$document = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read($source)
try {
    $steps = @($document.RootElement.GetProperty('RollingDecisions').EnumerateArray() | ForEach-Object {
        [ordered]@{ Action = $_.GetProperty('Action').ToObjectGraph(); IsEndTurn = $_.GetProperty('IsEndTurn').GetBoolean() }
    })
    if ($steps.Count -ne 28 -or $steps[3].Action.CardInstanceId -ne 7 -or $steps[3].Action.RoomIndex -ne 2 -or
        -not $steps[4].IsEndTurn) { throw 'Expected the original three-unit opening followed by Bloodsoaked Arena.' }
    $steps | ConvertTo-Json -Depth 40 -Compress | & dotnet $tool freeze-actions $source - $baseline
    if ($LASTEXITCODE -ne 0) { throw 'Original native route failed model replay.' }
    $baselineHash = (Get-FileHash $baseline).Hash
    $log = @(& dotnet $tool deployment-grid $source $baseline $actual 60000 8 2>&1)
    $log | Write-Output
    if ($LASTEXITCODE -ne 0 -or ($log -join "`n") -notmatch 'candidates=8 completed=8 duplicates=0 unsupported=0' -or
        ($log -join "`n") -notmatch 'fixedOpeningCards=1' -or -not (Test-Path -LiteralPath $actual)) {
        throw 'Mixed opening failed complete bounded joint deployment investigation.'
    }
    $exported = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read($actual)
    try {
        $rows = @($exported.RootElement.GetProperty('RollingDecisions').EnumerateArray())
        if (-not $rows[3].GetProperty('Action').ContentEquals($document.RootElement.GetProperty('RollingDecisions')[3].GetProperty('Action'))) {
            throw 'Deployment search changed or omitted the fixed opening spell.'
        }
    } finally { $exported.Dispose() }
    $actualHash = (Get-FileHash $actual).Hash
    $protected = @(& dotnet $tool deployment-grid $source $baseline $actual 100 1 2>&1)
    if ($LASTEXITCODE -eq 0 -or ($protected -join "`n") -notmatch 'Output already exists' -or
        (Get-FileHash $actual).Hash -ne $actualHash) { throw 'Existing output protection failed.' }
    # Move the independent floor2 spell before the third floor1 unit. The
    # complete route remains legal, but interleaved unit order is outside this
    # tool's calibrated joint-placement scope and must keep its explicit guard.
    $interleavedSteps = @($steps[0], $steps[1], $steps[3], $steps[2]) + @($steps[4..($steps.Count - 1)])
    $interleavedSteps | ConvertTo-Json -Depth 40 -Compress | & dotnet $tool freeze-actions $source - $interleaved
    if ($LASTEXITCODE -ne 0) { throw 'Independent interleaving control failed model replay.' }
    $rejected = @(& dotnet $tool deployment-grid $source $interleaved (Join-Path $testRoot 'rejected.mt2f') 100 1 2>&1)
    if ($LASTEXITCODE -eq 0 -or ($rejected -join "`n") -notmatch 'initial unit plays, optionally followed') {
        throw 'Interleaved unknown deployment scope lost its guard.'
    }
    $orderLog = @(& dotnet $tool deployment-order-spread $source $baseline (Join-Path $testRoot 'order.mt2f') 60000 4 any 0 2>&1)
    if ($LASTEXITCODE -ne 0 -or ($orderLog -join "`n") -notmatch 'candidates=4 completed=4' -or
        ($orderLog -join "`n") -notmatch 'spreadOrder=True') { throw 'Order spread did not complete four samples.' }
    $placements = @($orderLog | Where-Object { $_ -match '^DEPLOYMENT-SPREAD sample=\d+ placements=' })
    $orders = @($placements | ForEach-Object {
        $json = $_ -replace '^DEPLOYMENT-SPREAD sample=\d+ placements=', ''
        (@($json | ConvertFrom-Json) | ForEach-Object { $_.CardInstanceId }) -join ','
    })
    if ($orders.Count -ne 4 -or @($orders | Select-Object -Unique).Count -ne 4) { throw 'Unit order did not vary across samples.' }
    $resume = @(& dotnet $tool deployment-order-spread $source $baseline (Join-Path $testRoot 'resume.mt2f') 60000 1 any 1 2>&1)
    if ($LASTEXITCODE -ne 0) { throw 'Order spread resume failed.' }
    $resumePlacement = @($resume | Where-Object { $_ -match '^DEPLOYMENT-SPREAD sample=1 placements=' })
    if ($resumePlacement.Count -ne 1 -or $resumePlacement[0] -cne $placements[1]) { throw 'Order spread resume changed the sampled placement.' }
    if ((Get-FileHash $source).Hash -ne $originalHash -or (Get-FileHash $baseline).Hash -ne $baselineHash) {
        throw 'Native source or baseline was modified.'
    }
    "DEPLOYMENT-OPENING PASS: eight complete native-root placements preserve the exact closing spell, independently replay the full result and retain interleaving/output guards; four distinct unit orders and deterministic resume; artifacts=$testRoot"
} finally { $document.Dispose() }
