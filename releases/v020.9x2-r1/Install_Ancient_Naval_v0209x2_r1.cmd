@echo off
setlocal
powershell.exe -NoLogo -NoProfile -Command "$ErrorActionPreference='Stop'; [Net.ServicePointManager]::SecurityProtocol=[Net.SecurityProtocolType]::Tls12; $installer=Join-Path ([IO.Path]::GetTempPath()) ('AncientNaval-'+[Guid]::NewGuid().ToString('N')+'.ps1'); try { $client=New-Object Net.WebClient; try { $client.DownloadFile('https://raw.githubusercontent.com/Beygelman/Dev_ancient_naval/codex/ancient-naval-v020.9x2/releases/v020.9x2-r1/Install-PlayableRelease.ps1',$installer) } finally { $client.Dispose() }; if ((Get-FileHash -LiteralPath $installer -Algorithm SHA256).Hash.ToLowerInvariant() -ne '75dd784f28f8b74f6a2d7086f3d881a7eb842419b04a5947ff28a98536de9129') { throw 'Installer checksum mismatch.' }; & powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File $installer -Launch; if ($LASTEXITCODE -ne 0) { throw 'Game preparation failed.' } } finally { if (Test-Path -LiteralPath $installer) { Remove-Item -LiteralPath $installer -Force } }"
if errorlevel 1 (
  pause
  exit /b 1
)
endlocal
