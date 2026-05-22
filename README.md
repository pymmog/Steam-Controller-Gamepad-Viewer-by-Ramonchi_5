# Steam Controller Gamepad Viewer

Local Steam Controller overlay for OBS. It runs a small local-only web server on `127.0.0.1`, reads the Steam Controller through SDL3 plus Valve HID reports, and draws a Steam Controller viewer with live trackpad finger tracking.

This is a browser-source overlay for the first release. A native OBS plugin would be a better long-term version, but the local URL works offline once the app is running and does not require gamepadviewer.com or any internet service.

## How It Runs

The URL only works while the app is running. After restarting your system, launch the app again before OBS can load the local URL.

**Windows:** The portable release starts only when you explicitly run `SteamControllerGamepadViewer.exe`. If you want to avoid clicking the exe every time, use the `Start with Windows` release instead.

There are two Windows v1 release options:

- Portable: run `SteamControllerGamepadViewer.exe` whenever you want the OBS URL to work.
- Start with Windows: includes install/uninstall scripts for a Windows Startup shortcut.

**Linux/Bazzite:** Run `./run.sh` from a terminal, or build a self-contained binary with `./build-release-linux.sh` and launch it directly. See the [Linux / Bazzite](#linux--bazzite) section below.

## AI Disclosure

This viewer was coded with help from OpenAI Codex. The project is human-directed and reviewed, but AI assistance was part of the implementation. If AI-assisted software is a deal-breaker for you, please use another viewer or OBS input overlay.

## Run

### Windows — release build

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

### Windows — from source

Use Command Prompt or double-click:

```text
Run-SteamControllerViewer.cmd
```

That script uses `dotnet run` and avoids PowerShell execution-policy issues. The PowerShell script is kept only as a developer convenience.

### Linux / Bazzite

#### Prerequisites

- [.NET 8 SDK or Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
- SDL3 (`libSDL3.so.0`) — see options below

**Install SDL3 on Bazzite / Fedora-based systems:**

```bash
# Bazzite uses rpm-ostree for system packages (changes apply after reboot):
sudo rpm-ostree install SDL3

# Or install the Flatpak SDL3 extension if you prefer not to rebase:
# SDL3 may also already be present via the Steam Runtime — the app checks
# ~/.local/share/Steam/ubuntu12_32/steam-runtime/ automatically.
```

**Install SDL3 on Debian/Ubuntu-based systems:**

```bash
sudo apt install libsdl3-dev
```

#### Run from source (development)

Requires .NET 8 SDK.

```bash
./run.sh
```

#### Build and install a self-contained binary (no .NET needed at runtime)

```bash
./build-release-linux.sh          # defaults to linux-x64
./build-release-linux.sh linux-arm64  # for ARM64 (e.g. Raspberry Pi)
```

The binary is placed in `artifacts/release/linux-x64/`. **Install it to a standard location before running** — systemd and some desktop environments generate service/scope names from the binary path, and long or special-character paths will cause an "Invalid unit name" error.

Use the install script (builds if needed, then offers service and udev setup):

```bash
./install-linux.sh          # linux-x64 (default)
./install-linux.sh linux-arm64
```

To remove everything the installer placed:

```bash
./uninstall-linux.sh
```

Then open `http://127.0.0.1:31337` in a browser or add it as an OBS browser source.

#### SDL3 path override

The app probes common system locations automatically. If SDL3 is installed somewhere non-standard, point to it explicitly:

```bash
# Environment variable (persists for the session):
SDL3_PATH=/path/to/libSDL3.so.0 ~/.local/bin/SteamControllerGamepadViewer

# Command-line flag:
~/.local/bin/SteamControllerGamepadViewer --sdl3 /path/to/libSDL3.so.0
```

> **Note:** Running the binary directly from a long source-checkout path (e.g. a directory with the branch name in it) will produce an `Invalid unit name` error because systemd auto-generates a scope name from the full path. Always use `./install-linux.sh` or copy manually to `~/.local/bin/` before using auto-start.

#### Linux notes

On Linux, touchpad data is read directly from the raw HID device (`/dev/hidraw*`), the same way the Windows version reads from `hid.dll`. This requires read access to the hidraw device.

**If the trackpads show no input**, the app cannot open the HID device. Re-run `./install-linux.sh` and choose `y` when prompted about the udev rule, or add your user to the `input` group manually:

```bash
sudo usermod -aG input $USER   # log out and back in after
```

On Bazzite, Steam typically holds the controller and the app reads alongside it; Steam being open is sufficient in most cases.

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

SDL3 is loaded from the app folder first, then from default Steam install folders and common system library paths. You can override the path with `--sdl3 "/path/to/libSDL3.so.0"` (or `SDL3.dll` on Windows) or the `SDL3_PATH` environment variable.

If SDL3 cannot open the controller after a firmware update, the app falls back to fresh Valve HID reports when they are available. That keeps the overlay connected for Steam Controller firmware/device-id changes that still expose the same raw HID report shape.

## Building Releases

### Windows

From a source checkout:

```text
Build-Release.cmd v1.0.0
```

Release zips are created under `artifacts\release`:

- `Steam Controller Viewer v1.0.0 win-x64.zip`
- `Steam Controller Viewer v1.0.0 win-x64 (start with Windows).zip`

Upload those zips to GitHub Releases. Do not upload the normal `publish` folder unless you specifically want a framework-dependent developer build.

### Linux

```bash
./build-release-linux.sh          # linux-x64 (default)
./build-release-linux.sh linux-arm64
```

The self-contained single-file binary is placed under `artifacts/release/<rid>/`.

## License And Notices

The original source code in this repository is MIT licensed. Third-party components, referenced projects, and Valve/Steam assets are documented in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

Important: the Steam Controller name, Steam name, Valve trademarks, and the controller artwork/assets remain Valve property. This project is unofficial and not endorsed by Valve.
