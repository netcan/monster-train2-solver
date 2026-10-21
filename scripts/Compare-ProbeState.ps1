#requires -Version 7.4
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $Archive,
    [ValidateSet('Actions', 'Stages', 'Turns', 'TrainPhases', 'Spawns', 'CardCycles', 'RollingDecisions')]
    [string] $Collection = 'Turns',
    [ValidateRange(-1, 2147483647)][int] $Index = -1,
    [ValidateRange(1, 1000)][int] $MaxDifferences = 30
)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
Add-Type -Path (Join-Path $workspace 'src/FixtureArchive/bin/Release/net8.0/FixtureArchive.dll')
function Compare-Value($left, $right, [string] $path) {
    if ($null -eq $left -or $null -eq $right) {
        if (($null -eq $left) -ne ($null -eq $right)) { "${path}: model=$left native=$right" }
        return
    }
    if ($left -is [System.Collections.IDictionary] -and $right -is [System.Collections.IDictionary]) {
        foreach ($key in @(@($left.Keys) + @($right.Keys) | Select-Object -Unique)) {
            Compare-Value $left[$key] $right[$key] "$path.$key"
        }
        return
    }
    if ($left -is [System.Collections.IList] -and $right -is [System.Collections.IList]) {
        if ($left.Count -ne $right.Count) { "${path}: model count=$($left.Count) native count=$($right.Count)" }
        for ($offset = 0; $offset -lt [Math]::Min($left.Count, $right.Count); $offset++) {
            Compare-Value $left[$offset] $right[$offset] "$path[$offset]"
        }
        return
    }
    if ($left -cne $right) { "${path}: model=$left native=$right" }
}
$document = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read((Resolve-Path -LiteralPath $Archive))
try {
    $rows = $document.RootElement.ToObjectGraph()[$Collection]
    if ($null -eq $rows) { throw "Missing collection: $Collection" }
    if ($rows.Count -eq 0) { return }
    if ($Index -ge $rows.Count) { throw "Index exceeds $Collection count $($rows.Count)." }
    $indices = if ($Index -ge 0) { @($Index) } else { @(0..($rows.Count - 1)) }
    foreach ($rowIndex in $indices) {
        $row = $rows[$rowIndex]
        $predicted = if ($Collection -eq 'RollingDecisions') { $row['Expected'] } else { $row['Predicted']['State'] }
        $differences = @(Compare-Value $predicted $row['Actual'] "`$.$Collection[$rowIndex]" | Select-Object -First $MaxDifferences)
        if ($differences.Count -gt 0) { $differences; break }
    }
}
finally { $document.Dispose() }
