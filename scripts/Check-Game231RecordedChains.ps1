#requires -Version 7.4
[CmdletBinding()]
param(
    [switch] $SkipBuild,
    [AllowEmptyCollection()]
    [string[]] $Archive = @(),
    [ValidateNotNullOrEmpty()] [string] $ChangedFrom,
    [ValidateRange(1, 32)] [int] $Workers = 1,
    [switch] $CheckCardFrontiers,
    [switch] $CheckEndTurnFrontiers
)

$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
$selecting = $PSBoundParameters.ContainsKey('Archive') -or $PSBoundParameters.ContainsKey('ChangedFrom')
$selected = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($path in $Archive) {
    if ([string]::IsNullOrWhiteSpace($path)) { throw 'Archive requires a registered path.' }
    $null = $selected.Add($path.Replace('\', '/'))
}

function Get-ChangedArchiveRegistrations([string] $revision, [string] $relativeDirectory) {
    $manifestPatterns = @(":(glob)${relativeDirectory}**/manifest.tsv", ":(glob)${relativeDirectory}**/revalidated.tsv",
        "${relativeDirectory}solver-stage.tsv")
    $changed = @(& git -C $workspace -c core.quotePath=false diff --name-status --no-renames --diff-filter=AM $revision -- @manifestPatterns)
    if ($LASTEXITCODE) { throw 'Could not compare archive manifests.' }
    $untracked = @(& git -C $workspace -c core.quotePath=false ls-files --others --exclude-standard -- @manifestPatterns)
    if ($LASTEXITCODE) { throw 'Could not list untracked archive manifests.' }

    foreach ($entry in $changed + @($untracked | ForEach-Object { "A`t$_" })) {
        $status, $manifest = $entry -split "`t", 2
        $previousRows = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        if ($status -eq 'M') {
            $previous = @(& git -C $workspace show "${revision}:$manifest")
            if ($LASTEXITCODE) { throw "Could not read previous archive manifest: $manifest" }
            foreach ($row in $previous | Select-Object -Skip 1) {
                $null = $previousRows.Add($row.Replace('"', ''))
            }
        }

        $directory = $manifest.Substring(0, $manifest.LastIndexOf('/') + 1)
        $pathColumn = [int]($manifest -eq "${relativeDirectory}solver-stage.tsv")
        foreach ($row in Get-Content -LiteralPath (Join-Path $workspace $manifest) | Select-Object -Skip 1) {
            $row = $row.Replace('"', '')
            if ($previousRows.Contains($row)) { continue }
            $fields = $row -split "`t"
            if ($fields.Count -ne (3 + $pathColumn) -or !$fields[$pathColumn].EndsWith('.mt2f', [StringComparison]::Ordinal)) {
                throw "Invalid archive registration: $manifest"
            }
            $directory + $fields[$pathColumn]
        }
    }
}

if ($PSBoundParameters.ContainsKey('ChangedFrom')) {
    $revision = & git -C $workspace rev-parse --verify --quiet --end-of-options "$ChangedFrom^{commit}"
    if ($LASTEXITCODE) { throw "Unknown Git commit or ref: $ChangedFrom" }
    $relativeDirectory = 'tests/compatibility/game-2.3.1/'
    $pattern = $relativeDirectory + '*.mt2f'
    $changed = @(& git -C $workspace -c core.quotePath=false diff --name-only --no-renames --diff-filter=AM $revision -- $pattern)
    if ($LASTEXITCODE) { throw "Could not compare native archives with $ChangedFrom." }
    $untracked = @(& git -C $workspace -c core.quotePath=false ls-files --others --exclude-standard -- $pattern)
    if ($LASTEXITCODE) { throw 'Could not list untracked native archives.' }
    $registrations = @(Get-ChangedArchiveRegistrations $revision $relativeDirectory)
    $candidates = @($changed + $untracked + $registrations | Sort-Object -Unique -CaseSensitive)
    if ($candidates.Count) {
        $catalogAssembly = Join-Path $workspace 'src/FixtureArchive/bin/Release/net8.0/FixtureArchive.dll'
        if (!(Test-Path -LiteralPath $catalogAssembly -PathType Leaf) -and !$SkipBuild) {
            dotnet build (Join-Path $workspace 'src/FixtureArchive/FixtureArchive.csproj') -c Release -f net8.0 -p:WarningLevel=0 -v:q
            if ($LASTEXITCODE) { throw 'FixtureArchive build failed.' }
        }
        [void][Reflection.Assembly]::Load([IO.File]::ReadAllBytes($catalogAssembly))
        $directory = [MonsterTrain2Poju.Fixtures.Game231FixtureCatalog]::DirectoryFor($workspace)
        $catalog = [MonsterTrain2Poju.Fixtures.Game231FixtureCatalog]::Load($directory)
        foreach ($candidate in $candidates) {
            $path = $candidate.Substring($relativeDirectory.Length)
            if (!$catalog.ContainsKey($path) -or $selected.Contains($path)) { continue }
            $registration = $catalog[$path]
            $document = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read($registration.Verify($directory))
            try {
                if ([MonsterTrain2Poju.Fixtures.Game231FixtureCatalog]::Classify($registration, $document.RootElement) -eq 'SettledBattle') {
                    $null = $selected.Add($path)
                }
            } finally { $document.Dispose() }
        }
    }
}
if ($selecting) {
    Write-Output "RECORDED-CHAIN-SELECTION selected=$($selected.Count)"
    if (!$selected.Count) { return }
    $Archive = [string[]]@($selected)
    [Array]::Sort($Archive, [StringComparer]::Ordinal)
    $Archive | ForEach-Object { Write-Output "RECORDED-CHAIN-SELECTED $_" }
}
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
$runId = (Get-Date -Format yyyyMMdd-HHmmss) + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
$report = Join-Path $workspace "results/recorded-chain-triage-$runId/report.tsv"
$checkerArguments = @('--game231-recorded-chain-triage', $workspace, $report, "--workers=$Workers")
if ($CheckCardFrontiers) { $checkerArguments += '--card-frontiers' }
if ($CheckEndTurnFrontiers) { $checkerArguments += '--end-turn-frontiers' }
$checkerArguments += $Archive
& dotnet $checker @checkerArguments
if ($LASTEXITCODE) { throw "Recorded chain triage failed. See $report" }
