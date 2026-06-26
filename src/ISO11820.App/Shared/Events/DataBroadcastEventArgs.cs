using ISO11820.App.Shared.Models;

namespace ISO11820.App.Shared.Events;

public sealed class DataBroadcastEventArgs : EventArgs
{
    public DataBroadcastEventArgs(RuntimeSnapshot snapshot)
    {
        Snapshot = snapshot;
    }

    public RuntimeSnapshot Snapshot { get; }
}
