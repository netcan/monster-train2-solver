#requires -Version 7.4
[CmdletBinding()]
param([Parameter(Mandatory)] [string] $Package)
$ErrorActionPreference = 'Stop'
$archive = [IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $Package))
try {
    $expected = @('BepInEx/plugins/PojuAdvisor/MonsterTrain2Poju.Advisor.dll',
        'BepInEx/plugins/PojuAdvisor/Model.dll', 'BepInEx/plugins/PojuAdvisor/FixtureArchive.dll',
        'runtime/BepInEx_macos_universal_5.4.23.5.zip', 'install.command', 'Start-Poju.command', 'poju-common.sh',
        'README-中文.md', 'THIRD-PARTY-NOTICES.md', 'licenses/BepInEx-LICENSE.txt', 'BUILD.json', 'SHA256SUMS')
    if (Compare-Object $expected @($archive.Entries.FullName)) { throw 'Unexpected or missing package contents.' }
    if (@($archive.Entries.FullName | Sort-Object -Unique).Count -ne $expected.Count) { throw 'Duplicate ZIP entries.' }
    function Read-Entry([string] $Name) {
        $reader = [IO.StreamReader]::new($archive.GetEntry($Name).Open())
        try { $reader.ReadToEnd() } finally { $reader.Dispose() }
    }
    $checksums = @((Read-Entry 'SHA256SUMS') -split "`n" | Where-Object { $_ })
    if ($checksums.Count -ne $expected.Count - 1) { throw 'Incomplete checksums.' }
    $checked = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($line in $checksums) {
        if ($line -notmatch '^([0-9a-f]{64})  (.+)$') { throw 'Invalid checksum line.' }
        $hash = $Matches[1]; $name = $Matches[2]
        if (-not $checked.Add($name) -or $name -eq 'SHA256SUMS') { throw 'Invalid checksum coverage.' }
        $entry = $archive.GetEntry($name)
        if (-not $entry) { throw "Checksum references absent entry: $name" }
        $stream = $entry.Open()
        try { $actual = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($stream)).ToLowerInvariant() }
        finally { $stream.Dispose() }
        if ($actual -cne $hash) { throw "Payload checksum differs: $name" }
    }
    foreach ($name in @('install.command', 'Start-Poju.command', 'poju-common.sh')) {
        $script = Read-Entry $name
        if (-not $script.StartsWith("#!/bin/bash`n") -or $script.Contains("`r")) { throw "Invalid shell encoding/line endings: $name" }
    }
    $stream = $archive.GetEntry('BepInEx/plugins/PojuAdvisor/MonsterTrain2Poju.Advisor.dll').Open()
    $bytes = [IO.MemoryStream]::new()
    try { $stream.CopyTo($bytes) } finally { $stream.Dispose() }
    $bytes.Position = 0
    $pe = [Reflection.PortableExecutable.PEReader]::new($bytes)
    try {
        if (-not $pe.HasMetadata -or ($pe.PEHeaders.CorHeader.Flags -band 1) -eq 0 -or
            ($pe.PEHeaders.CorHeader.Flags -band 2) -ne 0 -or
            $pe.PEHeaders.CoffHeader.Machine.ToString() -ne 'I386') { throw 'Expected portable AnyCPU IL assembly.' }
        $metadata = [Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($pe)
        $types = @($metadata.TypeDefinitions | ForEach-Object { $metadata.GetString($metadata.GetTypeDefinition($_).Name) })
        if ($types -notcontains 'AdvisorPlugin' -or $types -contains 'ProbePlugin' -or $types -contains 'NativeReplayScenario' -or
            $types -contains 'PlayerAdvisorProbe') {
            throw 'Player assembly includes a missing or development entry point.'
        }
    } finally { $pe.Dispose(); $bytes.Dispose() }
    $build = Read-Entry 'BUILD.json' | ConvertFrom-Json
    if ($build.sourceDirty) { throw 'Release package was produced from a dirty source tree.' }
    if ($build.macOSAcceptance -notlike 'Not run*') { throw 'Mac acceptance claim must remain explicit until verified.' }
    "ADVISOR-PACKAGE PASS: $($expected.Count) files, complete SHA256 coverage, LF scripts, AnyCPU IL, player entry point, clean source $($build.sourceRevision)."
} finally { $archive.Dispose() }
