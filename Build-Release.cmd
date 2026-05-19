@echo off
setlocal

set "RELEASE_VERSION=%~1"
if "%RELEASE_VERSION%"=="" set "RELEASE_VERSION=v1.0.0"

set "ROOT=%~dp0"
set "PROJECT=%ROOT%src\SteamControllerGamepadViewer\SteamControllerGamepadViewer.csproj"
set "RELEASE_ROOT=%ROOT%artifacts\release"
set "OUT=%RELEASE_ROOT%\SteamControllerGamepadViewer-%RELEASE_VERSION%-win-x64"
set "ZIP=%RELEASE_ROOT%\SteamControllerGamepadViewer-%RELEASE_VERSION%-win-x64.zip"

dotnet publish "%PROJECT%" -c Release -r win-x64 --self-contained true -o "%OUT%" -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false
if errorlevel 1 exit /b %errorlevel%

if exist "%OUT%\wwwroot" rmdir /s /q "%OUT%\wwwroot"
del /q "%OUT%\*.staticwebassets.*.json" 2>nul
del /q "%OUT%\web.config" 2>nul
del /q "%OUT%\Launch-Viewer.vbs" 2>nul

powershell -NoProfile -ExecutionPolicy Bypass -Command "if (Test-Path -LiteralPath $env:ZIP) { Remove-Item -LiteralPath $env:ZIP -Force }; Compress-Archive -Path (Join-Path $env:OUT '*') -DestinationPath $env:ZIP -Force"
if errorlevel 1 exit /b %errorlevel%

echo Created "%ZIP%"
