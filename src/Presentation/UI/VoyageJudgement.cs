using System;
using System.Linq;
using DevAncientNaval.Core.AI;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

namespace DevAncientNaval.Presentation.UI;

internal enum VoyageDistinction { Voyager, Admiral, Conqueror, Pathfinder, Strategist }
internal readonly record struct VoyageEvidence(int Turn, int Rivals, int Area, long Kills,
    long RivalBestKills, int OwnedTowns, int TotalTowns, long StructuresBuilt, long ShipsBuilt, int Income, long SeaStrongholdsBuilt = 0);

/// <summary>Read-only judgement from public end-of-voyage facts; no extra rewards or rules.</summary>
internal static class VoyageJudgement
{
    internal static int SwiftLimit(VoyageEvidence facts) => 6 + 4 * facts.Rivals + 3 * facts.Area;
    internal static int EconomyTarget(VoyageEvidence facts) => 2 + 2 * facts.Rivals + 2 * facts.Area
        + (int)Math.Ceiling((facts.ShipsBuilt + facts.StructuresBuilt) / 8.0) + (facts.OwnedTowns + 1) / 2;
    internal static VoyageDistinction Evaluate(VoyageEvidence facts)
    {
        if (facts.Kills >= Math.Max(8, 2 * facts.RivalBestKills)) return VoyageDistinction.Admiral;
        if (facts.SeaStrongholdsBuilt >= 6 && facts.TotalTowns > 0 && facts.OwnedTowns >= Math.Ceiling(facts.TotalTowns * .7))
            return VoyageDistinction.Conqueror;
        if (facts.Turn <= SwiftLimit(facts) && facts.Kills < 8
            && facts.OwnedTowns <= Math.Max(1, facts.TotalTowns * .3)) return VoyageDistinction.Pathfinder;
        if (facts.Turn >= SwiftLimit(facts) * 1.5 && facts.Income >= EconomyTarget(facts)) return VoyageDistinction.Strategist;
        return VoyageDistinction.Voyager;
    }
    internal static string Build(BattleState battle, bool won)
    {
        int area = (int)(battle.Board.MapSize ?? (battle.Board.Tiles.Count < 500 ? MapSize.Lake
            : battle.Board.Tiles.Count < 900 ? MapSize.Bay : battle.Board.Tiles.Count < 1400 ? MapSize.Sea : MapSize.Ocean));
        var facts = new VoyageEvidence(battle.Round, battle.OpponentCount, area, battle.Statistics.EnemyShipsDestroyed,
            battle.Factions.Where(s => s != Side.Player).Select(battle.DirectShipKills).DefaultIfEmpty(0).Max(),
            battle.Villages.Count(v => v.Owner == Side.Player), battle.Villages.Count,
            battle.Statistics.StructuresBuilt, battle.Statistics.ShipsBuilt, battle.Income(Side.Player), battle.Statistics.SeaStrongholdsBuilt);
        var distinction = Evaluate(facts);
        string story = (won, distinction) switch
        {
            (true, VoyageDistinction.Admiral) => "Your guns outmatched every rival fleet. The elders acclaim a great battle admiral whose courage carried our people to victory.",
            (true, VoyageDistinction.Conqueror) => "Your strongholds guard the sea, and most settlements now share our faith. The elders honor a great conqueror and pioneer who brought new followers into our covenant.",
            (true, VoyageDistinction.Pathfinder) => "With modest means and swift resolve, you brought your people victory. The elders honor an efficient general and fearless explorer of the unknown.",
            (true, VoyageDistinction.Strategist) => "You nurtured a lasting economy and carefully prepared the expedition. The elders welcome a true strategist and entrust you with the next voyage.",
            (true, _) => "You preserved your people and carried their hope across an unfamiliar sea. The elders welcome you home with honor.",
            (false, VoyageDistinction.Admiral) => "Your flagship fell, but your fleet's fierce resistance will be remembered. The elders honor the admiral who stood against overwhelming seas.",
            (false, VoyageDistinction.Conqueror) => "Your voyage ended, yet its settlements and strongholds bear witness to a bold pioneer. The elders remember the followers you gathered.",
            (false, VoyageDistinction.Strategist) => "The expedition is lost, but your patient foundations will guide those who sail after you. The elders honor your care for your people.",
            (false, _) => "Your flagship has fallen, yet the unknown sea did not silence your courage. The elders remember your voyage and the lessons it brought home."
        };
        string acclaim = battle.Difficulty switch
        {
            AiDifficulty.Admiral => won ? "Against the most formidable captains, your feat will become a legend sung beneath our sacred shrine." : "You faced the fiercest captains; such resolve deserves a place in our chronicles.",
            AiDifficulty.Captain => won ? "You prevailed over seasoned rivals. Our sacred chronicles shall keep your name." : "Against seasoned rivals, your courage leaves a worthy example.",
            _ => won ? "May this first triumph guide your future voyages." : "May the next voyage carry the wisdom earned here."
        };
        return Language.Translate(story) + "\n\n" + Language.Translate(acclaim);
    }
}
