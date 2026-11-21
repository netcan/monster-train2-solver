#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild, [string[]] $Case = @(), [string] $ModelChecker = '')
$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
if ($ModelChecker -and -not $SkipBuild) { throw 'ModelChecker requires SkipBuild and an isolated prebuilt checker.' }
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'solver-stage.tsv') -Delimiter "`t")
if ($manifest.Count -ne 8 -or @($manifest.case | Sort-Object -Unique).Count -ne 8 -or
    @($manifest.archive | Sort-Object -Unique).Count -ne 8 -or @($manifest.sha256 | Sort-Object -Unique).Count -ne 8) {
    throw 'Expected eight distinct fixed stage cases and native archives.'
}
foreach ($name in $Case) { if ($name -cnotin $manifest.case) { throw "Unknown stage case: $name" } }
$selected = @($manifest | Where-Object { $Case.Count -eq 0 -or $_.case -cin $Case })
foreach ($entry in $selected) {
    $path = Join-Path $directory $entry.archive
    if ((Get-Item -LiteralPath $path).Length -ne [long]$entry.bytes -or
        (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $entry.sha256) { throw "Stage archive changed: $($entry.case)" }
}
$outputDirectory = Join-Path $workspace ('results/solver-stage-' + (Get-Date -Format yyyyMMdd-HHmmss) + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
$null = New-Item -ItemType Directory -Path $outputDirectory
$report = Join-Path $outputDirectory 'report.tsv'
$rows = [Collections.Generic.List[object]]::new()
$checker = if ($ModelChecker) { (Resolve-Path -LiteralPath $ModelChecker).Path }
    else { Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll' }
if ((Split-Path $checker -Leaf) -cne 'ModelChecks.dll') { throw 'Expected ModelChecks.dll.' }
$checkerHash = (Get-FileHash -LiteralPath $checker -Algorithm SHA256).Hash
$modelHash = (Get-FileHash -LiteralPath (Join-Path (Split-Path $checker -Parent) 'Model.dll') -Algorithm SHA256).Hash
Write-Output "SOLVER-STAGE-START selected=$($selected.Count) fixedTotal=8 checkerSHA256=$checkerHash modelSHA256=$modelHash report=$report"
foreach ($entry in $selected) {
    $log = Join-Path $outputDirectory ($entry.case + '.log')
    $timer = [Diagnostics.Stopwatch]::StartNew()
    try {
        & dotnet $checker --solver-stage-case-only (Join-Path $directory $entry.archive) $entry.case *> $log
        $exitCode = $LASTEXITCODE
    } catch { $exitCode = 1; $_ | Out-File -LiteralPath $log -Append }
    $timer.Stop()
    $passPattern = '^SOLVER-STAGE-CASE PASS case=' + [regex]::Escape($entry.case) + ' '
    $detail = if ($exitCode -eq 0) { @(Select-String -LiteralPath $log -Pattern $passPattern -CaseSensitive | ForEach-Object Line) | Select-Object -Last 1 }
        else { @(Select-String -LiteralPath $log -Pattern 'Exception:|Unhandled exception' | ForEach-Object Line) | Select-Object -First 1 }
    $passed = $exitCode -eq 0 -and [bool]$detail
    $rows.Add([pscustomobject]@{ case=$entry.case; status=$(if ($passed) { 'PASS' } else { 'FAIL' });
        seconds=$timer.Elapsed.TotalSeconds.ToString('F3', [Globalization.CultureInfo]::InvariantCulture); detail=$detail; log=$log;
        archive=$entry.archive; archiveSHA256=$entry.sha256; checkerSHA256=$checkerHash; modelSHA256=$modelHash })
    $rows | Export-Csv -LiteralPath $report -Delimiter "`t" -NoTypeInformation
    Write-Output "SOLVER-STAGE $($entry.case) $($rows[-1].status) seconds=$($rows[-1].seconds) detail=$detail"
}
$failed = @($rows | Where-Object status -EQ 'FAIL').Count
Write-Output "SOLVER-STAGE-RESULT passed=$($rows.Count - $failed) failed=$failed selected=$($selected.Count) fixedTotal=8 report=$report"
if ($failed) { throw "$failed solver stage case(s) failed; see $report" }
