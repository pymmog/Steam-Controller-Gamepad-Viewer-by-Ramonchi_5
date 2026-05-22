using System.Globalization;
using SteamControllerGamepadViewer.State;

namespace SteamControllerGamepadViewer.Hid;

internal sealed class LinuxHidTouchpadService : BackgroundService
{
    private const int ValveVendorId = 0x28de;
    private const int ReportBufferSize = 256;

    private readonly SteamHidState _state;
    private readonly ILogger<LinuxHidTouchpadService> _logger;

    public LinuxHidTouchpadService(SteamHidState state, ILogger<LinuxHidTouchpadService> logger)
    {
        _state = state;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var paths = EnumerateValveHidrawPaths().ToArray();
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
        FileStream stream;
        try
        {
            stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                ReportBufferSize,
                FileOptions.Asynchronous);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Unable to open {Path}", path);
            _state.MarkError(ex.Message);
            return false;
        }

        _logger.LogInformation("Reading Steam Controller raw HID reports from {Path}", path);
        _state.MarkOpened(path);

        await using (stream)
        {
            var buffer = new byte[ReportBufferSize];
            try
            {
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
        }

        return true;
    }

    // Enumerate /sys/class/hidraw/ to find hidraw devices belonging to Valve.
    // Each device's uevent file contains a line like: HID_ID=0003:000028DE:00001102
    private static IEnumerable<string> EnumerateValveHidrawPaths()
    {
        const string sysHidraw = "/sys/class/hidraw";
        if (!Directory.Exists(sysHidraw))
        {
            yield break;
        }

        foreach (var hidrawDir in Directory.EnumerateDirectories(sysHidraw))
        {
            var ueventPath = Path.Combine(hidrawDir, "device", "uevent");
            if (!File.Exists(ueventPath))
            {
                continue;
            }

            string uevent;
            try
            {
                uevent = File.ReadAllText(ueventPath);
            }
            catch
            {
                continue;
            }

            if (!IsValveHidDevice(uevent))
            {
                continue;
            }

            var devPath = "/dev/" + Path.GetFileName(hidrawDir);
            if (File.Exists(devPath))
            {
                yield return devPath;
            }
        }
    }

    private static bool IsValveHidDevice(string uevent)
    {
        foreach (var line in uevent.AsSpan().EnumerateLines())
        {
            // HID_ID=BUSTYPE:VENDOR:PRODUCT  (8-digit hex each)
            if (!line.StartsWith("HID_ID=", StringComparison.Ordinal))
            {
                continue;
            }

            var id = line["HID_ID=".Length..];
            var firstColon = id.IndexOf(':');
            if (firstColon < 0)
            {
                continue;
            }

            var rest = id[(firstColon + 1)..];
            var secondColon = rest.IndexOf(':');
            if (secondColon < 0)
            {
                continue;
            }

            var vendorHex = rest[..secondColon];
            return int.TryParse(vendorHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var vendor)
                   && vendor == ValveVendorId;
        }

        return false;
    }
}
