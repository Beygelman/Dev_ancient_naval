using System;
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

public partial class TradeGlyph0204Checks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;
    private void Check(bool condition,string name)
    {
        if (!condition) throw new InvalidOperationException("Trade/glyph0204: "+name);
        _checks++;
    }
    private async Task Frame()
    {
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        RenderingServer.ForceDraw();
    }
    private async Task Capture(string suffix)
    {
        var arg=OS.GetCmdlineUserArgs().FirstOrDefault(a=>a.StartsWith("--capture="));
        if (arg is null || DisplayServer.GetName()=="headless") return;
        await Frame();
        Check(GetViewport().GetTexture().GetImage().SavePng(arg[10..].Replace(".png","-"+suffix+".png"))==Error.Ok,"capture "+suffix);
    }
    public override async void _Ready()
    {
        try
        {
            await Frame(); Game.FastChecks=true;
            var board=new GameBoard(32,22,p=>p.Y==5 && p.X is >=4 and <=25?TerrainType.Land:TerrainType.Water,seed:9149);
            var battle=new BattleState(board,Game.Battle.Rules,new[] {
                (Side.Player,ShipClass.Mothership,new GridPosition(4,11)),
                (Side.Enemy,ShipClass.Mothership,new GridPosition(29,19)),
                (Side.Player,ShipClass.Garrison,new GridPosition(11,10)),
                (Side.Player,ShipClass.Fishing,new GridPosition(13,10)),
                (Side.Player,ShipClass.Togus,new GridPosition(15,10)),
                (Side.Player,ShipClass.CannonTower,new GridPosition(17,10)) },
                Array.Empty<GridPosition>(),villageSpots:new[] {new GridPosition(6,5),new(11,5),new(16,5)});
            var snapshot=battle.CaptureSnapshot();
            snapshot.Villages=snapshot.Villages.Select(town=>town with { Owner=Side.Player,Level=3,Health=15,Port=true }).ToArray();
            var veteran=snapshot.Ships.Single(s=>s.Kind==ShipClass.Garrison);
            veteran.IsVeteran=true; veteran.Kills=3;
            battle=BattleState.LoadJson(BattleState.SerializeSnapshot(snapshot));
            battle.SetGodEye(true);
            Game.LoadScenario(battle);
            await Frame();
            var traffic=Game.Ambience.TradeTraffic;
            traffic.SetProcess(false);
            string before=battle.SaveJson();
            Check(traffic.BoatCount==0,"new ports do not emit before their 12–20 second departure interval");
            traffic.Advance(11);
            Check(traffic.BoatCount==0,"no early departure from any port");
            traffic.Advance(9);
            Check(traffic.Departures==3 && traffic.BoatCount==3,"each connected port emits exactly once in the first interval");
            Check(traffic.Skins.Distinct().Count()==3,"three different merchant skins are present");
            Check(traffic.Voyages.All(v=>v.Source!=v.Destination),"merchant destinations are other cities");
            Check(traffic.Voyages.Select(v=>v.Source).Distinct().Count()==3,"traffic leaves every connected town");
            var initial=traffic.Positions.ToArray();
            traffic.Advance(1);
            var moved=traffic.Positions.ToArray();
            Check(initial.Zip(moved,(a,b)=>a.DistanceTo(b)).All(d=>d>0 && d<=TradeTraffic.Speed*1.01f),"retained merchants move slowly at a bounded speed");
            Check(moved.All(p=>board.GetTile(Game.BoardView.Projection.WorldToGrid(p)).Terrain!=TerrainType.Land),"sea lanes keep moving merchants off land");
            Check(before==battle.SaveJson(),"cosmetic departures and movement do not change credits, rules, ships or simulation random state");
            await Capture("traffic-and-veteran");
            for(int i=0;i<120;i++) traffic.Advance(20);
            Check(traffic.BoatCount<=TradeTraffic.MaximumBoats,"long-running traffic stays bounded");
            Check(before==battle.SaveJson(),"traffic lifecycle does not mutate saved gameplay");
            Game.Ambience.Hide(); await Frame();
            Check(!traffic.IsProcessing(),"hidden title-map traffic stops processing");
            Game.Ambience.Show();
            Check(NavalGlyphArt.Symbol(ShipClass.Fishing)==ActionSymbol.Support,"support health seal uses the shipyard workshop motif");
            Check(NavalGlyphArt.Symbol(ShipClass.Garrison)==ActionSymbol.Scout && NavalGlyphArt.Symbol(ShipClass.Kolonel)==ActionSymbol.Heavy,
                "clay and production icons share class identities");
            Check(NavalGlyphArt.Symbol(ShipClass.Togus)==ActionSymbol.Mortar && NavalGlyphArt.Symbol(ShipClass.AncientGun)==ActionSymbol.Mortar,
                "mortars on ships and towers share the open-cup motif");
            Check(NavalGlyphArt.Symbol(ShipClass.CannonTower)==ActionSymbol.Tower,"defensive towers retain their own crenellated motif");
            var art=new BoardTerrainLayer { Position=new Vector2(20,20), ZIndex=100, DrawWorld=canvas=>
            {
                canvas.DrawRect(new Rect2(0,0,600,190),PapyrusStyle.Paper);
                var symbols=new[] {ActionSymbol.Support,ActionSymbol.Scout,ActionSymbol.Standard,ActionSymbol.Heavy,ActionSymbol.Mortar,ActionSymbol.Tower,ActionSymbol.Dock,ActionSymbol.Lighthouse};
                for(int i=0;i<symbols.Length;i++) NavalGlyphArt.Draw(canvas,new Vector2(35+i*65,38),symbols[i],PapyrusStyle.Ink,3);
                var motion=AmphoraMotion.Still(10,10);
                AmphoraBadgeArt.Draw(canvas,new Vector2(85,125),10,10,new Color("7e98a7"),ShipClass.Garrison,motion);
                AmphoraBadgeArt.Draw(canvas,new Vector2(180,125),10,10,new Color("7e98a7"),ShipClass.Garrison,motion,true);
                AmphoraBadgeArt.Draw(canvas,new Vector2(275,125),10,10,new Color("7e98a7"),ShipClass.Fishing,motion);
            }};
            var layer=new CanvasLayer { Layer=100 }; AddChild(layer); layer.AddChild(art);
            await Capture("shared-motifs");
            layer.QueueFree();
            GD.Print($"PASS: {_checks} v020.4 cosmetic trade traffic and shared clay/shipyard motif checks.");
            GetTree().Quit();
        }
        catch(Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}
