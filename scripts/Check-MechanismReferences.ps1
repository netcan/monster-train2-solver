#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
# Validates pinned catalog bytes/version, native MVID, and the complete generated table.
& (Join-Path $PSScriptRoot 'Get-MechanismInventory.ps1') -SkipBuild:$SkipBuild -Check
$rows = Import-Csv (Join-Path $workspace 'docs/inventory/game-2.3.1-mechanisms.tsv') -Delimiter "`t"
$byClass = @{}
foreach ($row in $rows) {
    if ($byClass.ContainsKey($row.NativeClass)) { throw "Duplicate native class: $($row.NativeClass)" }
    $byClass[$row.NativeClass] = $row
    foreach ($role in @('DefinitionOwners','SelectorOwners','ParameterOwners','DescriptorOwners','OtherOwners')) {
        if ([int]$row.$role -lt 0 -or [int]$row.$role -gt [int]$row.CatalogOwners) { throw "Invalid $role for $($row.NativeClass)" }
    }
    if ([int]$row.CatalogOwners -gt 0 -and [int]$row.DefinitionOwners -eq 0 -and
        [int]$row.SelectorOwners + [int]$row.ParameterOwners + [int]$row.DescriptorOwners + [int]$row.OtherOwners -eq 0) {
        throw "Lost catalog reference: $($row.NativeClass)"
    }
}
# Frozen native catalog counterexamples: mask-only names and generic trait defaults
# previously appeared ahead of actual definitions in the implementation work queue.
$expected = @{
    CardEffectAddStatusEffectNextMonster = @(5, 5, 0)
    CardEffectDamageByPercentRemainingHealth = @(4, 4, 0)
    CardEffectDamagePerTargetAttack = @(4, 4, 0)
    CardEffectDamageWithTrample = @(4, 4, 0)
    CardTraitAddMagicPowerToAttackDamage = @(5, 0, 5)
    CardTraitAllowUpgradeOnCardType = @(2, 0, 2)
    CardTraitFreeze = @(1, 1, 0)
}
$referenceOnly = @($rows | Where-Object { [int]$_.CatalogOwners -gt 0 -and [int]$_.DefinitionOwners -eq 0 })
if ($referenceOnly.Count -ne $expected.Count) { throw 'Pinned reference-only class set changed.' }
foreach ($name in $expected.Keys) {
    $row = $byClass[$name]; $counts = $expected[$name]
    if ($null -eq $row -or [int]$row.DefinitionOwners -ne 0 -or [int]$row.CatalogOwners -ne $counts[0] -or
        [int]$row.SelectorOwners -ne $counts[1] -or [int]$row.ParameterOwners -ne $counts[2] -or
        [int]$row.DescriptorOwners -ne 0 -or [int]$row.OtherOwners -ne 0 -or
        [int]$row.DefinitionConfigurations -ne 0 -or $row.DefinitionLocations.Length -ne 0) {
        throw "Native reference-role counterexample differs: $name"
    }
}
# Positive controls retain declarations even when the same class also appears in masks.
foreach ($name in @('CardEffectDamage','CardEffectAddStatusEffect','CardTraitJuice4','RelicEffectCardUpgrade')) {
    $row = $byClass[$name]
    if ([int]$row.DefinitionOwners -le 0 -or [int]$row.DefinitionConfigurations -le 0 -or
        $row.DefinitionLocations.Length -eq 0) { throw "Configured type declaration lost: $name" }
}
if ([int]$byClass.CardEffectDamage.SelectorOwners -le 0) { throw 'Mixed declaration/selector role was lost.' }
Write-Output 'MECHANISM-REFERENCES PASS: seven pinned reference-only classes, four configured positive controls, mixed roles and all owner-count bounds; no runtime-support inference.'
