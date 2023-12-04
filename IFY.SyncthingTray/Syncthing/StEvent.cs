using System.Text.Json.Serialization;

namespace IFY.SyncthingTray.Syncthing;

public class StEvent
{
    public required long Id { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required StEventType Type { get; set; }
    //public required long GlobalId { get; set; }
    //public required DateTime Time { get; set; }
    //public required object Data { get; set; }
}
