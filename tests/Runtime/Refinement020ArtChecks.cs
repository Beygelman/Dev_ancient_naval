using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using DevAncientNaval.Presentation;
using DevAncientNaval.Presentation.Map;
using DevAncientNaval.Presentation.UI;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Tests.Runtime;
public partial class Refinement020ArtChecks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;
    private void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("0.20 art: " + message);
        _checks++;
    }
    private async Task Frame()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        RenderingServer.ForceDraw();
    }
    private static IEnumerable<Node> Nodes(Node node)
    {
        yield return node;
        foreach (var child in node.GetChildren())
            foreach (var item in Nodes(child)) yield return item;
    }
    private async Task Capture(string suffix)
    {
        var arg = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--capture="));
        if (arg is null || DisplayServer.GetName() == "headless") return;
        await Frame();
        Check(GetViewport().GetTexture().GetImage().SavePng(arg[10..].Replace(".png", "-" + suffix + ".png")) == Error.Ok,"capture "+suffix);
    }
    public override async void _Ready()
    {
        try
        {
            await Frame();
            Game.FastChecks = true;
            var rules = Game.Battle.Rules;
            foreach (var color in Enum.GetValues<FleetColor>())
            {
                var towns = Enumerable.Range(0,5).Select(i=>new GridPosition(5+i*4,8)).ToArray();
                var board = new GameBoard(30,22,p=>p.Y is >=6 and <=8 && p.X is >=2 and <=24 ? TerrainType.Land : TerrainType.Water,seed:731);
                var battle = new BattleState(board,rules,new[] {(Side.Player,ShipClass.Mothership,new GridPosition(12,12)),(Side.Enemy,ShipClass.Mothership,new GridPosition(27,19)),(Side.Player,ShipClass.CannonTower,new GridPosition(9,12)),(Side.Player,ShipClass.Balloon,new GridPosition(15,12)),(Side.Player,ShipClass.Kolonel,new GridPosition(18,12))},Array.Empty<GridPosition>(),villageSpots:towns);
                battle.SetPlayerColor(color);
                battle.SetCreative(true);
                battle.SetGodEye(true);
                var snapshot = battle.CaptureSnapshot();
                snapshot.Villages = snapshot.Villages.Select((v,i)=>v with { Owner=Side.Player,Level=i+1,Health=(i+1)*5,Fortified=true,Port=i>=2 }).ToArray();
                snapshot.Ships[0].Level = 5;
                snapshot.Ships[0].Health = 35;
                battle=BattleState.LoadJson(BattleState.SerializeSnapshot(snapshot));
                Game.LoadScenario(battle);
                Game.MapCamera.Position=Game.BoardView.Projection.GridToWorld(new(13,10));
                Game.MapCamera.Zoom=Vector2.One*1.25f;
                Game.MapCamera.ForceUpdateScroll();
                for (int i=0;i<6;i++) await Frame();
                Check(Game.Fleet.HealthBadgeCount==5,"all five models observed in "+color);
                Check(battle.TradeRoutes(Side.Player).Routes.Count==3,"curved routes join the three ports");
                await Capture(color.ToString().ToLowerInvariant());
                Game.SelectCell(new(12,12));
                await ToSignal(GetTree().CreateTimer(.4),SceneTreeTimer.SignalName.Timeout);
                await Frame();
                var fan=Nodes(Game.Hud).OfType<RadialPapyrus>().Single(n=>n.Name=="ActionPapyrus");
                var origin=GetViewport().GetCanvasTransform()*Game.BoardView.ToGlobal(Game.BoardView.Projection.GridToWorld(new(12,12)));
                Check((fan.Position+SectorButton.Center-origin-new Vector2(0,36)).Length()<1,"fan follows the hull at "+color);
                var before=fan.Position;
                Game.MapCamera.Pan(new(40,20));
                await Frame();
                Check(fan.Position.DistanceTo(before)>5,"fan responds to camera movement");
                var yard=Nodes(Game.Hud).OfType<Button>().Single(n=>n.Name=="ActionBuild");
                yard.EmitSignal(BaseButton.SignalName.Pressed);
                await ToSignal(GetTree().CreateTimer(.4),SceneTreeTimer.SignalName.Timeout);
                await Frame();
                var build=Nodes(Game.Hud).OfType<SectorButton>().Where(n=>n.Name.ToString().StartsWith("Build") && n.IsVisibleInTree()).ToArray();
                Check(build.Length>0 && build.All(n=>n.Cost==battle.BuildPrice(Side.Player,(ShipClass)Enum.Parse(typeof(ShipClass),n.Name.ToString()[5..]))),"all ship prices come from active rules");
                await Capture(color.ToString().ToLowerInvariant()+"-shipyard");
            }
            GD.Print($"PASS: {_checks} six-fleet monuments, growing towns, ports, anchored fans and coin prices.");
            GetTree().Quit();
        }
        catch(Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}
