using System;
using System.Linq;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using DevAncientNaval.Presentation;
using DevAncientNaval.Presentation.Map;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Tests.Runtime;

public partial class Animation0206Checks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;
    private void Check(bool good, string text)
    { if (!good) throw new InvalidOperationException("Animation0206: " + text); _checks++; }
    private async Task Frame(int count = 3)
    { for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private async Task Capture(string suffix)
    {
        var arg = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--capture="));
        if (arg is null || DisplayServer.GetName() == "headless") return;
        await Frame(); await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Check(GetViewport().GetTexture().GetImage().SavePng(arg[10..].Replace(".png", "-" + suffix + ".png")) == Error.Ok, "capture " + suffix);
    }
    private void Load(BattleState battle, GridPosition at)
    {
        battle.SetGodEye(true);
        foreach (var award in battle.PendingAwards.ToArray()) battle.ClaimAward(award.Owner, award.Id);
        Game.LoadScenario(battle); Game.Home.Hide();
        Game.MapCamera.Position = Game.BoardView.Projection.GridToWorld(at);
        Game.MapCamera.Zoom = Vector2.One * 2.6f; Game.MapCamera.ForceUpdateScroll(); Game.Refresh();
    }
    private async Task Wreck(ShipClass kind)
    {
        var target = new GridPosition(7, 5);
        var battle = new BattleState(new GameBoard(20, 20, _ => TerrainType.Water), Game.Battle.Rules,
            new[] { (Side.Player, ShipClass.Mothership, new GridPosition(3, 3)),
                (Side.Player, ShipClass.Kolonel, new GridPosition(5, 5)),
                (Side.Enemy, ShipClass.Mothership, kind == ShipClass.Mothership ? target : new GridPosition(17,17)),
                (Side.Enemy, ShipClass.Invader, kind == ShipClass.Mothership ? new GridPosition(10,5) : target) },
            Array.Empty<GridPosition>(), villageSpots: Array.Empty<GridPosition>());
        var save = battle.CaptureSnapshot();
        var doomed=save.Ships.Single(s => s.Position == target);
        doomed.Health=1;
        if(kind==ShipClass.Mothership)doomed.Level=5;
        battle = BattleState.LoadJson(BattleState.SerializeSnapshot(save)); Load(battle, target); await Frame(8);
        var ship = battle.At(target)!;
        var order = battle.Prepare(b => b.Attack(Side.Player, 2, ship.Id));
        Check(order.Result.Success, "lethal fixture " + kind);
        var animation = Game.Fleet.Animate(order.Result, presentation: order);
        for (int frames = 0; Game.Fleet.SinkingCount == 0 && !animation.IsCompleted && frames < 240; frames++) await Frame(1);
        Check(Game.Fleet.SinkingCount > 0, "actual hull enters immersion " + kind);
        Check(Game.Fleet.WreckPieceCount >= (kind == ShipClass.Mothership ? 3 : 1), "actual hull fragments retained " + kind);
        Check(Game.Fleet.WrecksOpaque, "fragments clipped by waterline without alpha fading " + kind);
        Check(Game.Fleet.WreckPartCount(WreckPartKind.Keel)==2,"two solid hull sections break apart " + kind);
        if(kind==ShipClass.Mothership)
        {
            Check(Game.Fleet.WreckPartCount(WreckPartKind.House)==14 && Game.Fleet.WreckPartCount(WreckPartKind.Deck)==6,
                "city houses and split terraces fall as individual solid parts");
            Check(Game.Fleet.WreckPartCount(WreckPartKind.Sanctuary)==2 && Game.Fleet.WreckPartCount(WreckPartKind.Rubble)==3,
                "sanctuary breaks into separate base, crown and stones");
        }
        else Check(Game.Fleet.WreckPartCount(WreckPartKind.Mast)==2 && Game.Fleet.WreckPartCount(WreckPartKind.Gun)==4,
            "masts topple and four cannon carriages roll off the deck");
        await Capture("wreck-" + kind.ToString().ToLowerInvariant());
        await ToSignal(GetTree().CreateTimer(.7), SceneTreeTimer.SignalName.Timeout);
        Check(Game.Fleet.ImmersedWreckFaces>0 && Game.Fleet.WrecksOpaque,"3D debris crosses the sea plane while exposed faces stay opaque");
        await Capture("immersed-" + kind.ToString().ToLowerInvariant());
        await animation;
        Check(Game.Fleet.WreckPieceCount == 0 && Game.Fleet.SinkingCount == 0 && battle.Find(ship.Id) is null,
            "wreck sources released after one committed destruction " + kind);
    }
    public override async void _Ready()
    {
        try
        {
            await Frame();
            await Wreck(ShipClass.Invader); await Wreck(ShipClass.Mothership);
            var battle = new BattleState(new GameBoard(20,20,_ => TerrainType.Water), Game.Battle.Rules,
                new[] { (Side.Player,ShipClass.Mothership,new GridPosition(5,5)),
                    (Side.Enemy,ShipClass.Mothership,new GridPosition(18,18)),
                    (Side.Player,ShipClass.Lighthouse,new GridPosition(8,5)) },Array.Empty<GridPosition>(),villageSpots:Array.Empty<GridPosition>());
            var save = battle.CaptureSnapshot(); save.Credits[0] = 100; save.Ships[0].Level = 5;
            battle = BattleState.LoadJson(BattleState.SerializeSnapshot(save)); Load(battle,new(5,5)); await Frame(8);
            Game.Ambience.SpawnGulls(new(5,5),3); Game.Ambience.ScareGulls(new(5,5));
            Check(Game.Ambience.ScaredGullCount >= 3, "nearby gunfire scares wildlife");
            int scared = Game.Ambience.ScaredGullCount;
            Game.Ambience.ScareGulls(new(18,18)); Check(Game.Ambience.ScaredGullCount == scared,"distant gunfire cannot scare nearby flock");
            string untouched = battle.SaveJson(); int mesh = Game.Ambience.WaveMeshBuildCount;
            Game.SelectCell(new(5,5)); Game.BeginBuild(ShipClass.Lighthouse);
            var candidate = Game.BoardView.Reachable.First(p => battle.PreviewLighthouseTradeRoutes(Side.Player,p).Edges.Count > 0);
            Game.PreviewConstructionAt(candidate); await Frame();
            Check(Game.Construction?.TradePreviewEdges > 0,"lighthouse ghost predicts legal route");
            Check(Game.Construction?.CoverageEdges > 0,"lighthouse ghost predicts sight");
            await Capture("pencil-lighthouse-route");
            for (int i = 0; i < 12; i++) { Game.PreviewConstructionAt(candidate); Game.MapCamera.Pan(new Vector2(2,1)); await Frame(1); }
            Check(battle.SaveJson() == untouched && mesh == Game.Ambience.WaveMeshBuildCount,"ghost, wildlife and wave clock retain simulation and mesh");
            var ruinSave = battle.CaptureSnapshot();
            ruinSave.Treasuries = new[] { new Treasury(ruinSave.NextId++, candidate) };
            battle = BattleState.LoadJson(BattleState.SerializeSnapshot(ruinSave));
            Load(battle, candidate); await Frame(8);
            Check(Game.BoardView.TreasuryRuinVisible(candidate), "ruin scenery visible before construction");
            Check(battle.Build(Side.Player, 1, ShipClass.Lighthouse, candidate).Success, "lighthouse replaces ruin");
            Game.Refresh(); await Frame(5);
            Check(!Game.BoardView.TreasuryRuinVisible(candidate), "construction removes retained ruin model as well as Core site");
            GD.Print($"PASS: {_checks} v020.6 solid 3D wreck pieces, gull alarm, pencil trade previews and retained waves."); GetTree().Quit();
        }
        catch(Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}
