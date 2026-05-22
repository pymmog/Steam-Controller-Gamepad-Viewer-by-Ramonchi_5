#!/bin/bash
set -e
cd "$(dirname "$0")"

RID="${1:-linux-x64}"
BINARY_SRC="artifacts/release/${RID}/SteamControllerGamepadViewer"
BINARY_DEST="$HOME/.local/bin/SteamControllerGamepadViewer"
SERVICE_FILE="$HOME/.config/systemd/user/steam-controller-viewer.service"
UDEV_RULE="/etc/udev/rules.d/70-steam-controller.rules"

# Build if needed
if [ ! -f "$BINARY_SRC" ]; then
    echo "Building for ${RID}..."
    ./build-release-linux.sh "$RID"
fi

# Install binary
echo "Installing binary to $BINARY_DEST ..."
mkdir -p "$HOME/.local/bin"
cp "$BINARY_SRC" "$BINARY_DEST"
chmod +x "$BINARY_DEST"
echo "  Done."

# Systemd service
echo ""
read -r -p "Install systemd user service (auto-start on login)? [y/N] " yn
if [[ "$yn" =~ ^[Yy]$ ]]; then
    mkdir -p "$(dirname "$SERVICE_FILE")"
    cat > "$SERVICE_FILE" <<'EOF'
[Unit]
Description=Gamepad Viewer
After=graphical-session.target

[Service]
ExecStart=%h/.local/bin/SteamControllerGamepadViewer
Restart=on-failure

[Install]
WantedBy=default.target
EOF
    systemctl --user daemon-reload
    systemctl --user enable --now steam-controller-viewer.service
    echo "  Service installed and started."
fi

# udev rule for HID trackpad access
echo ""
read -r -p "Install udev rule for trackpad HID access (requires sudo)? [y/N] " yn
if [[ "$yn" =~ ^[Yy]$ ]]; then
    echo 'SUBSYSTEM=="hidraw", ATTRS{idVendor}=="28de", MODE="0660", GROUP="input"' \
        | sudo tee "$UDEV_RULE" > /dev/null
    sudo udevadm control --reload-rules
    sudo udevadm trigger
    echo "  udev rule installed. Replug the controller if already connected."
fi

echo ""
echo "Installation complete."
echo "Run:  $BINARY_DEST"
echo "Then open http://127.0.0.1:31337 in a browser or OBS browser source."
