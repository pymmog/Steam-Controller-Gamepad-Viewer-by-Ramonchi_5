@echo off
setlocal

set "ROOT=%~dp0"
set "TARGET=%ROOT%SteamControllerGamepadViewer.exe"

if not exist "%TARGET%" (
  echo SteamControllerGamepadViewer.exe was not found next to this script.
  pause
  exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -Command "$shortcut = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\Startup\Steam Controller Gamepad Viewer.lnk'; $shell = New-Object -ComObject WScript.Shell; $link = $shell.CreateShortcut($shortcut); $link.TargetPath = $env:TARGET; $link.WorkingDirectory = $env:ROOT; $link.Save(); Write-Host 'Installed startup shortcut:' $shortcut"
if errorlevel 1 exit /b %errorlevel%

echo.
echo Steam Controller Gamepad Viewer will start when Windows signs in.
pause
