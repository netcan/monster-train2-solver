#requires -Version 7.4
param(
    [Parameter(Mandatory)][string]$Catalog,
    [string]$OutputPath
)
$ErrorActionPreference = 'Stop'
$workspace = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Add-Type -Path (Join-Path $workspace 'src/FixtureArchive/bin/Release/net8.0/FixtureArchive.dll')
$accepted = @{}
$evidenceKind = 'accepted-native-battle'
foreach ($line in Get-Content (Join-Path $workspace 'tests/fixtures/inventory/relic-coverage.md')) {
    if ($line -match '^## ') {
        $evidenceKind = switch ($line) {
            '## Campaign interface evidence' { 'accepted-native-campaign' }
            '## Native no-op evidence' { 'accepted-native-noop' }
            default { 'accepted-native-battle' }
        }
    }
    if ($line -match '^\| `(?<key>[^`]+)` \| `(?<id>[0-9a-f-]{36})` \|') {
        if ($accepted.ContainsKey($Matches.id)) {
            $existing = $accepted[$Matches.id]
            if ($existing.Key -cne $Matches.key -or $existing.Kind -cne $evidenceKind) {
                throw "Conflicting accepted owner record $($Matches.id)"
            }
            continue
        }
        $accepted[$Matches.id] = [pscustomobject]@{ Key = $Matches.key; Kind = $evidenceKind }
    }
}
$document = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read((Resolve-Path -LiteralPath $Catalog).Path)
try {
    $root = $document.RootElement
    if ($root.GetProperty('Schema').GetInt32() -ne 9 -or
        $root.GetProperty('GameModuleMvid').GetString() -ne '8fb07b96-f4db-4d2b-884d-c00536d6ccf4' -or
        $root.GetProperty('FrameBefore').GetInt32() -ne $root.GetProperty('FrameAfter').GetInt32() -or
        -not $root.GetProperty('RngBefore').ContentEquals($root.GetProperty('RngAfter'))) {
        throw 'Expected the complete definition catalog from the validated game build, with unchanged frame/RNG.'
    }
    $relics = @($root.GetProperty('Relics').ToObjectGraph() | Where-Object Collection -eq 'collectableRelicDatas')
    if ($relics.Count -ne 224 -or @($relics.Id | Sort-Object -Unique).Count -ne 224) {
        throw 'Original collectible catalog must contain exactly 224 unique owners.'
    }
    foreach ($id in $accepted.Keys) {
        if (@($relics | Where-Object { $_.Id -eq $id -and $_.AssetKey -ceq $accepted[$id].Key }).Count -ne 1) {
            throw "Accepted owner identity is absent from the original catalog: $id"
        }
    }
    $rows = @(foreach ($relic in $relics | Sort-Object AssetKey) {
        [pscustomobject][ordered]@{
            AssetKey = $relic.AssetKey
            DataId = $relic.Id
            Evidence = if ($accepted.ContainsKey($relic.Id)) { $accepted[$relic.Id].Kind } else { 'pending' }
            Effects = $relic.Effects.ClassName -join ','
            MissingNativeSource = @($relic.Effects.ClassName | Where-Object {
                -not (Test-Path -LiteralPath (Join-Path $workspace ".game-reference/native-source/$_.cs"))
            }) -join ','
        }
    })
    if ($OutputPath) {
        $rows | ConvertTo-Csv -Delimiter "`t" -NoTypeInformation -UseQuotes Always |
            Set-Content -LiteralPath $OutputPath -Encoding utf8NoBOM
    }
    [pscustomobject]@{
        CatalogEntries = $rows.Count
        AcceptedOriginal = $accepted.Count
        AcceptedNativeBattle = @($accepted.Values | Where-Object Kind -eq 'accepted-native-battle').Count
        AcceptedNativeCampaign = @($accepted.Values | Where-Object Kind -eq 'accepted-native-campaign').Count
        AcceptedNativeNoop = @($accepted.Values | Where-Object Kind -eq 'accepted-native-noop').Count
        Remaining = @($rows | Where-Object Evidence -eq 'pending').Count
        MissingNativeSource = @($rows | Where-Object MissingNativeSource -ne '').Count
        OutputPath = $OutputPath
    }
} finally { $document.Dispose() }
