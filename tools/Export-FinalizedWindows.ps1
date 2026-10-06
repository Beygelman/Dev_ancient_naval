[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$GodotPath,
    [Parameter(Mandatory = $true)][string]$OutputPath,
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$Preset = 'Windows Desktop',
    [ValidateSet('v020.7', 'v020.7b')][string]$Version = 'v020.7'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$taskRoot = (Resolve-Path -LiteralPath $ProjectRoot).Path.TrimEnd('\', '/')
$taskGodot = (Resolve-Path -LiteralPath $GodotPath).Path
$taskOutput = [IO.Path]::GetFullPath($OutputPath).TrimEnd('\', '/')
if (-not (Test-Path -LiteralPath (Join-Path $taskRoot 'project.godot') -PathType Leaf)) { throw 'Project root does not contain project.godot.' }
if ($taskOutput.StartsWith($taskRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or $taskOutput -eq $taskRoot) { throw 'Export outside the project tree to prevent recursive export pollution.' }
if (Test-Path -LiteralPath $taskOutput) { throw "Output already exists; choose a fresh export directory: $taskOutput" }
$taskProject = Get-Content -LiteralPath (Join-Path $taskRoot 'project.godot') -Raw
if ($taskProject -notmatch ('(?m)^config/version="' + [regex]::Escape($Version) + '"\s*$')) { throw "Project identity does not match the explicitly selected $Version release." }
$taskPresets = Get-Content -LiteralPath (Join-Path $taskRoot 'export_presets.cfg') -Raw
$taskPresetIndex = $null
foreach ($taskSection in [regex]::Matches($taskPresets, '(?ms)^\[preset\.(\d+)\]\s*\r?\n(.*?)(?=^\[|\z)')) {
    if ($taskSection.Groups[2].Value -match ('(?m)^name="' + [regex]::Escape($Preset) + '"\s*$')) {
        $taskPresetIndex = $taskSection.Groups[1].Value
        $taskSettings = $taskSection.Groups[2].Value
        break
    }
}
if ($null -eq $taskPresetIndex) { throw "Export preset does not exist: $Preset" }
if ($taskSettings -notmatch '(?m)^platform="Windows Desktop"\s*$') { throw 'The selected preset is not Windows Desktop.' }
$taskOptionsMatch = [regex]::Match($taskPresets, '(?ms)^\[preset\.' + $taskPresetIndex + '\.options\]\s*\r?\n(.*?)(?=^\[|\z)')
if (-not $taskOptionsMatch.Success) { throw 'Missing preset options.' }
$taskOptions = $taskOptionsMatch.Groups[1].Value
foreach ($taskRequired in @('dotnet/include_debug_symbols=false', 'binary_format/embed_pck=false', 'dotnet/include_scripts_content=false')) {
    if ($taskOptions -notmatch ('(?m)^' + [regex]::Escape($taskRequired) + '\s*$')) { throw "Clean export requires $taskRequired" }
}
if ($taskSettings -notmatch '(?m)^encrypt_(pck|directory)=true\s*$') {
    # The verifier reads the unencrypted directory rather than trusting the UI.
} else { throw 'Encrypted exports cannot be inspected by the finalization gate.' }
if ($taskSettings -notmatch '(?m)^exclude_filter="([^"]*)"') { throw 'Missing export exclusion filter.' }
$taskExclusions = $Matches[1].Split(',')
foreach ($taskRequired in @('releases/*', 'release-evidence/*', 'docs/*', 'tests/*', 'tools/*', 'work/*', '.local/*')) {
    if ($taskExclusions -notcontains $taskRequired) { throw "Clean export must exclude $taskRequired" }
}

New-Item -ItemType Directory -Path $taskOutput | Out-Null
$taskExport = Join-Path $taskOutput 'export'
New-Item -ItemType Directory -Path $taskExport | Out-Null
$taskExecutable = Join-Path $taskExport 'Ancient Naval.exe'
$taskStdout = Join-Path $taskOutput 'godot-export.stdout.log'
$taskStderr = Join-Path $taskOutput 'godot-export.stderr.log'
$taskArguments = @('--headless', '--path', ('"' + $taskRoot + '"'), '--export-release', ('"' + $Preset + '"'), ('"' + $taskExecutable + '"'))
# Use the actual Godot executable, not the console wrapper whose launcher may
# outlive its child. All paths are passed as arguments, never shell commands.
$taskProcess = Start-Process -FilePath $taskGodot -ArgumentList $taskArguments -WorkingDirectory $taskRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput $taskStdout -RedirectStandardError $taskStderr
$taskProcess.WaitForExit()
$taskProcess.Refresh()
$taskExitCode = $taskProcess.ExitCode
[IO.File]::WriteAllText((Join-Path $taskOutput 'godot-export.exit.txt'), "$taskExitCode`n", [Text.UTF8Encoding]::new($false))
if ($taskExitCode -ne 0) { throw "Godot export failed ($taskExitCode). Full logs: $taskOutput" }
$taskLog = (Get-Content -LiteralPath $taskStdout -Raw) + "`n" + (Get-Content -LiteralPath $taskStderr -Raw)
if ($taskLog -match '(?im)(^\s*(ERROR|SCRIPT ERROR):|Export failed|Exporting project failed|Could not export|Build FAILED\.)') { throw "Godot reported an export/build error. Full logs: $taskOutput" }
foreach ($taskRequired in @('Ancient Naval.exe', 'Ancient Naval.pck', 'data_Dev_ancient_naval_windows_x86_64/Dev_ancient_naval.dll')) {
    if (-not (Test-Path -LiteralPath (Join-Path $taskExport $taskRequired) -PathType Leaf)) { throw "Godot did not produce $taskRequired" }
}
& (Join-Path $PSScriptRoot 'Verify-FinalizedRelease.ps1') -PckPath (Join-Path $taskExport 'Ancient Naval.pck') -PckInventoryPath (Join-Path $taskOutput 'pck-resources.txt')
[pscustomobject]@{ Export = $taskExport; ExitCode = $taskExitCode; Logs = $taskOutput; PckInventory = (Join-Path $taskOutput 'pck-resources.txt') }
