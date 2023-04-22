using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Shared.Solar;

public abstract class SharedPowerSolarSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming GameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SolarPanelComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SolarPanelComponent, EntityUnpausedEvent>(OnUnpause);
        SubscribeLocalEvent<SolarPanelComponent, ComponentGetState>(GetSolarPanelState);
        SubscribeLocalEvent<SolarPanelComponent, ComponentHandleState>(HandleSolarPanelState);

        SubscribeLocalEvent<SolarSunComponent, ComponentInit>(OnSunInit);
        SubscribeLocalEvent<SolarSunComponent, EntityUnpausedEvent>(OnSunUnpaused);
    }

    #region Panel

    private void OnInit(EntityUid uid, SolarPanelComponent component, ComponentInit args)
    {
        if (component.LastUpdate < GameTiming.CurTime)
            component.LastUpdate = GameTiming.CurTime;
    }

    public Angle GetAngle(SolarPanelComponent component)
    {
        return component.StartAngle + component.AngularVelocity * (GameTiming.CurTime - component.LastUpdate).TotalSeconds;
    }

    private void OnUnpause(EntityUid uid, SolarPanelComponent component, ref EntityUnpausedEvent args)
    {
        component.LastUpdate += args.PausedTime;
        Dirty(component);
    }

    private void GetSolarPanelState(EntityUid uid, SolarPanelComponent component, ref ComponentGetState args)
    {
        args.State = new SolarPanelComponentState(component.StartAngle, component.AngularVelocity, component.LastUpdate);
    }

    private void HandleSolarPanelState(EntityUid uid, SolarPanelComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not SolarPanelComponentState state)
            return;

        component.StartAngle = state.Angle;
        component.AngularVelocity = state.AngularVelocity;
        component.LastUpdate = state.LastUpdate;
    }

    #endregion

    #region Sun

    private void OnSunInit(EntityUid uid, SolarSunComponent component, ComponentInit args)
    {
        if (component.LastUpdate < GameTiming.CurTime)
            component.LastUpdate = GameTiming.CurTime;
    }

    private void OnSunUnpaused(EntityUid uid, SolarSunComponent component, ref EntityUnpausedEvent args)
    {
        component.LastUpdate += args.PausedTime;
    }

    #endregion
}
