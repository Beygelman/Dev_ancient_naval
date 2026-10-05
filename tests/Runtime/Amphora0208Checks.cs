using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Presentation;
using DevAncientNaval.Presentation.Map;
using DevAncientNaval.Presentation.UI;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Tests.Runtime;

/// <summary>Reference silhouette, native triangulation, health policy and printed-counter input.</summary>
public partial class Amphora0208Checks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;
    private void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException("Amphora0208: " + message);
        _checks++;
    }
    private static IEnumerable<Node> Nodes(Node root)
    {
        yield return root;
        foreach (Node node in root.GetChildren())
            foreach (Node child in Nodes(node)) yield return child;
    }
    private async Task Frames(int count = 5)
    {
        for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }
    private static double Area(Vector2[] points)
    {
        double area = 0;
        for (int i = 0; i < points.Length; i++) area += points[i].Cross(points[(i + 1) % points.Length]);
        return Math.Abs(area) / 2;
    }
    private void CheckProfiles()
    {
        foreach (bool veteran in new[] { false, true })
            for (int stage = 0; stage < 4; stage++)
            {
                var profile = ClayAmphoraShape.Profile(stage, veteran);
                Check(profile.Distinct().Count() == profile.Length, "no duplicate polygon closure in stage " + stage);
                var triangles = Geometry2D.TriangulatePolygon(profile);
                Check(triangles.Length > 0, "native renderer triangulates stage " + stage + " veteran=" + veteran);
                double covered = 0;
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    Vector2 a = profile[triangles[i]], b = profile[triangles[i + 1]], c = profile[triangles[i + 2]];
                    covered += Math.Abs((b - a).Cross(c - a)) / 2;
                }
                Check(Math.Abs(covered - Area(profile)) < .002,
                    "native triangles cover the whole undistorted reference silhouette");
            }
        var intact = ClayAmphoraShape.Profile(0);
        float mouth = intact.Where(p => p.Y < -25).Max(p => p.X) - intact.Where(p => p.Y < -25).Min(p => p.X);
        float belly = intact.Max(p => p.X) - intact.Min(p => p.X);
        float neck = intact.Where(p => p.Y is >= -20 and <= -11).Max(p => p.X)
            - intact.Where(p => p.Y is >= -20 and <= -11).Min(p => p.X);
        Check(mouth > 30 && neck < mouth * .6f && belly > mouth,
            "vessel has a broad flared mouth, short narrow neck and larger rounded pear belly");
        Check(ClayAmphoraShape.Profile(0, true).Min(p => p.Y) < intact.Min(p => p.Y) - 10,
            "veteran retains its longer neck and contrasting bands");
        foreach (var pair in new[] { (100d, 0), (75d, 1), (50d, 2), (25d, 3) })
            Check(AmphoraBadgeArt.Stage(pair.Item1, 100) == pair.Item2, "health-stage policy remains unchanged at " + pair.Item1);
        Check(AmphoraBadgeArt.DigitAnchor(new(13, 29)) == new Vector2(13, 33),
            "damage digits retain the established health-jug anchor");
    }
    private async Task Capture(string suffix)
    {
        string? path = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--capture="))?[10..];
        if (path is null || DisplayServer.GetName() == "headless") return;
        await Frames();
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Check(GetViewport().GetTexture().GetImage().SavePng(path.Replace(".png", "-" + suffix + ".png")) == Error.Ok,
            "native reference capture " + suffix);
        await Frames(1);
    }
    private async Task Gallery()
    {
        var layer = new CanvasLayer { Layer = 90 };
        AddChild(layer);
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        layer.AddChild(root);
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(new ColorRect { Color = new("233f49"), Size = GetViewport().GetVisibleRect().Size,
            MouseFilter = Control.MouseFilterEnum.Ignore });
        var title = new Label { Text = "Clay vessels · curled ears · faction glaze", Position = new(24, 16) };
        title.AddThemeFontSizeOverride("font_size", 23);
        root.AddChild(title);
        FleetColor[] nations = { FleetColor.Blue, FleetColor.Purple, FleetColor.Yellow,
            FleetColor.White, FleetColor.Green, FleetColor.Red };
        ShipClass?[] kinds = { ShipClass.Mothership, ShipClass.Garrison, ShipClass.Fishing,
            ShipClass.Kolonel, ShipClass.Togus, null };
        float column = GetViewport().GetVisibleRect().Size.X / 6;
        for (int i = 0; i < nations.Length; i++)
        {
            var jug = new ReadyActionJug { Position = new(column * (i + .5f) - 56, 82), Size = new(112, 124),
                MouseFilter = Control.MouseFilterEnum.Ignore };
            root.AddChild(jug);
            jug.Update(12 + i, FleetPalette.Color(nations[i]), nations[i]);
            for (int stage = 0; stage < 4; stage++)
            {
                var badge = new StudyBadge { Position = new(column * (i + .5f) + 14, 285 + stage * 90),
                    Scale = Vector2.One * 2.6f, Color = FleetPalette.Color(nations[i]), Kind = kinds[i],
                    Health = new[] { 100d, 70d, 40d, 20d }[stage], Veteran = i % 2 == 1 };
                root.AddChild(badge);
            }
        }
        await Capture("faction-vessels");
        layer.QueueFree();
        await Frames();
    }
    private async Task LiveCounter()
    {
        Game.Home.Hide();
        Game.FastChecks = true;
        Game.Hud.InstantPaperAnimations = true;
        UiHints.Set(true, false);
        Game.Refresh();
        await Frames();
        var jug = Nodes(Game.Hud).OfType<ReadyActionJug>().Single();
        Check(jug.IsVisibleInTree(), "reference-shaped counter remains available on the player's turn");
        int ready = Game.Battle.ReadyActions(Side.Player).Count;
        Check(jug.Count == ready, "new art retains the exact useful-object count");
        string unchanged = Game.Battle.SaveJson();
        var point = jug.GetGlobalTransformWithCanvas() * jug.PrintedCountCenter;
        GetViewport().PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
        foreach (bool down in new[] { true, false })
            GetViewport().PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point,
                ButtonIndex = MouseButton.Left, Pressed = down,
                ButtonMask = down ? MouseButtonMask.Left : (MouseButtonMask)0 }, true);
        await Frames();
        Check(Game.Hud.ReadyActionsMenuVisible, "native click on the rounded vase still opens its action list");
        Check(Game.Battle.SaveJson() == unchanged, "new vessel art and its input change no health, commands or battle policy");
        await Capture("live-counter");
        Game.Hud.CloseReadyActionsMenu();
    }
    public override async void _Ready()
    {
        try
        {
            await Frames();
            CheckProfiles();
            await Gallery();
            await LiveCounter();
            GD.Print($"PASS: {_checks} v020.8 reference-shaped amphora profile, native triangulation, health-anchor and counter-input checks.");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PushError(error.ToString());
            GetTree().Quit(1);
        }
    }
    private partial class StudyBadge : Node2D
    {
        internal Color Color { get; init; }
        internal ShipClass? Kind { get; init; }
        internal double Health { get; init; }
        internal bool Veteran { get; init; }
        public override void _Draw() => AmphoraBadgeArt.Draw(this, Vector2.Zero, Health, 100, Color, Kind,
            AmphoraMotion.Still(Health, 100), Veteran);
    }
}
