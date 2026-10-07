[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$PlayerExportPath,
    [Parameter(Mandatory = $true)][string]$OutputPath,
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$SourceCommit = 'b03c5bdc85498f5f5d76f03f4d070adcaa13aa34',
    [string[]]$AdditionalSourceFiles = @('tools/Export-FinalizedWindows.ps1', 'tools/Package-FinalizedRelease.ps1', 'tools/Verify-FinalizedRelease.ps1', 'docs/RELEASE-PROCEDURE-v020.7.md'),
    [string[]]$EvidenceFiles = @(),
    [string]$EvidenceRoot,
    [string]$PlayerReadmePath,
    [switch]$Candidate,
    [switch]$Corrected,
    [ValidateSet('v020.7', 'v020.7b', 'v020.8b')][string]$Version = 'v020.7'
)

$ErrorActionPreference = 'Stop'
if ($Candidate -and $Corrected) { throw 'A package cannot be both a candidate and a verified correction.' }
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.IO.Compression.FileSystem
$taskRoot = (Resolve-Path -LiteralPath $ProjectRoot).Path.TrimEnd('\', '/')
$taskExport = (Resolve-Path -LiteralPath $PlayerExportPath).Path.TrimEnd('\', '/')
$taskOutput = [IO.Path]::GetFullPath($OutputPath).TrimEnd('\', '/')
if (Test-Path -LiteralPath $taskOutput) { throw "Output already exists; do not overwrite historical archives: $taskOutput" }
if ($taskOutput -eq $taskRoot -or $taskOutput.StartsWith($taskRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Package outside the source tree; copy verified finished artifacts into a fresh release subfolder afterwards.' }
if ((Get-Content -LiteralPath (Join-Path $taskRoot 'project.godot') -Raw) -notmatch ('(?m)^config/version="' + [regex]::Escape($Version) + '"\s*$')) { throw "Project identity does not match the explicitly selected $Version release." }
if ($SourceCommit -notmatch '^[a-fA-F0-9]{40}$') { throw 'SourceCommit must be a full Git commit ID.' }

function Read-GitLines([string[]]$Arguments) {
    $taskGitResult = @(& git -C $taskRoot -c core.quotepath=false @Arguments)
    if ($LASTEXITCODE -ne 0) { throw "Git inventory failed: $Arguments" }
    return $taskGitResult
}
function Assert-SourceName([string]$Name) {
    if ($Name -match '(^/|\\|^[A-Za-z]:|(^|/)\.\.(/|$))') { throw "Unsafe relative source path: $Name" }
    if ($Name -match '(^|/)(releases|\.godot|\.git|\.local|bin|obj|work|__MACOSX)(/|$)|\.(pdb|keystore|jks|exe|pck|zip)$|(^|/)(export_credentials\.cfg|NuGet\.local\.config|NuGet\.Config|\.DS_Store)$') { throw "Generated/private/archive file cannot enter source snapshot: $Name" }
}
function New-StableZip([string]$Path, [Collections.Generic.Dictionary[string,string]]$Files) {
    $taskNames = [string[]]@($Files.Keys)
    [Array]::Sort($taskNames, [StringComparer]::Ordinal)
    $taskFile = [IO.File]::Open($Path, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
    $taskArchive = [IO.Compression.ZipArchive]::new($taskFile, [IO.Compression.ZipArchiveMode]::Create, $false)
    try {
        foreach ($taskName in $taskNames) {
            $taskEntry = $taskArchive.CreateEntry($taskName, [IO.Compression.CompressionLevel]::Optimal)
            $taskEntry.LastWriteTime = [DateTimeOffset]::new(2000, 1, 1, 0, 0, 0, [TimeSpan]::Zero)
            $taskEntry.ExternalAttributes = 0
            $taskInput = [IO.File]::OpenRead($Files[$taskName])
            $taskEntryStream = $taskEntry.Open()
            try { $taskInput.CopyTo($taskEntryStream) } finally { $taskInput.Dispose(); $taskEntryStream.Dispose() }
        }
    } finally { $taskArchive.Dispose(); $taskFile.Dispose() }
}

$taskHead = @(Read-GitLines @('rev-parse', 'HEAD'))[0]
$taskCommit = @(Read-GitLines @('rev-parse', '--verify', "$SourceCommit^{commit}"))[0]
if ($taskCommit -ne $SourceCommit) { throw 'Source commit did not resolve exactly.' }
$taskTracked = @(Read-GitLines @('ls-files', '--cached'))
$taskSourceNames = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$taskSourceExcluded = [Collections.Generic.List[string]]::new()
foreach ($taskName in $taskTracked) {
    # Required source/config/assets plus accumulated engineering documentation.
    # The original unused sound-library distribution and OS/editor metadata are
    # not required by current scene/script references. Tracked assets stay intact.
    if ($taskName -notmatch '^(src|tests|assets|data|scenes|tools|docs)/|^[^/]+$') { continue }
    # Historical diagnostic captures stay in Git and their original releases.
    # The source package carries engineering text and selected current evidence.
    if ($taskName -match '^docs/diagnostics/.*\.(png|jpg|jpeg|gif|webp)(\.import)?$') {
        $taskSourceExcluded.Add($taskName)
        continue
    }
    if ($taskName -match '(^|/)(releases|\.godot|\.git|\.local|bin|obj|work|__MACOSX)(/|$)|\.(pdb|keystore|jks|exe|pck|zip)$|(^|/)(export_credentials\.cfg|NuGet\.local\.config|NuGet\.Config|\.DS_Store)$') { continue }
    Assert-SourceName $taskName
    if (-not (Test-Path -LiteralPath (Join-Path $taskRoot $taskName) -PathType Leaf)) { throw "Tracked source file is missing; reconcile before packaging: $taskName" }
    $null = $taskSourceNames.Add($taskName)
}
foreach ($taskName in $AdditionalSourceFiles) {
    Assert-SourceName $taskName
    if (-not (Test-Path -LiteralPath (Join-Path $taskRoot $taskName) -PathType Leaf)) { throw "Explicit additional source is missing: $taskName" }
    $null = $taskSourceNames.Add($taskName)
    $null = $taskSourceExcluded.Remove($taskName)
}
$taskUntracked = @(Read-GitLines @('ls-files', '--others', '--exclude-standard', '--', 'src', 'tests', 'assets', 'data', 'scenes', 'tools', 'docs'))
foreach ($taskName in $taskUntracked) {
    if (-not $taskSourceNames.Contains($taskName)) { throw "Untracked source/evidence requires explicit AdditionalSourceFiles review: $taskName" }
}
$taskSourceFiles = [Collections.Generic.Dictionary[string,string]]::new([StringComparer]::Ordinal)
$taskInventory = [Collections.Generic.List[object]]::new()
$taskNames = [string[]]@($taskSourceNames)
[Array]::Sort($taskNames, [StringComparer]::Ordinal)
foreach ($taskName in $taskNames) {
    $taskFile = Join-Path $taskRoot $taskName
    $taskSourceFiles.Add("dev-ancient-naval/$taskName", $taskFile)
    $taskInventory.Add([pscustomobject]@{ path = $taskName; sha256 = (Get-FileHash -LiteralPath $taskFile -Algorithm SHA256).Hash.ToLowerInvariant() })
}
foreach ($taskRequired in @('project.godot', 'export_presets.cfg', 'Dev_ancient_naval.csproj', 'src/Core/DevAncientNaval.Core.csproj', 'tests/CoreChecks/CoreChecks.csproj', 'scenes/Main.tscn', 'data/balance.json', 'README.md', 'AGENTS.md')) {
    if (-not $taskSourceNames.Contains($taskRequired)) { throw "Missing required source snapshot file: $taskRequired" }
}
& (Join-Path $PSScriptRoot 'Verify-FinalizedRelease.ps1') -PckPath (Join-Path $taskExport 'Ancient Naval.pck') | Out-Null
foreach ($taskRequired in @('Ancient Naval.exe', 'data_Dev_ancient_naval_windows_x86_64/Dev_ancient_naval.dll')) {
    if (-not (Test-Path -LiteralPath (Join-Path $taskExport $taskRequired) -PathType Leaf)) { throw "Incomplete raw player export: $taskRequired" }
}

New-Item -ItemType Directory -Path $taskOutput | Out-Null
$taskPlayer = Join-Path $taskOutput 'player'
$taskEvidence = Join-Path $taskOutput 'evidence'
New-Item -ItemType Directory -Path $taskPlayer | Out-Null
New-Item -ItemType Directory -Path $taskEvidence | Out-Null
$taskPlayerFiles = [Collections.Generic.Dictionary[string,string]]::new([StringComparer]::Ordinal)
$taskRemoved = [Collections.Generic.List[string]]::new()
foreach ($taskFile in Get-ChildItem -LiteralPath $taskExport -Recurse -File -Force) {
    $taskRelative = $taskFile.FullName.Substring($taskExport.Length + 1).Replace('\', '/')
    if ($taskFile.Extension -eq '.pdb' -or ($taskRelative -notin @('Ancient Naval.exe', 'Ancient Naval.pck') -and -not $taskRelative.StartsWith('data_Dev_ancient_naval_windows_x86_64/', [StringComparison]::Ordinal))) {
        $taskRemoved.Add($taskRelative)
        continue
    }
    $taskDestination = Join-Path $taskPlayer $taskRelative
    New-Item -ItemType Directory -Path (Split-Path -Parent $taskDestination) -Force | Out-Null
    Copy-Item -LiteralPath $taskFile.FullName -Destination $taskDestination
    $taskPlayerFiles.Add("Ancient Naval $Version/$taskRelative", $taskDestination)
}
$taskReadme = Join-Path $taskPlayer 'READ_ME_RU.md'
if ($PlayerReadmePath) { Copy-Item -LiteralPath (Resolve-Path -LiteralPath $PlayerReadmePath).Path -Destination $taskReadme }
else {
    $taskReadmeText = @'
# Ancient Naval v020.7 — The Sacred Voyage

Распакуйте всю папку и запустите **Ancient Naval.exe**. Папка
`data_Dev_ancient_naval_windows_x86_64` и файл `Ancient Naval.pck`
необходимы для игры — оставьте их рядом с программой.

В меню доступны English, Українська и Nederlands. **Continue** продолжает
последнее сохранённое путешествие; **New Voyage** создаёт новое сохранение.
Для выхода используйте меню игры. Не удаляйте файлы сохранений и их резервные
копии из пользовательских данных игры.

Extract the complete folder and start **Ancient Naval.exe**. Keep the `.pck`
and the complete runtime folder beside it. No separate .NET installation is
required. Choose the language in Settings. Continue resumes your saved voyage.

Repository: https://github.com/Beygelman/Dev_ancient_naval
'@
    if ($Version -ne 'v020.7') { $taskReadmeText = $taskReadmeText.Replace('v020.7 — The Sacred Voyage', $Version) }
    if ($Candidate) {
        $taskReadmeText = "Verification candidate: final native frame-pacing approval is pending. / Кандидат: окончательная проверка плавности ещё не завершена.`n`n" + $taskReadmeText
    }
    [IO.File]::WriteAllText($taskReadme, $taskReadmeText.Replace("`r`n", "`n") + "`n", [Text.UTF8Encoding]::new($false))
}
$taskPlayerFiles.Add("Ancient Naval $Version/READ_ME_RU.md", $taskReadme)
& (Join-Path $PSScriptRoot 'Verify-FinalizedRelease.ps1') -PlayerDirectory $taskPlayer -RuntimeReferencePath $taskExport -PckInventoryPath (Join-Path $taskEvidence 'pck-resources.txt') | Out-Null
$taskRemoved.Sort([StringComparer]::Ordinal)
[IO.File]::WriteAllLines((Join-Path $taskEvidence 'player-excluded-files.txt'), $taskRemoved.ToArray(), [Text.UTF8Encoding]::new($false))
$taskSnapshot = [ordered]@{
    version = $Version
    snapshot_type = $(if ($Candidate) { 'candidate' } elseif ($Corrected) { 'correction' } else { 'finalization' })
    baseline_commit = $SourceCommit
    snapshot_head = $taskHead
    source_policy = 'Git-tracked source/config/assets/tests/docs/tools plus explicitly reviewed AdditionalSourceFiles; working-tree bytes preserved; historical diagnostics images/archives and generated/private files excluded; all tracked assets retained; selected current captures are validation evidence.'
    zip_metadata = 'Ordinal path order; fixed UTC 2000-01-01 timestamps; ExternalAttributes=0; Optimal compression. Byte reproducibility is scoped to identical inputs and the same PowerShell/.NET compressor.'
    files = $taskInventory.ToArray()
}
$taskInventoryPath = Join-Path $taskEvidence 'SOURCE-INVENTORY.json'
[IO.File]::WriteAllText($taskInventoryPath, ($taskSnapshot | ConvertTo-Json -Depth 5) + "`n", [Text.UTF8Encoding]::new($false))
$taskSourceFiles.Add('dev-ancient-naval/release-evidence/SOURCE-INVENTORY.json', $taskInventoryPath)
$taskEvidenceIgnore = Join-Path $taskEvidence '.gdignore'
[IO.File]::WriteAllText($taskEvidenceIgnore, '', [Text.UTF8Encoding]::new($false))
$taskSourceFiles.Add('dev-ancient-naval/release-evidence/.gdignore', $taskEvidenceIgnore)
$taskSourceFiles.Add('dev-ancient-naval/release-evidence/pck-resources.txt', (Join-Path $taskEvidence 'pck-resources.txt'))
$taskSourceFiles.Add('dev-ancient-naval/release-evidence/player-excluded-files.txt', (Join-Path $taskEvidence 'player-excluded-files.txt'))
$taskSourceExcluded.Sort([StringComparer]::Ordinal)
$taskSourceExcludedPath = Join-Path $taskEvidence 'source-excluded-files.txt'
[IO.File]::WriteAllLines($taskSourceExcludedPath, $taskSourceExcluded.ToArray(), [Text.UTF8Encoding]::new($false))
$taskSourceFiles.Add('dev-ancient-naval/release-evidence/source-excluded-files.txt', $taskSourceExcludedPath)
foreach ($taskFile in $EvidenceFiles) {
    $taskEvidenceSource = (Resolve-Path -LiteralPath $taskFile).Path
    if (-not (Test-Path -LiteralPath $taskEvidenceSource -PathType Leaf)) { throw "Not an evidence file: $taskEvidenceSource" }
    $taskName = [IO.Path]::GetFileName($taskEvidenceSource)
    if ($taskName -match '(?i)(save|settings|credentials|secret|token)|\.(zip|exe|pck|pdb|keystore|jks)$') { throw "Evidence path resembles a save/private/archive file: $taskName" }
    if ($EvidenceRoot) {
        $taskEvidenceRoot = (Resolve-Path -LiteralPath $EvidenceRoot).Path.TrimEnd('\', '/')
        if (-not $taskEvidenceSource.StartsWith($taskEvidenceRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw "Evidence is outside EvidenceRoot: $taskEvidenceSource" }
        $taskName = $taskEvidenceSource.Substring($taskEvidenceRoot.Length + 1).Replace('\', '/')
        Assert-SourceName $taskName
        $taskSourceFiles.Add("dev-ancient-naval/release-evidence/validation/$taskName", $taskEvidenceSource)
    } else {
        $taskSourceFiles.Add("dev-ancient-naval/release-evidence/$taskName", $taskEvidenceSource)
    }
}
$taskPlayerName = if ($Candidate) { "Ancient_Naval_${Version}_Windows_PLAYER_CANDIDATE.zip" } elseif ($Corrected) { "Ancient_Naval_${Version}_Windows_PLAYER_CORRECTED.zip" } elseif ($Version -eq 'v020.7') { 'Ancient_Naval_v020.7_Windows_PLAYER_CLEAN.zip' } else { "Ancient_Naval_${Version}_Windows.zip" }
$taskSourceName = if ($Candidate) { "Ancient_Naval_${Version}_Source_CANDIDATE.zip" } elseif ($Corrected) { "Ancient_Naval_${Version}_Source_CORRECTED.zip" } elseif ($Version -eq 'v020.7') { 'Ancient_Naval_v020.7_Source_FINALIZED.zip' } else { "Ancient_Naval_${Version}_Source.zip" }
$taskPlayerZip = Join-Path $taskOutput $taskPlayerName
$taskSourceZip = Join-Path $taskOutput $taskSourceName
New-StableZip $taskPlayerZip $taskPlayerFiles
New-StableZip $taskSourceZip $taskSourceFiles
foreach ($taskZipPath in @($taskPlayerZip, $taskSourceZip)) {
    if ((Get-Item -LiteralPath $taskZipPath).Length -gt 100MB) { throw "Archive exceeds the ordinary GitHub 100 MiB file limit; inspect source/evidence policy before delivery: $taskZipPath" }
}
$taskManifest = Join-Path $taskOutput $(if ($Candidate) { "SHA256-${Version}-candidate.txt" } elseif ($Corrected) { "SHA256-${Version}-corrected.txt" } elseif ($Version -eq 'v020.7') { 'SHA256-v020.7-finalized.txt' } else { "SHA256-${Version}.txt" })
$taskHashLines = @($taskPlayerZip, $taskSourceZip) | ForEach-Object { "{0}  {1}" -f (Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant(), [IO.Path]::GetFileName($_) }
[IO.File]::WriteAllLines($taskManifest, $taskHashLines, [Text.UTF8Encoding]::new($false))
& (Join-Path $PSScriptRoot 'Verify-FinalizedRelease.ps1') -PlayerZip $taskPlayerZip -SourceZip $taskSourceZip -ManifestPath $taskManifest -Version $Version
[pscustomobject]@{ Version = $Version; Windows = $taskPlayerZip; Source = $taskSourceZip; SHA256 = $taskManifest; Player = $taskPlayer; SourceFiles = $taskSourceFiles.Count; PlayerFiles = $taskPlayerFiles.Count; ExcludedPlayerFiles = $taskRemoved.ToArray() }
