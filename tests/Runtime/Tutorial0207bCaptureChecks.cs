using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using DevAncientNaval.Presentation;
using DevAncientNaval.Presentation.UI;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Tests.Runtime;

/// <summary>Native photographs of controlled public scenes, written outside the source tree for review.</summary>
public partial class Tutorial0207bCaptureChecks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;
    private void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException("Tutorial0207bCapture: " + message);
        _checks++;
    }
    internal static IEnumerable<Node> Nodes(Node root)
    {
        yield return root;
        foreach (Node child in root.GetChildren()) foreach (var node in Nodes(child)) yield return node;
    }
    internal static BattleState Fixture(BattleRules rules, string topic)
    {
        if (topic is "resources" or "kolonel" or "commands")
        {
            var original = TutorialCapture0206Checks.Fixture(rules, topic == "commands" ? "resources" : topic);
            var snapshot = original.CaptureSnapshot();
            var mother = snapshot.Ships.Single(s => s.Owner == Side.Player && s.Kind == ShipClass.Mothership);
            if (topic != "kolonel")
            {
                mother.Level = topic == "commands" ? 5 : 2;
                mother.Resources = topic == "commands" ? 0 : 2;
                mother.Health = rules.Get(ShipClass.Mothership).MaxHealth
                    + (mother.Level - 1) * rules.Get(ShipClass.Mothership).HealthPerLevel;
            }
            return BattleState.LoadJson(BattleState.SerializeSnapshot(snapshot));
        }
        var town = new GridPosition(7, 7);
        bool land = topic == "village";
        var setup = new List<(Side Owner, ShipClass Class, GridPosition Position)> {
            (Side.Player, ShipClass.Mothership, land ? new(6, 7) : new(6, 9)),
            (Side.Enemy, ShipClass.Mothership, new(21, 21)) };
        if (topic == "veterancy")
        {
            setup.Add((Side.Player, ShipClass.Invader, new(8, 7)));
            setup.Add((Side.Player, ShipClass.Invader, new(10, 7)));
        }
        var battle = new BattleState(new GameBoard(24, 24,
                p => land && p.X is >= 7 and <= 10 && p.Y is >= 5 and <= 7 ? TerrainType.Land : TerrainType.Water,
                seed: land ? 2708 : 2709),
            rules, setup, fishSpots: Array.Empty<GridPosition>(),
            villageSpots: land ? new[] { town } : Array.Empty<GridPosition>());
        var saved = battle.CaptureSnapshot();
        saved.Credits[0] = 80;
        if (land) saved.Villages[0] = saved.Villages[0] with { Owner = Side.Player, Level = 2, Health = 10 };
        if (topic == "veterancy")
        {
            var crews = saved.Ships.Where(s => s.Kind == ShipClass.Invader).OrderBy(s => s.Id).ToArray();
            crews[0].Kills = 2;
            crews[1].Kills = 3;
            crews[1].IsVeteran = true;
            crews[1].Health = Ship.Whole(rules.Get(ShipClass.Invader).MaxHealth * 1.25);
        }
        battle = BattleState.LoadJson(BattleState.SerializeSnapshot(saved));
        foreach (var award in battle.PendingAwards.ToArray()) battle.ClaimAward(Side.Player, award.Id);
        return battle;
    }
    private async Task Frames(int count = 5)
    {
        for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }
    private Vector2 Screen(GridPosition cell) => GetViewport().GetCanvasTransform()
        * Game.BoardView.ToGlobal(Game.BoardView.Projection.GridToWorld(cell));
    private void KeepOnlyWorldPaper(string? paper)
    {
        foreach (var layer in Nodes(Game).OfType<CanvasLayer>()) layer.Hide();
        if (paper is null) return;
        Game.Hud.Show();
        var root = Game.Hud.GetChildren().OfType<Control>().Single();
        foreach (Control child in root.GetChildren().OfType<Control>())
            child.Visible = child.Name.ToString() == paper;
    }
    private async Task Photograph(string topic, string output)
    {
        var battle = Fixture(Game.Battle.Rules, topic);
        Game.LoadScenario(battle);
        Game.Home.Hide();
        Game.Hud.Show();
        Game.FastChecks = true;
        Game.Hud.InstantPaperAnimations = true;
        GridPosition focus = topic == "veterancy" ? new(9, 7) : new(7, 7);
        Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(focus);
        Game.MapCamera.Zoom = Vector2.One * (topic == "commands" ? 1.25f : topic == "village" ? 1.5f : 1.6f);
        Game.MapCamera.ForceUpdateScroll();
        await Frames(12);
        string untouched = battle.SaveJson();
        Vector2 center;
        if (topic == "kolonel")
        {
            var gun = battle.OwnShips(Side.Player).Single(s => s.Definition.Class == ShipClass.Kolonel);
            var target = battle.Ships.Single(s => s.Owner == Side.Enemy && s.Definition.Class == ShipClass.Invader);
            Game.SelectCell(gun.Position);
            Game.SelectCell(target.Position);
            await Frames(20);
            Check(Game.Hud.SalvoChoiceVisible, "real target selection opens the single/double parchment before firing");
            var choices = Nodes(Game.Hud).OfType<SectorButton>()
                .Where(b => b.Name == "SingleShot" || b.Name == "DoubleShot").ToArray();
            Check(choices.Length == 2 && choices.All(b => b.IsVisibleInTree() && !b.Disabled),
                "both real charge choices are visible and legal");
            Check(Game.Fleet.ActiveProjectileCount == 0 && battle.PendingPresentation is null && gun.AttacksUsed == 0,
                "the new double-salvo photograph is taken before launching or spending charges");
            KeepOnlyWorldPaper("TargetSalvoPapyrus");
            center = Screen(target.Position) + new Vector2(-45, 0);
        }
        else if (topic == "commands")
        {
            var mother = battle.Mothership(Side.Player)!;
            Game.SelectCell(mother.Position);
            await Frames(20);
            var paper = Nodes(Game.Hud).OfType<RadialPapyrus>().Single(p => p.Name == "ActionPapyrus");
            Check(Nodes(paper).OfType<SectorButton>().Where(b => b.IsVisibleInTree()).All(b => b.ShortcutNumber > 0),
                "the commands photograph shows real current keyboard numbers");
            KeepOnlyWorldPaper("ActionPapyrus");
            center = Screen(mother.Position) + new Vector2(0, 22);
        }
        else if (topic == "village")
        {
            var village = battle.Villages.Single();
            Game.SelectCell(village.Position);
            await Frames(20);
            Check(Nodes(Game.Hud).OfType<SectorButton>().Any(b => b.Name == "ActionVillageUpgrade" && b.IsVisibleInTree() && !b.Disabled),
                "the village photograph shows an actual affordable level-up order");
            KeepOnlyWorldPaper("ActionPapyrus");
            center = GetViewport().GetCanvasTransform() * Game.BoardView.ToGlobal(Game.BoardView.VillageWorldAnchor(village))
                + new Vector2(0, 24);
        }
        else
        {
            KeepOnlyWorldPaper(null);
            center = Screen(focus) + new Vector2(0, topic == "resources" ? 24 : -12);
            Check(topic != "resources" || battle.Mothership(Side.Player)!.Resources == 2,
                "resource photograph retains a real partly filled Mothership progress row");
            Check(topic != "veterancy" || battle.OwnShips(Side.Player).Count(s => s.Definition.Class == ShipClass.Invader && s.IsVeteran) == 1,
                "veterancy photograph compares a progressing crew and a real veteran");
        }
        await Frames(18);
        Check(battle.SaveJson() == untouched, topic + " photograph changes no gameplay, money, RNG or save policy");
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        var image = GetViewport().GetTexture().GetImage();
        var size = image.GetSize();
        var crop = image.GetRegion(new Rect2I(
            Mathf.Clamp((int)MathF.Round(center.X) - 256, 0, size.X - 512),
            Mathf.Clamp((int)MathF.Round(center.Y) - 128, 0, size.Y - 256), 512, 256));
        Check(crop.SavePng(Path.Combine(output, $"tutorial-{topic}-0207b.png")) == Error.Ok, "save native photograph " + topic);
        await Frames(1);
    }
    public override async void _Ready()
    {
        try
        {
            if (DisplayServer.GetName() == "headless") throw new InvalidOperationException("Tutorial photographs need the native renderer.");
            string output = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--tutorial-art-output="))?[22..]
                ?? throw new InvalidOperationException("Provide an explicit disposable --tutorial-art-output directory.");
            output = Path.GetFullPath(output);
            string source = Path.GetFullPath(ProjectSettings.GlobalizePath("res://"));
            if (output.StartsWith(source.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Capture into an external review directory; do not overwrite source screenshots automatically.");
            Directory.CreateDirectory(output);
            await Frames(); UiScale.Set(1, false); Language.Set("en", false); UiHints.Set(false, persist: false);
            foreach (string topic in new[] { "resources", "kolonel", "village", "veterancy", "commands" })
                await Photograph(topic, output);
            GD.Print($"PASS: {_checks} v020.7b real native tutorial photographs and pre-fire salvo choice checks. Output: {output}");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}
