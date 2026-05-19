using System.Text.Json;
using System.Reflection;
using SteamControllerGamepadViewer.Hid;
using SteamControllerGamepadViewer.Sdl;
using SteamControllerGamepadViewer.State;

SdlNative.Configure(args);

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = WebRootResolver.Resolve(),
});

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
});

builder.WebHost.UseUrls(builder.Configuration["urls"] ?? builder.Configuration["Urls"] ?? "http://127.0.0.1:31337");

builder.Services.AddSingleton<ControllerStateHub>();
builder.Services.AddSingleton<SteamHidState>();
builder.Services.AddHostedService<SteamHidTouchpadService>();
builder.Services.AddHostedService<SdlControllerService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/state", (ControllerStateHub hub) => Results.Json(hub.Current, AppJson.Options));

app.MapGet("/events", async (HttpContext context, ControllerStateHub hub) =>
{
    context.Response.Headers.CacheControl = "no-cache";
    context.Response.Headers.Connection = "keep-alive";
    context.Response.ContentType = "text/event-stream";

    await foreach (var snapshot in hub.Subscribe(context.RequestAborted))
    {
        var json = JsonSerializer.Serialize(snapshot, AppJson.Options);
        await context.Response.WriteAsync($"data: {json}\n\n", context.RequestAborted);
        await context.Response.Body.FlushAsync(context.RequestAborted);
    }
});

app.MapGet("/health", (ControllerStateHub hub) => Results.Json(new
{
    ok = true,
    connected = hub.Current.Connected,
    name = hub.Current.Name,
    status = hub.Current.Status,
}, AppJson.Options));

app.MapGet("/api/hid", (SteamHidState hid) => Results.Json(hid.Status, AppJson.Options));

app.MapGet("/{**assetPath}", async (string? assetPath, HttpContext context) =>
{
    var path = string.IsNullOrWhiteSpace(assetPath) ? "index.html" : assetPath;
    if (!EmbeddedWebAssets.TryOpen(path, out var stream, out var contentType))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    await using (stream)
    {
        context.Response.ContentType = contentType;
        await stream.CopyToAsync(context.Response.Body, context.RequestAborted);
    }
});

app.Run();

internal static class AppJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
    };
}

internal static class WebRootResolver
{
    public static string Resolve()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.CurrentDirectory, "wwwroot"),
            Path.Combine(Environment.CurrentDirectory, "src", "SteamControllerGamepadViewer", "wwwroot"),
            Path.Combine(AppContext.BaseDirectory, "wwwroot"),
        };

        return candidates.FirstOrDefault(Directory.Exists) ?? candidates[0];
    }
}

internal static class EmbeddedWebAssets
{
    private static readonly Assembly Assembly = typeof(EmbeddedWebAssets).Assembly;
    private static readonly IReadOnlyDictionary<string, string> ResourceNames = Assembly
        .GetManifestResourceNames()
        .ToDictionary(NormalizeResourceName, StringComparer.OrdinalIgnoreCase);

    public static bool TryOpen(string path, out Stream stream, out string contentType)
    {
        path = path.Replace('\\', '/').TrimStart('/');
        if (path.Length == 0)
        {
            path = "index.html";
        }

        contentType = ContentTypeFor(path);
        if (!ResourceNames.TryGetValue($"wwwroot/{path}", out var resourceName))
        {
            stream = Stream.Null;
            return false;
        }

        stream = Assembly.GetManifestResourceStream(resourceName) ?? Stream.Null;
        return !ReferenceEquals(stream, Stream.Null);
    }

    private static string NormalizeResourceName(string resourceName)
        => resourceName.Replace('\\', '/').TrimStart('/');

    private static string ContentTypeFor(string path)
        => Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".css" => "text/css; charset=utf-8",
            ".html" => "text/html; charset=utf-8",
            ".js" => "text/javascript; charset=utf-8",
            ".json" => "application/json; charset=utf-8",
            ".svg" => "image/svg+xml",
            _ => "application/octet-stream",
        };
}
