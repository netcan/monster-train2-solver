#requires -Version 7.4
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $Profile,
    [Parameter(Mandatory)][string] $Archive,
    [switch] $Revalidate,
    [switch] $SkipBuild,
    [switch] $CheckCardFrontiers,
    [switch] $CheckEndTurnFrontiers
)

$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
if (-not $SkipBuild) {
    & dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
$checkerArguments = @('--register-game231-native-battle', $workspace, [IO.Path]::GetFullPath($Profile, (Get-Location).ProviderPath), $Archive)
if ($Revalidate) { $checkerArguments += '--revalidate' }
if ($CheckCardFrontiers) { $checkerArguments += '--card-frontiers' }
if ($CheckEndTurnFrontiers) { $checkerArguments += '--end-turn-frontiers' }
& dotnet $checker @checkerArguments
if ($LASTEXITCODE) { throw 'Native battle registration failed.' }
