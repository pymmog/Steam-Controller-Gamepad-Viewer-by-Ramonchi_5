#!/bin/bash
set -e
cd "$(dirname "$0")"

RID="${1:-linux-x64}"
OUT="artifacts/release/${RID}"

echo "Building self-contained release for ${RID}..."
dotnet publish src/SteamControllerGamepadViewer/SteamControllerGamepadViewer.csproj \
    -c Release \
    -r "${RID}" \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:PublishTrimmed=false \
    -o "${OUT}"

echo ""
echo "Done. Output: ${OUT}/"
echo ""
echo "Run with:"
echo "  ${OUT}/SteamControllerGamepadViewer"
echo ""
echo "Then open http://127.0.0.1:31337 in a browser or OBS browser source."
echo ""
echo "Notes:"
echo "  - SDL3 (libSDL3.so.0) must be installed or on LD_LIBRARY_PATH."
echo "  - On Bazzite/SteamOS: SDL3 may be available via the Steam Runtime."
echo "  - Override SDL3 path: SDL3_PATH=/path/to/libSDL3.so.0 ./SteamControllerGamepadViewer"
