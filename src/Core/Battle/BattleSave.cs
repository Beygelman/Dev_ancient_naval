using System.Text.Json;
using System.Text.Json.Serialization;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;

namespace DevAncientNaval.Core.Battle;
/// <summary>Maps the battle aggregate to the versioned storage contract.</summary>
public sealed partial class BattleState
{
    private static readonly JsonSerializerOptions SaveOptions = new()
    {
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };
    public FleetColor PlayerColor { get; private set; } = FleetColor.Blue;

    public void SetPlayerColor(FleetColor color)
    {
        if (!Enum.IsDefined(color))
            throw new ArgumentException("Invalid fleet color.");
        PlayerColor = color;
        AssignFactionColors(color);
<<<<<<< Updated upstream
        RethemeUnplayedWorld();
    }

    public BattleSave CaptureSnapshot() => PendingPresentation is null ? CreateSnapshot() : throw new InvalidOperationException("A projectile is still in flight.");
    public static string SerializeSnapshot(BattleSave snapshot) => JsonSerializer.Serialize(snapshot, SaveOptions);
    public string SaveJson() => SerializeSnapshot(CaptureSnapshot());
    private BattleSave CreateSnapshot(SavedBoard? board = null) => new()
    {
        Board = board ?? new(Board.Width, Board.Height, Board.Seed, Board.Tiles.Where(t => t.Terrain == TerrainType.Land).Select(t => t.Position).ToArray(), Board.Mesh?.Save(), Board.Kind),
        Statistics = Statistics,
        Rules = Rules,
        Ships = _ships.Select(SavedShip.From).ToArray(),
        Villages = _villages.Select(v => new SavedVillage(v.Id, v.Position, v.Owner, v.Level, v.Health, v.TurnsOwned, v.IsFortified, v.HasProduced, v.HasRepaired, v.HasAttacked, v.Name, v.HasPort)).ToArray(),
=======
    }

    public BattleSave CaptureSnapshot() => CreateSnapshot();
    public static string SerializeSnapshot(BattleSave snapshot) => JsonSerializer.Serialize(snapshot, SaveOptions);
    public string SaveJson() => SerializeSnapshot(CaptureSnapshot());
    private BattleSave CreateSnapshot() => new()
    {
        Board = new(Board.Width, Board.Height, Board.Seed, Board.Tiles.Where(t => t.Terrain == TerrainType.Land).Select(t => t.Position).ToArray(), Board.Mesh?.Save()),
        Rules = Rules,
        Ships = _ships.Select(SavedShip.From).ToArray(),
        Villages = _villages.Select(v => new SavedVillage(v.Id, v.Position, v.Owner, v.Level, v.Health, v.TurnsOwned, v.IsFortified, v.HasProduced, v.HasRepaired, v.HasAttacked)).ToArray(),
>>>>>>> Stashed changes
        Fish = _fish.ToArray(),
        Shoals = _shoals.ToArray(),
        Treasuries = _treasuries.ToArray(),
        Whirlpools = _whirlpools.Select(whirlpool => new Whirlpool(whirlpool.Position, whirlpool.Cells.ToArray())).ToArray(),
        CaptureWaits = _captureWaits.Select(e => new SavedWait(e.Key.Village, e.Key.Side, e.Key.Ship, e.Value.Position, e.Value.Since)).ToArray(),
        TreasuryWaits = _treasuryWaits.Select(e => new SavedWait(e.Key, Find(e.Value.ShipId)!.Owner, e.Value.ShipId, e.Value.Position, e.Value.Since)).ToArray(),
        PirateHomes = _pirateHomes.Select(e => new SavedHome(e.Key, e.Value)).ToArray(),
        Outcomes = _treasuryOutcomes.Select(e => new SavedOutcome(e.Key, e.Value)).ToArray(),
        Income = _incomeSources.ToArray(),
        Vision = Vision.Save(),
        Credits = _credits.ToArray(),
        EverProduced = _everProduced.ToArray(),
        NextId = _nextId,
        Round = Round,
        TurnSerial = TurnSerial,
        ActiveSide = ActiveSide,
        Winner = Winner,
        IsDraw = IsDraw,
        Creative = Creative,
<<<<<<< Updated upstream
        GodEye = GodEye,
        Difficulty = Difficulty,
        Color = PlayerColor,
        Factions = _factions.ToArray(),
        FactionNames = _factionNames.Select(e => new FactionIdentity(e.Key, e.Value)).ToArray(),
        FactionColors = _factionColors.Select(entry => new FactionColor(entry.Key, entry.Value)).ToArray(),
        PendingAwards = _pendingAwards.ToArray(),
        Encounters = _encounters.Values.ToArray(),
        FlagshipSightings = SaveFlagshipSightings(),
        PersonalTurnStarts = _personalTurnStarts.ToArray(),
        FlagshipKills = _flagshipKills.ToArray(),
=======
        Color = PlayerColor,
        Factions = _factions.ToArray(),
        FactionColors = _factionColors.Select(entry => new FactionColor(entry.Key, entry.Value)).ToArray(),
>>>>>>> Stashed changes
        EventSeed = _eventSeed,
        EventDraws = _eventDraws,
        LastReward = LastTreasuryReward
    };
    public static BattleState LoadJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        if (json.Length > 8_000_000)
            throw new ArgumentException("Save file is too large.");
        var snapshot = JsonSerializer.Deserialize<BattleSave>(json, SaveOptions) ?? throw new ArgumentException("Empty save file.");
        BattleSaveValidation.ValidateEnvelope(snapshot);
        var rules = snapshot.Rules;
        rules.Validate();
        var board = RestoreBoard(snapshot.Board);
        BattleSaveValidation.ValidateContents(snapshot, board, rules);
        // Construction establishes the aggregate's normal dependencies. Its temporary
        // fleet is replaced before any restored state is exposed to callers.
        var anchors = board.Tiles.Where(t => t.Terrain != TerrainType.Land && !board.IsNarrowPassage(t.Position)).Take(2).Select(t => t.Position).ToArray();
        if (anchors.Length != 2)
            throw new ArgumentException("Saved board has no fleet berths.");
        var battle = new BattleState(board, rules, new[] { (Side.Player, ShipClass.Mothership, anchors[0]), (Side.Enemy, ShipClass.Mothership, anchors[1]) }, Array.Empty<GridPosition>(), villageSpots: Array.Empty<GridPosition>());
        battle.RestoreEntities(snapshot);
        battle.RestoreProgress(snapshot);
        battle.Vision.Restore(snapshot.Vision, battle._factions);
        battle.UpdateVision();
        return battle;
    }

    private static GameBoard RestoreBoard(SavedBoard saved)
    {
        var mesh = saved.Mesh is null ? null : OrganicMesh.Restore(saved.Mesh);
        var land = saved.Land.ToHashSet();
<<<<<<< Updated upstream
        var board = new GameBoard(saved.Width, saved.Height, p => land.Contains(p) ? TerrainType.Land : TerrainType.Water, saved.Seed, mesh is null ? null : mesh.Faces.ContainsKey, mesh?.Boundary, mesh, saved.Kind);
=======
        var board = new GameBoard(saved.Width, saved.Height, p => land.Contains(p) ? TerrainType.Land : TerrainType.Water, saved.Seed, mesh is null ? null : mesh.Faces.ContainsKey, mesh?.Boundary, mesh);
>>>>>>> Stashed changes
        if (saved.Land.Any(p => !board.Contains(p)) || mesh is not null && mesh.Faces.Keys.Any(p => !board.Contains(p)))
            throw new ArgumentException("Saved terrain lies outside the board.");
        return board;
    }

    private void RestoreEntities(BattleSave saved)
    {
<<<<<<< Updated upstream
        _tradeNetworks.Clear();
        var previousShips = _ships.ToDictionary(s => s.Id);
        var previousVillages = _villages.ToDictionary(v => v.Id);
        _ships.Clear();
        _villages.Clear();
        _incomeSources.Clear();
        _fish.Clear();
        _shoals.Clear();
        foreach (var state in saved.Ships)
        {
            var ship = previousShips.TryGetValue(state.Id, out var current) && current.Owner == state.Owner && current.Definition.Class == state.Kind ? current : state.Restore(Rules);
            state.Apply(ship);
            _ships.Add(ship);
        }

        if (_ships.Any(s => s.Health > s.MaxHealth))
            throw new ArgumentException("Invalid saved fleet health.");
        foreach (var state in saved.Villages)
        {
            var village = previousVillages.TryGetValue(state.Id, out var current) ? current : new Village(state.Id, state.Position);
            village.Owner = state.Owner;
            village.Level = state.Level;
            village.Health = state.Health;
            village.TurnsOwned = state.TurnsOwned;
            village.IsFortified = state.Fortified;
            village.HasPort = state.Port;
            village.HasProduced = state.Produced;
            village.HasRepaired = state.Repaired;
            village.HasAttacked = state.Attacked;
            village.Name = state.Name;
            _villages.Add(village);
        }

=======
        _ships.Clear();
        _incomeSources.Clear();
        _fish.Clear();
        _shoals.Clear();
        _ships.AddRange(saved.Ships.Select(s => s.Restore(Rules)));
        if (_ships.Any(s => s.Health > s.MaxHealth))
            throw new ArgumentException("Invalid saved fleet health.");
        _villages.AddRange(saved.Villages.Select(v => new Village(v.Id, v.Position) { Owner = v.Owner, Level = v.Level, Health = v.Health, TurnsOwned = v.TurnsOwned, IsFortified = v.Fortified, HasProduced = v.Produced, HasRepaired = v.Repaired, HasAttacked = v.Attacked }));
>>>>>>> Stashed changes
        _fish.UnionWith(saved.Fish);
        _shoals.UnionWith(saved.Shoals);
        _treasuries.AddRange(saved.Treasuries);
        _whirlpools.AddRange(saved.Whirlpools);
        _forbidden.UnionWith(saved.Whirlpools.SelectMany(w => w.Cells));
        _incomeSources.AddRange(saved.Income);
        foreach (var wait in saved.CaptureWaits)
            _captureWaits.Add((wait.Target, wait.Side, wait.Ship), new(wait.Ship, wait.Position, wait.Since));
        foreach (var wait in saved.TreasuryWaits)
            _treasuryWaits.Add(wait.Target, new(wait.Ship, wait.Position, wait.Since));
        foreach (var home in saved.PirateHomes)
            _pirateHomes.Add(home.Ship, home.Position);
        foreach (var outcome in saved.Outcomes)
            _treasuryOutcomes.Add(outcome.Treasury, outcome.Reward);
    }

    private void RestoreProgress(BattleSave saved)
    {
<<<<<<< Updated upstream
        RestoreFlagshipSightings(saved.FlagshipSightings);
        Array.Clear(_personalTurnStarts);
        Array.Clear(_flagshipKills);
        Array.Copy(saved.PersonalTurnStarts, _personalTurnStarts, saved.PersonalTurnStarts.Length);
        Array.Copy(saved.FlagshipKills, _flagshipKills, saved.FlagshipKills.Length);
        _pendingAwards.Clear();
        _pendingAwards.AddRange(saved.PendingAwards);
        _encounters.Clear();
        foreach (var encounter in saved.Encounters)
            _encounters.Add(encounter.Side, encounter);
=======
>>>>>>> Stashed changes
        Array.Clear(_credits);
        Array.Copy(saved.Credits, _credits, saved.Credits.Length);
        Array.Copy(saved.EverProduced, _everProduced, saved.EverProduced.Length);
        _nextId = saved.NextId;
        Round = saved.Round;
        TurnSerial = saved.TurnSerial;
        ActiveSide = saved.ActiveSide;
        Winner = saved.Winner;
        IsDraw = saved.IsDraw;
        Creative = saved.Creative;
<<<<<<< Updated upstream
        GodEye = saved.GodEye;
        Difficulty = saved.Difficulty;
        Statistics = saved.Statistics;
=======
>>>>>>> Stashed changes
        PlayerColor = saved.Color;
        _factions.Clear();
        _factions.AddRange(saved.Factions.Length == 0 ? new[] { Side.Player, Side.Enemy } : saved.Factions);
        AssignFactionColors(PlayerColor);
<<<<<<< Updated upstream
        _factionNames.Clear();
        foreach (var entry in saved.FactionNames)
            _factionNames[entry.Side] = entry.Name;
        foreach (var entry in saved.FactionColors)
            _factionColors[entry.Side] = entry.Color;
        _worldNamesRestored = true;
        AssignWorldNames();
=======
        foreach (var entry in saved.FactionColors)
            _factionColors[entry.Side] = entry.Color;
>>>>>>> Stashed changes
        // The seeded sequence and draw count are part of the v1 contract: loading
        // must not reroll an undiscovered treasury or a pirate patrol choice.
        _eventSeed = saved.EventSeed;
        _eventRandom = new Random(saved.EventSeed);
        _eventDraws = saved.EventDraws;
        for (int i = 0; i < saved.EventDraws; i++)
            _eventRandom.Next();
        LastTreasuryReward = saved.LastReward;
    }
}
