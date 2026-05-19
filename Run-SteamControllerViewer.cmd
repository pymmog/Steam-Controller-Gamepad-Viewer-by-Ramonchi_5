@echo off
setlocal

set "REPO=%~dp0"
set "PROJECT=%REPO%src\SteamControllerGamepadViewer\SteamControllerGamepadViewer.csproj"

cd /d "%REPO%"
dotnet run --project "%PROJECT%" --urls "http://127.0.0.1:31337"
