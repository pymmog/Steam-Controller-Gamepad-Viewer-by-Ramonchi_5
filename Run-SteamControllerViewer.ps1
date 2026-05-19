$ErrorActionPreference = "Stop"

$repo = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $repo "src\SteamControllerGamepadViewer\SteamControllerGamepadViewer.csproj"

dotnet run --project $project --urls "http://127.0.0.1:31337"
