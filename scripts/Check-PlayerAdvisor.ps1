#requires -Version 7.4
[CmdletBinding()]
param(
    [ValidateSet('Moon', 'OuterBoss', 'OuterBossRolling', 'Conduit', 'RoomMoon', 'RoomMoonMovement', 'EventGraft', 'NaturalRing7')]
    [string[]] $Case = @('Moon', 'OuterBoss', 'OuterBossRolling', 'Conduit', 'RoomMoon', 'RoomMoonMovement', 'EventGraft', 'NaturalRing7'),
    [switch] $SkipBuild
)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE -ne 0) { throw 'Player acceptance checker build failed.' }
}
$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
$manifest = Import-Csv -LiteralPath (Join-Path $workspace 'tests/fixtures/manifest.tsv') -Delimiter "`t"
foreach ($name in $Case) {
    $prefix = switch ($name) { 'Moon' { 'player-moon' }; 'OuterBoss' { 'player-boss' }; 'OuterBossRolling' { 'player-boss-rolling' }; 'Conduit' { 'player-conduit' }; 'RoomMoon' { 'player-room-moon' }; 'RoomMoonMovement' { 'player-room-moon-movement' }; 'EventGraft' { 'player-event-graft' }; 'NaturalRing7' { 'player-natural-ring7' } }
    $search = Join-Path $workspace "tests/fixtures/full-battle-$prefix-search.mt2f"
    $replay = Join-Path $workspace "tests/fixtures/full-battle-$prefix-replay.mt2f"
    foreach ($path in @($search, $replay)) {
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing player acceptance archive: $path" }
        $rows = @($manifest | Where-Object archive -CEQ ([IO.Path]::GetFileName($path)))
        if ($rows.Count -ne 1 -or (Get-Item -LiteralPath $path).Length -ne [long]$rows[0].archive_bytes -or
            (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $rows[0].archive_sha256) {
            throw "Player acceptance archive metadata differs: $path"
        }
    }
    $mode = if ($name -eq 'OuterBossRolling') { '--player-boss-winning-pair-only' } else { "--$prefix-pair-only" }
    dotnet $checker $mode $search $replay
    if ($LASTEXITCODE -ne 0) { throw "Player $name acceptance failed." }
}
Write-Output "PLAYER-ADVISOR-FOCUSED PASS cases=$($Case -join ','); no full regression executed."
