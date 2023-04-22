using Content.Server.Solar.Components;
using Content.Server.UserInterface;
using Content.Shared.Solar;
using JetBrains.Annotations;

namespace Content.Server.Solar.EntitySystems;

/// <summary>
/// Responsible for updating solar control consoles.
/// </summary>
internal sealed class PowerSolarControlConsoleSystem : EntitySystem
{
    [Dependency] private readonly PowerSolarSystem _powerSolarSystem = default!;

    /// <summary>
    /// Timer used to avoid updating the UI state every frame (which would be overkill)
    /// </summary>
    private float _updateTimer;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SolarControlConsoleComponent, SolarControlConsoleAdjustMessage>(OnUIMessage);
    }

    public override void Update(float frameTime)
    {
        _updateTimer += frameTime;
        if (_updateTimer >= 1)
        {
            _updateTimer -= 1;
            var state = new SolarControlConsoleBoundInterfaceState(_powerSolarSystem.TargetPanelRotation, _powerSolarSystem.TargetPanelVelocity, _powerSolarSystem.TotalPanelPower, _powerSolarSystem.TowardsSun, _powerSolarSystem.IsPaused);
            foreach (var component in EntityManager.EntityQuery<SolarControlConsoleComponent>(true))
            {
                component.Owner.GetUIOrNull(SolarControlConsoleUiKey.Key)?.SetState(state);
            }
        }
    }

    private void OnUIMessage(EntityUid uid, SolarControlConsoleComponent component, SolarControlConsoleAdjustMessage msg)
    {
        bool updated = false;
        if (double.IsFinite(msg.Rotation))
        {
            _powerSolarSystem.TargetPanelRotation = msg.Rotation.Reduced();
            updated = true;
        }

        if (double.IsFinite(msg.AngularVelocity))
        {
            var degrees = msg.AngularVelocity.Degrees;
            degrees = Math.Clamp(degrees, -PowerSolarSystem.MaxPanelVelocityDegrees, PowerSolarSystem.MaxPanelVelocityDegrees);
            _powerSolarSystem.TargetPanelVelocity = Angle.FromDegrees(degrees);
            updated = true;
        }

        if (updated)
        {
            _powerSolarSystem.RefreshAllPanels();
        }
    }

}
