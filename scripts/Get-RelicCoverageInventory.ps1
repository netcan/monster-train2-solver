#requires -Version 7.4
param(
    [Parameter(Mandatory)][string]$Catalog,
    [string]$OutputPath
)
$ErrorActionPreference = 'Stop'
$workspace = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Add-Type -Path (Join-Path $workspace 'src/FixtureArchive/bin/Release/net8.0/FixtureArchive.dll')
$accepted = @{}
foreach ($line in Get-Content (Join-Path $workspace 'tests/fixtures/relic-coverage.md')) {
    if ($line -match '^\| `(?<key>[^`]+)` \| `(?<id>[0-9a-f-]{36})` \|') {
        if ($accepted.ContainsKey($Matches.id)) { throw "Duplicate accepted owner $($Matches.id)" }
        $accepted[$Matches.id] = $Matches.key
    }
}
$document = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read((Resolve-Path -LiteralPath $Catalog).Path)
try {
    $root = $document.RootElement
    if ($root.GetProperty('Schema').GetInt32() -ne 5 -or
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
        if (@($relics | Where-Object { $_.Id -eq $id -and $_.AssetKey -ceq $accepted[$id] }).Count -ne 1) {
            throw "Accepted owner identity is absent from the original catalog: $id"
        }
    }
    $rows = @(foreach ($relic in $relics | Sort-Object AssetKey) {
        [pscustomobject][ordered]@{
            AssetKey = $relic.AssetKey
            DataId = $relic.Id
            Evidence = if ($accepted.ContainsKey($relic.Id)) { 'accepted-native-battle' } else { 'pending' }
            Effects = $relic.Effects.ClassName -join ','
            MissingNativeSource = @($relic.Effects.ClassName | Where-Object {
                -not (Test-Path -LiteralPath (Join-Path $workspace ".game-reference/native-source/$_.cs"))
            }) -join ','
        }
    })
    if ($OutputPath) {
        $rows | ConvertTo-Csv -Delimiter "`t" -NoTypeInformation -UseQuotes AsNeeded |
            Set-Content -LiteralPath $OutputPath -Encoding utf8NoBOM
    }
    [pscustomobject]@{
        CatalogEntries = $rows.Count
        AcceptedNativeBattle = $accepted.Count
        Remaining = @($rows | Where-Object Evidence -eq 'pending').Count
        MissingNativeSource = @($rows | Where-Object MissingNativeSource -ne '').Count
        OutputPath = $OutputPath
    }
} finally { $document.Dispose() }
