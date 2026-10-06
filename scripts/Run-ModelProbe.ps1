#requires -Version 7.4
param()

$ErrorActionPreference = 'Stop'
$workspace = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$gameRoot = Join-Path $workspace '.sandbox-game'
$profile = Join-Path $workspace ('.probe-runs\model-unit-play-' +
    (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
$originalLogRoot = Join-Path ([Environment]::GetFolderPath('UserProfile')) `
    'AppData\LocalLow\Shiny Shoe\MonsterTrain2'

function Get-OriginalLogSignature {
    if (-not (Test-Path -LiteralPath $originalLogRoot)) {
        return ''
    }
    $parts = Get-ChildItem -LiteralPath $originalLogRoot -Filter 'logfile*.log' -File -Force |
        Sort-Object Name | ForEach-Object {
            $_.Name + ':' + $_.LastWriteTimeUtc.Ticks + ':' +
                (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
        }
    return [string]::Join('|', [string[]] @($parts))
}

dotnet build (Join-Path $workspace 'src\Probe\Probe.csproj') -c Release
if ($LASTEXITCODE -ne 0) {
    throw 'Probe build failed.'
}

$pluginDir = Join-Path $gameRoot 'BepInEx\plugins'
Copy-Item -LiteralPath (Join-Path $workspace 'src\Probe\bin\Release\netstandard2.1\Probe.dll') `
    -Destination $pluginDir -Force
Copy-Item -LiteralPath (Join-Path $workspace 'src\Model\bin\Release\netstandard2.1\Model.dll') `
    -Destination $pluginDir -Force
Copy-Item -LiteralPath (Join-Path $workspace '.probe-baseline') -Destination $profile -Recurse

$environment = @{
    MT2_PROBE_DATA_DIR = $profile
    MT2_PROBE_SCENARIO = 'two-turns'
    MT2_PROBE_BATTLE_ID = 'natural'
    MT2_PROBE_SKIP_FTUE = '1'
    MT2_PROBE_PLAY_CARD = '1'
    MT2_PROBE_DIRECT_PLAY = '1'
    MT2_PROBE_MODEL_UNIT_PLAY = '1'
    MT2_PROBE_UNDO = '0'
    MT2_PROBE_RESTART = '0'
    MT2_PROBE_BRANCH_TURN1 = '0'
    MT2_PROBE_PLAY_TURN1 = '0'
}
$originalLogsBefore = Get-OriginalLogSignature
$unityLog = Join-Path $profile 'unity-scenario.log'
$process = Start-Process -FilePath (Join-Path $gameRoot 'MonsterTrain2.exe') `
    -WorkingDirectory $gameRoot -ArgumentList @('-logFile', $unityLog) `
    -Environment $environment -WindowStyle Hidden -PassThru
if (-not $process.WaitForExit(180000)) {
    $process.Kill()
    $process.WaitForExit()
    throw "Model probe timed out; profile: $profile"
}

$modelPassed = [bool] (Select-String -LiteralPath $unityLog -Pattern 'MODEL-PASS' -Quiet)
$nativePassed = [bool] (Select-String -LiteralPath $unityLog -Pattern 'SMOKE-PASS' -Quiet)
$originalLogsUnchanged = (Get-OriginalLogSignature) -ceq $originalLogsBefore
$result = [pscustomobject]@{
    Profile = $profile
    ExitCode = $process.ExitCode
    ModelPassed = $modelPassed
    NativePassed = $nativePassed
    OriginalLogsUnchanged = $originalLogsUnchanged
    Projection = (Join-Path $profile 'model-unit-play.json')
}
$result | ConvertTo-Json
if (-not ($modelPassed -and $nativePassed -and $originalLogsUnchanged)) {
    throw "Model probe failed; inspect $unityLog"
}
