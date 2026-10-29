using System.Numerics;
using Content.Server.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Movement.Systems;
using Content.Shared.Shuttles.Components;
using Robust.Shared.Timing;

namespace Content.Server.Shuttles.Systems;

/// <summary>
/// Admin logs for shuttle piloting: start, first input, and a stop summary of inputs used.
/// </summary>
/// <remarks>
/// Held keys are polled each tick instead of logged per press, to keep the log small. Sessions end through
/// <see cref="Stopped"/>, which pilot shutdown also reaches via ShuttleConsoleSystem.RemovePilot.
/// </remarks>
public sealed partial class ShuttlePilotLogSystem : EntitySystem
{
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private IGameTiming _timing = default!;

    private readonly Dictionary<EntityUid, PilotSession> _sessions = new();

    private sealed class PilotSession
    {
        public TimeSpan Started;
        public ShuttleButtons LastHeld;
        public int Presses;
        public TimeSpan HeldTime;
    }

    public void Started(EntityUid pilot, EntityUid console)
    {
        _sessions[pilot] = new PilotSession { Started = _timing.CurTime };

        _adminLog.Add(LogType.ShuttlePiloting, LogImpact.Low,
            $"{ToPrettyString(pilot):player} started piloting {Describe(console)}");
    }

    public void Stopped(EntityUid pilot, EntityUid console)
    {
        if (!_sessions.Remove(pilot, out var session))
        {
            _adminLog.Add(LogType.ShuttlePiloting, LogImpact.Low,
                $"{ToPrettyString(pilot):player} stopped piloting {Describe(console)}");
            return;
        }

        var duration = FormatTime(_timing.CurTime - session.Started);
        var inputs = session.Presses == 0
            ? "no inputs"
            : $"{session.Presses} key presses, keys held {FormatTime(session.HeldTime)}";

        _adminLog.Add(LogType.ShuttlePiloting, LogImpact.Low,
            $"{ToPrettyString(pilot):player} stopped piloting {Describe(console)} after {duration}: {inputs}");
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_sessions.Count == 0)
            return;

        var tick = TimeSpan.FromSeconds(frameTime);
        var query = EntityQueryEnumerator<PilotComponent>();
        while (query.MoveNext(out var uid, out var pilot))
        {
            if (pilot.Console is not { } console || !_sessions.TryGetValue(uid, out var session))
                continue;

            var held = pilot.HeldButtons;
            var pressed = held & ~session.LastHeld;
            session.LastHeld = held;

            if (held != ShuttleButtons.None)
                session.HeldTime += tick;

            if (pressed == ShuttleButtons.None)
                continue;

            if (session.Presses == 0)
            {
                _adminLog.Add(LogType.ShuttlePiloting, LogImpact.Low,
                    $"{ToPrettyString(uid):player} made their first input piloting {Describe(console)}");
            }

            session.Presses += BitOperations.PopCount((uint) pressed);
        }
    }

    // The shuttle a console moves, through remote consoles, and the console.
    private string Describe(EntityUid console)
    {
        var target = TryComp<DroneConsoleComponent>(console, out var drone) ? drone.Entity : console;
        if (target is { } targetConsole
            && TryComp(targetConsole, out TransformComponent? xform)
            && xform.GridUid is { } shuttle)
        {
            return $"{ToPrettyString(shuttle)} from {ToPrettyString(console)}";
        }

        return $"no shuttle from {ToPrettyString(console)}";
    }

    private static string FormatTime(TimeSpan time) => $"{(int) time.TotalMinutes}m {time.Seconds}s";
}
