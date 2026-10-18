#requires -Version 7.4
[CmdletBinding()]
param(
    [switch] $Check,
    [switch] $SkipBuild
)
$ErrorActionPreference = 'Stop'
$workspace = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/FixtureTools/FixtureTools.csproj') -c Release --no-restore -p:WarningLevel=0
    if ($LASTEXITCODE -ne 0) { throw 'FixtureTools build failed.' }
}
$inventoryArguments = @(
    (Join-Path $workspace 'src/FixtureTools/bin/Release/net8.0/FixtureTools.dll'),
    'card-inventory', $workspace
)
if ($Check) { $inventoryArguments += '--check' }
dotnet @inventoryArguments
if ($LASTEXITCODE -ne 0) { throw 'Card coverage inventory validation failed.' }
