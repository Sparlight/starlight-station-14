// ReSharper disable CheckNamespace

using Content.Shared.Database;
using Content.Shared.Movement.Systems;
using Content.Shared.Shuttles.Components;

namespace Content.Server.Shuttles.Systems;

public sealed partial class ShuttleSystem
{
    // Logs each grid's pilots and held keys alongside the impact log.
    private void LogImpactPilots(EntityUid ours, EntityUid other, LogImpact impact)
    {
        // Same condition as the impact log; runs before this impact is recorded.
        if (!CheckShouldLog(ours) || !CheckShouldLog(other))
            return;

        var pilots = new Dictionary<EntityUid, List<string>>();
        var query = EntityQueryEnumerator<PilotComponent>();
        while (query.MoveNext(out var pilot, out var comp))
        {
            if (comp.Console is not { } console)
                continue;

            var target = TryComp<DroneConsoleComponent>(console, out var drone) ? drone.Entity : console;
            if (target is not { } targetConsole
                || !TryComp(targetConsole, out TransformComponent? xform)
                || xform.GridUid is not { } grid
                || (grid != ours && grid != other))
            {
                continue;
            }

            var keys = comp.HeldButtons == ShuttleButtons.None ? "no keys" : comp.HeldButtons.ToString();
            if (!pilots.TryGetValue(grid, out var list))
                pilots[grid] = list = new List<string>();

            list.Add($"{ToPrettyString(pilot)} at {ToPrettyString(console)} holding {keys}");
        }

        foreach (var grid in new[] { ours, other })
        {
            var text = pilots.TryGetValue(grid, out var list) ? string.Join("; ", list) : "none";
            _logger.Add(LogType.ShuttleImpact, impact, $"Pilots of {ToPrettyString(grid):shuttle} at impact: {text}");
        }
    }
}
