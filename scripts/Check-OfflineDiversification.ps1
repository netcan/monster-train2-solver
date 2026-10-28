#requires -Version 7.4
param(
 [string]$Fixture='tests/fixtures/campaign/random/full-battle-random-campaign-14-ring6-release165-refined-loss.mt2f',
 [string]$ToolsRoot='src/FixtureTools/bin/Release/net8.0',
 [switch]$SkipBuild
)
$ErrorActionPreference='Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
if(-not $SkipBuild){
 dotnet build src/FixtureTools/FixtureTools.csproj -c Release -p:WarningLevel=0 -v:q
 if($LASTEXITCODE -ne 0){throw 'FixtureTools build failed'}
}
$tool=Join-Path (Resolve-Path $ToolsRoot) 'FixtureTools.dll'
Add-Type -Path (Join-Path (Resolve-Path $ToolsRoot) 'FixtureArchive.dll')
$source=(Resolve-Path $Fixture).Path
$originalHash=(Get-FileHash $source).Hash
$testRoot=Join-Path ([IO.Path]::GetTempPath()) ('poju-diversification-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testRoot | Out-Null
$expected=Join-Path $testRoot 'expected.mt2f'
$baseline=Join-Path $testRoot 'worse-baseline.mt2f'
$actual=Join-Path $testRoot 'actual.mt2f'
$document=[MonsterTrain2Poju.Fixtures.FixtureDocument]::Read($source)
try{
 $rows=@($document.RootElement.GetProperty('RollingDecisions').EnumerateArray())
 $steps=@($rows|ForEach-Object {
  [ordered]@{Action=$_.GetProperty('Action').ToObjectGraph();IsEndTurn=$_.GetProperty('IsEndTurn').GetBoolean()}
 })
 $root=($rows[0].GetProperty('Root').ToObjectGraph()|ConvertTo-Json -Depth 100 -Compress)|ConvertFrom-Json -Depth 100
 $rule=@($root.PlayRules.Cards|Where-Object AssetKey -eq 'GrowRoom')
 if($rule.Count -ne 1){throw 'Expected the native GrowRoom counterexample'}
 $card=@($root.Spawn.Train.Context.CardInstances|Where-Object DataId -eq $rule[0].DataId)
 if($card.Count -ne 1){throw 'Expected one exact GrowRoom card instance'}
 $indices=@(0..($steps.Count-1)|Where-Object {$steps[$_].Action.CardInstanceId -eq $card[0].InstanceId})
 if($indices.Count -ne 1 -or $steps[$indices[0]].Action.RoomIndex -ne 1){throw 'Expected the calibrated middle-floor route'}
 $steps|ConvertTo-Json -Depth 40 -Compress|& dotnet $tool freeze-actions $source - $expected
 if($LASTEXITCODE -ne 0){throw 'Expected route failed independent replay'}
 $steps[$indices[0]].Action.RoomIndex=0
 $steps|ConvertTo-Json -Depth 40 -Compress|& dotnet $tool freeze-actions $source - $baseline
 if($LASTEXITCODE -ne 0){throw 'Controlled alternative failed independent replay'}
 $log=@(& dotnet $tool improve-diverse $source $baseline $actual 60000 4 $indices[0] 6 1 2 0 2>&1)
 $log|Write-Output
 if($LASTEXITCODE -ne 0){throw 'Diversified investigation failed'}
 $summary=@($log|Where-Object {$_ -match '^IMPROVE-POLICY foundWin='})
 if($summary.Count -ne 1 -or $summary[0] -notmatch 'improvements=[1-9]' -or
    $summary[0] -notmatch 'diversifiedRoutes=[1-9]' -or $summary[0] -notmatch 'exported=True'){
  throw 'Control must improve and then explore a worse complete alternative'
 }
 if($summary[0] -notmatch 'diversifiedRoutes=(\d+)' -or [int]$Matches[1] -ge 4){
  throw 'An alternative selected after the final pass cannot be counted as explored'
 }
 $actualHash=(Get-FileHash $actual).Hash
 if($actualHash -ne (Get-FileHash $expected).Hash){throw 'Export lost the best complete route after diversification'}
 $singleOutput=Join-Path $testRoot 'single-pass.mt2f'
 $singleLog=@(& dotnet $tool improve-diverse $source $baseline $singleOutput 100 1 $indices[0] 1 1 2 0 2>&1)
 if($LASTEXITCODE -ne 0 -or ($singleLog -join "`n") -notmatch 'IMPROVE-DIVERSIFY WARNING: one pass cannot explore retained alternative routes'){
  throw 'Single-pass diversification warning missing'
 }
 $protectedResult=@(& dotnet $tool improve-diverse $source $baseline $actual 100 1 0 1 1 1 0 2>&1)
 if($LASTEXITCODE -eq 0 -or ($protectedResult -join "`n") -notmatch 'Output already exists' -or
    (Get-FileHash $actual).Hash -ne $actualHash){throw 'Existing output protection failed'}
 foreach($bad in @(@(0,1,1,0),@(1,0,1,0),@(1,1,17,0),@(1,1,1,33))){
  $rejected=@(& dotnet $tool improve-diverse missing-source missing-prediction (Join-Path $testRoot 'rejected.mt2f') 100 1 0 @bad 2>&1)
  if($LASTEXITCODE -eq 0 -or ($rejected -join "`n") -notmatch 'ArgumentOutOfRangeException'){
   throw 'Invalid bounds were not rejected before opening inputs'
  }
 }
 if((Get-FileHash $source).Hash -ne $originalHash){throw 'Native evidence was modified'}
 "DIVERSIFICATION PASS: complete native-route control restores the best result after worse alternatives, byte-identical independent replay, bounds and existing-output protection; artifacts=$testRoot"
}finally{$document.Dispose()}
