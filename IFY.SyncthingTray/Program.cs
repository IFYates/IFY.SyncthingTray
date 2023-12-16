using IFY.SyncthingTray;
using IFY.SyncthingTray.Syncthing;
using Microsoft.Toolkit.Uwp.Notifications;
using System.Diagnostics;

Status[] _stateOrder = [Status.Disconnected, Status.Error, Status.OutOfSync, Status.Busy, Status.Unknown];

// Configure environment
var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile("appsettings.local.json", optional: true);

builder.Services.Configure<SyncthingAPI.Configuration>(builder.Configuration.GetSection("Syncthing"));
builder.Services.Configure<SyncthingMonitor.Configuration>(builder.Configuration.GetSection("Syncthing"));

builder.Services.AddHostedService<SyncthingMonitor>();
builder.Services.AddSingleton<SyncthingAPI>();

// Configure systray icon
var systrayIcon = new SystrayForm(Application.ProductName!, Images.icon_X)
{
    HoverText = "Initialising..."
};

// Configure monitor service
var host = builder.Build();

var monitor = (SyncthingMonitor)host.Services.GetService<IHostedService>()!;
var api = host.Services.GetService<SyncthingAPI>()!;

systrayIcon.IconDoubleClickAction = () =>
{
    // Open Syncthing website
    Process.Start(new ProcessStartInfo(api.Host) { UseShellExecute = true });
};
systrayIcon.IconRightClickAction = () => systrayIcon.Close();
systrayIcon.IconLeftClickAction = () =>
{
    showNotification(getOverallState());
};

string? _lastError = null;
monitor.OnError = (reason) =>
{
    systrayIcon.HoverText = reason;
    systrayIcon.ReplaceIcon(GetStateIcon(Status.Error));

    if (_lastError != reason && monitor.NotifyOnFailure)
    {
        showNotification(Status.Error);
        _lastError = reason;
    }
};
monitor.OnStateChanged = () =>
{
    // Derive overall state
    var state = getOverallState();
    var reason = state switch
    {
        Status.Disconnected => "Disconnected",
        Status.Error => "Folders need attention",
        Status.Busy => "Syncing...",
        Status.OutOfSync => "Work pending",
        Status.OK => "Up-to-date",
        _ => "Unknown",
    };

    systrayIcon.ReplaceIcon(GetStateIcon(state));
    systrayIcon.HoverText = reason;

    if (_lastError != reason && monitor.NotifyOnFailure && state is Status.Error or Status.Disconnected)
    {
        showNotification(state);
        _lastError = reason;
    }
};

// Start
var cts = new CancellationTokenSource();
_ = host.RunAsync(cts.Token).ContinueWith(_ => systrayIcon.Stop());
Application.Run(systrayIcon);
cts.Cancel();

Status getOverallState()
{
    // Derive overall state
    var folderStates = monitor.Folders.Select(f => f.Status).Distinct().ToArray();
    foreach (var state in _stateOrder)
    {
        if (folderStates.Contains(state))
        {
            return state;
        }
    }
    return Status.OK;
}
static Bitmap GetStateIcon(Status state)
    => state switch
    {
        Status.Busy => Images.icon_work,
        Status.OK => Images.icon_G,
        Status.OutOfSync => Images.icon_A,
        Status.Error or Status.Disconnected => Images.icon_R,
        _ => Images.icon_X
    };
void showNotification(Status state)
{
    // Copy image to temp file
    var imgPath = Path.GetTempFileName();
    var img = GetStateIcon(state);
    img.Save(imgPath);

    new ToastContentBuilder()
        .AddAppLogoOverride(new Uri("file:///" + imgPath.Replace('\\', '/')))
        .AddText("State: " + systrayIcon.HoverText, AdaptiveTextStyle.Base)
        .AddText($"{monitor.Folders.Count} folders", AdaptiveTextStyle.Body)
        .SetToastDuration(ToastDuration.Short)
        //.AddProgressBar(
        .AddButton("Open website", ToastActivationType.Foreground, string.Empty)
        .Show(t =>
        {
            t.Tag = Application.ProductName; // For replacing previous
            //t.SuppressPopup = true;

            t.Activated += (_, _) => systrayIcon.IconDoubleClickAction();
            t.Dismissed += (_, _) => File.Delete(imgPath);
        });

    // IDEA: One toast for overall + individual progress per folder
}