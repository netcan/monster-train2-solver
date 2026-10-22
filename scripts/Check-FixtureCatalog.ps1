#requires -Version 7.4
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
$directory = Join-Path $workspace 'tests/fixtures'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if (@($manifest.archive | Select-Object -Unique).Count -ne $manifest.Count) {
    throw 'Duplicate fixture manifest rows.'
}
$tokens = $null; $errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile(
    (Join-Path $PSScriptRoot 'Check-Models.ps1'), [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw 'Check-Models.ps1 contains parse errors.' }
$assignment = $ast.Find({ param($node)
    $node -is [Management.Automation.Language.AssignmentStatementAst] -and
    $node.Left -is [Management.Automation.Language.VariableExpressionAst] -and
    $node.Left.VariablePath.UserPath -eq 'fixtureNames'
}, $true)
if ($null -eq $assignment) { throw 'Curated fixture registration was not found.' }
$registered = @($assignment.Right.FindAll({ param($node)
    $node -is [Management.Automation.Language.StringConstantExpressionAst]
}, $true) | ForEach-Object Value)
if ($registered.Count -ne $manifest.Count -or
    @($registered | Select-Object -Unique).Count -ne $registered.Count -or
    @($registered | Where-Object { $_ -notin $manifest.archive }).Count) {
    throw 'Fixture manifest and curated registrations differ or contain duplicates.'
}
foreach ($row in $manifest) {
    $path = Join-Path $directory $row.archive
    if (-not (Test-Path -LiteralPath $path -PathType Leaf) -or
        (Get-Item -LiteralPath $path).Length -ne [long]$row.archive_bytes -or
        (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $row.archive_sha256) {
        throw "Fixture bytes/hash mismatch: $($row.archive)"
    }
}
if (@(Get-ChildItem -LiteralPath $directory -File -Filter '*.mt2f' |
    Where-Object Name -NotIn $manifest.archive).Count) { throw 'Unregistered fixture archives.' }
Write-Output "FIXTURE-CATALOG PASS: $($manifest.Count) archive hashes/registrations; no battle simulations."
