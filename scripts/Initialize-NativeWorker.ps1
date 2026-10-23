#requires -Version 7.4
[CmdletBinding()]
param([Parameter(Mandatory)][ValidateRange(2, 4)][int] $Worker)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$workspace = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$template = Join-Path $workspace '.sandbox-game'
$destination = Join-Path $workspace ".sandbox-game-worker$Worker"
if (Test-Path -LiteralPath $destination) { throw "Worker directory already exists: $destination" }
foreach ($relative in @('MonsterTrain2.exe', 'MonsterTrain2_Data', 'MonoBleedingEdge', 'BepInEx/core/BepInEx.dll')) {
    if (-not (Test-Path -LiteralPath (Join-Path $template $relative))) { throw "Incomplete isolated template: $relative" }
}
# Full private copies avoid any shared writable loader/configuration/game paths.
# Logs and runtime caches are regenerated; the running template is never changed.
$items = @(Get-ChildItem -LiteralPath $template -File -Force)
foreach ($directory in @('MonsterTrain2_Data', 'MonoBleedingEdge')) {
    $items += Get-Item -LiteralPath (Join-Path $template $directory)
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
foreach ($relative in @('MonsterTrain2.exe', 'UnityPlayer.dll', 'winhttp.dll', 'BepInEx/core/BepInEx.dll')) {
    if ((Get-FileHash -LiteralPath (Join-Path $template $relative)).Hash -ne
        (Get-FileHash -LiteralPath (Join-Path $destination $relative)).Hash) {
        throw "Worker copy differs: $relative"
    }
}
Write-Output "NATIVE-WORKER-READY worker=$Worker root=$destination; private files/config/plugins, existing workers unchanged."
