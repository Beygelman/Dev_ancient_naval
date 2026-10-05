using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Presentation.UI;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation;

public partial class Main
{
    private VoyageWelcome _voyageWelcome = null!;
    internal VoyageWelcome VoyageWelcome => _voyageWelcome;
    private async Task DescendToFlagship(FleetColor color)
    {
        if (FastChecks) return;
        var mother = Battle.Mothership(Side.Player)!;
        var target = BoardView.Projection.GridToWorld(mother.Position);
        float finalZoom = Mathf.Clamp(.75f, Camera.MapCamera.MinZoom, Camera.MapCamera.MaxZoom);
        MapCamera.CancelFlight();
        MapCamera.Position = new Vector2(MapCamera.MapBounds.GetCenter().X, MapCamera.MapBounds.Position.Y - 160);
        MapCamera.Zoom = Vector2.One * Mathf.Max(Camera.MapCamera.MinZoom, finalZoom * .38f);
        MapCamera.ForceUpdateScroll();
        _voyageWelcome.BeginDescent();
        _home.Hide();
        Hud.Hide();
        MapInput.SetProcessInput(false); MapInput.SetProcessUnhandledInput(false);
        var descent = CreateTween().SetParallel();
        descent.TweenProperty(MapCamera, "position", target, 3.2).SetTrans(Tween.TransitionType.Quint).SetEase(Tween.EaseType.InOut);
        descent.TweenProperty(MapCamera, "zoom", Vector2.One * finalZoom, 3.2).SetTrans(Tween.TransitionType.Quint).SetEase(Tween.EaseType.InOut);
        var reveal = _voyageWelcome.RevealSea();
        await ToSignal(descent, Tween.SignalName.Finished);
        await reveal;
        MapCamera.ForceUpdateScroll();
        _voyageWelcome.InstantAnimations = FastChecks;
        await _voyageWelcome.Welcome(color);
        Hud.Show();
        MapInput.SetProcessInput(true); MapInput.SetProcessUnhandledInput(true);
    }
}
