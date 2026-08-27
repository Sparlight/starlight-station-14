using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.PDA.Components;

/// <summary>
/// Marks a PDA whose ID card name should track its wearer's name on equip.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SyncIdNameToWearerComponent : Component;
