#!/bin/bash
set -e
cd "$(dirname "$0")"

# Optional: point to a specific SDL3 library via SDL3_PATH or --sdl3 <path>
# SDL3 is searched automatically in common system locations.
# On Bazzite/SteamOS you can also run: SDL3_PATH=/path/to/libSDL3.so.0 ./run.sh

dotnet run --project src/SteamControllerGamepadViewer/SteamControllerGamepadViewer.csproj "$@"
