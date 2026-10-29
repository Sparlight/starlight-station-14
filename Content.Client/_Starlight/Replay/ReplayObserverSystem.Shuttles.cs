using Content.Client._Starlight.Replay.UI;
using Content.Client.Replay.Spectator;
using Content.Shared._Starlight.Replay;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;

namespace Content.Client._Starlight.Replay;

// Shuttle pilots per console, with held keys, from ReplayShuttlePilotComponent.
public sealed partial class ReplayObserverSystem
{
    private static readonly EntProtoId _shuttlePilotsActionProto = "ActionReplayShuttlePilots";

    private const float ShuttlePilotsRefreshInterval = 0.1f;

    private EntityUid? _shuttlePilotsAction;
    private ReplayShuttlePilotsWindow? _shuttlePilotsWindow;
    private EntityUid? _shuttlePilotsChosen;
    private float _shuttlePilotsAccumulator;

    private void OnShowShuttlePilots(EntityUid uid, ReplaySpectatorComponent component, ReplayShowShuttlePilotsActionEvent args)
    {
        if (args.Handled || !IsReplayActive)
            return;

        args.Handled = true;

        if (_shuttlePilotsWindow != null)
        {
            _shuttlePilotsWindow.Close();
            return;
        }

        var window = new ReplayShuttlePilotsWindow();
        window.OnClose += () =>
        {
            _shuttlePilotsWindow = null;
            _actions.SetToggled(_shuttlePilotsAction, false);
        };
        window.OnShuttleSelected += shuttle =>
        {
            _shuttlePilotsChosen = shuttle;
            RefreshShuttlePilots();
        };
        window.OnPilotPressed += FollowPilot;

        _shuttlePilotsWindow = window;
        _shuttlePilotsChosen = null;
        _actions.SetToggled(_shuttlePilotsAction, true);
        RefreshShuttlePilots();
        window.OpenCenteredRight();
    }

    private void AddShuttlePilotsAction(EntityUid observer)
    {
        _shuttlePilotsAction = null;
        _actions.AddAction(observer, ref _shuttlePilotsAction, _shuttlePilotsActionProto);
        _actions.SetToggled(_shuttlePilotsAction, _shuttlePilotsWindow != null);
    }

    private void UpdateShuttlePilots(float frameTime)
    {
        if (_shuttlePilotsWindow == null)
            return;

        _shuttlePilotsAccumulator += frameTime;
        if (_shuttlePilotsAccumulator < ShuttlePilotsRefreshInterval)
            return;

        _shuttlePilotsAccumulator = 0f;
        RefreshShuttlePilots();
    }

    private void CloseShuttlePilots()
    {
        // Cleared first; the action may already be deleted when the close handler runs.
        _shuttlePilotsAction = null;
        _shuttlePilotsWindow?.Close();
        _shuttlePilotsWindow = null;
        _shuttlePilotsChosen = null;
        _shuttlePilotsAccumulator = 0f;
    }

    private void RefreshShuttlePilots()
    {
        if (_shuttlePilotsWindow is not { } window)
            return;

        // Shuttle -> console -> pilots.
        var flown = new Dictionary<EntityUid, Dictionary<EntityUid, List<PilotView>>>();
        var remoteConsoles = new HashSet<EntityUid>();

        var query = EntityQueryEnumerator<ReplayShuttlePilotComponent>();
        while (query.MoveNext(out var pilot, out var comp))
        {
            if (comp.Shuttle is not { } shuttle || comp.Console is not { } console || !Exists(shuttle) || !Exists(console))
                continue;

            if (!flown.TryGetValue(shuttle, out var consoles))
                flown[shuttle] = consoles = new Dictionary<EntityUid, List<PilotView>>();

            if (!consoles.TryGetValue(console, out var pilots))
                consoles[console] = pilots = new List<PilotView>();

            pilots.Add(new PilotView(pilot, PilotName(pilot), (ShuttleButtons) comp.Buttons));

            if (comp.Remote)
                remoteConsoles.Add(console);
        }

        // Keep the chosen shuttle listed after its pilots leave.
        if (_shuttlePilotsChosen is { } chosen && !Exists(chosen))
            _shuttlePilotsChosen = null;

        var shuttles = new List<(EntityUid Shuttle, string Name)>();
        foreach (var shuttle in flown.Keys)
        {
            shuttles.Add((shuttle, Name(shuttle)));
        }

        if (_shuttlePilotsChosen is { } kept && !flown.ContainsKey(kept))
            shuttles.Add((kept, Name(kept)));

        shuttles.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));

        var selected = _shuttlePilotsChosen ?? DefaultShuttle(flown, shuttles);

        var consoleViews = new List<ConsoleView>();
        string? motion = null;
        if (selected is { } sel)
        {
            if (flown.TryGetValue(sel, out var consoles))
            {
                foreach (var (console, pilots) in consoles)
                {
                    pilots.Sort((a, b) => a.Pilot.CompareTo(b.Pilot));
                    consoleViews.Add(new ConsoleView(console, ConsoleTitle(console, remoteConsoles.Contains(console)), pilots));
                }

                consoleViews.Sort((a, b) => string.Compare(a.Title, b.Title, StringComparison.CurrentCultureIgnoreCase));
            }

            motion = ShuttleMotion(sel);
        }

        window.Update(new ShuttlePilotsView(shuttles, selected, consoleViews, motion));
    }

    // The observer's shuttle, else the first one flown.
    private EntityUid? DefaultShuttle(
        Dictionary<EntityUid, Dictionary<EntityUid, List<PilotView>>> flown,
        List<(EntityUid Shuttle, string Name)> shuttles)
    {
        if (_observer is { } observer
            && Exists(observer)
            && Transform(observer).GridUid is { } grid
            && flown.ContainsKey(grid))
        {
            return grid;
        }

        return shuttles.Count > 0 ? shuttles[0].Shuttle : null;
    }

    private string ConsoleTitle(EntityUid console, bool remote)
    {
        if (!remote)
            return Name(console);

        var location = Transform(console).GridUid is { } grid ? Name(grid) : Loc.GetString("generic-unknown-title");
        return Loc.GetString("replay-observer-shuttle-console-remote", ("console", Name(console)), ("location", location));
    }

    private string PilotName(EntityUid pilot)
    {
        var name = Name(pilot);
        if (TryComp<MindContainerComponent>(pilot, out var container)
            && TryComp<MindComponent>(container.Mind, out var mind)
            && mind.UserId is { } userId
            && _player.TryGetSessionById(userId, out var session))
        {
            return Loc.GetString("replay-observer-shuttle-pilot-name", ("name", name), ("player", session.Name));
        }

        return name;
    }

    private string? ShuttleMotion(EntityUid shuttle)
    {
        if (!TryComp<PhysicsComponent>(shuttle, out var body))
            return null;

        // Positive angular velocity is counter-clockwise.
        var degrees = MathHelper.RadiansToDegrees(body.AngularVelocity);
        var turn = MathF.Abs(degrees) < 0.5f
            ? Loc.GetString("replay-observer-shuttle-turn-none")
            : Loc.GetString("replay-observer-shuttle-turn",
                ("degrees", MathF.Round(MathF.Abs(degrees))),
                ("direction", Loc.GetString(degrees > 0 ? "replay-observer-shuttle-key-ccw" : "replay-observer-shuttle-key-cw")));

        return Loc.GetString("replay-observer-shuttle-motion",
            ("speed", MathF.Round(body.LinearVelocity.Length(), 1)),
            ("turn", turn));
    }

    private void FollowPilot(EntityUid pilot)
    {
        if (_observer is not { } observer || observer != _player.LocalEntity || !Exists(pilot))
            return;

        _follower.StartFollowingEntity(observer, pilot);
    }
}
