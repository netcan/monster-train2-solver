param(
    [string]$GameAssembly = 'F:/SteamLibrary/steamapps/common/Monster Train 2/MonsterTrain2_Data/Managed/Assembly-CSharp.dll',
    [switch]$SkipBuild,
    [switch]$Check
)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (!(Test-Path -LiteralPath $GameAssembly -PathType Leaf)) { throw "Native assembly not found: $GameAssembly" }
if (!$SkipBuild) {
    dotnet build (Join-Path $workspace 'src/FixtureTools/FixtureTools.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'FixtureTools build failed.' }
}
$arguments = @('mechanism-inventory', $workspace, (Resolve-Path -LiteralPath $GameAssembly).Path)
if ($Check) { $arguments += '--check' }
dotnet (Join-Path $workspace 'src/FixtureTools/bin/Release/net8.0/FixtureTools.dll') @arguments
if ($LASTEXITCODE) { throw 'Native mechanism inventory failed.' }
