using System.Buffers.Binary;
using SteamControllerGamepadViewer.State;

namespace SteamControllerGamepadViewer.Hid;

internal static class SteamHidReportParser
{
    private const byte TritonControllerState = 0x42;
    private const byte TritonControllerStateNoQuaternion = 0x45;

    public static bool TryParseSnapshot(
        ReadOnlySpan<byte> report,
        out SteamHidSnapshot snapshot)
    {
        snapshot = default;

        if (TryParseTritonState(report, out snapshot))
        {
            return true;
        }

        for (var offset = 0; offset <= Math.Min(4, report.Length - 24); offset++)
        {
            if (report[offset] != 0x01 || report[offset + 1] != 0x00)
            {
                continue;
            }

            var data = report[offset..];
            var left = new ControllerTouchpad();
            var right = new ControllerTouchpad();
            var parsed = data[2] switch
            {
                0x01 => TryParseClassicState(data, out left, out right),
                0x09 => TryParseDeckState(data, out left, out right),
                _ => false,
            };

            if (parsed)
            {
                snapshot = new SteamHidSnapshot(null, null, left, right, DateTimeOffset.UtcNow);
                return true;
            }
        }

        return false;
    }

    private static bool TryParseTritonState(ReadOnlySpan<byte> report, out SteamHidSnapshot snapshot)
    {
        snapshot = default;

        if (report.Length < 30 ||
            (report[0] != TritonControllerState && report[0] != TritonControllerStateNoQuaternion))
        {
            return false;
        }

        var buttons = BinaryPrimitives.ReadUInt32LittleEndian(report[2..6]);
        var leftPressure = NormalizePressure(BinaryPrimitives.ReadUInt16LittleEndian(report[22..24]));
        var rightPressure = NormalizePressure(BinaryPrimitives.ReadUInt16LittleEndian(report[28..30]));
        var left = CreatePad(
            HasFlag(buttons, 0x02000000),
            HasFlag(buttons, 0x04000000),
            ReadSigned(report, 18),
            (short)-ReadSigned(report, 20),
            leftPressure);
        var right = CreatePad(
            HasFlag(buttons, 0x00200000),
            HasFlag(buttons, 0x00400000),
            ReadSigned(report, 24),
            (short)-ReadSigned(report, 26),
            rightPressure);

        snapshot = new SteamHidSnapshot(
            new ControllerButtons
            {
                A = HasFlag(buttons, 0x00000001),
                B = HasFlag(buttons, 0x00000002),
                X = HasFlag(buttons, 0x00000004),
                Y = HasFlag(buttons, 0x00000008),
                QuickAccess = HasFlag(buttons, 0x00000010),
                RightStick = HasFlag(buttons, 0x00000020),
                View = HasFlag(buttons, 0x00004000),
                RightGripUpper = HasFlag(buttons, 0x00000080),
                RightGripLower = HasFlag(buttons, 0x00000100),
                RightBumper = HasFlag(buttons, 0x00000200),
                DpadDown = HasFlag(buttons, 0x00000400),
                DpadRight = HasFlag(buttons, 0x00000800),
                DpadLeft = HasFlag(buttons, 0x00001000),
                DpadUp = HasFlag(buttons, 0x00002000),
                Menu = HasFlag(buttons, 0x00000040),
                LeftStick = HasFlag(buttons, 0x00008000),
                Steam = HasFlag(buttons, 0x00010000),
                LeftGripUpper = HasFlag(buttons, 0x00020000),
                LeftGripLower = HasFlag(buttons, 0x00040000),
                LeftBumper = HasFlag(buttons, 0x00080000),
            },
            new ControllerAxes
            {
                LeftTrigger = NormalizeTrigger(BinaryPrimitives.ReadUInt16LittleEndian(report[6..8])),
                RightTrigger = NormalizeTrigger(BinaryPrimitives.ReadUInt16LittleEndian(report[8..10])),
                LeftStickX = NormalizeAxis(ReadSigned(report, 10)),
                LeftStickY = NormalizeAxis((short)-ReadSigned(report, 12)),
                RightStickX = NormalizeAxis(ReadSigned(report, 14)),
                RightStickY = NormalizeAxis((short)-ReadSigned(report, 16)),
            },
            left,
            right,
            DateTimeOffset.UtcNow);
        return true;
    }

    private static bool TryParseClassicState(
        ReadOnlySpan<byte> data,
        out ControllerTouchpad left,
        out ControllerTouchpad right)
    {
        left = new ControllerTouchpad();
        right = new ControllerTouchpad();

        if (data.Length < 24)
        {
            return false;
        }

        var b10 = data[10];
        var leftTouched = HasBit(b10, 3) || HasBit(b10, 7);
        var rightTouched = HasBit(b10, 4);
        var leftClicked = HasBit(b10, 1);
        var rightClicked = HasBit(b10, 2);

        left = CreatePad(
            leftTouched,
            leftClicked,
            ReadSigned(data, 16),
            (short)-ReadSigned(data, 18),
            leftClicked ? 1 : leftTouched ? 0.35 : 0);

        right = CreatePad(
            rightTouched,
            rightClicked,
            ReadSigned(data, 20),
            (short)-ReadSigned(data, 22),
            rightClicked ? 1 : rightTouched ? 0.35 : 0);

        return true;
    }

    private static bool TryParseDeckState(
        ReadOnlySpan<byte> data,
        out ControllerTouchpad left,
        out ControllerTouchpad right)
    {
        left = new ControllerTouchpad();
        right = new ControllerTouchpad();

        if (data.Length < 60)
        {
            return false;
        }

        var b10 = data[10];
        var leftPressure = NormalizePressure(BinaryPrimitives.ReadUInt16LittleEndian(data[56..58]));
        var rightPressure = NormalizePressure(BinaryPrimitives.ReadUInt16LittleEndian(data[58..60]));
        var leftTouched = HasBit(b10, 3) || leftPressure > 0.02;
        var rightTouched = HasBit(b10, 4) || rightPressure > 0.02;
        var leftClicked = HasBit(b10, 1);
        var rightClicked = HasBit(b10, 2);

        left = CreatePad(
            leftTouched,
            leftClicked,
            ReadSigned(data, 16),
            ReadSigned(data, 18),
            leftClicked ? 1 : leftTouched ? Math.Max(0.35, leftPressure) : 0);

        right = CreatePad(
            rightTouched,
            rightClicked,
            ReadSigned(data, 20),
            ReadSigned(data, 22),
            rightClicked ? 1 : rightTouched ? Math.Max(0.35, rightPressure) : 0);

        return true;
    }

    private static ControllerTouchpad CreatePad(bool touched, bool clicked, short x, short y, double pressure)
        => new()
        {
            Touched = touched,
            Clicked = clicked,
            X = NormalizeSigned(x),
            Y = NormalizeSigned(y),
            Pressure = Clamp01(pressure),
        };

    private static short ReadSigned(ReadOnlySpan<byte> data, int offset)
        => BinaryPrimitives.ReadInt16LittleEndian(data[offset..(offset + 2)]);

    private static bool HasBit(byte value, int bit)
        => (value & (1 << bit)) != 0;

    private static bool HasFlag(uint value, uint flag)
        => (value & flag) != 0;

    private static double NormalizeAxis(short value)
    {
        var divisor = value < 0 ? 32768d : 32767d;
        return Math.Clamp(value / divisor, -1d, 1d);
    }

    private static double NormalizeSigned(short value)
        => Clamp01((value + 32767d) / 65534d);

    private static double NormalizeTrigger(ushort value)
        => Clamp01(value / 32767d);

    private static double NormalizePressure(ushort value)
        => Clamp01(value / 32767d);

    private static double Clamp01(double value)
        => Math.Min(1, Math.Max(0, value));
}
