#requires -Version 7.4
[CmdletBinding()]
param(
    [string] $GameDir = '',
    [string] $GameManagedDir = '',
    [string] $OutputDirectory = '',
    [switch] $SkipBuild
)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
$version = '0.2.0'
$runtimeName = 'BepInEx_macos_universal_5.4.23.5.zip'
$runtimeSha = '01c2ae782eb016dfd6c345a18dbd2dcafffb3d9d318449d6486689f426b4a323'
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $workspace ('results/mod-dist/' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
}
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
$package = Join-Path $OutputDirectory "PojuAdvisor-$version-macOS"
if (Test-Path -LiteralPath $package) { throw "Build destination already exists: $package" }
if (-not $SkipBuild) {
    $buildArgs = @('build', (Join-Path $workspace 'src/Advisor/Advisor.csproj'), '-c', 'Release', '--nologo', '-v:q', '-p:WarningLevel=0')
    if ($GameDir) { $buildArgs += "-p:GameDir=$GameDir" }
    if ($GameManagedDir) { $buildArgs += "-p:GameManagedDir=$GameManagedDir" }
    & dotnet @buildArgs
    if ($LASTEXITCODE -ne 0) { throw 'Advisor build failed.' }
}
$cache = Join-Path $workspace 'results/mod-build-cache'
New-Item -ItemType Directory -Path $cache -Force | Out-Null
$runtime = Join-Path $cache $runtimeName
if (-not (Test-Path -LiteralPath $runtime)) {
    Invoke-WebRequest "https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.5/$runtimeName" -OutFile $runtime
}
if ((Get-FileHash -LiteralPath $runtime -Algorithm SHA256).Hash.ToLowerInvariant() -cne $runtimeSha) {
    throw 'Official BepInEx runtime checksum differs.'
}
$plugin = Join-Path $package 'BepInEx/plugins/PojuAdvisor'
New-Item -ItemType Directory -Path $plugin, (Join-Path $package 'runtime'), (Join-Path $package 'licenses') -Force | Out-Null
$output = Join-Path $workspace 'src/Advisor/bin/Release/netstandard2.1'
foreach ($name in @('MonsterTrain2Poju.Advisor', 'Model', 'FixtureArchive')) {
    Copy-Item -LiteralPath (Join-Path $output "$name.dll") -Destination $plugin
}
$actualVersion = [Reflection.AssemblyName]::GetAssemblyName((Join-Path $plugin 'MonsterTrain2Poju.Advisor.dll')).Version
if ($actualVersion -ne [Version] "$version.0") { throw "Unexpected plugin version: $actualVersion" }
Copy-Item -LiteralPath $runtime -Destination (Join-Path $package 'runtime')
Copy-Item -LiteralPath (Join-Path $workspace 'packaging/macos/install.command'), (Join-Path $workspace 'packaging/macos/Start-Poju.command') -Destination $package
Copy-Item -LiteralPath (Join-Path $workspace 'docs/macOS.md') -Destination (Join-Path $package 'README-中文.md')
Copy-Item -LiteralPath (Join-Path $workspace 'packaging/THIRD-PARTY-NOTICES.md') -Destination $package
Invoke-WebRequest 'https://raw.githubusercontent.com/BepInEx/BepInEx/v5.4.23.5/LICENSE' -OutFile (Join-Path $package 'licenses/BepInEx-LICENSE.txt')
$revision = & git -C $workspace rev-parse HEAD
if ($LASTEXITCODE -ne 0) { throw 'Cannot determine build source revision.' }
$dirty = [bool] (& git -C $workspace status --porcelain)
[ordered]@{
    version = $version; sourceRevision = $revision; sourceDirty = $dirty
    builtUtc = [DateTime]::UtcNow.ToString('O'); target = 'netstandard2.1 / AnyCPU / BepInEx 5 Mono'
    macOSAcceptance = 'Not run; verify loader and native battle on target Mac.'
    runtimeSha256 = $runtimeSha
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $package 'BUILD.json') -Encoding utf8NoBOM
$hashes = Get-ChildItem -LiteralPath $package -File -Recurse | Sort-Object FullName | ForEach-Object {
    $relative = [IO.Path]::GetRelativePath($package, $_.FullName).Replace('\', '/')
    (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant() + '  ' + $relative
}
[IO.File]::WriteAllText((Join-Path $package 'SHA256SUMS'), (($hashes -join "`n") + "`n"), [Text.UTF8Encoding]::new($false))
$zip = "$package.zip"
$archive = [IO.Compression.ZipFile]::Open($zip, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in Get-ChildItem -LiteralPath $package -File -Recurse | Sort-Object FullName) {
        $relative = [IO.Path]::GetRelativePath($package, $file.FullName).Replace('\', '/')
        $entry = [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file.FullName, $relative)
        # Unix regular file mode; preserve .command executable bits on macOS unzip.
        $entry.ExternalAttributes = if ($relative.EndsWith('.command')) { -2115174400 } else { -2119958528 }
    }
} finally { $archive.Dispose() }
$hash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText("$zip.sha256", "$hash  $([IO.Path]::GetFileName($zip))`n", [Text.UTF8Encoding]::new($false))
[pscustomobject]@{ Package = $zip; Bytes = (Get-Item -LiteralPath $zip).Length; SHA256 = $hash; Source = $revision; Dirty = $dirty } | ConvertTo-Json -Compress
