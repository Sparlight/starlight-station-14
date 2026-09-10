namespace Content.Shared._Starlight.Footprints;

/// <summary>
/// Goes alongside FootprintOwnerComponent. Makes standing prints use PawFootprint instead
/// of the default Footprint entity. Doesn't affect dragged/downed body prints - those stay
/// on the default sprite sheet regardless of species.
/// </summary>
[RegisterComponent]
public sealed partial class PawPrintsComponent : Component
{
}
