#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild)

$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
Push-Location $workspace
try {
    if (-not $SkipBuild) {
        foreach ($project in @('ModelChecks', 'FixtureTools')) {
            dotnet build "src/$project/$project.csproj" -c Release --no-restore -p:WarningLevel=0 -v:q
            if ($LASTEXITCODE -ne 0) { throw "$project build failed." }
        }
    }
    $checks = 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
    $tools = 'src/FixtureTools/bin/Release/net8.0/FixtureTools.dll'
    $original = 'tests/fixtures/full-battle-progressed-ring7-holdover-win.mt2f'
    $shortened = 'tests/fixtures/full-battle-progressed-ring7-holdover-exact-replay.mt2f'
    dotnet $checks --holdover-variants-only tests/fixtures/holdover-variants-calibration.mt2f
    if ($LASTEXITCODE -ne 0) { throw 'Native upgrade/clone variant matrix failed.' }
    foreach ($mode in @('--holdover-only', '--starting-duality-only', '--rolling-advice-only')) {
        dotnet $checks $mode $shortened
        if ($LASTEXITCODE -ne 0) { throw "Shortened native victory failed: $mode" }
    }
    foreach ($mode in @('--exact-battle-only', '--belief-battle-only')) {
        dotnet $checks $mode $original
        if ($LASTEXITCODE -ne 0) { throw "Search certificate acceptance failed: $mode" }
    }
    $output = Join-Path '.probe-runs' ('solver-completion-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
    $null = New-Item -ItemType Directory -Path $output
    dotnet $tools solve-exact $original (Join-Path $output 'node-cutoff.mt2f') 1 30000 1
    if ($LASTEXITCODE -ne 0) { throw 'Exact-search CLI export/independent replay failed.' }
    $policy = Join-Path $output 'belief.mt2f'
    dotnet $tools plan-belief $original $policy 8 2 128 30000 7
    if ($LASTEXITCODE -ne 0) { throw 'Probability-policy CLI export failed.' }
    dotnet $tools verify-belief $policy
    if ($LASTEXITCODE -ne 0) { throw 'Fresh-process policy verification failed.' }

    # Byte/hash/registration checks inspect metadata, not simulated regressions.
    $manifest = @(Import-Csv tests/fixtures/manifest.tsv -Delimiter "`t")
    if (@($manifest.archive | Select-Object -Unique).Count -ne $manifest.Count) { throw 'Duplicate fixture manifest rows.' }
    $taskTokens = $null; $taskErrors = $null
    $ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $workspace 'scripts/Check-Models.ps1'), [ref]$taskTokens, [ref]$taskErrors)
    if ($taskErrors.Count -gt 0) { throw 'Check-Models.ps1 contains parse errors.' }
    $assignment = $ast.Find({ param($node) $node -is [Management.Automation.Language.AssignmentStatementAst] -and
        $node.Left -is [Management.Automation.Language.VariableExpressionAst] -and $node.Left.VariablePath.UserPath -eq 'fixtureNames' }, $true)
    $registered = @($assignment.Right.FindAll({ param($node) $node -is [Management.Automation.Language.StringConstantExpressionAst] }, $true) | ForEach-Object Value)
    foreach ($row in $manifest) {
        $path = Join-Path 'tests/fixtures' $row.archive
        if (-not (Test-Path -LiteralPath $path) -or (Get-Item -LiteralPath $path).Length -ne [long]$row.archive_bytes -or
            (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $row.archive_sha256 -or $row.archive -notin $registered) {
            throw "Fixture bytes/hash/registration mismatch: $($row.archive)"
        }
    }
    if (@(Get-ChildItem tests/fixtures -File -Filter '*.mt2f' | Where-Object Name -NotIn $manifest.archive).Count -gt 0) {
        throw 'Unregistered fixture archives.'
    }
    pwsh -NoProfile -File scripts/Get-CardCoverageInventory.ps1 -Check -SkipBuild
    if ($LASTEXITCODE -ne 0) { throw 'Coverage inventory is stale.' }
    Write-Output "SOLVER-COMPLETION PASS: focused native/model/CLI checks; $($manifest.Count) fixture hashes/registrations; coverage index current. No full regression. Predictions: $output"
} finally { Pop-Location }
