using Content.Shared.AbstractAnalyzer;
using Robust.Shared.Audio;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Robust.Shared.Prototypes; //FarHorizons
using Content.Shared.Actions;//FarHorizons

namespace Content.Server.Medical.Components;

/// <inheritdoc/>
[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(HealthAnalyzerSystem), typeof(CryoPodSystem))]
public sealed partial class HealthAnalyzerComponent : AbstractAnalyzerComponent
{
    /// <inheritdoc/>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public override TimeSpan NextUpdate { get; set; } = TimeSpan.Zero;


    // Starlight-start: Printable health reports.
    /// <summary>
    /// Sound played when printing a patient report.
    /// </summary>
    [DataField]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/Machines/short_print_and_rip.ogg");

    /// <summary>
    /// When the analyzer will be ready to print again.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan PrintReadyAt = TimeSpan.Zero;

    /// <summary>
    /// How often the analyzer can print patient reports.
    /// </summary>
    [DataField]
    public TimeSpan PrintCooldown = TimeSpan.FromSeconds(5);
    // Starlight-end

    [DataField("damageContainers", customTypeSerializer: typeof(PrototypeIdListSerializer<DamageContainerPrototype>))]
    public List<string>? DamageContainers;

    # region Starlight

    /// <summary>
    /// Whether to show up the messages in chat
    /// </summary>
    [DataField]
    public bool Talk;

    [DataField]
    public LocId TalkMessage = "health-analyzer-chat-message";

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextTalk = TimeSpan.Zero;

    /// <summary>
    /// The delay between talk updates
    /// </summary>
    [DataField]
    public TimeSpan TalkInterval = TimeSpan.FromSeconds(5);

    #endregion Starlight
    //FarHorizons Start
    [DataField]
    public EntProtoId Action = "ActionMedTek";

    [DataField]
    public EntityUid? ActionEntity;
    //FarHorizons End
}
