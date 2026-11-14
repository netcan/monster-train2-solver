#requires -Version 7.4
[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateRange(2, 4)][int] $Worker,
    [string] $GameDirectory = ''
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$workspace = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$template = Join-Path $workspace '.sandbox-game'
$gameSource = if ($GameDirectory) { (Resolve-Path -LiteralPath $GameDirectory).Path } else { $template }
$destination = Join-Path $workspace ".sandbox-game-worker$Worker"
if (Test-Path -LiteralPath $destination) { throw "Worker directory already exists: $destination" }
foreach ($relative in @('MonsterTrain2.exe', 'MonsterTrain2_Data', 'MonoBleedingEdge')) {
    if (-not (Test-Path -LiteralPath (Join-Path $gameSource $relative))) { throw "Incomplete game source: $relative" }
}
if (-not (Test-Path -LiteralPath (Join-Path $template 'BepInEx/core/BepInEx.dll'))) { throw 'Incomplete isolated loader template.' }
# Full private copies avoid any shared writable loader/configuration/game paths.
# Logs and runtime caches are regenerated; the running template is never changed.
$items = @(Get-ChildItem -LiteralPath $gameSource -File -Force)
foreach ($directory in @('MonsterTrain2_Data', 'MonoBleedingEdge')) {
    $items += Get-Item -LiteralPath (Join-Path $gameSource $directory)
}
$loader = Join-Path $template 'BepInEx'
$loaderItems = @(Get-ChildItem -LiteralPath $loader -Directory -Force |
    Where-Object Name -In @('core', 'patchers'))
foreach ($item in @($items) + @($loaderItems)) {
    if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint -or
        $item.PSIsContainer -and @(Get-ChildItem -LiteralPath $item.FullName -Force -Recurse |
            Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }).Count) {
        throw "Worker template contains a shared/reparse path: $($item.FullName)"
    }
}
New-Item -ItemType Directory -Path $destination | Out-Null
foreach ($item in $items) { Copy-Item -LiteralPath $item.FullName -Destination $destination -Recurse }
$destinationLoader = Join-Path $destination 'BepInEx'
New-Item -ItemType Directory -Path $destinationLoader | Out-Null
foreach ($item in $loaderItems) { Copy-Item -LiteralPath $item.FullName -Destination $destinationLoader -Recurse }
foreach ($directory in @('plugins', 'config')) {
    New-Item -ItemType Directory -Path (Join-Path $destinationLoader $directory) | Out-Null
}
Copy-Item -LiteralPath (Join-Path $loader 'config/BepInEx.cfg') -Destination (Join-Path $destinationLoader 'config/BepInEx.cfg')
foreach ($relative in @('MonsterTrain2.exe', 'UnityPlayer.dll', 'MonsterTrain2_Data/Managed/Assembly-CSharp.dll')) {
    if ((Get-FileHash -LiteralPath (Join-Path $gameSource $relative)).Hash -ne
        (Get-FileHash -LiteralPath (Join-Path $destination $relative)).Hash) {
        throw "Worker copy differs: $relative"
    }
}
foreach ($relative in @('winhttp.dll', 'doorstop_config.ini', 'steam_appid.txt')) {
    Copy-Item -LiteralPath (Join-Path $template $relative) -Destination (Join-Path $destination $relative) -Force
    if ((Get-FileHash -LiteralPath (Join-Path $template $relative)).Hash -ne
        (Get-FileHash -LiteralPath (Join-Path $destination $relative)).Hash) { throw "Worker loader copy differs: $relative" }
}
if ((Get-FileHash -LiteralPath (Join-Path $template 'BepInEx/core/BepInEx.dll')).Hash -ne
    (Get-FileHash -LiteralPath (Join-Path $destination 'BepInEx/core/BepInEx.dll')).Hash) { throw 'Worker loader copy differs.' }
Write-Output "NATIVE-WORKER-READY worker=$Worker root=$destination gameSource=$gameSource; private files/config/plugins, existing workers unchanged."
