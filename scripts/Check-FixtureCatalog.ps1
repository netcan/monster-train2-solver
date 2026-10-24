#requires -Version 7.4
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
$directory = Join-Path $workspace 'tests/fixtures'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if (@($manifest.archive | Select-Object -Unique).Count -ne $manifest.Count -or
    @($manifest.path | Select-Object -Unique).Count -ne $manifest.Count) {
    throw 'Duplicate fixture manifest names or paths.'
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
$expectedPaths = @{}
foreach ($row in $manifest) {
    if (-not $row.path -or $row.path.Contains('\') -or
        @($row.path.Split('/') | Where-Object { $_ -in @('', '.', '..') }).Count -or
        [IO.Path]::IsPathRooted($row.path) -or (Split-Path $row.path -Leaf) -cne $row.archive) {
        throw "Invalid fixture relative path: $($row.archive)"
    }
    $path = [IO.Path]::GetFullPath((Join-Path $directory $row.path))
    if (-not $path.StartsWith($directory + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Fixture path escapes its root: $($row.archive)"
    }
    $expectedPaths[$path] = $true
    if (-not (Test-Path -LiteralPath $path -PathType Leaf) -or
        (Get-Item -LiteralPath $path).Length -ne [long]$row.archive_bytes -or
        (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $row.archive_sha256) {
        throw "Fixture bytes/hash mismatch: $($row.archive)"
    }
}
if (@(Get-ChildItem -LiteralPath $directory -Recurse -File -Filter '*.mt2f' |
    Where-Object { -not $expectedPaths.ContainsKey($_.FullName) }).Count) { throw 'Unregistered fixture archive locations.' }
Write-Output "FIXTURE-CATALOG PASS: $($manifest.Count) archive hashes/registrations; no battle simulations."
