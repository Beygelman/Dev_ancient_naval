using System;
using System.Linq;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using DevAncientNaval.Presentation;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Tests.Runtime;
public partial class Heavens0202Checks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;
    private void Check(bool value, string text) { if (!value) throw new Exception(text); _checks++; }
    private async Task Frame() => await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    private async Task Capture(string name)
    {
        var arg = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--capture="));
        if (arg is null || DisplayServer.GetName() == "headless") return;
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Check(GetViewport().GetTexture().GetImage().SavePng(arg[10..].Replace(".png", "-"+name+".png")) == Error.Ok, "heavens screenshot");
    }
    public override async void _Ready()
    {
        try
        {
            await Frame();
            var battle = new BattleState(new GameBoard(24,24,_=>TerrainType.Water), Game.Battle.Rules,
                new[] { (Side.Player,ShipClass.Mothership,new GridPosition(2,2)),
                    (Side.Enemy,ShipClass.Mothership,new GridPosition(21,21)) },
                Array.Empty<GridPosition>(), villageSpots:Array.Empty<GridPosition>());
            Game.LoadScenario(battle); Game.Home.Hide();
            var showing = Game.Hud.ShowHeavenlyAssistance(new(Side.Enemy,4,2,false),battle);
            await Frame();
            Check(Game.Hud.HeavenlyText == "Other nations receive heavenly aid", "unknown nation's aid hides identity and amount");
            await showing;
            Check(Game.Hud.HeavenlyText == "" && !Game.Hud.HeavenlyLightVisible, "heaven overlay stops after finite animation");
            // A real incoming personal turn pays once and feeds the actual Main turn pipeline.
            var save = battle.CaptureSnapshot();
            save.Round = 4; save.TurnSerial = 6; save.ActiveSide = Side.Enemy;
            save.PersonalTurnStarts[(int)Side.Player] = 4;
            battle = BattleState.LoadJson(BattleState.SerializeSnapshot(save));
            Game.LoadScenario(battle);
            int money = battle.Credits(Side.Player);
            var ended = battle.EndTurn(Side.Enemy);
            Check(ended.HeavenlyReceipts?.Single().Amount == 2 && battle.Credits(Side.Player)>money, "real fifth personal turn carries a paid receipt");
            Game.FastChecks = false;
            showing = Game.PresentHeavenlyAssistance(ended);
            await Frame();
            Check(Game.Hud.HeavenlyText.Contains("+2 Thors") && Game.Hud.HeavenlyLightVisible, "paid religious blessing displays Thor reward and heavenly rays");
            Check(Game.Hud.FindChild("HeavenlyLight", true, false) is Control light && light.MouseFilter == Control.MouseFilterEnum.Ignore, "light never intercepts ship interaction");
            await ToSignal(GetTree().CreateTimer(.45), SceneTreeTimer.SignalName.Timeout);
            await Capture("blessing");
            await showing;
            Check(Game.Hud.HeavenlyText == "", "banner retracts without leaving a center notice");
            GD.Print($"PASS: {_checks} v0.20.2 heavenly notification, privacy, payment and light checks.");
            GetTree().Quit();
        }
        catch(Exception e) { GD.PushError(e.ToString()); GetTree().Quit(1); }
    }
}
