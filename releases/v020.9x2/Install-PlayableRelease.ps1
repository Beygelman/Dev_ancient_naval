[CmdletBinding()]
param(
    [string]$ProjectRoot = 'C:\__Beygelman\! -11\dev-ancient-naval',
    [string]$ArchivePath,
    [switch]$Launch
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$version = 'v020.9x2'
$archiveName = "Ancient_Naval_${version}_Windows.zip"
$expectedHash = 'dbae71452080480ef728ab1c192eeec2f3a2fe240162bdf4637cb2e643421159'
$release = Join-Path ([IO.Path]::GetFullPath($ProjectRoot)) "releases\$version"
$playable = Join-Path $release 'playable'
$stage = $null
$download = $null
Add-Type -AssemblyName System.IO.Compression.FileSystem

function Assert-Archive([string]$Path) {
    if ((Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expectedHash) {
        throw "Archive checksum mismatch. Existing files were preserved: $Path"
    }
}

function Assert-Playable([string]$Directory, [string]$ZipPath) {
    foreach ($required in @('Ancient Naval.exe', 'Ancient Naval.pck',
        'data_Dev_ancient_naval_windows_x86_64\Dev_ancient_naval.dll',
        'data_Dev_ancient_naval_windows_x86_64\coreclr.dll')) {
        if (!(Test-Path -LiteralPath (Join-Path $Directory $required) -PathType Leaf)) {
            throw "Incomplete player: $required"
        }
    }
    $zip = [IO.Compression.ZipFile]::OpenRead($ZipPath)
    try {
        foreach ($entry in $zip.Entries) {
            if ($entry.FullName.EndsWith('/')) { continue }
            $target = Join-Path $Directory $entry.FullName
            if (!(Test-Path -LiteralPath $target -PathType Leaf)) { throw "Missing player file: $target" }
            $sha = [Security.Cryptography.SHA256]::Create()
            $stream = $entry.Open()
            try { $entryHash = [BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-', '') }
            finally { $stream.Dispose(); $sha.Dispose() }
            if ((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $entryHash) {
                throw "Player file differs; existing files were preserved: $target"
            }
        }
    } finally { $zip.Dispose() }
}

try {
    if (!$ArchivePath) {
        $ArchivePath = Join-Path $release $archiveName
        if (!(Test-Path -LiteralPath $ArchivePath)) {
            New-Item -ItemType Directory -Force -Path $release | Out-Null
            $download = Join-Path $release ('.download-' + [Guid]::NewGuid().ToString('N') + '.zip')
            $url = "https://raw.githubusercontent.com/Beygelman/Dev_ancient_naval/codex/ancient-naval-v020.9x2/releases/$version/$archiveName"
            [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
            Write-Output "Downloading Ancient Naval $version..."
            $client = New-Object Net.WebClient
            try { $client.DownloadFile($url, $download) } finally { $client.Dispose() }
            Assert-Archive $download
            Move-Item -LiteralPath $download -Destination $ArchivePath
            $download = $null
        }
    }
    $ArchivePath = (Resolve-Path -LiteralPath $ArchivePath).Path
    Assert-Archive $ArchivePath
    if (Test-Path -LiteralPath $playable) {
        Assert-Playable $playable $ArchivePath
    } else {
        New-Item -ItemType Directory -Force -Path $release | Out-Null
        $stage = Join-Path $release ('.prepare-' + [Guid]::NewGuid().ToString('N'))
        [IO.Compression.ZipFile]::ExtractToDirectory($ArchivePath, $stage)
        Assert-Playable $stage $ArchivePath
        [IO.Directory]::Move($stage, $playable)
        $stage = $null
    }
    $exe = Join-Path $playable 'Ancient Naval.exe'
    Write-Output "READY: $exe"
    if ($Launch) { Start-Process -FilePath $exe -WorkingDirectory $playable }
} finally {
    if ($download -and (Test-Path -LiteralPath $download)) { Remove-Item -LiteralPath $download -Force }
    if ($stage -and (Test-Path -LiteralPath $stage)) { Remove-Item -LiteralPath $stage -Recurse -Force }
}
