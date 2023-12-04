using IFY.SyncthingTray.Syncthing;
using Microsoft.Extensions.Options;

namespace IFY.SyncthingTray;

public class SyncthingMonitor(ILogger<SyncthingMonitor> logger, SyncthingAPI api, IOptions<SyncthingMonitor.Configuration> config) : BackgroundService
{
    public sealed class Configuration
    {
        public int PollDelay { get; set; } = 60_000;
        public bool NotifyOnFailure { get; set; }
    }

    private readonly ILogger<SyncthingMonitor> _logger = logger;
    private readonly SyncthingAPI _api = api;

    public int PollDelay { get; } = config.Value.PollDelay;
    public bool NotifyOnFailure { get; } = config.Value.NotifyOnFailure;

    public Action<string>? OnError { get; set; }
    public Action? OnStateChanged { get; set; }

    private readonly Dictionary<string, StFolder> _folderStates = [];
    public ICollection<StFolder> Folders => _folderStates.Values;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _ = _api.WaitForEventsAsync(stoppingToken); // Initialise events

        await update(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            // Sleep for delay or until event
            var cts = new CancellationTokenSource(PollDelay);
            stoppingToken.Register(cts.Cancel);
            try
            {
                while (!cts.IsCancellationRequested)
                {
                    var (isSuccess, events) = await _api.WaitForEventsAsync(cts.Token);
                    if (!isSuccess)
                    {
                        // Wait for poll
                        cts.Token.WaitHandle.WaitOne();
                    }

                    // React to important event types
                    if (events!.Any(e => e.Type is StEventType.DeviceConnected or StEventType.DeviceDisconnected or StEventType.StateChanged))
                    {
                        cts.Cancel();
                    }
                }
            }
            catch (TaskCanceledException)
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }

            await update(stoppingToken);
        }
    }

    private async Task update(CancellationToken token)
    {
        // Check overall state
        var (isSuccess, conn) = await _api.GetOverallStateAsync(token);
        if (!isSuccess)
        {
            OnError?.Invoke("Failed to update");
            return;
        }
        if (conn?.AllConnected != true)
        {
            OnError?.Invoke("Connection issues");
            return;
        }

        // Get folder list
        var (isSuccess2, folderIds) = await _api.GetFolderIdsAsync(token);
        if (!isSuccess2)
        {
            OnError?.Invoke("Failed to update");
            return;
        }

        // Refresh each folder state
        var failed = false;
        foreach (var folderId in folderIds!)
        {
            var updated = await updateFolder(folderId, token);
            if (!updated)
            {
                failed = true;
                break;
            }
        }

        // Fire appropriate event
        if (failed)
        {
            OnError?.Invoke("Failed to update");
        }
        else
        {
            OnStateChanged?.Invoke();
        }
    }

    private async Task<bool> updateFolder(string folderId, CancellationToken stoppingToken)
    {
        // Cache
        if (!_folderStates.TryGetValue(folderId, out var folder))
        {
            folder = new StFolder
            {
                Id = folderId
            };
            _folderStates.Add(folderId, folder);
        }

        // Get latest state
        var (isSuccess, state) = await _api.UpdateFolderAsync(folder, stoppingToken);
        if (!isSuccess)
        {
            _logger.LogWarning("Failed to get folder state: {folderId}", folderId);
            return false;
        }

        _folderStates[folderId] = state!;
        return true;
    }
}
