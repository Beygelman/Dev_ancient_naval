param([Parameter(Mandatory = $true)][string]$Godot, [Parameter(Mandatory = $true)][string]$Output)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$match = Select-String -Path (Join-Path $projectRoot 'project.godot') -Pattern '^config/version="([^"]+)"'
$version = $match.Matches[0].Groups[1].Value
if ($version -notmatch '^v[0-9][A-Za-z0-9.]*(-r[1-9][0-9]*)?$') { throw 'Invalid project version.' }
if (Test-Path -LiteralPath $Output) { throw 'Use a fresh output directory.' }
New-Item -ItemType Directory -Path $Output | Out-Null
$player = Join-Path $Output 'player'
New-Item -ItemType Directory -Path $player | Out-Null

# Match Export-FinalizedWindows.ps1: launch the real executable, not Godot's
# console wrapper, and capture native process logs without a shell pipeline.
function Invoke-CapturedProcess([string]$File, [string[]]$Arguments, [string]$Name, [int]$Seconds = 300) {
    $stdout = Join-Path $Output "$Name.stdout.log"
    $stderr = Join-Path $Output "$Name.stderr.log"
    $start = @{
        FilePath = $File
        ArgumentList = @($Arguments | ForEach-Object { '"' + $_ + '"' })
        WorkingDirectory = $projectRoot
        WindowStyle = 'Hidden'
        PassThru = $true
        RedirectStandardOutput = $stdout
        RedirectStandardError = $stderr
    }
    Write-Output "START: $Name"
    $process = Start-Process @start
    if (!$process.WaitForExit($Seconds * 1000)) {
        $process.Kill($true)
        throw "$Name timed out after $Seconds seconds. Logs: $Output"
    }
    $process.WaitForExit()
    if ($process.ExitCode -ne 0) { throw "$Name failed with exit code $($process.ExitCode). Logs: $Output" }
    if (Select-String -Path @($stdout, $stderr) -Pattern '^\s*(ERROR|SCRIPT ERROR):|Unhandled exception|Build FAILED' -Quiet) {
        throw "$Name reported an error. Logs: $Output"
    }
    Write-Output "PASS: $Name"
}
Push-Location $projectRoot
try {
    Invoke-CapturedProcess -File $Godot -Arguments @('--headless', '--editor', '--path', $projectRoot, '--import', '--quit') -Name 'import'
    $exe = Join-Path $player 'Ancient Naval.exe'
    Invoke-CapturedProcess -File $Godot -Arguments @('--headless', '--path', $projectRoot, '--export-release', 'Windows Desktop', $exe) -Name 'export' -Seconds 600
    if (!(Test-Path -LiteralPath $exe)) { throw 'Windows executable missing after export.' }
    Get-ChildItem -LiteralPath $player -Filter '*.pdb' -Recurse | Remove-Item
    @"
Ancient Naval $version — экспериментальная версия
Запустите Ancient Naval.exe. Держите все файлы из архива вместе.
Новая партия включает экспериментальную экономику, рейды, развитие городов и роли кораблей.
Боты получают сохраняемые стратегические характеры. Материнский корабль не изменён.
Продолжение сохраняет прежние правила партии.
Камера и быстрые действия исправлены для всех сохранений.
"@ | Set-Content -LiteralPath (Join-Path $player 'READ_ME_RU.txt') -Encoding utf8
    $smoke = Join-Path $Output 'smoke'
    New-Item -ItemType Directory -Path $smoke | Out-Null
    $smokeSave = Join-Path $smoke 'save.json'
    Invoke-CapturedProcess -File $exe -Arguments @('--headless', '--audio-driver', 'Dummy', '--quit-after', '90', '--', "--save-file=$smokeSave") -Name 'player' -Seconds 120
    $zip = Join-Path $Output "Ancient_Naval_${version}_Windows.zip"
    Compress-Archive -Path (Join-Path $player '*') -DestinationPath $zip -CompressionLevel Optimal
    $hash = Get-FileHash -LiteralPath $zip -Algorithm SHA256
    "$($hash.Hash.ToLowerInvariant())  $([IO.Path]::GetFileName($zip))" | Set-Content (Join-Path $Output 'SHA256.txt') -Encoding ascii
    Write-Output "PASS: Windows exported player startup and package $version"
} finally { Pop-Location }

