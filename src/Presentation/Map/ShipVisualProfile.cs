using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Presentation.Map;

/// <summary>Presentation-only dimensions and timing; never changes combat damage.</summary>
internal readonly record struct ShipVisualProfile(float Size, float DeckWidth, float TravelSeconds, int Cannonballs, float Wake)
{
<<<<<<< Updated upstream
    internal static float ProgressY(ShipClass kind) => 32 * For(kind).Size;
=======
>>>>>>> Stashed changes
    public static ShipVisualProfile For(ShipClass kind) => kind switch
    {
        ShipClass.Mothership => new(1.23f, 1, .64f, 2, 1.3f),
        ShipClass.Kolonel => new(1.12f, 1, .45f, 3, 1.05f),
        ShipClass.Invader => new(.94f, 1, .38f, 3, .85f),
        ShipClass.Togus => new(.94f, 1.35f, .44f, 1, 1),
        ShipClass.Garrison => new(.70f, 1, .32f, 1, .56f),
        ShipClass.Fishing => new(.64f, 1, .34f, 0, .45f),
        ShipClass.PirateSchooner => new(.78f,1,.34f,1,.68f),
        ShipClass.AncientGun => new(.9f,1,0,1,0),
        ShipClass.CannonTower => new(.8f,1,0,1,0),
        ShipClass.FishingDock => new(.8f, 1, 0, 0, 0),
<<<<<<< Updated upstream
        ShipClass.Lighthouse => new(.85f, 1, 0, 0, 0),
=======
>>>>>>> Stashed changes
        _ => new(.7f, 1, .20f, 0, 0)
    };
}
