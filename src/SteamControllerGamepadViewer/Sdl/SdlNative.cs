using System.Reflection;
using System.Runtime.InteropServices;

namespace SteamControllerGamepadViewer.Sdl;

internal static class SdlNative
{
    private const string LibraryName = "SDL3";
    private static string? _dllPath;

    static SdlNative()
    {
        NativeLibrary.SetDllImportResolver(typeof(SdlNative).Assembly, ResolveLibrary);
    }

    public static void Configure(IReadOnlyList<string> args)
    {
        _dllPath = FindConfiguredPath(args) ?? FindDefaultPath();
    }

    public static string? LoadedPath => _dllPath;

    public static string GetLastError()
    {
        var error = SDL_GetError();
        return error == IntPtr.Zero ? string.Empty : Marshal.PtrToStringUTF8(error) ?? string.Empty;
    }

    public static string? PtrToString(IntPtr ptr)
        => ptr == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(ptr);

    private static IntPtr ResolveLibrary(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (!libraryName.Equals(LibraryName, StringComparison.OrdinalIgnoreCase))
        {
            return IntPtr.Zero;
        }

        if (!string.IsNullOrWhiteSpace(_dllPath) && File.Exists(_dllPath))
        {
            return NativeLibrary.Load(_dllPath);
        }

        return IntPtr.Zero;
    }

    private static string? FindConfiguredPath(IReadOnlyList<string> args)
    {
        var environmentPath = Environment.GetEnvironmentVariable("SDL3_PATH");
        if (File.Exists(environmentPath))
        {
            return environmentPath;
        }

        for (var i = 0; i < args.Count; i++)
        {
            if (args[i].Equals("--sdl3", StringComparison.OrdinalIgnoreCase) ||
                args[i].Equals("--sdl3-path", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 < args.Count && File.Exists(args[i + 1]))
                {
                    return args[i + 1];
                }
            }
        }

        return null;
    }

    private static string? FindDefaultPath()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "SDL3.dll"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam", "SDL3.dll"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam", "SDL3.dll"),
            Path.Combine(Environment.CurrentDirectory, "SDL3.dll"),
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool SDL_Init(uint flags);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void SDL_Quit();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void SDL_PumpEvents();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void SDL_UpdateGamepads();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool SDL_SetHint(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string value);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr SDL_GetError();

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr SDL_GetGamepads(out int count);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr SDL_OpenGamepad(int instanceId);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void SDL_CloseGamepad(IntPtr gamepad);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool SDL_GamepadConnected(IntPtr gamepad);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr SDL_GetGamepadName(IntPtr gamepad);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern ushort SDL_GetGamepadVendor(IntPtr gamepad);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern ushort SDL_GetGamepadProduct(IntPtr gamepad);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool SDL_GetGamepadButton(IntPtr gamepad, SdlGamepadButton button);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern short SDL_GetGamepadAxis(IntPtr gamepad, SdlGamepadAxis axis);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int SDL_GetNumGamepadTouchpads(IntPtr gamepad);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int SDL_GetNumGamepadTouchpadFingers(IntPtr gamepad, int touchpad);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool SDL_GetGamepadTouchpadFinger(
        IntPtr gamepad,
        int touchpad,
        int finger,
        [MarshalAs(UnmanagedType.I1)] out bool down,
        out float x,
        out float y,
        out float pressure);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void SDL_free(IntPtr mem);
}
