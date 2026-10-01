[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+(\.\d+)?$')]
    [string]$Version,

    [Parameter(Mandatory = $true)]
    [string]$OutputsPath
)

$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
if (-not (Test-Path -LiteralPath (Join-Path $taskRoot 'project.godot'))) {
    throw 'Run this tool from the Ancient Naval source project.'
}
$taskOutputs = (Resolve-Path -LiteralPath $OutputsPath).Path
$taskBuild = Join-Path $taskOutputs "Ancient Naval $Version"
$taskPrefix = "Ancient_Naval_$Version"
$taskReleaseRoot = Join-Path $taskRoot 'releases'
$taskDestination = Join-Path $taskReleaseRoot $Version
$taskPlayable = Join-Path $taskDestination 'playable'

foreach ($taskRequired in @("${taskPrefix}_Windows.zip", "${taskPrefix}_Source.zip", "${taskPrefix}_Notes_RU.md", "${taskPrefix}_SHA256.txt")) {
    if (-not (Test-Path -LiteralPath (Join-Path $taskOutputs $taskRequired) -PathType Leaf)) {
        throw "Missing packaged artifact: $taskRequired"
    }
}
if (-not (Test-Path -LiteralPath (Join-Path $taskBuild 'Ancient Naval.exe') -PathType Leaf)) {
    throw 'Missing unpacked Windows build.'
}

$taskFiles = [Collections.Generic.List[object]]::new()
foreach ($taskSource in Get-ChildItem -LiteralPath $taskOutputs -File -Filter "${taskPrefix}_*") {
    $taskFiles.Add([pscustomobject]@{ Source = $taskSource.FullName; Target = Join-Path $taskDestination $taskSource.Name })
}
foreach ($taskSource in Get-ChildItem -LiteralPath $taskBuild -Recurse -File -Force) {
    $taskRelative = $taskSource.FullName.Substring($taskBuild.Length).TrimStart('\', '/')
    $taskFiles.Add([pscustomobject]@{ Source = $taskSource.FullName; Target = Join-Path $taskPlayable $taskRelative })
}

# Verify existing copies before writing anything; never partially overwrite a release.
foreach ($taskFile in $taskFiles) {
    $taskFile | Add-Member -NotePropertyName Hash -NotePropertyValue (Get-FileHash -LiteralPath $taskFile.Source -Algorithm SHA256).Hash
    if (Test-Path -LiteralPath $taskFile.Target) {
        if ((Get-FileHash -LiteralPath $taskFile.Target -Algorithm SHA256).Hash -ne $taskFile.Hash) {
            throw "Existing release differs: $($taskFile.Target). Use a new version."
        }
    }
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
foreach ($taskArchiveName in @("${taskPrefix}_Source.zip", "${taskPrefix}_Windows.zip")) {
    $taskArchive = [IO.Compression.ZipFile]::OpenRead((Join-Path $taskOutputs $taskArchiveName))
    try {
        if ($taskArchive.Entries.Count -eq 0) { throw "Empty archive: $taskArchiveName" }
        if ($taskArchiveName.EndsWith('_Source.zip') -and $null -eq $taskArchive.GetEntry('dev-ancient-naval/README.md')) {
            throw 'Source archive does not contain the project README.'
        }
        if ($taskArchiveName.EndsWith('_Windows.zip') -and $null -eq $taskArchive.GetEntry("Ancient Naval $Version/Ancient Naval.exe")) {
            throw 'Windows archive does not contain the executable.'
        }
    }
    finally { $taskArchive.Dispose() }
}
foreach ($taskLine in Get-Content -LiteralPath (Join-Path $taskOutputs "${taskPrefix}_SHA256.txt")) {
    if ($taskLine -notmatch '^([a-fA-F0-9]{64})\s+(.+)$') { throw 'Invalid SHA256 manifest line.' }
    $taskExpectedHash = $Matches[1]
    $taskManifestPath = Join-Path $taskOutputs $Matches[2]
    if ((Get-FileHash -LiteralPath $taskManifestPath -Algorithm SHA256).Hash -ne $taskExpectedHash) {
        throw "Release manifest mismatch: $taskManifestPath"
    }
}

foreach ($taskFile in $taskFiles) {
    New-Item -ItemType Directory -Path (Split-Path -Parent $taskFile.Target) -Force | Out-Null
    if (-not (Test-Path -LiteralPath $taskFile.Target)) {
        Copy-Item -LiteralPath $taskFile.Source -Destination $taskFile.Target
    }
    if ((Get-FileHash -LiteralPath $taskFile.Target -Algorithm SHA256).Hash -ne $taskFile.Hash) {
        throw "Copy verification failed: $($taskFile.Target)"
    }
}

# Same folder layout as the original checksum manifest, relative to releases/.
$taskBuildAlias = "Ancient Naval $Version/"
$taskLocalManifest = Get-Content -LiteralPath (Join-Path $taskOutputs "${taskPrefix}_SHA256.txt") | ForEach-Object {
    if ($_ -notmatch '^([a-fA-F0-9]{64})\s+(.+)$') { throw 'Invalid checksum record.' }
    $taskName = $Matches[2].Replace('\', '/')
    $taskCopyName = if ($taskName.StartsWith($taskBuildAlias)) {
        "$Version/playable/" + $taskName.Substring($taskBuildAlias.Length)
    } else { "$Version/$taskName" }
    "$($Matches[1])  $taskCopyName"
}
$taskLocalManifest | Set-Content -LiteralPath (Join-Path $taskReleaseRoot "SHA256-$Version.txt") -Encoding utf8
@{
    version = $Version
    windows = "$Version/${taskPrefix}_Windows.zip"
    source = "$Version/${taskPrefix}_Source.zip"
    notes = "$Version/${taskPrefix}_Notes_RU.md"
    playable = "$Version/playable/Ancient Naval.exe"
    copied_files = $taskFiles.Count
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskReleaseRoot 'latest.json') -Encoding utf8
Write-Output "Copied and SHA256-verified $($taskFiles.Count) files in $taskDestination"
