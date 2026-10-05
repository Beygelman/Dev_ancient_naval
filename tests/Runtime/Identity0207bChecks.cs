using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevAncientNaval.Presentation;
using DevAncientNaval.Presentation.UI;
using Godot;

namespace DevAncientNaval.Tests.Runtime;
public partial class Identity0207bChecks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;
    private void Check(bool value, string name) { if (!value) throw new InvalidOperationException(name); _checks++; }
    private static IEnumerable<Node> Nodes(Node root)
    {
        yield return root;
        foreach (Node child in root.GetChildren()) foreach (var item in Nodes(child)) yield return item;
    }
    private async Task Frames(int count = 5)
    { for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    public override async void _Ready()
    {
        try
        {
            await Frames();
            Check(GameIdentity.Version == "v020.7b" && GameIdentity.IssueDate == "06.10.2026", "source issue identity");
            var home = Nodes(Game.Home).OfType<Label>().Single(n => n.Name == "HomeVersionSignature");
            var voyage = Nodes(Game.Hud).OfType<Label>().Single(n => n.Name == "VoyageVersionSignature");
            Check(home.Text == GameIdentity.Signature && voyage.Text == home.Text, "both signatures share checked-in identity");
            Game.Home.ShowHome(false); await Frames();
            Check(home.IsVisibleInTree(), "main screen signature visible");
            Game.Home.Hide(); Game.FastChecks = true; Game.Hud.InstantPaperAnimations = true;
            Game.Hud.SetMenuVisible(true); await Frames();
            Check(voyage.IsVisibleInTree(), "in-session menu signature visible");
            var viewport = UiScale.LogicalViewport(this);
            Check(voyage.Position.X < 20 && voyage.Position.Y > viewport.Y - 45, "in-session signature bottom left");
            Game.Hud.SetMenuVisible(false); await Frames();
            var camera = Game.MapCamera;
            var initial = camera.Position;
            camera.PanByKeys(Vector2.Right, .05);
            Check(Math.Abs((camera.Position.X - initial.X) * camera.Zoom.X - 42.5f) < .1f, "WASD travels 850 screen pixels per second");
            camera.Position = initial; camera.ForceUpdateScroll();
            Check(!voyage.IsVisibleInTree(), "signature never overlays gameplay when menu hidden");
            GD.Print($"PASS: {_checks} v020.7b issue identity, both menus and faster camera checks.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}
