namespace IFY.SyncthingTray.Syncthing;

public enum StEventType
{
    ClusterConfigReceived,
    ConfigSaved,
    DeviceConnected,
    DeviceDisconnected,
    DeviceDiscovered,
    DevicePaused,
    DeviceRejected,  // DEPRECATED
    DeviceResumed,
    DownloadProgress,
    Failure,
    FolderCompletion,
    FolderErrors,
    FolderPaused,
    FolderRejected, // DEPRECATED
    FolderResumed,
    FolderScanProgress,
    FolderSummary,
    FolderWatchStateChanged,
    ItemFinished,
    ItemStarted,
    ListenAddressesChanged,
    LocalChangeDetected,
    LocalIndexUpdated,
    LoginAttempt,
    PendingDevicesChanged,
    PendingFoldersChanged,
    RemoteChangeDetected,
    RemoteDownloadProgress,
    RemoteIndexUpdated,
    Starting,
    StartupComplete,
    StateChanged
}
