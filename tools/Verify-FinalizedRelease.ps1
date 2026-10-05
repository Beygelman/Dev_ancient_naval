[CmdletBinding()]
param(
    [string]$PlayerDirectory,
    [string]$PlayerZip,
    [string]$SourceZip,
    [string]$ManifestPath,
    [string]$PckPath,
    [string]$PckInventoryPath,
    [string]$RuntimeReferencePath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.IO.Compression.FileSystem
$taskRuntimeName = 'data_Dev_ancient_naval_windows_x86_64'
$taskRuntimeRequired = @('Dev_ancient_naval.dll', 'DevAncientNaval.Core.dll', 'GodotSharp.dll', 'coreclr.dll', 'hostfxr.dll', 'System.Private.CoreLib.dll')
$taskSourceRequired = @('project.godot', 'export_presets.cfg', 'Dev_ancient_naval.csproj', 'src/Core/DevAncientNaval.Core.csproj', 'tests/CoreChecks/CoreChecks.csproj', 'scenes/Main.tscn', 'data/balance.json', 'README.md', 'AGENTS.md')

function Assert-SafeArchiveName([string]$Name) {
    if ($Name -match '(^/|\\|^[A-Za-z]:|(^|/)\.\.(/|$))') { throw "Unsafe archive member: $Name" }
}

function Test-ForbiddenResource([string]$Name) {
    $taskName = $Name.Replace('res://', '').Replace('\', '/').TrimStart('/')
    return $taskName -match '(^|/)(docs|tests|tools|releases|release-evidence|work|\.local|\.git|bin|obj|__MACOSX)(/|$)|(^|/)Sound FX Starter Pack Vol\. 1/|\.(pdb|keystore|jks)$|(^|/)(export_credentials\.cfg|NuGet\.local\.config|Preview map\.cmd)$'
}

function Read-PckInventory([IO.Stream]$Stream) {
    # Standalone unencrypted Godot PCK v2/v3/v4 directory layout. See the
    # versioned engine loader linked in RELEASE-PROCEDURE-v020.7.md.
    if (-not $Stream.CanSeek) { throw 'PCK inspection requires a seekable stream.' }
    $taskReader = [IO.BinaryReader]::new($Stream, [Text.Encoding]::UTF8, $true)
    try {
        if ($taskReader.ReadUInt32() -ne 0x43504447) { throw 'Not a standalone Godot PCK.' }
        $taskFormat = $taskReader.ReadUInt32()
        if ($taskFormat -notin @(2, 3, 4)) { throw "Unsupported PCK format $taskFormat; inspect the engine loader before changing this check." }
        $taskEngine = @($taskReader.ReadUInt32(), $taskReader.ReadUInt32(), $taskReader.ReadUInt32())
        if (($taskEngine -join '.') -ne '4.7.2') { throw "Finalized v020.7 requires Godot 4.7.2 PCK, got $($taskEngine -join '.')." }
        $taskFlags = $taskReader.ReadUInt32()
        if (($taskFlags -band 1) -ne 0) { throw 'Encrypted PCK directory cannot be audited by this release tool.' }
        if (($taskFlags -band 4) -ne 0) { throw 'Sparse/bundled PCK is not the self-contained release format.' }
        $taskFileBase = $taskReader.ReadUInt64()
        if ($taskFormat -ge 3) {
            $taskDirectory = $taskReader.ReadUInt64()
            if ($taskDirectory -gt [uint64]$Stream.Length) { throw 'PCK directory offset is outside the pack.' }
            $Stream.Position = [long]$taskDirectory
        } else { $null = $taskReader.ReadBytes(64) }
        $taskCount = $taskReader.ReadUInt32()
        if ($taskCount -eq 0 -or $taskCount -gt 1000000) { throw "Invalid PCK entry count: $taskCount" }
        $taskPaths = [Collections.Generic.List[string]]::new()
        $taskSeen = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        for ($taskIndex = 0; $taskIndex -lt $taskCount; $taskIndex++) {
            $taskLength = $taskReader.ReadUInt32()
            if ($taskLength -eq 0 -or $taskLength -gt 1048576) { throw 'Invalid PCK path length.' }
            $taskBytes = $taskReader.ReadBytes([int]$taskLength)
            if ($taskBytes.Length -ne $taskLength) { throw 'Truncated PCK path.' }
            $taskRawName = [Text.Encoding]::UTF8.GetString($taskBytes).TrimEnd([char]0)
            # Current Godot exports store relative names, while older directories
            # may use res://. Canonicalize both spellings before required-resource
            # and duplicate checks; aliases must not hide a duplicated resource.
            $taskRelativeName = if ($taskRawName.StartsWith('res://', [StringComparison]::Ordinal)) { $taskRawName.Substring(6) } else { $taskRawName }
            Assert-SafeArchiveName $taskRelativeName
            if ([string]::IsNullOrEmpty($taskRelativeName) -or $taskRelativeName -match '(^|/)\.(/|$)|//|:') { throw "Invalid PCK resource path: $taskRawName" }
            $taskName = 'res://' + $taskRelativeName
            $taskOffset = $taskReader.ReadUInt64()
            $taskSize = $taskReader.ReadUInt64()
            if ($taskReader.ReadBytes(16).Length -ne 16) { throw 'Truncated PCK digest.' }
            $taskFileFlags = $taskReader.ReadUInt32()
            if ($taskFileFlags -ne 0) { throw "Unexpected encrypted/removal/delta PCK resource flags ($taskFileFlags): $taskName" }
            if (($taskFileBase + $taskOffset + $taskSize) -gt [uint64]$Stream.Length) { throw "PCK resource points outside the pack: $taskName" }
            if (Test-ForbiddenResource $taskName) { throw "Development content found inside PCK: $taskName" }
            if (-not $taskSeen.Add($taskName)) { throw "Duplicate PCK resource: $taskName" }
            $taskPaths.Add($taskName)
        }
        $taskPaths.Sort([StringComparer]::Ordinal)
        # English is the compiled primary catalog; UK/NL are external JSON.
        foreach ($taskRequired in @('res://project.binary', 'res://data/balance.json', 'res://data/localization/uk.json', 'res://data/localization/nl.json')) {
            if ($taskPaths -notcontains $taskRequired) { throw "Missing player PCK resource: $taskRequired" }
        }
        return [pscustomobject]@{ Format = $taskFormat; Engine = ($taskEngine -join '.'); Count = $taskCount; Paths = $taskPaths.ToArray() }
    } finally { $taskReader.Dispose() }
}

function Test-PlayerNames([string[]]$Names) {
    $taskSeen = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($taskName in $Names) {
        Assert-SafeArchiveName $taskName
        if (-not $taskSeen.Add($taskName)) { throw "Duplicate player archive member: $taskName" }
        if ($taskName -match '\.pdb$' -or (Test-ForbiddenResource $taskName)) { throw "Development file in player package: $taskName" }
        if ($taskName -notin @('Ancient Naval.exe', 'Ancient Naval.pck', 'READ_ME_RU.md') -and -not $taskName.StartsWith("$taskRuntimeName/", [StringComparison]::Ordinal)) { throw "Unexpected player file: $taskName" }
    }
    foreach ($taskRequired in @('Ancient Naval.exe', 'Ancient Naval.pck', 'READ_ME_RU.md')) {
        if ($Names -notcontains $taskRequired) { throw "Missing player file: $taskRequired" }
    }
    foreach ($taskRequired in $taskRuntimeRequired) {
        if ($Names -notcontains "$taskRuntimeName/$taskRequired") { throw "Missing .NET runtime/assembly: $taskRequired" }
    }
}

$taskResults = [Collections.Generic.List[object]]::new()
$taskPckResult = $null
if ($PlayerDirectory) {
    $taskPlayer = (Resolve-Path -LiteralPath $PlayerDirectory).Path.TrimEnd('\', '/')
    $taskFiles = @(Get-ChildItem -LiteralPath $taskPlayer -Recurse -File -Force)
    $taskNames = @($taskFiles | ForEach-Object { $_.FullName.Substring($taskPlayer.Length + 1).Replace('\', '/') })
    Test-PlayerNames $taskNames
    $PckPath = Join-Path $taskPlayer 'Ancient Naval.pck'
    if ($RuntimeReferencePath) {
        $taskReference = (Resolve-Path -LiteralPath $RuntimeReferencePath).Path.TrimEnd('\', '/')
        $taskRuntime = Join-Path $taskReference $taskRuntimeName
        foreach ($taskFile in Get-ChildItem -LiteralPath $taskRuntime -Recurse -File -Force) {
            if ($taskFile.Extension -eq '.pdb') { continue }
            $taskRelative = $taskFile.FullName.Substring($taskReference.Length + 1)
            $taskTarget = Join-Path $taskPlayer $taskRelative
            if (-not (Test-Path -LiteralPath $taskTarget -PathType Leaf)) { throw "Exporter runtime file was omitted: $taskRelative" }
            if ((Get-FileHash -LiteralPath $taskTarget -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath $taskFile.FullName -Algorithm SHA256).Hash) { throw "Runtime file changed during packaging: $taskRelative" }
        }
    }
    $taskResults.Add([pscustomobject]@{ Check = 'PlayerDirectory'; Files = $taskFiles.Count })
}
if ($PckPath) {
    $taskStream = [IO.File]::OpenRead((Resolve-Path -LiteralPath $PckPath).Path)
    try { $taskPckResult = Read-PckInventory $taskStream } finally { $taskStream.Dispose() }
    $taskResults.Add([pscustomobject]@{ Check = 'PckDirectory'; Format = $taskPckResult.Format; Engine = $taskPckResult.Engine; Files = $taskPckResult.Count })
}
if ($PlayerZip) {
    $taskArchive = [IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $PlayerZip).Path)
    try {
        $taskPrefix = 'Ancient Naval v020.7/'
        $taskNames = @($taskArchive.Entries | ForEach-Object {
            if (-not $_.FullName.StartsWith($taskPrefix, [StringComparison]::Ordinal)) { throw "Unexpected player ZIP root: $($_.FullName)" }
            $_.FullName.Substring($taskPrefix.Length)
        })
        Test-PlayerNames $taskNames
        $taskMemory = [IO.MemoryStream]::new()
        $taskInput = $taskArchive.GetEntry("${taskPrefix}Ancient Naval.pck").Open()
        try {
            $taskInput.CopyTo($taskMemory)
            $taskMemory.Position = 0
            $taskPckResult = Read-PckInventory $taskMemory
        } finally { $taskInput.Dispose(); $taskMemory.Dispose() }
        $taskResults.Add([pscustomobject]@{ Check = 'PlayerZip'; Files = $taskNames.Count; PckFiles = $taskPckResult.Count })
    } finally { $taskArchive.Dispose() }
}
if ($SourceZip) {
    $taskArchive = [IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $SourceZip).Path)
    try {
        $taskNames = @($taskArchive.Entries | ForEach-Object { $_.FullName })
        $taskSeen = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        foreach ($taskName in $taskNames) {
            Assert-SafeArchiveName $taskName
            if (-not $taskSeen.Add($taskName)) { throw "Duplicate source archive member: $taskName" }
            if ($taskName -notmatch '^dev-ancient-naval/' -or $taskName -match '(^|/)(releases|\.godot|\.git|\.local|bin|obj|work|__MACOSX)(/|$)|\.(pdb|keystore|jks|exe|pck|zip)$|(^|/)(export_credentials\.cfg|NuGet\.local\.config|\.DS_Store)$') { throw "Generated/private/archive content in source ZIP: $taskName" }
        }
        foreach ($taskRequired in $taskSourceRequired) {
            if ($taskNames -notcontains "dev-ancient-naval/$taskRequired") { throw "Source ZIP is incomplete: $taskRequired" }
        }
        $taskInventoryEntry = $taskArchive.GetEntry('dev-ancient-naval/release-evidence/SOURCE-INVENTORY.json')
        if ($null -eq $taskInventoryEntry) { throw 'Source ZIP lacks its byte inventory.' }
        $taskReader = [IO.StreamReader]::new($taskInventoryEntry.Open(), [Text.Encoding]::UTF8)
        try { $taskInventory = $taskReader.ReadToEnd() | ConvertFrom-Json } finally { $taskReader.Dispose() }
        $taskInventoried = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        foreach ($taskRecord in $taskInventory.files) {
            Assert-SafeArchiveName $taskRecord.path
            if (-not $taskInventoried.Add("dev-ancient-naval/$($taskRecord.path)")) { throw 'Duplicate source inventory record.' }
            $taskEntry = $taskArchive.GetEntry("dev-ancient-naval/$($taskRecord.path)")
            if ($null -eq $taskEntry) { throw "Missing inventoried source: $($taskRecord.path)" }
            $taskInput = $taskEntry.Open()
            $taskDigest = [Security.Cryptography.SHA256]::Create()
            try { $taskActual = [BitConverter]::ToString($taskDigest.ComputeHash($taskInput)).Replace('-', '').ToLowerInvariant() }
            finally { $taskDigest.Dispose(); $taskInput.Dispose() }
            if ($taskActual -ne $taskRecord.sha256) { throw "Source byte inventory mismatch: $($taskRecord.path)" }
        }
        foreach ($taskName in $taskNames) {
            if (-not $taskName.StartsWith('dev-ancient-naval/release-evidence/', [StringComparison]::Ordinal) -and -not $taskInventoried.Contains($taskName)) { throw "Source ZIP member omitted from inventory: $taskName" }
        }
        $taskResults.Add([pscustomobject]@{ Check = 'SourceZip'; Files = $taskNames.Count; InventoriedSourceFiles = $taskInventoried.Count })
    } finally { $taskArchive.Dispose() }
}
if ($ManifestPath) {
    $taskManifest = (Resolve-Path -LiteralPath $ManifestPath).Path
    $taskManifestRoot = Split-Path -Parent $taskManifest
    $taskRecords = 0
    foreach ($taskLine in Get-Content -LiteralPath $taskManifest) {
        if ($taskLine -notmatch '^([a-fA-F0-9]{64})  (.+)$') { throw 'Invalid SHA256 manifest record.' }
        $taskExpected = $Matches[1]
        $taskName = $Matches[2]
        Assert-SafeArchiveName $taskName
        $taskFile = Join-Path $taskManifestRoot $taskName
        if ((Get-FileHash -LiteralPath $taskFile -Algorithm SHA256).Hash -ne $taskExpected) { throw "SHA256 mismatch: $taskName" }
        $taskRecords++
    }
    $taskResults.Add([pscustomobject]@{ Check = 'SHA256'; Files = $taskRecords })
}
if ($PckInventoryPath) {
    if ($null -eq $taskPckResult) { throw 'A PCK or player input is required to write the PCK inventory.' }
    if (Test-Path -LiteralPath $PckInventoryPath) { throw "Inventory already exists: $PckInventoryPath" }
    [IO.File]::WriteAllLines([IO.Path]::GetFullPath($PckInventoryPath), $taskPckResult.Paths, [Text.UTF8Encoding]::new($false))
}
if ($taskResults.Count -eq 0) { throw 'Supply a player directory/ZIP, source ZIP, PCK or manifest to verify.' }
$taskResults.ToArray()
