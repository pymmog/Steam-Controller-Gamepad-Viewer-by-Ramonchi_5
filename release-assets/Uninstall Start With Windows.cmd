@echo off
setlocal

set "SHORTCUT=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup\Steam Controller Gamepad Viewer.lnk"

if exist "%SHORTCUT%" (
  del "%SHORTCUT%"
  echo Removed startup shortcut.
) else (
  echo Startup shortcut was not installed.
)

pause
