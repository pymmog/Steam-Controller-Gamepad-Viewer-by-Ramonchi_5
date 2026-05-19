using SteamControllerGamepadViewer.Hid;
using SteamControllerGamepadViewer.State;

namespace SteamControllerGamepadViewer.Sdl;

internal sealed class SdlControllerService : BackgroundService
{
    private const uint SdlInitJoystick = 0x00000200;
    private const uint SdlInitGamepad = 0x00002000;
    private const uint SdlInitEvents = 0x00004000;
    private const int ValveVendorId = 0x28de;
    private static readonly TimeSpan HidSnapshotMaxAge = TimeSpan.FromMilliseconds(1500);

    private readonly ControllerStateHub _hub;
    private readonly SteamHidState _hidState;
    private readonly ILogger<SdlControllerService> _logger;
    private IntPtr _gamepad;
    private GamepadIdentity _identity = GamepadIdentity.Empty;
    private bool _sdlInitialized;

    public SdlControllerService(ControllerStateHub hub, SteamHidState hidState, ILogger<SdlControllerService> logger)
    {
        _hub = hub;
        _hidState = hidState;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            SetHints();

            if (!SdlNative.SDL_Init(SdlInitJoystick | SdlInitGamepad | SdlInitEvents))
            {
                PublishUnavailable($"SDL init failed: {SdlNative.GetLastError()}");
                return;
            }

            _sdlInitialized = true;
            _logger.LogInformation("Loaded SDL3 from {Path}", SdlNative.LoadedPath ?? "system search path");
            _hub.Publish(ControllerSnapshot.Starting() with
            {
                Status = "waiting",
                Name = "Waiting for Steam Controller",
            });

            while (!stoppingToken.IsCancellationRequested)
            {
                SdlNative.SDL_PumpEvents();
                SdlNative.SDL_UpdateGamepads();

                if (_gamepad == IntPtr.Zero || !SdlNative.SDL_GamepadConnected(_gamepad))
                {
                    CloseCurrentGamepad();

                    if (!TryOpenSteamGamepad())
                    {
                        if (TryPublishHidOnlySnapshot())
                        {
                            await Task.Delay(TimeSpan.FromMilliseconds(16), stoppingToken);
                            continue;
                        }

                        _hub.Publish(ControllerSnapshot.Starting() with
                        {
                            Status = "waiting",
                            Name = "Waiting for Steam Controller",
                        });
                        await Task.Delay(TimeSpan.FromMilliseconds(750), stoppingToken);
                        continue;
                    }
                }

                _hub.Publish(ReadSnapshot());
                await Task.Delay(TimeSpan.FromMilliseconds(16), stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (DllNotFoundException ex)
        {
            PublishUnavailable($"SDL3.dll was not found. {ex.Message}");
        }
        catch (EntryPointNotFoundException ex)
        {
            PublishUnavailable($"The loaded SDL3.dll is missing a needed function. {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Steam Controller reader failed");
            PublishUnavailable(ex.Message);
        }
        finally
        {
            CloseCurrentGamepad();
            if (_sdlInitialized)
            {
                SdlNative.SDL_Quit();
            }
        }
    }

    private static void SetHints()
    {
        SdlNative.SDL_SetHint("SDL_JOYSTICK_HIDAPI", "1");
        SdlNative.SDL_SetHint("SDL_JOYSTICK_HIDAPI_STEAM", "1");
        SdlNative.SDL_SetHint("SDL_JOYSTICK_HIDAPI_STEAMDECK", "1");
    }

    private bool TryOpenSteamGamepad()
    {
        var ids = SdlNative.SDL_GetGamepads(out var count);
        if (ids == IntPtr.Zero || count <= 0)
        {
            return false;
        }

        try
        {
            for (var i = 0; i < count; i++)
            {
                var instanceId = System.Runtime.InteropServices.Marshal.ReadInt32(ids, i * sizeof(int));
                var gamepad = SdlNative.SDL_OpenGamepad(instanceId);
                if (gamepad == IntPtr.Zero)
                {
                    continue;
                }

                var identity = ReadIdentity(gamepad, instanceId);
                if (IsSteamController(identity))
                {
                    _gamepad = gamepad;
                    _identity = identity;
                    _logger.LogInformation("Opened {Name} ({VendorId:X4}:{ProductId:X4})", identity.Name, identity.VendorId, identity.ProductId);
                    return true;
                }

                SdlNative.SDL_CloseGamepad(gamepad);
            }
        }
        finally
        {
            SdlNative.SDL_free(ids);
        }

        return false;
    }

    private ControllerSnapshot ReadSnapshot()
    {
        var leftTouchpad = ReadTouchpad(0);
        var rightTouchpad = ReadTouchpad(1);
        var buttons = new ControllerButtons
        {
            A = Button(SdlGamepadButton.South),
            B = Button(SdlGamepadButton.East),
            X = Button(SdlGamepadButton.West),
            Y = Button(SdlGamepadButton.North),
            View = Button(SdlGamepadButton.Back),
            Menu = Button(SdlGamepadButton.Start),
            Steam = Button(SdlGamepadButton.Guide),
            QuickAccess = Button(SdlGamepadButton.Misc1),
            LeftBumper = Button(SdlGamepadButton.LeftShoulder),
            RightBumper = Button(SdlGamepadButton.RightShoulder),
            LeftStick = Button(SdlGamepadButton.LeftStick),
            RightStick = Button(SdlGamepadButton.RightStick),
            DpadUp = Button(SdlGamepadButton.DpadUp),
            DpadDown = Button(SdlGamepadButton.DpadDown),
            DpadLeft = Button(SdlGamepadButton.DpadLeft),
            DpadRight = Button(SdlGamepadButton.DpadRight),
            LeftGripUpper = Button(SdlGamepadButton.LeftPaddle1),
            LeftGripLower = Button(SdlGamepadButton.LeftPaddle2),
            RightGripUpper = Button(SdlGamepadButton.RightPaddle1),
            RightGripLower = Button(SdlGamepadButton.RightPaddle2),
        };
        var axes = new ControllerAxes
        {
            LeftStickX = Axis(SdlGamepadAxis.LeftX),
            LeftStickY = Axis(SdlGamepadAxis.LeftY),
            RightStickX = Axis(SdlGamepadAxis.RightX),
            RightStickY = Axis(SdlGamepadAxis.RightY),
            LeftTrigger = Trigger(SdlGamepadAxis.LeftTrigger),
            RightTrigger = Trigger(SdlGamepadAxis.RightTrigger),
        };

        if (_hidState.TryGetCurrent(TimeSpan.FromMilliseconds(1500), out var hidSnapshot))
        {
            leftTouchpad = hidSnapshot.Left;
            rightTouchpad = hidSnapshot.Right;
            buttons = hidSnapshot.Buttons ?? buttons;
            axes = hidSnapshot.Axes ?? axes;
        }

        return new ControllerSnapshot
        {
            Connected = true,
            Status = "connected",
            Name = _identity.Name,
            InstanceId = _identity.InstanceId,
            VendorId = _identity.VendorId,
            ProductId = _identity.ProductId,
            Buttons = buttons,
            Axes = axes,
            LeftTouchpad = leftTouchpad,
            RightTouchpad = rightTouchpad,
        };
    }

    private bool TryPublishHidOnlySnapshot()
    {
        if (!_hidState.TryGetCurrent(HidSnapshotMaxAge, out var hidSnapshot))
        {
            return false;
        }

        var hidStatus = _hidState.Status;
        _hub.Publish(new ControllerSnapshot
        {
            Connected = true,
            Status = "connected",
            Name = "Steam Controller (HID)",
            VendorId = ValveVendorId,
            ProductId = TryReadHexDeviceId(hidStatus.OpenedPath, "pid_"),
            Buttons = hidSnapshot.Buttons ?? new ControllerButtons(),
            Axes = hidSnapshot.Axes ?? new ControllerAxes(),
            LeftTouchpad = hidSnapshot.Left,
            RightTouchpad = hidSnapshot.Right,
        });
        return true;
    }

    private ControllerTouchpad ReadTouchpad(int index)
    {
        if (_gamepad == IntPtr.Zero ||
            SdlNative.SDL_GetNumGamepadTouchpads(_gamepad) <= index ||
            SdlNative.SDL_GetNumGamepadTouchpadFingers(_gamepad, index) <= 0)
        {
            return new ControllerTouchpad();
        }

        if (!SdlNative.SDL_GetGamepadTouchpadFinger(_gamepad, index, 0, out var down, out var x, out var y, out var pressure))
        {
            return new ControllerTouchpad();
        }

        return new ControllerTouchpad
        {
            Touched = down,
            Clicked = down && pressure >= 0.75f,
            X = Clamp01(x),
            Y = Clamp01(y),
            Pressure = Clamp01(pressure),
        };
    }

    private bool Button(SdlGamepadButton button)
        => _gamepad != IntPtr.Zero && SdlNative.SDL_GetGamepadButton(_gamepad, button);

    private double Axis(SdlGamepadAxis axis)
    {
        if (_gamepad == IntPtr.Zero)
        {
            return 0;
        }

        var value = SdlNative.SDL_GetGamepadAxis(_gamepad, axis);
        var divisor = value < 0 ? 32768d : 32767d;
        return Math.Clamp(value / divisor, -1d, 1d);
    }

    private double Trigger(SdlGamepadAxis axis)
    {
        if (_gamepad == IntPtr.Zero)
        {
            return 0;
        }

        var value = SdlNative.SDL_GetGamepadAxis(_gamepad, axis);
        return Math.Clamp(value / 32767d, 0d, 1d);
    }

    private static GamepadIdentity ReadIdentity(IntPtr gamepad, int instanceId)
    {
        var name = SdlNative.PtrToString(SdlNative.SDL_GetGamepadName(gamepad)) ?? "Unknown Controller";
        return new GamepadIdentity(
            instanceId,
            name,
            SdlNative.SDL_GetGamepadVendor(gamepad),
            SdlNative.SDL_GetGamepadProduct(gamepad));
    }

    private static bool IsSteamController(GamepadIdentity identity)
    {
        if (identity.VendorId == ValveVendorId)
        {
            return true;
        }

        return identity.Name.Contains("Steam", StringComparison.OrdinalIgnoreCase) ||
               identity.Name.Contains("Valve", StringComparison.OrdinalIgnoreCase);
    }

    private static int TryReadHexDeviceId(string? path, string prefix)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return 0;
        }

        var start = path.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (start < 0 || start + prefix.Length + 4 > path.Length)
        {
            return 0;
        }

        return int.TryParse(
            path.AsSpan(start + prefix.Length, 4),
            System.Globalization.NumberStyles.HexNumber,
            System.Globalization.CultureInfo.InvariantCulture,
            out var value)
            ? value
            : 0;
    }

    private static double Clamp01(double value) => Math.Clamp(value, 0d, 1d);

    private void CloseCurrentGamepad()
    {
        if (_gamepad != IntPtr.Zero)
        {
            SdlNative.SDL_CloseGamepad(_gamepad);
            _gamepad = IntPtr.Zero;
            _identity = GamepadIdentity.Empty;
        }
    }

    private void PublishUnavailable(string message)
    {
        _logger.LogWarning("{Message}", message);
        _hub.Publish(ControllerSnapshot.Starting() with
        {
            Status = "unavailable",
            Name = message,
        });
    }

    private sealed record GamepadIdentity(int InstanceId, string Name, int VendorId, int ProductId)
    {
        public static readonly GamepadIdentity Empty = new(0, "Steam Controller", 0, 0);
    }
}
