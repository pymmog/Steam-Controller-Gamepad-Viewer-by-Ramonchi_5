@echo off
setlocal

set "ROOT=%~dp0"
set "VIEWER=%ROOT%SteamControllerGamepadViewer.exe"

if not exist "%VIEWER%" (
  echo SteamControllerGamepadViewer.exe was not found next to this script.
  pause
  exit /b 1
)

tasklist /FI "IMAGENAME eq SteamControllerGamepadViewer.exe" | find /I "SteamControllerGamepadViewer.exe" >nul
if errorlevel 1 start "" "%VIEWER%"

call :StartIfExists "%ProgramFiles%\obs-studio\bin\64bit\obs64.exe"
if not errorlevel 1 exit /b 0

call :StartIfExists "%ProgramFiles(x86)%\obs-studio\bin\64bit\obs64.exe"
if not errorlevel 1 exit /b 0

echo OBS Studio was not found in the default install folders.
echo The viewer has been started; please open OBS manually.
pause
exit /b 1

:StartIfExists
if exist "%~1" (
  start "" /D "%~dp1" "%~1"
  exit /b 0
)
exit /b 1
