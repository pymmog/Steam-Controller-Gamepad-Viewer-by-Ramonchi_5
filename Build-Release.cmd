@echo off
setlocal

set "RELEASE_VERSION=%~1"
if "%RELEASE_VERSION%"=="" set "RELEASE_VERSION=v1.0.0"

set "ROOT=%~dp0"
set "PROJECT=%ROOT%src\SteamControllerGamepadViewer\SteamControllerGamepadViewer.csproj"
set "RELEASE_ROOT=%ROOT%artifacts\release"
set "BASE_NAME=Steam Controller Viewer %RELEASE_VERSION% win-x64"
set "BASE_OUT=%RELEASE_ROOT%\%BASE_NAME%"

dotnet publish "%PROJECT%" -c Release -r win-x64 --self-contained true -o "%BASE_OUT%" -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false
if errorlevel 1 exit /b %errorlevel%

call :CleanPublish "%BASE_OUT%"
call :ZipFolder "%BASE_OUT%" "%RELEASE_ROOT%\%BASE_NAME%.zip"
if errorlevel 1 exit /b %errorlevel%

call :MakeVariant "%BASE_NAME% (start with Windows)" "%ROOT%release-assets\Install Start With Windows.cmd" "%ROOT%release-assets\Uninstall Start With Windows.cmd"
if errorlevel 1 exit /b %errorlevel%

echo Created release zips in "%RELEASE_ROOT%"
exit /b 0

:CleanPublish
set "OUT=%~1"
if exist "%OUT%\wwwroot" rmdir /s /q "%OUT%\wwwroot"
del /q "%OUT%\*.staticwebassets.*.json" 2>nul
del /q "%OUT%\web.config" 2>nul
del /q "%OUT%\*.pdb" 2>nul
exit /b 0

:MakeVariant
set "VARIANT_NAME=%~1"
set "VARIANT_OUT=%RELEASE_ROOT%\%VARIANT_NAME%"
if exist "%VARIANT_OUT%" rmdir /s /q "%VARIANT_OUT%"
mkdir "%VARIANT_OUT%"
xcopy "%BASE_OUT%\*" "%VARIANT_OUT%\" /E /I /Y >nul
:CopyVariantFiles
shift
if "%~1"=="" goto VariantReady
copy "%~1" "%VARIANT_OUT%\" >nul
goto CopyVariantFiles
:VariantReady
call :ZipFolder "%VARIANT_OUT%" "%RELEASE_ROOT%\%VARIANT_NAME%.zip"
exit /b %errorlevel%

:ZipFolder
set "ZIP_SOURCE=%~1"
set "ZIP_PATH=%~2"
powershell -NoProfile -ExecutionPolicy Bypass -Command "if (Test-Path -LiteralPath $env:ZIP_PATH) { Remove-Item -LiteralPath $env:ZIP_PATH -Force }; Compress-Archive -Path (Join-Path $env:ZIP_SOURCE '*') -DestinationPath $env:ZIP_PATH -Force"
exit /b %errorlevel%
