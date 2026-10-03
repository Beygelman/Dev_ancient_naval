using System.Text.Json;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

internal static class Culture0202Checks
{
    internal static int Run(BattleRules rules)
    {
        int checks=0;
        void Check(bool valid,string message)
        {
            if (!valid) throw new Exception("0.20.2 culture: " + message);
            checks++;
        }
        bool Latin(string text) => text.All(c=>c is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or ' ' or '\'' or '-');
        var cultures=Enum.GetValues<FleetColor>().Select(CultureNamePools.For).ToArray();
        Check(cultures.SelectMany(c=>c.Captains).Distinct().Count()==cultures.Sum(c=>c.Captains.Count),"captain pools are unique across colors");
        Check(cultures.SelectMany(c=>c.Towns).Concat(WorldNames.PirateTowns).Distinct().Count()==cultures.Sum(c=>c.Towns.Count)+WorldNames.PirateTowns.Count,
            "town pools are unique across colors and pirate bays");
        foreach (var color in Enum.GetValues<FleetColor>())
        {
            var culture=CultureNamePools.For(color);
            Check(culture.Captains.Count>=24 && culture.Towns.Count>=32,"expanded pools for "+color);
            Check(culture.Captains.All(Latin) && culture.Towns.All(Latin),"all names use Latin letters for "+color);
            Check(WorldNames.CaptainsFor(color).SequenceEqual(culture.Captains) && WorldNames.TownsFor(color).SequenceEqual(culture.Towns),"public culture lookup for "+color);
        }
        Check(WorldNames.PirateTowns.All(Latin),"pirate names use Latin letters");
        var anchors=new[] {new GridPosition(3,3),new GridPosition(36,3),new GridPosition(36,36),new GridPosition(3,36),new GridPosition(20,20)};
        var sites=anchors.SelectMany(p=>new[] {new GridPosition(p.X+2,p.Y+2),new GridPosition(p.X+3,p.Y+2)}).ToArray();
        var land=sites.ToHashSet();
        var board=new GameBoard(42,42,p=>land.Contains(p)?TerrainType.Land:TerrainType.Water,1729);
        BattleState Create(BattleRules active)=>new(board,active,anchors.Select((p,i)=>(BattleState.PlayableSides[i],ShipClass.Mothership,p)),
            Array.Empty<GridPosition>(),villageSpots:sites,generatedSettlements:true);
        var battle=Create(rules);
        foreach (var color in Enum.GetValues<FleetColor>())
        {
            var oldPlayerNames=battle.Villages.Where(v=>v.Owner!=Side.Pirates && Nearest(battle,v)==Side.Player).Select(v=>v.Name).ToArray();
            var oldColor=battle.PlayerColor;
            battle.SetPlayerColor(color);
            foreach (var faction in battle.Factions.Where(s=>s!=Side.Player))
            {
                string captain=battle.FactionName(faction)["Captain ".Length..];
                Check(WorldNames.CaptainsFor(battle.ColorFor(faction)).Contains(captain),"captain follows faction color "+color+"/"+faction);
            }
            foreach (var village in battle.Villages)
            {
                var pool=village.Owner==Side.Pirates?WorldNames.PirateTowns:WorldNames.TownsFor(battle.ColorFor(Nearest(battle,village)));
                Check(pool.Contains(village.Name),"town follows nearest nation or pirate theme "+color);
            }
            Check(battle.Villages.Select(v=>v.Name).Distinct().Count()==battle.Villages.Count,"all generated town names unique "+color);
            if (oldColor!=color)
                Check(!oldPlayerNames.SequenceEqual(battle.Villages.Where(v=>v.Owner!=Side.Pirates && Nearest(battle,v)==Side.Player).Select(v=>v.Name)),
                    "fresh player color change rethemes local towns "+color);
            string saved=battle.SaveJson();
            var resumed=BattleState.LoadJson(saved);
            Check(resumed.SaveJson()==saved,"Continue preserves exact culture names "+color);
            var restoredNames=resumed.Villages.Select(v=>v.Name).ToArray();
            resumed.SetPlayerColor((FleetColor)(((int)color+1)%6));
            Check(restoredNames.SequenceEqual(resumed.Villages.Select(v=>v.Name)),"loaded world identities never retheme "+color);
        }
        Check(battle.Villages.Count(v=>v.Owner==Side.Pirates && v.Level==3 && v.IsFortified)>=2,"generated worlds guarantee two named fortified pirate bays");
        var oldJson=System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(rules))!.AsObject();
        oldJson.Remove("ThemedWorldNames");
        var oldRules=BattleRules.FromJson(oldJson.ToJsonString());
        var historical=Create(oldRules);
        var names=historical.Villages.Select(v=>v.Name).ToArray();
        var captains=historical.Factions.Where(s=>s!=Side.Player).Select(historical.FactionName).ToArray();
        Check(names.All(WorldNames.LegacyTowns.Contains) && captains.All(n=>WorldNames.LegacyCaptains.Contains(n["Captain ".Length..])),
            "missing old naming rule uses historical pools");
        historical.SetPlayerColor(FleetColor.Red);
        Check(names.SequenceEqual(historical.Villages.Select(v=>v.Name)) && captains.SequenceEqual(historical.Factions.Where(s=>s!=Side.Player).Select(historical.FactionName)),
            "changing old voyage colors cannot rewrite old identities");
        string legacySave=historical.SaveJson();
        Check(BattleState.LoadJson(legacySave).SaveJson()==legacySave,"old captain and settlement names survive exact Continue");
        // Hand-authored defeated fixtures can contain blank names and no surviving
        // flagships; loading these must choose a safe cosmetic fallback.
        var fixture=historical.CaptureSnapshot();
        fixture.Ships=Array.Empty<SavedShip>(); fixture.Winner=null; fixture.IsDraw=true;
        fixture.Income=fixture.Income.Where(i=>i.BoundShipId is null).ToArray();
        fixture.Rules=rules;
        fixture.Villages=fixture.Villages.Select(v=>v with {Name=""}).ToArray();
        var defeated=BattleState.LoadJson(BattleState.SerializeSnapshot(fixture));
        Check(defeated.Villages.All(v=>!string.IsNullOrWhiteSpace(v.Name)),"unnamed defeated fixtures have a safe culture fallback without a flagship");
        return checks;
    }
    private static Side Nearest(BattleState battle,Village village)=>battle.Ships.Where(s=>s.IsMothership)
        .OrderBy(s=>battle.Board.Distance(s.Position,village.Position)).ThenBy(s=>s.Id).First().Owner;
}
