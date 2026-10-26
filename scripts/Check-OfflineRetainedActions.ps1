#requires -Version 7.4
param(
 [string]$Prediction='tests/fixtures/campaign/random/full-battle-random-campaign-14-ring6-release165-refined-loss.mt2f',
 [string]$ToolsRoot='src/FixtureTools/bin/Release/net8.0',
 [string]$CheckerRoot='src/ModelChecks/bin/Release/net8.0',
 [switch]$SkipBuild
)
$ErrorActionPreference='Stop'
$workspace=Split-Path $PSScriptRoot -Parent
Set-Location $workspace
if(-not $SkipBuild){
 foreach($project in @('ModelChecks','FixtureTools')){
  dotnet build "src/$project/$project.csproj" -c Release -p:WarningLevel=0 -v:q
  if($LASTEXITCODE -ne 0){throw "$project build failed"}
 }
}
foreach($name in @('Model','FixtureArchive','ModelChecks')){Add-Type -Path (Resolve-Path (Join-Path $checkerRoot ($name+'.dll')))}
Add-Type -Path (Resolve-Path (Join-Path $toolsRoot 'FixtureTools.dll'))
$checks=[Reflection.Assembly]::LoadFrom((Resolve-Path (Join-Path $checkerRoot 'ModelChecks.dll')))
$options=$checks.GetType('ModelJson').GetField('Options',[Reflection.BindingFlags]'NonPublic,Static').GetValue($null)
$tools=[Reflection.Assembly]::LoadFrom((Resolve-Path (Join-Path $toolsRoot 'FixtureTools.dll')))
$retain=$tools.GetType('OfflineBattleSearch').GetMethod('RetainAction',[Reflection.BindingFlags]'NonPublic,Static')
function State($value){[Text.Json.JsonSerializer]::Deserialize(($value|ConvertTo-Json -Depth 100 -Compress),[MonsterTrain2Poju.Model.BattleTurnState],$options)}
function Action($value){[Text.Json.JsonSerializer]::Deserialize(($value|ConvertTo-Json -Depth 20 -Compress),[MonsterTrain2Poju.Model.PlayCardAction],$options)}
$d=[MonsterTrain2Poju.Fixtures.FixtureDocument]::Read((Resolve-Path $Prediction))
try{
 $r=$d.RootElement.ToObjectGraph();$identity=0
 foreach($row in $r.RollingDecisions|Where-Object {-not $_.IsEndTurn}){
  $s=State $row.Root;$a=Action $row.Action
  if(-not [object]::ReferenceEquals($a,$retain.Invoke($null,@($s,$s,$a)))){throw 'Unchanged prefix action identity changed'};$identity++
 }
 $row=@($r.RollingDecisions|Where-Object {$_.Action.CardInstanceId -eq 23})[0]
 $original=State $row.Root;$a=Action $row.Action
 if($a.TargetUnitId -ne 2){throw 'Unexpected source target'}
 $before=[Text.Json.JsonSerializer]::Serialize($original,$options)
 foreach($case in @('ReusedId','MovedCard','MissingCard','DeadCard','AmbiguousCard','DifferentCardInstance','Activator','CardlessBirth')){
  $changed=$before|ConvertFrom-Json -Depth 100 -AsHashtable
  $rooms=$changed.Spawn.Train.Rooms
  $selected=@($rooms.Units|Where-Object Id -eq 2)[0]
  $selected.Id=102
  $oldHost=@($rooms|Where-Object {$_.Units.Id -contains 102})[0]
  if($case -eq 'ReusedId'){@($rooms.Units|Where-Object Id -eq 3)[0].Id=2}
  if($case -eq 'MovedCard'){$oldHost.Units=@($oldHost.Units|Where-Object Id -ne 102);$rooms[1].Units=@($rooms[1].Units)+@($selected)}
  if($case -eq 'MissingCard'){$oldHost.Units=@($oldHost.Units|Where-Object Id -ne 102)}
  if($case -eq 'DeadCard'){$selected.Health=0;$selected.DeathState.IsDestroyed=$true}
  if($case -in @('AmbiguousCard','DifferentCardInstance')){
   $copy=($selected|ConvertTo-Json -Depth 100 -Compress)|ConvertFrom-Json -AsHashtable -Depth 100
   $copy.Id=103
   if($case -eq 'DifferentCardInstance'){$copy.SpawnerCardId=999}
   $oldHost.Units=@($oldHost.Units)+@($copy)
  }
  $currentAction=$a
  $currentOriginal=$original
  if($case -eq 'CardlessBirth'){
   $cardless=$before|ConvertFrom-Json -Depth 100 -AsHashtable
   @($cardless.Spawn.Train.Rooms.Units|Where-Object Id -eq 2)[0].SpawnerCardId=0
   $currentOriginal=State $cardless;$selected.SpawnerCardId=0
  }
  if($case -eq 'Activator'){$currentAction=[MonsterTrain2Poju.Model.PlayCardAction]::new(0,0,-1,0,2,-1,$null,$null,0)}
  $result=$retain.Invoke($null,@($currentOriginal,(State $changed),$currentAction))
  if($case -in @('MissingCard','DeadCard','AmbiguousCard','CardlessBirth')){if($null -ne $result){throw "Unresolved identity guessed: $case"}}
  elseif($case -eq 'Activator'){if($result.ActivatorUnitId -ne 102){throw 'Lost ability host identity'}}
  elseif($result.TargetUnitId -ne 102 -or ($case -eq 'MovedCard' -and $result.RoomIndex -ne 1)){throw "Wrong target: $case"}
  if($before -cne [Text.Json.JsonSerializer]::Serialize($original,$options)){throw 'Mutated original'}
 }
 "RETAINED-IDENTITY PASS: $identity unchanged native-route inputs; reused numeric ID, moved card, missing/dead/ambiguous/cardless identity rejection, exact card instance and ability host."
}finally{$d.Dispose()}
