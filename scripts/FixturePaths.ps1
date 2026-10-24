# Shared curated fixture lookup. Archive names are stable IDs; the manifest
# owns their locations so checker pairs can span feature subdirectories.
$script:TestFixtureRoot = Join-Path (Split-Path $PSScriptRoot -Parent) 'tests/fixtures'
$script:TestFixturePaths = $null

function Get-TestFixturePath {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string] $Name)
    if ($null -eq $script:TestFixturePaths) {
        $script:TestFixturePaths = @{}
        foreach ($row in Import-Csv -LiteralPath (Join-Path $script:TestFixtureRoot 'manifest.tsv') -Delimiter "`t") {
            if ($script:TestFixturePaths.ContainsKey($row.archive)) { throw "Duplicate fixture: $($row.archive)" }
            $script:TestFixturePaths[$row.archive] = Join-Path $script:TestFixtureRoot $row.path
        }
    }
    if (-not $script:TestFixturePaths.ContainsKey($Name)) { throw "Unknown curated fixture: $Name" }
    $script:TestFixturePaths[$Name]
}
