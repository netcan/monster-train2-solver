#requires -Version 7.4
[CmdletBinding()]
param(
    [ValidateSet(2, 10)]
    [int[]] $Rank = @(2, 10),
    [switch] $Player,
    [switch] $SkipBuild
)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE -ne 0) { throw 'Native Covenant final-boss checker build failed.' }
}
$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
$manifest = Import-Csv -LiteralPath (Join-Path $workspace 'tests/fixtures/manifest.tsv') -Delimiter "`t"
foreach ($level in $Rank) {
    $suffix = $level.ToString('D2', [Globalization.CultureInfo]::InvariantCulture)
    $names = if ($Player) {
        @("full-battle-player-native-covenant-final-boss-$suffix-search.mt2f", "full-battle-player-native-covenant-final-boss-$suffix-replay.mt2f")
    } else {
        @("full-battle-native-covenant-final-boss-$suffix.mt2f", "true-final-boss-scheduled-native-covenant-$suffix.mt2f")
    }
    $paths = @($names | ForEach-Object { Join-Path $workspace "tests/fixtures/$_" })
    foreach ($path in $paths) {
        $rows = @($manifest | Where-Object archive -CEQ ([IO.Path]::GetFileName($path)))
        if ($rows.Count -ne 1 -or -not (Test-Path -LiteralPath $path -PathType Leaf) -or
            (Get-Item -LiteralPath $path).Length -ne [long]$rows[0].archive_bytes -or
            (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $rows[0].archive_sha256) {
            throw "Native Covenant final-boss fixture missing or metadata differs: $path"
        }
    }
    $mode = if ($Player) { '--player-native-covenant-final-boss-pair-only' } else { '--native-covenant-final-boss-only' }
    dotnet $checker $mode $paths[0] $paths[1] $level
    if ($LASTEXITCODE -ne 0) { throw "Native Covenant final-boss acceptance failed: rank $level, player=$Player" }
}
