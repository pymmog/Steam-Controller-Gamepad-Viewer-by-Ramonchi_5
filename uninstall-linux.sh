#!/bin/bash
set -e

BINARY="$HOME/.local/bin/SteamControllerGamepadViewer"
SERVICE_NAME="steam-controller-viewer.service"
SERVICE_FILE="$HOME/.config/systemd/user/$SERVICE_NAME"
UDEV_RULE="/etc/udev/rules.d/70-steam-controller.rules"

echo "Uninstalling Gamepad Viewer..."

# Stop and disable systemd service
if systemctl --user is-active --quiet "$SERVICE_NAME" 2>/dev/null; then
    echo "Stopping service..."
    systemctl --user stop "$SERVICE_NAME"
fi
if systemctl --user is-enabled --quiet "$SERVICE_NAME" 2>/dev/null; then
    echo "Disabling service..."
    systemctl --user disable "$SERVICE_NAME"
fi
if [ -f "$SERVICE_FILE" ]; then
    echo "Removing $SERVICE_FILE"
    rm -f "$SERVICE_FILE"
    systemctl --user daemon-reload
fi

# Remove binary
if [ -f "$BINARY" ]; then
    echo "Removing $BINARY"
    rm -f "$BINARY"
fi

# Remove udev rule
if [ -f "$UDEV_RULE" ]; then
    echo "Removing $UDEV_RULE (requires sudo)"
    sudo rm -f "$UDEV_RULE"
    sudo udevadm control --reload-rules
    sudo udevadm trigger
fi

echo ""
echo "Uninstall complete."
