#requires -Version 7.4
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $InputArchive,
    [Parameter(Mandatory)][string] $OutputArchive,
    [switch] $SkipBuild
)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/FixtureTools/FixtureTools.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE -ne 0) { throw 'Native input freezer build failed.' }
}
Add-Type -Path (Join-Path $workspace 'src/FixtureTools/bin/Release/net8.0/FixtureArchive.dll')
$document = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read((Resolve-Path -LiteralPath $InputArchive).Path)
try {
    $fixture = $document.RootElement.ToObjectGraph()
    if (@($fixture['RollingDecisions']).Count) {
        throw 'Player/rolling sources must replay their original publications directly; do not rebase their forecasts.'
    }
    $actions = @($fixture['Actions'])
    $index = 0
    $steps = @(foreach ($turn in $fixture['Turns']) {
        $number = $turn['Before']['Spawn']['Turn']
        while ($index -lt $actions.Count -and $actions[$index]['Before']['Spawn']['Turn'] -eq $number) {
            @{ IsEndTurn = $false; Action = $actions[$index]['Action'] }
            $index++
        }
        @{ IsEndTurn = $true; Action = $null }
    })
    if ($index -ne $actions.Count) { throw 'Chronological native inputs omitted an action.' }
    ConvertTo-Json -InputObject $steps -Depth 32 -Compress |
        dotnet (Join-Path $workspace 'src/FixtureTools/bin/Release/net8.0/FixtureTools.dll') freeze-actions $InputArchive - $OutputArchive
    if ($LASTEXITCODE -ne 0) { throw 'Native input freezing or complete model replay failed.' }
}
finally { $document.Dispose() }
