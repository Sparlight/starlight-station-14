using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.Replay;

/// <summary>
/// A shuttle pilot's console, shuttle and held keys, for replays. Sent only to the replay recorder.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ReplayShuttlePilotComponent : Component
{
    public override bool SessionSpecific => true;

    /// <summary>
    /// The console in use. For remote consoles, the remote console.
    /// </summary>
    [AutoNetworkedField]
    public EntityUid? Console;

    /// <summary>
    /// The shuttle grid the inputs move, if any.
    /// </summary>
    [AutoNetworkedField]
    public EntityUid? Shuttle;

    /// <summary>
    /// Whether <see cref="Console"/> is a remote console, such as a cargo shuttle console.
    /// </summary>
    [AutoNetworkedField]
    public bool Remote;

    /// <summary>
    /// Held <c>ShuttleButtons</c> flags.
    /// </summary>
    [AutoNetworkedField]
    public byte Buttons;
}
