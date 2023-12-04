namespace IFY.SyncthingTray.Syncthing;

public enum Status
{
    Unknown,
    Disconnected,
    Error,
    OutOfSync, // Needs to sync
    Busy, // Syncing or scanning
    OK
}
