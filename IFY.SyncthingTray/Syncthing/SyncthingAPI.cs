using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace IFY.SyncthingTray.Syncthing;

public class SyncthingAPI
{
    public sealed class Configuration
    {
        public required string Host { get; set; }
        public required string ApiKey { get; set; }
    }

    private readonly HttpClient _client;

    public string Host { get; }

    public SyncthingAPI(IOptions<Configuration> config)
    {
        Host = !config.Value.Host.Contains("://")
            ? $"https://{config.Value.Host}"
            : config.Value.Host;

        // Allow self-signed Syncthing SSL
        var handler = new HttpClientHandler
        {
            ClientCertificateOptions = ClientCertificateOption.Manual,
            ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) =>
            {
                return policyErrors == System.Net.Security.SslPolicyErrors.None
                    || (httpRequestMessage.RequestUri?.Authority == config.Value.Host
                    && cert?.Issuer.EndsWith(", OU=Automatically Generated, O=Syncthing") == true);
            }
        };

        _client = new HttpClient(handler)
        {
            BaseAddress = new Uri(Host),
        };
        _client.DefaultRequestHeaders.Add("X-API-KEY", config.Value.ApiKey);
    }

    public async Task<(bool IsSuccess, StEvent[]? Events)> WaitForEventsAsync(CancellationToken token)
    {
        var uri = _eventId == 0
            ? $"/rest/events?limit=1"
            : $"/rest/events?since={_eventId}";
        using var resp = await _client.GetAsync(uri, token); // Blocking
        if (!resp.IsSuccessStatusCode)
        {
            return (false, null);
        }

        var events = await resp.Content.ReadFromJsonAsync<StEvent[]>();
        if (events == null)
        {
            return (false, null);
        }

        _eventId = events.Length > 0 ? events.Max(v => v.Id) : _eventId;
        return (true, events);
    }
    private long _eventId = 0;

    public async Task<(bool IsSuccess, StConnections? Value)> GetOverallStateAsync(CancellationToken token)
    {
        // Overall state
        using var resp = await _client.GetAsync("/rest/system/connections", token);
        if (!resp.IsSuccessStatusCode)
        {
            return (false, null);
        }

        var value = await resp.Content.ReadFromJsonAsync<StConnections>();
        return (true, value);
    }

    public async Task<(bool IsSuccess, string[]? Value)> GetFolderIdsAsync(CancellationToken token)
    {
        using var resp = await _client.GetAsync("/rest/stats/folder", token);
        if (!resp.IsSuccessStatusCode)
        {
            return (false, null);
        }

        // Folder list
        var folderList = (await resp.Content.ReadFromJsonAsync<Dictionary<string, object>>())?.Keys.ToArray();
        if (folderList == null)
        {
            return (false, null);
        }

        return (true, folderList);
    }

    public async Task<(bool IsSuccess, StFolder? Value)> UpdateFolderAsync(StFolder folder, CancellationToken token)
    {
        // TODO: if missing folder names: http://127.0.0.1:8384/rest/config .folders

        // Get folder stats
        using var resp = await _client.GetAsync("/rest/db/status?folder=" + folder.Id, token);
        if (!resp.IsSuccessStatusCode)
        {
            return (false, null);
        }

        var state = await resp.Content.ReadFromJsonAsync<StFolder>();
        if (state == null || state.StateChanged == DateTime.MinValue)
        {
            return (false, null);
        }

        // Return updated
        state.UpdateState(folder);
        return (true, state);
    }
}
