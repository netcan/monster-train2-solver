#requires -Version 7.4
[CmdletBinding(DefaultParameterSetName = 'Export')]
param(
    [Parameter(Mandatory, ParameterSetName = 'Export')] [string] $Profile,
    [Parameter(Mandatory)] [string] $Archive,
    [Parameter(Mandatory, ParameterSetName = 'Restore')] [switch] $Restore
)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
$profilesRoot = [IO.Path]::GetFullPath((Join-Path $workspace '.probe-runs')).TrimEnd('\')
$Archive = [IO.Path]::GetFullPath($Archive)
function Get-SHA256([byte[]] $Bytes) { [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($Bytes)) }
if ($Restore) {
    $zip = [IO.Compression.ZipFile]::OpenRead($Archive)
    try {
        $entry = $zip.GetEntry('checkpoint-manifest.json')
        if ($null -eq $entry) { throw 'Archive has no checkpoint manifest.' }
        $reader = [IO.StreamReader]::new($entry.Open())
        try { $manifest = $reader.ReadToEnd() | ConvertFrom-Json } finally { $reader.Dispose() }
        if ($manifest.Schema -ne 1) { throw 'Unknown campaign checkpoint schema.' }
        $files = @{}
        foreach ($file in $manifest.Files) {
            if ($file.Path -notmatch '^(saves/[A-Za-z0-9._-]+|[A-Za-z0-9._-]+)$') { throw 'Unsafe checkpoint member path.' }
            $entry = $zip.GetEntry($file.Path)
            if ($null -eq $entry -or $entry.Length -ne $file.Bytes) { throw "Missing or altered checkpoint member: $($file.Path)" }
            $stream = $entry.Open(); $buffer = [IO.MemoryStream]::new()
            try { $stream.CopyTo($buffer); $bytes = $buffer.ToArray() } finally { $stream.Dispose(); $buffer.Dispose() }
            if ((Get-SHA256 $bytes) -cne $file.SHA256) { throw "Checkpoint hash mismatch: $($file.Path)" }
            $files[$file.Path] = $bytes
        }
        $destination = Join-Path $profilesRoot ('restored-campaign-ring' + $manifest.Ring + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
        $null = New-Item -ItemType Directory -Path (Join-Path $destination 'saves')
        foreach ($path in $files.Keys) { [IO.File]::WriteAllBytes((Join-Path $destination $path), $files[$path]) }
        [IO.File]::WriteAllText((Join-Path $destination 'checkpoint-manifest.json'), ($manifest | ConvertTo-Json -Depth 8))
        [pscustomobject]@{ Profile = $destination; Ring = $manifest.Ring; Seed = $manifest.Seed; FilesVerified = $files.Count } | ConvertTo-Json -Compress
    } finally { $zip.Dispose() }
    return
}
$Profile = (Resolve-Path -LiteralPath $Profile).Path.TrimEnd('\')
if ((Split-Path $Profile -Parent) -ine $profilesRoot) { throw 'Profile must be a direct child of .probe-runs.' }
$savePath = Join-Path $Profile 'saves/save-singlePlayer.json'
$save = Get-Content -LiteralPath $savePath -Raw | ConvertFrom-Json
$distance = [int]$save.runState.distance
if ($save.gameSequence -ne 6 -or $save.wasDevCheatUsed -or $distance -lt 1) { throw 'Expected an unmodified progressed native BattleIntro save.' }
$completed = @($save.completedBattles | Sort-Object battleIndex)
if ($completed.Count -ne $distance -or @($completed | Where-Object bossHPRemaining -GT 0).Count -gt 0 -or
    (($completed.battleIndex -join ',') -ne ((0..($distance - 1)) -join ','))) { throw 'The native save does not contain every preceding battle victory.' }
$members = [ordered]@{}
foreach ($name in @('saves/save-singlePlayer.json', 'metagameSave.json', 'preferences.json', 'preferences-local.json')) {
    $members[$name] = [IO.File]::ReadAllBytes((Join-Path $Profile $name))
}
# Bundle the retained native choice history and each predecessor's input hash.
$cursor = $Profile; $visited = @{}; $depth = 0
while ((Split-Path $cursor -Parent) -ieq $profilesRoot -and -not $visited.ContainsKey($cursor)) {
    $visited[$cursor] = $true
    foreach ($name in @('campaign-audit.jsonl', 'campaign-parent-audit.jsonl', 'campaign-input.json', 'native-checkpoint-input.json')) {
        $path = Join-Path $cursor $name
        $memberName = if ($depth -eq 0) { $name } else { "history-$depth-$name" }
        if (Test-Path -LiteralPath $path) { $members[$memberName] = [IO.File]::ReadAllBytes($path) }
    }
    foreach ($history in Get-ChildItem -LiteralPath $cursor -File -Filter 'history-*') {
        $members["history-$depth-$($history.Name)"] = [IO.File]::ReadAllBytes($history.FullName)
    }
    $inputPath = Join-Path $cursor 'native-checkpoint-input.json'
    if (-not (Test-Path -LiteralPath $inputPath)) { $inputPath = Join-Path $cursor 'campaign-input.json' }
    if (-not (Test-Path -LiteralPath $inputPath)) { break }
    $cursor = (Get-Content -LiteralPath $inputPath -Raw | ConvertFrom-Json).InputProfile
    if (-not $cursor) { break }
    $depth++
}
if (@($members.Keys | Where-Object { $_ -like '*campaign-audit.jsonl' }).Count -eq 0) { throw 'Missing native campaign choice history.' }
$manifest = [ordered]@{ Schema = 1; Ring = $distance + 1; Seed = $save.startingConditions.seed;
    CompletedBattles = $completed.Count; DeckCards = $save.deckState.Count; Relics = $save.blessings.Count;
    Files = @($members.Keys | ForEach-Object { [pscustomobject]@{ Path = $_; Bytes = $members[$_].Length; SHA256 = Get-SHA256 $members[$_] } }) }
$members['checkpoint-manifest.json'] = [Text.Encoding]::UTF8.GetBytes(($manifest | ConvertTo-Json -Depth 8))
$zip = [IO.Compression.ZipFile]::Open($Archive, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($name in $members.Keys) {
        $stream = $zip.CreateEntry($name, [IO.Compression.CompressionLevel]::Optimal).Open()
        try { $stream.Write($members[$name]) } finally { $stream.Dispose() }
    }
} finally { $zip.Dispose() }
[pscustomobject]@{ Archive = $Archive; SHA256 = (Get-FileHash -LiteralPath $Archive -Algorithm SHA256).Hash;
    Ring = $manifest.Ring; CompletedBattles = $completed.Count; Files = $members.Count } | ConvertTo-Json -Compress
