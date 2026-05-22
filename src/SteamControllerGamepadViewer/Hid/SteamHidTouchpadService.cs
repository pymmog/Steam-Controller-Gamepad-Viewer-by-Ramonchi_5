using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using SteamControllerGamepadViewer.State;

namespace SteamControllerGamepadViewer.Hid;

internal sealed class SteamHidTouchpadService : BackgroundService
{
    private const int ValveVendorId = 0x28de;
    private const int HidpStatusSuccess = 0x00110000;
    private const int SettingLizardMode = 9;
    private const byte SetSettingsValues = 0x87;

    private readonly SteamHidState _state;
    private readonly ILogger<SteamHidTouchpadService> _logger;

    public SteamHidTouchpadService(SteamHidState state, ILogger<SteamHidTouchpadService> logger)
    {
        _state = state;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var paths = EnumerateSteamHidPaths().ToArray();
            _state.MarkScan(paths.Length);
            if (paths.Length == 0)
            {
                await Task.Delay(1000, stoppingToken);
                continue;
            }

            var readers = paths
                .Select(path => Task.Run(() => TryReadDeviceAsync(path, stoppingToken), stoppingToken))
                .ToArray();

            await Task.WhenAll(readers);
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task<bool> TryReadDeviceAsync(string path, CancellationToken stoppingToken)
    {
        SafeFileHandle handle;
        var canWrite = true;
        try
        {
            handle = WindowsHidNative.CreateFile(
                path,
                WindowsHidNative.GenericRead | WindowsHidNative.GenericWrite,
                WindowsHidNative.FileShareRead | WindowsHidNative.FileShareWrite,
                IntPtr.Zero,
                WindowsHidNative.OpenExisting,
                WindowsHidNative.FileFlagOverlapped,
                IntPtr.Zero);

            if (handle.IsInvalid)
            {
                canWrite = false;
                handle.Dispose();
                handle = WindowsHidNative.CreateFile(
                    path,
                    WindowsHidNative.GenericRead,
                    WindowsHidNative.FileShareRead | WindowsHidNative.FileShareWrite,
                    IntPtr.Zero,
                    WindowsHidNative.OpenExisting,
                    WindowsHidNative.FileFlagOverlapped,
                    IntPtr.Zero);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Unable to open Steam HID device {Path}", path);
            _state.MarkError(ex.Message);
            return false;
        }

        if (handle.IsInvalid)
        {
            _state.MarkError($"Unable to open {path}: Win32 error {Marshal.GetLastWin32Error()}");
            handle.Dispose();
            return false;
        }

        var reportLength = GetInputReportLength(handle);
        if (reportLength < 64)
        {
            handle.Dispose();
            return false;
        }

        _logger.LogInformation("Reading Steam Controller raw HID touchpad reports from {Path}", path);
        _state.MarkOpened(path);

        try
        {
            if (canWrite && IsTritonPath(path))
            {
                TryDisableTritonLizardMode(handle);
            }

            await using var stream = new FileStream(handle, FileAccess.Read, reportLength, isAsync: true);
            var buffer = new byte[reportLength];

            while (!stoppingToken.IsCancellationRequested)
            {
                Array.Clear(buffer);
                var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), stoppingToken);
                if (read <= 0)
                {
                    return true;
                }

                _state.MarkReport(read, buffer[0]);

                if (SteamHidReportParser.TryParseSnapshot(buffer.AsSpan(0, read), out var snapshot))
                {
                    _state.Update(snapshot);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Steam HID read ended for {Path}", path);
            _state.MarkError(ex.Message);
        }

        return true;
    }

    private static bool IsTritonPath(string path)
        => path.Contains("pid_1302", StringComparison.OrdinalIgnoreCase) ||
           path.Contains("pid_1304", StringComparison.OrdinalIgnoreCase);

    private static void TryDisableTritonLizardMode(SafeFileHandle handle)
    {
        var report = new byte[64];
        report[0] = 1;
        report[1] = SetSettingsValues;
        report[2] = 3;
        report[3] = SettingLizardMode;
        report[4] = 0;
        report[5] = 0;

        WindowsHidNative.HidD_SetFeature(handle, report, report.Length);
    }

    private static int GetInputReportLength(SafeFileHandle handle)
    {
        if (!WindowsHidNative.HidD_GetPreparsedData(handle, out var preparsedData))
        {
            return 64;
        }

        try
        {
            return WindowsHidNative.HidP_GetCaps(preparsedData, out var caps) == HidpStatusSuccess
                ? Math.Max(64, (int)caps.InputReportByteLength)
                : 64;
        }
        finally
        {
            WindowsHidNative.HidD_FreePreparsedData(preparsedData);
        }
    }

    private static IEnumerable<string> EnumerateSteamHidPaths()
    {
        WindowsHidNative.HidD_GetHidGuid(out var hidGuid);
        var deviceInfoSet = WindowsHidNative.SetupDiGetClassDevs(
            ref hidGuid,
            IntPtr.Zero,
            IntPtr.Zero,
            WindowsHidNative.DigcfPresent | WindowsHidNative.DigcfDeviceInterface);

        if (deviceInfoSet == new IntPtr(WindowsHidNative.InvalidHandleValue))
        {
            yield break;
        }

        try
        {
            for (uint index = 0; ; index++)
            {
                var interfaceData = new WindowsHidNative.SpDeviceInterfaceData
                {
                    CbSize = Marshal.SizeOf<WindowsHidNative.SpDeviceInterfaceData>(),
                };

                if (!WindowsHidNative.SetupDiEnumDeviceInterfaces(
                        deviceInfoSet,
                        IntPtr.Zero,
                        ref hidGuid,
                        index,
                        ref interfaceData))
                {
                    yield break;
                }

                WindowsHidNative.SetupDiGetDeviceInterfaceDetail(
                    deviceInfoSet,
                    ref interfaceData,
                    IntPtr.Zero,
                    0,
                    out var requiredSize,
                    IntPtr.Zero);

                if (requiredSize <= 0)
                {
                    continue;
                }

                var detailData = Marshal.AllocHGlobal(requiredSize);
                try
                {
                    Marshal.WriteInt32(detailData, IntPtr.Size == 8 ? 8 : 6);
                    if (!WindowsHidNative.SetupDiGetDeviceInterfaceDetail(
                            deviceInfoSet,
                            ref interfaceData,
                            detailData,
                            requiredSize,
                            out _,
                            IntPtr.Zero))
                    {
                        continue;
                    }

                    var path = ReadDevicePath(detailData);
                    if (path.Contains($"vid_{ValveVendorId:x4}", StringComparison.OrdinalIgnoreCase))
                    {
                        yield return path;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(detailData);
                }
            }
        }
        finally
        {
            WindowsHidNative.SetupDiDestroyDeviceInfoList(deviceInfoSet);
        }
    }

    private static string ReadDevicePath(IntPtr detailData)
    {
        foreach (var offset in new[] { 4, 8, 6 })
        {
            var path = Marshal.PtrToStringUni(IntPtr.Add(detailData, offset));
            if (!string.IsNullOrWhiteSpace(path) && path.StartsWith(@"\\?\", StringComparison.Ordinal))
            {
                return path;
            }
        }

        return string.Empty;
    }
}
