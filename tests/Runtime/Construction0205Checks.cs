using System;
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

public partial class Construction0205Checks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;
    private void Check(bool value,string name)
    { if(!value)throw new InvalidOperationException("Construction0205: "+name);_checks++; }
    private async Task Frame(int count=4)
    { for(int i=0;i<count;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame); }
    private async Task Capture(string suffix)
    {
        var arg=OS.GetCmdlineUserArgs().FirstOrDefault(a=>a.StartsWith("--capture="));
        if(arg is null||DisplayServer.GetName()=="headless")return;
        await Frame();RenderingServer.ForceDraw();
        Check(GetViewport().GetTexture().GetImage().SavePng(arg[10..].Replace(".png","-"+suffix+".png"))==Error.Ok,"capture "+suffix);
    }
    public override async void _Ready()
    {
        try
        {
            await Frame();Game.FastChecks=true;
            var mother=new GridPosition(4,4);var support=new GridPosition(5,4);
            var battle=new BattleState(new GameBoard(18,18,_=>TerrainType.Water),Game.Battle.Rules,
                new[]{(Side.Player,ShipClass.Mothership,mother),(Side.Player,ShipClass.Fishing,support),
                    (Side.Enemy,ShipClass.Mothership,new GridPosition(16,16))},Array.Empty<GridPosition>(),villageSpots:Array.Empty<GridPosition>());
            var save=battle.CaptureSnapshot();save.Credits[0]=100;save.Ships[0].Level=5;
            save.Treasuries=new[]{new Treasury(save.NextId++,new(3,4)),new Treasury(save.NextId++,new(3,3),true)};
            save.Outcomes=new[]{new SavedOutcome(save.Treasuries[0].Id,TreasuryReward.Currency)};
            battle=BattleState.LoadJson(BattleState.SerializeSnapshot(save));
            Game.LoadScenario(battle);Game.Home.Hide();Game.MapCamera.Zoom=Vector2.One*1.5f;
            Game.MapCamera.Position=Game.BoardView.Projection.GridToWorld(mother);Game.MapCamera.ForceUpdateScroll();
            Game.SelectCell(mother);await Frame();
            string untouched=battle.SaveJson();int terrain=Game.BoardView.TerrainTextureUpdateRequests;
            foreach(var kind in new[]{ShipClass.Garrison,ShipClass.Fishing,ShipClass.CannonTower,ShipClass.Lighthouse})
            {
                Game.BeginBuild(kind);Check(Game.Mode==OrderMode.Build,"build mode "+kind);
                var cell=Game.BoardView.Reachable.First();
                // Exercise actual map hover input in canvas coordinates.
                var screen=GetViewport().GetCanvasTransform()*Game.BoardView.Projection.GridToWorld(cell);
                Godot.Input.WarpMouse(screen);
                GetViewport().PushInput(new InputEventMouseMotion{Position=screen,GlobalPosition=screen},true);
                await Frame();
                Check(Game.Construction?.Cell==cell&&Game.Construction.Kind==kind&&Game.Construction.Visible,"legal blueprint "+kind);
                var blueprint=Game.Construction ?? throw new InvalidOperationException("Missing preview");
                Check(kind is ShipClass.CannonTower or ShipClass.Lighthouse?blueprint.CoverageEdges>0:blueprint.CoverageEdges==0,"only tower/sight coverage "+kind);
                int rebuilds=blueprint.Rebuilds;
                for(int i=0;i<20;i++)Game.PreviewConstructionAt(cell);
                Check(blueprint.Rebuilds==rebuilds,"stationary hover retains blueprint "+kind);
                await Capture("blueprint-"+kind);
                Check(blueprint.Visible&&blueprint.Cell==cell,"projection remains visible through rendered frames "+kind);
                Game.PreviewConstructionAt(new(16,16));
                Check(!Game.Construction.Visible,"no illegal or hidden projection "+kind);
                Game.CancelOrder();Check(Game.Construction?.Visible!=true,"clear projection on deselect "+kind);
                Game.SelectCell(mother);
            }
            Check(battle.SaveJson()==untouched,"hover does not create ships/spend money/change randomness");
            Check(Game.BoardView.TerrainTextureUpdateRequests==terrain,"hover never rebuilds terrain");
            Game.SelectCell(support);Game.BeginBuild(ShipClass.CannonTower);
            Check(Game.Mode==OrderMode.Build,"support brigade tower construction is reachable throughUI");
            Game.CancelOrder();
            Check(NavalGlyphArt.Symbol(ShipClass.Fishing)==ActionSymbol.Support,"support icon shared with amphora");
            Game.Ambience.RefreshVisibility();
            Check(Game.Ambience.TreasureGlowVertexCount>0,"uncollected ruin glows without waiting crew");
            await Capture("support-and-ruins");
            Game.SelectCell(new(3,3));await Frame();
            Check(Game.SelectedShipId is null&&Game.SelectedVillageId is null&&!Game.Hud.InformationVisible,"collected ruins no longer open action/info parchment");
            Check(AncientLore.Cell(battle,new(3,3)) is null,"collected ruin has no informational action");
            GD.Print($"PASS: {_checks} construction projection, support art and ruin checks.");GetTree().Quit();
        }
        catch(Exception error){GD.PushError(error.ToString());GetTree().Quit(1);}
    }
}
