#requires -Version 7.4
[CmdletBinding()]
param(
    [ValidateRange(1, 10)]
    [int[]] $Rank = @(1..10),
    [switch] $Player,
    [switch] $SkipBuild
)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE -ne 0) { throw 'Native Covenant startup checker build failed.' }
}
$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
$manifest = Import-Csv -LiteralPath (Join-Path $workspace 'tests/fixtures/manifest.tsv') -Delimiter "`t"
foreach ($level in $Rank) {
    $suffix = $level.ToString('D2', [Globalization.CultureInfo]::InvariantCulture)
    $prefix = if ($Player) {
        'full-battle-player-native-covenant-startup' + $(if ($level -eq 1) { '' } else { "-$suffix" })
    } else { "full-battle-native-covenant-startup-$suffix" }
    $names = if ($Player) { @("$prefix-search.mt2f", "$prefix-replay.mt2f") } else { @("$prefix.mt2f") }
    $paths = @($names | ForEach-Object { Join-Path $workspace "tests/fixtures/$_" })
    foreach ($path in $paths) {
        $rows = @($manifest | Where-Object archive -CEQ ([IO.Path]::GetFileName($path)))
        if ($rows.Count -ne 1 -or -not (Test-Path -LiteralPath $path -PathType Leaf) -or
            (Get-Item -LiteralPath $path).Length -ne [long]$rows[0].archive_bytes -or
            (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $rows[0].archive_sha256) {
            throw "Native Covenant startup fixture missing or metadata differs: $path"
        }
    }
    if ($Player) {
        $mode = '--player-native-covenant-startup' + $(if ($level -eq 1) { '' } else { "-$suffix" }) + '-pair-only'
        dotnet $checker $mode $paths[0] $paths[1]
    } else {
        dotnet $checker --native-covenant-startup-only $paths[0]
    }
    if ($LASTEXITCODE -ne 0) { throw "Native Covenant startup acceptance failed: rank $level, player=$Player" }
}
