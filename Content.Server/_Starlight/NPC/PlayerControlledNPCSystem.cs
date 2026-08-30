using Content.Server.NPC.Systems;
using Robust.Shared.Player;

namespace Content.Server._Starlight.NPC;

/// <summary>
/// Sleeps an entity's AI while a player is attached to it, wakes it back up
/// on detach.
/// </summary>
public sealed class PlayerControlledNPCSystem : EntitySystem
{
    [Dependency] private NPCSystem _npc = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnPlayerAttached(PlayerAttachedEvent ev)
    {
        _npc.SleepNPC(ev.Entity);
    }

    private void OnPlayerDetached(PlayerDetachedEvent ev)
    {
        _npc.WakeNPC(ev.Entity);
    }
}
