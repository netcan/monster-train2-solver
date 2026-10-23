#requires -Version 7.4
[CmdletBinding()]
param([switch] $Check, [switch] $SkipBuild)
$ErrorActionPreference = 'Stop'
$workspace = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/FixtureTools/FixtureTools.csproj') -c Release --no-restore -p:WarningLevel=0
    if ($LASTEXITCODE -ne 0) { throw 'FixtureTools build failed.' }
}
$arguments = @((Join-Path $workspace 'src/FixtureTools/bin/Release/net8.0/FixtureTools.dll'), 'boss-inventory', $workspace)
if ($Check) { $arguments += '--check' }
dotnet @arguments
if ($LASTEXITCODE -ne 0) { throw 'Boss coverage inventory validation failed.' }
