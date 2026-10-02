using DevAncientNaval.Core.Units;
using Godot;

namespace DevAncientNaval.Presentation.Map;

public partial class FleetView
{
    internal static Vector2 HealthBadgeOffset(ShipClass kind) => kind switch
    {
        ShipClass.Balloon => new(32, -65),
        ShipClass.AncientGun or ShipClass.CannonTower => new(30, -28),
        ShipClass.FishingDock => new(34, -23),
        _ => new(31 * ShipVisualProfile.For(kind).Size + 9, -18)
    };

    internal static Vector2 HealthAnchor(Vector2 center, ShipClass kind = ShipClass.Garrison) =>
        AmphoraBadgeArt.DigitAnchor(center + HealthBadgeOffset(kind));

    internal int HealthBadgeCount => _hulls.Count;
    internal int? DisplayedHealthStage(int id) => _hulls.TryGetValue(id, out var hull) ? hull.Health.Stage : null;
    internal AmphoraMotion? DisplayedHealthMotion(int id) => _hulls.TryGetValue(id, out var hull) ? hull.Health.Motion(_clock) : null;
}
