using System;
using System.Collections.Generic;
using System.Linq;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using DevAncientNaval.Presentation;
using DevAncientNaval.Presentation.UI;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Tests.Runtime;
/// <summary>Checks the new selectors, scroll geometry and rule-derived chart text.</summary>
internal static class UiPapyrusChecks
{
    public static int Run(Main game)
    {
        int checks = 0;
        void Check(bool ok, string name)
        {
            if (!ok)
                throw new InvalidOperationException(name);
            checks++;
        }

        IEnumerable<Node> Descendants(Node node)
        {
            yield return node;
            foreach (var child in node.GetChildren())
                foreach (var descendant in Descendants(child))
                    yield return descendant;
        }

        var difficulties = Descendants(game.Home).OfType<Button>().Where(b => b.Name.ToString().StartsWith("Difficulty")).ToArray();
        Check(difficulties.Length == 3, "All three captain difficulties exist.");
        var initialDifficulty = game.Home.Difficulty;
        foreach (var difficulty in Enum.GetValues<AiDifficulty>())
        {
            difficulties.Single(b => b.Name == "Difficulty" + difficulty).EmitSignal(BaseButton.SignalName.Pressed);
            Check(game.Home.Difficulty == difficulty, "Difficulty selector " + difficulty);
        }
        difficulties.Single(b => b.Name == "Difficulty" + initialDifficulty).EmitSignal(BaseButton.SignalName.Pressed);
        var oldEye = game.Battle.GodEye;
        var eye = Descendants(game.Hud).OfType<Button>().Single(b => b.Name == "GodEye");
        eye.EmitSignal(BaseButton.SignalName.Pressed);
        Check(game.Battle.GodEye != oldEye && game.Battle.Board.Tiles.All(t => game.Battle.Vision.IsVisible(Side.Player, t.Position)), "God's eye menu reveals the complete chart.");
        eye.EmitSignal(BaseButton.SignalName.Pressed);
        Check(game.Battle.GodEye == oldEye, "God's eye menu restores the prior setting.");
        int before = game.Home.OpponentCount;
        var settings = Descendants(game.Home).OfType<Button>().Where(b => b.Name.ToString().StartsWith("OpponentCount")).ToArray();
        Check(settings.Length == 4, "All four rival fleet choices exist.");
        for (int count = 1; count <= 4; count++)
        {
            settings.Single(b => b.Name == "OpponentCount" + count).EmitSignal(BaseButton.SignalName.Pressed);
            Check(game.Home.OpponentCount == count, "Rival fleet selector " + count);
        }

        settings.Single(b => b.Name == "OpponentCount" + before).EmitSignal(BaseButton.SignalName.Pressed);
        var scroll = new RadialPapyrus
        {
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        game.AddChild(scroll);
        var commands = new List<SectorButton>();
        for (int i = 0; i < 8; i++)
        {
            var command = new SectorButton();
            scroll.AddChild(command);
            command.SetSector(i, 8);
            commands.Add(command);
        }

        Check(Math.Abs(commands[0].CenterAngle + commands[^1].CenterAngle - Mathf.Pi) < .001, "Scroll opens symmetrically below its object.");
        scroll.Configure(commands, true);
        Check(scroll.Reveal == 0 && scroll.ActionCount == 8, "Scroll starts rolled and retains locked commands.");
        Check(!commands[0]._HasPoint(commands[0].IconCenter), "Rolled commands cannot receive accidental clicks.");
        scroll._Process(.13);
        Check(scroll.Reveal is> 0 and < 1, "Scroll unfolds gradually.");
        scroll._Process(.2);
        Check(scroll.Reveal == 1 && !scroll.IsProcessing(), "Scroll stops processing after opening.");
        Check(commands.All(c => c._HasPoint(c.IconCenter)), "Unfolded commands have usable hit areas.");
        Check(commands.All(c => !c._HasPoint(SectorButton.Center)), "The map remains clickable in the scroll's center.");
        scroll.QueueFree();
        var mother = game.Battle.Mothership(Side.Player)!;
        checks += LoreChecks.Run(game.Battle);
        string lore = AncientLore.Ship(game.Battle, mother).PlainText;
        Check(lore.Contains($"Health: {mother.Health:0.##}/{mother.MaxHealth:0.##}"), "Chart describes actual ship health.");
        Check(lore.Contains($"{mother.Definition.AttackRange} tiles"), "Chart uses active weapon range.");
        Check(lore.Contains($"Passive repair: +{game.Battle.Rules.AutoRepairAmount} HP"), "Chart uses active passive repair value.");
        var fisher = game.Battle.OwnShips(Side.Player).First(s => s.Definition.Class == ShipClass.Fishing);
        Check(AncientLore.Ship(game.Battle, fisher).PlainText.Contains($"+{fisher.Definition.IncomePerTurn} Thors"), "Fishing income comes from active rules.");
        game.Hud.ShowInformation(game.Battle, mother.Position);
        Check(game.Hud.InformationVisible && game.Hud.InformationText.Contains(mother.Name == "Mothership" ? "city may sail" : "Health"), "Information button opens a readable chart.");
        game.Hud.CloseMenus();
        Check(!game.Hud.InformationVisible, "Cancel closes the information scroll.");
        // The human's defeat ends their UI even while two AI factions survive.
        var active = game.Battle.Rules;
        var terminalRules = new BattleRules
        {
            StartingCredits = active.StartingCredits,
            IncomePerMothership = active.IncomePerMothership,
            RepairAmount = active.RepairAmount,
            FleetLimit = active.FleetLimit,
            Ships = active.Ships.Select(s => s.Class == ShipClass.Mothership ? s with { Damage = 50 } : s).ToArray()
        };
        var defeated = new BattleState(new GameBoard(20, 20, _ => TerrainType.Water), terminalRules, new[] { (Side.Player, ShipClass.Mothership, new GridPosition(5, 5)), (Side.Enemy, ShipClass.Mothership, new GridPosition(6, 5)), (Side.Enemy2, ShipClass.Mothership, new GridPosition(16, 16)) }, Array.Empty<GridPosition>(), villageSpots: Array.Empty<GridPosition>());
        Check(defeated.EndTurn(Side.Player).Success && defeated.Attack(Side.Enemy, 2, 1).Success, "Defeat fixture sinks the human flagship.");
        Check(defeated.PlayerDefeated && !defeated.IsOver, "Rival factions still survive after human defeat.");
        game.Hud.UpdateBattle(defeated, null, false, OrderMode.None);
        Check(game.Hud.StatusText == "DEFEAT" && game.Hud.BannerText == "DEFEAT", "Human defeat immediately appears in the HUD.");
        Check(Descendants(game.Hud).OfType<Button>().Single(b => b.Name == "EndTurn").Disabled && !game.Hud.UpgradeVisible, "A defeated player cannot continue turns or pick upgrades.");
        game.Refresh();
        return checks;
    }
}
