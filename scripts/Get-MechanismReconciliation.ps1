#requires -Version 7.4
[CmdletBinding()]
param(
    [string] $GameAssembly = 'F:/SteamLibrary/steamapps/common/Monster Train 2/MonsterTrain2_Data/Managed/Assembly-CSharp.dll',
    [switch] $SkipBuild,
    [switch] $Check
)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    foreach ($project in @('Probe','ModelChecks','FixtureTools')) {
        dotnet build (Join-Path $workspace "src/$project/$project.csproj") -c Release --no-restore -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE) { throw "$project build failed." }
    }
}
$arguments = @('mechanism-reconciliation', $workspace, (Resolve-Path -LiteralPath $GameAssembly).Path)
if ($Check) { $arguments += '--check' }
dotnet (Join-Path $workspace 'src/FixtureTools/bin/Release/net8.0/FixtureTools.dll') @arguments
if ($LASTEXITCODE) { throw 'Native/model mechanism reconciliation failed.' }
