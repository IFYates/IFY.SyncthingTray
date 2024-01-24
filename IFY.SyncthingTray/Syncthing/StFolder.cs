namespace IFY.SyncthingTray.Syncthing;

public class StFolder
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public Status Status { get; set; }

    #region From JSON

    public int Errors { get; set; }
    public int PullErrors { get; set; }
    public string Invalid { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;

    public long NeedFiles { get; set; }
    public long NeedBytes { get; set; }
    public long InSyncBytes { get; set; }

    public string State { get; set; } = string.Empty;
    public DateTime StateChanged { get; set; }

    #endregion From JSON

    public void UpdateState(StFolder folder)
    {
        Id = folder.Id;
        Name = folder.Name;

        if (Invalid?.Length > 0 || PullErrors > 0 || Errors > 0 || Error?.Length > 0)
        {
            Status = Status.Error;
        }
        else if (State is "scanning" or "syncing")
        {
            Status = Status.Busy;
        }
        else if (NeedBytes > 0 || NeedFiles > 0)
        {
            Status = Status.OutOfSync;
        }
        else
        {
            Status = Status.OK;
        }
    }
}
