#requires -Version 7.4
[CmdletBinding()]
param(
    [ValidateSet('AllyBuffer', 'Fragile', 'Corrupter')]
    [string[]] $Variant = @('AllyBuffer', 'Fragile', 'Corrupter'),
    [ValidateSet('01', '00-01')]
    [string[]] $Covenant = @('01', '00-01'),
    [switch] $Player,
    [switch] $SkipBuild
)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE -ne 0) { throw 'Covenant outer-boss checker build failed.' }
}
$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
$manifest = Import-Csv -LiteralPath (Join-Path $workspace 'tests/fixtures/manifest.tsv') -Delimiter "`t"
foreach ($boss in $Variant) {
    foreach ($context in $Covenant) {
        $suffix = "$($boss.ToLowerInvariant())-$context"
        $names = if ($Player) {
            @("full-battle-player-covenant-outer-boss-$suffix-search.mt2f", "full-battle-player-covenant-outer-boss-$suffix-replay.mt2f")
        } else {
            @("full-battle-covenant-outer-boss-$suffix.mt2f", "outer-boss-birth-covenant-$suffix.mt2f")
        }
        $paths = @($names | ForEach-Object { Join-Path $workspace "tests/fixtures/$_" })
        foreach ($path in $paths) {
            $rows = @($manifest | Where-Object archive -CEQ ([IO.Path]::GetFileName($path)))
            if ($rows.Count -ne 1 -or -not (Test-Path -LiteralPath $path -PathType Leaf) -or
                (Get-Item -LiteralPath $path).Length -ne [long]$rows[0].archive_bytes -or
                (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $rows[0].archive_sha256) {
                throw "Covenant outer-boss fixture metadata differs: $path"
            }
        }
        $mode = if ($Player) { '--player-covenant-outer-boss-pair-only' } else { '--covenant-outer-boss-only' }
        dotnet $checker $mode $paths[0] $paths[1] $boss ($context -eq '00-01').ToString()
        if ($LASTEXITCODE -ne 0) { throw "Covenant outer-boss acceptance failed: $boss/$context" }
    }
}
