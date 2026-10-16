@echo off
setlocal
powershell.exe -NoLogo -NoProfile -Command "$ErrorActionPreference='Stop'; [Net.ServicePointManager]::SecurityProtocol=[Net.SecurityProtocolType]::Tls12; $installer=Join-Path ([IO.Path]::GetTempPath()) ('AncientNaval-'+[Guid]::NewGuid().ToString('N')+'.ps1'); try { $client=New-Object Net.WebClient; try { $client.DownloadFile('https://raw.githubusercontent.com/Beygelman/Dev_ancient_naval/codex/ancient-naval-v020.9x1/tools/Install-PlayableRelease.ps1',$installer) } finally { $client.Dispose() }; if ((Get-FileHash -LiteralPath $installer -Algorithm SHA256).Hash.ToLowerInvariant() -ne 'f18b3dcb154dc32f5cf528142a2e406a88c3376e8ee178a05497028630c5b8ba') { throw 'Installer checksum mismatch.' }; & powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File $installer -Launch; if ($LASTEXITCODE -ne 0) { throw 'Game preparation failed. Existing releases were preserved.' } } finally { if (Test-Path -LiteralPath $installer) { Remove-Item -LiteralPath $installer -Force } }"
if errorlevel 1 (
  echo Could not prepare Ancient Naval. Read the error above.
  pause
  exit /b 1
)
endlocal
