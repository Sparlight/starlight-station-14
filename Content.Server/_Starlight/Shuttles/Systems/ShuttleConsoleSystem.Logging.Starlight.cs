// ReSharper disable CheckNamespace

namespace Content.Server.Shuttles.Systems;

public sealed partial class ShuttleConsoleSystem
{
    [Dependency] private ShuttlePilotLogSystem _pilotLog = default!;

    private void LogPilotChange(EntityUid pilot, EntityUid console, bool started)
    {
        if (started)
            _pilotLog.Started(pilot, console);
        else
            _pilotLog.Stopped(pilot, console);
    }
}
