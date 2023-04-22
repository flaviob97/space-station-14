using System.Linq;
using Content.Shared.Physics;
using Content.Shared.Solar;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Server.Solar.EntitySystems;

/// <summary>
///     Responsible for maintaining the solar-panel sun angle and updating <see cref='SolarPanelComponent'/> coverage.
/// </summary>
public sealed class PowerSolarSystem : SharedPowerSolarSystem
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly SharedPhysicsSystem _physicsSystem = default!;

    /// <summary>
    /// Maximum panel angular velocity range - used to stop people rotating panels fast enough that the lag prevention becomes noticable
    /// </summary>
    public const float MaxPanelVelocityDegrees = 1f;

    /// <summary>
    /// The distance before the sun is considered to have been 'visible anyway'.
    /// This value, like the occlusion semantics, is borrowed from all the other SS13 stations with solars.
    /// </summary>
    public float SunOcclusionCheckDistance = 20;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SolarSunComponent, MapInitEvent>(OnSunMapInit);
    }

    private void OnSunMapInit(EntityUid uid, SolarSunComponent component, MapInitEvent args)
    {
        // Initialize the sun to something random
        component.LastAngle = MathHelper.TwoPi * _robustRandom.NextDouble();
        component.AngularVelocity = Angle.FromDegrees(0.1 + ((_robustRandom.NextDouble() - 0.5) * 0.05));
        Dirty(component);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<SolarPanelComponent, TransformComponent>();
        var curTime = GameTiming.CurTime;
        var sunQuery = GetEntityQuery<SolarSunComponent>();

        while (query.MoveNext(out var uid, out var panel, out var xform))
        {
            if (!panel.Running)
            {
                panel.Supply = 0;
                continue;
            }

            var targetAngle = panel.StartAngle + panel.AngularVelocity * (curTime - panel.LastUpdate).TotalSeconds;
            panel.Angle = targetAngle.Reduced();
            UpdatePanelCoverage(uid, panel, xform, sunQuery);
        }
    }

    private void UpdatePanelCoverage(EntityUid entity, SolarPanelComponent panel, TransformComponent xform, EntityQuery<SolarSunComponent> sunQuery)
    {
        if (!sunQuery.TryGetComponent(xform.MapUid, out var sun))
        {
            panel.Supply = 0;
            return;
        }

        Angle panelAngle = panel.Angle;
        var sunAngle = sun.LastAngle + sun.AngularVelocity * (GameTiming.CurTime - sun.LastUpdate).TotalSeconds;

        // So apparently, and yes, I *did* only find this out later,
        // this is just a really fancy way of saying "Lambert's law of cosines".
        // ...I still think this explaination makes more sense.

        // In the 'sunRelative' coordinate system:
        // the sun is considered to be an infinite distance directly up.
        // this is the rotation of the panel relative to that.
        // directly upwards (theta = 0) = coverage 1
        // left/right 90 degrees (abs(theta) = (pi / 2)) = coverage 0
        // directly downwards (abs(theta) = pi) = coverage -1
        // as TowardsSun + = CCW,
        // panelRelativeToSun should - = CW
        var panelRelativeToSun = panelAngle - sunAngle;
        // essentially, given cos = X & sin = Y & Y is 'downwards',
        // then for the first 90 degrees of rotation in either direction,
        // this plots the lower-right quadrant of a circle.
        // now basically assume a line going from the negated X/Y to there,
        // and that's the hypothetical solar panel.
        //
        // since, again, the sun is considered to be an infinite distance upwards,
        // this essentially means Cos(panelRelativeToSun) is half of the cross-section,
        // and since the full cross-section has a max of 2, effectively-halving it is fine.
        //
        // as for when it goes negative, it only does that when (abs(theta) > pi)
        // and that's expected behavior.
        float coverage = (float)Math.Max(0, Math.Cos(panelRelativeToSun));

        if (coverage > 0)
        {
            // Determine if the solar panel is occluded, and zero out coverage if so.
            var ray = new CollisionRay(xform.WorldPosition, sunAngle.ToWorldVec(), (int) CollisionGroup.Opaque);
            var rayCastResults = _physicsSystem.IntersectRayWithPredicate(
                xform.MapID,
                ray,
                SunOcclusionCheckDistance,
                e => !xform.Anchored || e == entity);
            if (rayCastResults.Any())
                coverage = 0;
        }

        // Total coverage calculated; apply it to the panel.
        panel.Supply = panel.MaxSupply * coverage;
    }
}
