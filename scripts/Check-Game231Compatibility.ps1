#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild)

$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1'
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
Add-Type -Path (Join-Path $workspace 'src/FixtureArchive/bin/Release/net8.0/FixtureArchive.dll')
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
$versions = @(
    @{ Version='2.2.1'; Mvid='8fb07b96-f4db-4d2b-884d-c00536d6ccf4'; Cards=668; Logbook=563; Relics=224 },
    @{ Version='2.3.1'; Mvid='bf93f060-beda-46b1-b808-95ebf1e9a99d'; Cards=724; Logbook=614; Relics=236 }
)
$names = @('card-catalog', 'relic-catalog', 'rule-catalog', 'full-battle')
$expectedPaths = @($versions | ForEach-Object { $version = $_.Version; $names | ForEach-Object { "$version/$_.mt2f" } })
if ($manifest.Count -ne $expectedPaths.Count -or
    @($manifest.path | Select-Object -Unique).Count -ne $expectedPaths.Count -or
    @($manifest | Where-Object { $_.path -notin $expectedPaths }).Count) {
    throw 'Versioned compatibility manifest differs from the eight registered captures.'
}
foreach ($row in $manifest) {
    $path = Join-Path $directory $row.path
    if ((Get-Item -LiteralPath $path).Length -ne [long]$row.bytes -or
        (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $row.sha256) {
        throw "Compatibility capture hash/size differs: $($row.path)"
    }
}

# Retain documents until the comparisons finish: FixtureValues reference their owner.
$documents = [Collections.Generic.List[IDisposable]]::new()
$catalogs = @{}
try {
    foreach ($version in $versions) {
        $roots = @{}
        foreach ($name in $names) {
            $document = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read((Join-Path $directory "$($version.Version)/$name.mt2f"))
            $documents.Add($document)
            $root = $document.RootElement
            if ($root.GetProperty('GameModuleMvid').GetString() -ne $version.Mvid -or
                ($name -ne 'rule-catalog' -and $root.GetProperty('GameVersion').GetString() -ne $version.Version)) {
                throw "Wrong native version/module: $($version.Version)/$name"
            }
            $roots[$name] = $root
        }
        $cards = $roots['card-catalog']
        $entries = @($cards.GetProperty('Cards').EnumerateArray())
        $cardIds = @($entries | ForEach-Object { $_.GetProperty('Id').GetString() })
        if ($entries.Count -ne $version.Cards -or
            $cards.GetProperty('NativeDefinitionCount').GetInt32() -ne $version.Cards -or
            @($cardIds | Where-Object { -not $_ }).Count -or
            @($cardIds | Select-Object -Unique).Count -ne $version.Cards -or
            @($entries | Where-Object { $_.GetProperty('AppearsInLogbook').GetBoolean() }).Count -ne $version.Logbook -or
            @($entries | Where-Object { $_.GetProperty('ProjectionError').ValueKind -ne 'Null' }).Count -or
            -not $cards.GetProperty('DecisionUnchanged').GetBoolean()) {
            throw "Invalid native card catalog: $($version.Version)"
        }
        foreach ($catalog in @($cards, $roots['relic-catalog'])) {
            if ($catalog.GetProperty('FrameBefore').GetInt32() -ne $catalog.GetProperty('FrameAfter').GetInt32() -or
                -not $catalog.GetProperty('RngBefore').ContentEquals($catalog.GetProperty('RngAfter'))) {
                throw "Catalog inspection changed native frame/RNG: $($version.Version)"
            }
        }
        $relics = @($roots['relic-catalog'].GetProperty('Relics').EnumerateArray())
        $collectable = @($relics | Where-Object { $_.GetProperty('Collection').GetString() -eq 'collectableRelicDatas' })
        if ($collectable.Count -ne $version.Relics -or
            @($relics | ForEach-Object { $_.GetProperty('Collection').GetString() } | Select-Object -Unique).Count -ne 8 -or
            @($collectable | ForEach-Object { $_.GetProperty('Id').GetString() } | Select-Object -Unique).Count -ne $version.Relics) {
            throw "Invalid native relic catalog: $($version.Version)"
        }
        $battle = $roots['full-battle']
        if (-not $battle.GetProperty('NativeWon').GetBoolean() -or
            $battle.GetProperty('Actions').GetArrayLength() -ne 0 -or
            $battle.GetProperty('Turns').GetArrayLength() -ne 7) {
            throw "Incomplete seven-turn no-cards native victory: $($version.Version)"
        }
        foreach ($counter in @('CaptureFailures', 'Mismatches', 'Unsupported', 'Pending')) {
            if ($battle.GetProperty($counter).GetInt32() -ne 0) { throw "Nonzero native counter: $($version.Version)/$counter" }
        }
        $catalogs[$version.Version] = @{ Cards=$entries; Relics=$collectable }
        Write-Output "GAME-COMPATIBILITY-CAPTURE PASS: $($version.Version), $($version.Cards) cards / $($version.Logbook) logbook / $($version.Relics) collectable relics; native victory and four zero counters."
    }

    function Compare-Definitions($oldEntries, $newEntries, [string[]] $fields) {
        $oldById = @{}; $newById = @{}
        foreach ($entry in $oldEntries) { $oldById[$entry.GetProperty('Id').GetString()] = $entry }
        foreach ($entry in $newEntries) { $newById[$entry.GetProperty('Id').GetString()] = $entry }
        $added = @($newEntries | Where-Object { -not $oldById.ContainsKey($_.GetProperty('Id').GetString()) })
        $removed = @($oldEntries | Where-Object { -not $newById.ContainsKey($_.GetProperty('Id').GetString()) })
        $changed = @($newEntries | Where-Object {
            $entry = $_; $id = $entry.GetProperty('Id').GetString()
            $oldById.ContainsKey($id) -and @($fields | Where-Object {
                -not $entry.GetProperty($_).ContentEquals($oldById[$id].GetProperty($_))
            }).Count -gt 0
        })
        return @{ Added=$added; Removed=$removed; Changed=$changed }
    }
    $old = $catalogs['2.2.1']; $new = $catalogs['2.3.1']
    $cards = Compare-Definitions $old.Cards $new.Cards @('Effects','Traits','Triggers')
    $rules = Compare-Definitions $old.Cards $new.Cards @('Rule')
    $relics = Compare-Definitions $old.Relics $new.Relics @('Effects','DisallowedInPlacementPhase')
    if ($cards.Added.Count -ne 65 -or $cards.Removed.Count -ne 9 -or $cards.Changed.Count -ne 10 -or
        $rules.Changed.Count -ne 15 -or $relics.Added.Count -ne 12 -or $relics.Removed.Count -ne 0 -or $relics.Changed.Count -ne 13) {
        throw 'Native definition delta differs from the reviewed migration scope.'
    }
    Write-Output 'GAME-COMPATIBILITY-DELTA PASS: card IDs +65/-9, 10 changed effect/trait/trigger payloads, 15 changed Rule projections; collectable relic IDs +12/-0, 13 changed definitions.'
} finally {
    foreach ($document in $documents) { $document.Dispose() }
}

$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
foreach ($version in $versions) {
    dotnet $checker --recorded-chain-diagnose (Join-Path $directory "$($version.Version)/full-battle.mt2f")
    if ($LASTEXITCODE) { throw "Independent model chain failed: $($version.Version)" }
}
$currentVersion = $versions | Where-Object Version -eq '2.3.1'
dotnet $checker --native-equipment-selection-only (Join-Path $directory 'mechanics/red-crown-equipment-observation/native-branch.mt2f') $currentVersion.Mvid
if ($LASTEXITCODE) { throw 'Native equipment observation failed.' }
dotnet $checker --native-red-crown-equipment (Join-Path $directory 'mechanics/red-crown-equipment-observation/native-supported-branch.mt2f')
if ($LASTEXITCODE) { throw 'Native equipment branch did not replay.' }
Write-Output 'GAME-231-COMPATIBILITY PASS: eight binary capture hashes, versioned catalogs, both seven-turn continuous model chains and 16 immutable parallel branches each. New/changed content is not accepted by this smoke check.'
