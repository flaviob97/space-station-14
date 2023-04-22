using Content.Shared.Solar;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Power;

public sealed class PowerSolarSystem : SharedPowerSolarSystem
{
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SolarPanelComponent, SpriteComponent, TransformComponent>();
        while (query.MoveNext(out var panel, out var sprite, out var xform))
        {
            var targetAngle = panel.StartAngle + panel.AngularVelocity * (GameTiming.CurTime - panel.LastUpdate).TotalSeconds;
            panel.Angle = targetAngle.Reduced();
            sprite.Rotation = panel.Angle - xform.LocalRotation;
        }
    }
}
