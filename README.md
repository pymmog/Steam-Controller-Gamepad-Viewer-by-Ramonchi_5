# Steam Controller Gamepad Viewer

Local Steam Controller overlay for OBS. It runs a small local-only web server on `127.0.0.1`, reads the Steam Controller through SDL3 plus Valve HID reports, and draws a Steam Controller viewer with live trackpad finger tracking.

This is a browser-source overlay for the first release. A native OBS plugin would be a better long-term version, but the local URL works offline once the app is running and does not require gamepadviewer.com or any internet service.

## How It Runs

The URL only works while `SteamControllerGamepadViewer.exe` is running. After restarting Windows, run `SteamControllerGamepadViewer.exe` again before OBS can load the local URL.

The portable release starts only when you explicitly run `SteamControllerGamepadViewer.exe`. If you want to avoid clicking the exe every time, use the `Start with Windows` release instead.

There are two v1 release options:

- Portable: run `SteamControllerGamepadViewer.exe` whenever you want the OBS URL to work.
- Start with Windows: includes install/uninstall scripts for a Windows Startup shortcut.

## AI Disclosure

This viewer was coded with help from OpenAI Codex. The project is human-directed and reviewed, but AI assistance was part of the implementation. If AI-assisted software is a deal-breaker for you, please use another viewer or OBS input overlay.

## Run

### Release build

1. Download and extract the release zip.
2. Double-click `SteamControllerGamepadViewer.exe`.
3. Add this URL as an OBS Browser Source:

```text
http://127.0.0.1:31337/?clean=1&title=0
```

The standard v1 release zip only needs these files:

- `SteamControllerGamepadViewer.exe`
- `README.md`
- `LICENSE`
- `THIRD_PARTY_NOTICES.md`

### From source

Use Command Prompt or double-click:

```text
Run-SteamControllerViewer.cmd
```

That script uses `dotnet run` and avoids PowerShell execution-policy issues. The PowerShell script is kept only as a developer convenience.

## OBS Setup

Add a Browser Source with:

```text
URL: http://127.0.0.1:31337/?clean=1&title=0
Width: 1200
Height: 900
Custom CSS: leave empty
```

Useful URL options:

- `?clean=1` hides the status label.
- `?title=0` hides the title.
- `?debug=1` shows connection/status text.
- `?bg=solid` gives the page a dark background for normal browser testing.
- `?preview=all` forces overlay layers visible for visual checking.

The URL is local loopback, not a website. If the URL does not load, start the app first.

## Controller Support

The current target is Valve's Steam Controller. The app does not open Steam's controller tester window; it reads current state and redraws the overlay directly, so button releases are shown as releases too.

Supported inputs:

- ABXY, dpad, bumpers, analog triggers, sticks, L3/R3, Steam/View/Menu.
- Left and right trackpad touch position, click pressure, and live finger position.
- Four rear grip buttons.

SDL3 is loaded from the app folder first, then from the default Steam install folders. You can override the path with `--sdl3 "C:\path\to\SDL3.dll"` or the `SDL3_PATH` environment variable.

If SDL3 cannot open the controller after a firmware update, the app falls back to fresh Valve HID reports when they are available. That keeps the overlay connected for Steam Controller firmware/device-id changes that still expose the same raw HID report shape.

## Building Release Zips

From a source checkout:

```text
Build-Release.cmd v1.0.0
```

Release zips are created under `artifacts\release`:

- `Steam Controller Viewer v1.0.0 win-x64.zip`
- `Steam Controller Viewer v1.0.0 win-x64 (start with Windows).zip`

Upload those zips to GitHub Releases. Do not upload the normal `publish` folder unless you specifically want a framework-dependent developer build.

## License And Notices

The original source code in this repository is MIT licensed. Third-party components, referenced projects, and Valve/Steam assets are documented in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

Important: the Steam Controller name, Steam name, Valve trademarks, and the controller artwork/assets remain Valve property. This project is unofficial and not endorsed by Valve.

## Repository

GitHub target:

```text
https://github.com/ramonchi5/Steam-Controller-Gamepad-Viewer-by-Ramonchi_5
```
