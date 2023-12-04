namespace IFY.SyncthingTray.Syncthing;

public class StConnections
{
    public Dictionary<string, StConnection> Connections { get; set; } = [];

    public bool AllConnected => Connections.Count > 0 && Connections.Values.All(c => c.Connected);
}

public class StConnection
{
    public bool Connected { get; set; }
}
