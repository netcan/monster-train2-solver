#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild, [string[]] $Fixture = @())

$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/lifelink-sacrifice'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
$expected = @(foreach ($mode in @('zero', 'retained')) {
    foreach ($target in @('dominion', 'savagery', 'entropybot', 'entropymid', 'entropytop')) { "$target-$mode.mt2f" }
})
$expected += 'terminal-command.mt2f'
if ($manifest.Count -ne 11 -or ($manifest.path -join ',') -cne ($expected -join ',')) { throw 'Unexpected LifeLink sacrifice manifest.' }
if (@($Fixture | Where-Object { $_ -cnotin $expected }).Count) { throw 'Unknown LifeLink sacrifice fixture.' }
$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
Add-Type -Path (Join-Path $workspace 'src/FixtureArchive/bin/Release/net8.0/FixtureArchive.dll')
foreach ($entry in $manifest) {
    $path = Join-Path $directory $entry.path
    if ((Get-Item -LiteralPath $path).Length -ne [long]$entry.bytes -or
        (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $entry.sha256) { throw 'LifeLink sacrifice capture hash/size differs.' }
    $document = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read($path)
    try {
        $control = $document.RootElement.GetProperty('TrueFinalBossMaximumBattle')
        if ($entry.path -ceq 'terminal-command.mt2f') {
            if (-not $control.GetProperty('TerminalCommand').GetBoolean()) { throw 'Missing original terminal command control.' }
        } else {
            $name = $control.GetProperty('SacrificeTarget').GetString().ToLowerInvariant() + '-' +
                $(if ($control.GetProperty('RetainMaximum').GetBoolean()) { 'retained' } else { 'zero' }) + '.mt2f'
            if ($name -cne $entry.path) { throw 'LifeLink sacrifice fixture does not match its declared native target/mode.' }
        }
    } finally { $document.Dispose() }
}
if (-not $Fixture.Count) {
    dotnet $checker --true-final-boss-maximum-sacrifice-matrix $directory
    if ($LASTEXITCODE) { throw 'Independent LifeLink sacrifice matrix validation failed.' }
    $count = 11
} else {
    foreach ($name in $Fixture) {
        $option = if ($name -ceq 'terminal-command.mt2f') { '--true-final-boss-maximum-command-only' } else { '--true-final-boss-maximum-sacrifice-only' }
        dotnet $checker $option (Join-Path $directory $name)
        if ($LASTEXITCODE) { throw "Independent LifeLink maximum-health validation failed: $name" }
    }
    $count = $Fixture.Count
}
Write-Output "GAME-231-LIFE-LINK-SACRIFICE PASS: $count selected native final-boss victories, full state/RNG chains, 16 immutable branches each and bounded serial/parallel search. Individual results identify maximum-health sacrifice or the original terminal command. Controlled cards add no original coverage; linked temporary previews remain separate."
