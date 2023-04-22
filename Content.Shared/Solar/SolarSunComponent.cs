using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Solar;

/// <summary>
/// Sun for the purposes of solars.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedPowerSolarSystem)), AutoGenerateComponentState]
public sealed partial class SolarSunComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("angle"), AutoNetworkedField]
    public Angle LastAngle;

    [ViewVariables(VVAccess.ReadWrite), DataField("lastUpdate", customTypeSerializer:typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan LastUpdate;

    [ViewVariables(VVAccess.ReadWrite), DataField("angularVelocity")]
    public Angle AngularVelocity;
}
