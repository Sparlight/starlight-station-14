using Content.Server.Shuttles;
using Content.Shared._Starlight.Replay;
using Content.Shared.Shuttles.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Replays;

namespace Content.Server._Starlight.Replay;

/// <summary>
/// Keeps <see cref="ReplayShuttlePilotComponent"/> current on shuttle pilots while a replay is recording.
/// </summary>
/// <remarks>
/// Polled each tick; only changes are dirtied. Remote consoles resolve as in the mover controller.
/// </remarks>
public sealed partial class ReplayShuttlePilotRecordingSystem : EntitySystem
{
    [Dependency] private IReplayRecordingManager _replay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReplayShuttlePilotComponent, ComponentGetStateAttemptEvent>(OnGetStateAttempt);
    }

    // Only the replay recorder has no session.
    private void OnGetStateAttempt(Entity<ReplayShuttlePilotComponent> ent, ref ComponentGetStateAttemptEvent args)
    {
        if (args.Player != null)
            args.Cancelled = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // PilotComponent shutdown is already subscribed elsewhere, so stale copies are removed here.
        var recorded = EntityQueryEnumerator<ReplayShuttlePilotComponent>();
        while (recorded.MoveNext(out var uid, out _))
        {
            if (!TryComp<PilotComponent>(uid, out var pilot) || pilot.Console == null)
                RemCompDeferred<ReplayShuttlePilotComponent>(uid);
        }

        if (!_replay.IsRecording)
            return;

        var pilots = EntityQueryEnumerator<PilotComponent>();
        while (pilots.MoveNext(out var uid, out var pilot))
        {
            if (pilot.Console is not { } console)
            {
                RemCompDeferred<ReplayShuttlePilotComponent>(uid);
                continue;
            }

            var remote = TryComp<DroneConsoleComponent>(console, out var drone);
            var target = remote ? drone!.Entity : console;
            EntityUid? shuttle = null;
            if (target is { } targetConsole
                && Transform(targetConsole).GridUid is { } grid
                && HasComp<ShuttleComponent>(grid))
            {
                shuttle = grid;
            }

            var buttons = (byte) pilot.HeldButtons;
            var comp = EnsureComp<ReplayShuttlePilotComponent>(uid);
            if (comp.Console == console && comp.Shuttle == shuttle && comp.Remote == remote && comp.Buttons == buttons)
                continue;

            comp.Console = console;
            comp.Shuttle = shuttle;
            comp.Remote = remote;
            comp.Buttons = buttons;
            Dirty(uid, comp);
        }
    }
}
