param([Parameter(Mandatory = $true)][string]$Godot, [Parameter(Mandatory = $true)][string]$Output)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$match = Select-String -Path (Join-Path $projectRoot 'project.godot') -Pattern '^config/version="([^"]+)"'
$version = $match.Matches[0].Groups[1].Value
if ($version -notmatch '^v[0-9][A-Za-z0-9.]*$') { throw 'Invalid project version.' }
if (Test-Path -LiteralPath $Output) { throw 'Use a fresh output directory.' }
New-Item -ItemType Directory -Path $Output | Out-Null
$player = Join-Path $Output 'player'
New-Item -ItemType Directory -Path $player | Out-Null
Push-Location $projectRoot
try {
    & $Godot --headless --editor --path $projectRoot --import --quit 2>&1 | Tee-Object (Join-Path $Output 'import.log')
    if ($LASTEXITCODE -ne 0) { throw 'Godot import failed.' }
    $exe = Join-Path $player 'Ancient Naval.exe'
    & $Godot --headless --path $projectRoot --export-release 'Windows Desktop' $exe 2>&1 | Tee-Object (Join-Path $Output 'export.log')
    if ($LASTEXITCODE -ne 0 -or !(Test-Path -LiteralPath $exe)) { throw 'Windows export failed.' }
    Get-ChildItem -LiteralPath $player -Filter '*.pdb' -Recurse | Remove-Item
    @"
Ancient Naval $version — экспериментальная версия
Запустите Ancient Naval.exe. Держите все файлы из архива вместе.
Новая партия включает эксперименты боя, торговли и инфраструктуры.
Продолжение сохраняет прежние правила партии.
Камера и быстрые действия исправлены для всех сохранений.
"@ | Set-Content -LiteralPath (Join-Path $player 'READ_ME_RU.txt') -Encoding utf8
    $smoke = Join-Path $Output 'smoke'
    New-Item -ItemType Directory -Path $smoke | Out-Null
    $smokeSave = Join-Path $smoke 'save.json'
    $start = @{
        FilePath = $exe
        ArgumentList = @('--headless', '--audio-driver', 'Dummy', '--quit-after', '90', '--', "--save-file=$smokeSave")
        Wait = $true
        PassThru = $true
        RedirectStandardOutput = Join-Path $Output 'player-stdout.log'
        RedirectStandardError = Join-Path $Output 'player-stderr.log'
    }
    $process = Start-Process @start
    if ($process.ExitCode -ne 0) { throw "Exported player exited with $($process.ExitCode)." }
    if (Select-String -Path (Join-Path $Output 'player-*.log') -Pattern 'ERROR:|Unhandled exception' -Quiet) {
        throw 'Exported player reported a startup error.'
    }
    $zip = Join-Path $Output "Ancient_Naval_${version}_Windows.zip"
    Compress-Archive -Path (Join-Path $player '*') -DestinationPath $zip -CompressionLevel Optimal
    $hash = Get-FileHash -LiteralPath $zip -Algorithm SHA256
    "$($hash.Hash.ToLowerInvariant())  $([IO.Path]::GetFileName($zip))" | Set-Content (Join-Path $Output 'SHA256.txt') -Encoding ascii
    Write-Output "PASS: Windows exported player startup and package $version"
} finally { Pop-Location }

