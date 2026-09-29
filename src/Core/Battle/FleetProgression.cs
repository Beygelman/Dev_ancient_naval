using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using DevAncientNaval.Core.Vision;

namespace DevAncientNaval.Core.Battle;

public sealed partial class BattleState
{
    public const int CollectionPrice = 2;
    private readonly HashSet<GridPosition> _fish = new();
    public IReadOnlyCollection<GridPosition> FishSpots => _fish.ToArray();
    public IEnumerable<GridPosition> KnownFish(Side side) => _fish.Where(p => Vision.IsVisible(side,p));
    public Ship? Mothership(Side side) => OwnShips(side).FirstOrDefault(s => s.IsMothership);
    public Ship? PendingUpgrade(Side side) => OwnShips(side).FirstOrDefault(s => s.PendingUpgradeLevel > 0);

    private void InitializeFishing(IEnumerable<GridPosition>? supplied, int seed)
    {
        if(supplied is not null)
        {
            foreach(var cell in supplied)
            {
                if(!Board.Contains(cell) || Board.GetTile(cell).Terrain==TerrainType.Land) throw new ArgumentException("Fish must be at sea.");
                _fish.Add(cell);
            }
            InitializeShoals(seed);
            return;
        }
        InitializeShoals(seed);
        var random=new Random(seed);
        var candidates=Board.Tiles.Where(t=>t.Terrain!=TerrainType.Land && At(t.Position) is null).Select(t=>t.Position).ToList();
        for(int i=candidates.Count-1;i>0;i--) { int j=random.Next(i+1); (candidates[i],candidates[j])=(candidates[j],candidates[i]); }
        foreach(var cell in candidates.Where(p=>!_shoals.Contains(p)).Take(Math.Min(16,candidates.Count))) _fish.Add(cell);
        // Every starting fleet can demonstrate collection without relying on a lucky seed.
        foreach(var mother in Ships.Where(s=>s.IsMothership))
            foreach(var cell in candidates.Where(p=>!_shoals.Contains(p)&&BattleVision.InRadius(p,mother.Position,mother.Definition.CollectionRange)).Take(2)) _fish.Add(cell);
    }

    public string? RadarBlockReason(Side requester,int id)
    {
        var error=ValidateActor(requester,id,out var ship);
        if(error is not null) return error;
        if(ship!.Definition.Class is not (ShipClass.Mothership or ShipClass.Kolonel)) return "У этого класса нет радара.";
        if(ship.HasRadar) return "Радар уже установлен.";
        if(Credits(requester)<ship.Definition.RadarPrice) return "Недостаточно Thors.";
        return null;
    }
    public CommandResult BuyRadar(Side requester,int id)
    {
        var error=RadarBlockReason(requester,id);
        if(error is not null) return CommandResult.Rejected(error);
        var ship=Find(id)!; _credits[(int)requester]-=ship.Definition.RadarPrice; ship.HasRadar=true; UpdateVision();
        return new(true,"Радар установлен.",CommandKind.Radar,id);
    }

    public IReadOnlyCollection<GridPosition> CollectionCells(int id)
    {
        var ship=Find(id);
        if(ship is null || ship.Definition.CollectionRange<=0 || IsOver || ship.Owner!=ActiveSide ||
            PendingUpgrade(ship.Owner) is not null || Mothership(ship.Owner) is not { Level:<5 }) return Array.Empty<GridPosition>();
        return _fish.Where(p=>Vision.IsVisible(ship.Owner,p) && BattleVision.InRadius(p,ship.Position,ship.Definition.CollectionRange)).ToArray();
    }
    public CommandResult Collect(Side requester,int id,GridPosition cell)
    {
        var error=ValidateActor(requester,id,out var ship);
        if(error is not null) return CommandResult.Rejected(error);
        if(!CollectionCells(id).Contains(cell)) return CommandResult.Rejected("Рыба должна быть видна и находиться в радиусе сбора.");
        if(Credits(requester)<CollectionCost(requester)) return CommandResult.Rejected("Для сбора нужно 2 Thors.");
        var mother=Mothership(requester)!;
        _credits[(int)requester]-=CollectionCost(requester); _fish.Remove(cell);
        int oldLevel=mother.Level; GrantResources(mother,1);
        bool advanced=mother.Level>oldLevel;
        return new(true,advanced?$"Mothership: уровень {mother.Level}!":$"+1 ресурс Mothership · −{CollectionCost(requester)} Thors",CommandKind.Collect,id,mother.Id,1);
    }
    public IReadOnlyList<UpgradeChoice> UpgradeOptions(int motherId) => Find(motherId)?.PendingUpgradeLevel switch
    {
        2 => new[] { UpgradeChoice.Income,UpgradeChoice.Mobility },
        3 => new[] { UpgradeChoice.SecondAttack,UpgradeChoice.Balloon },
        4 => new[] { UpgradeChoice.Fortification,UpgradeChoice.Shipwright },
        _ => Array.Empty<UpgradeChoice>()
    };
    public CommandResult ChooseUpgrade(Side requester,int id,UpgradeChoice choice)
    {
        var mother=Find(id);
        if(IsOver || requester!=ActiveSide || mother?.Owner!=requester || !UpgradeOptions(id).Contains(choice))
            return CommandResult.Rejected("Это улучшение сейчас недоступно.");
        switch(choice)
        {
            case UpgradeChoice.Fortification: mother.FortificationUpgrade=true; mother.Health+=5; break;
            case UpgradeChoice.Shipwright: mother.ShipwrightUpgrade=true; break;
            case UpgradeChoice.Income: mother.IncomeUpgrade=true; RegisterShipIncome(mother); break;
            case UpgradeChoice.Mobility: mother.MobilityUpgrade=true; break;
            case UpgradeChoice.SecondAttack: mother.SecondAttackUpgrade=true; break;
            case UpgradeChoice.Balloon:
                _ships.Add(new Ship(_nextId++,requester,Rules.Get(ShipClass.Balloon),mother.Position));
                break;
        }
        mother.PendingUpgradeLevel=0; UpdateVision();
        return new(true,"Улучшение установлено.",CommandKind.Upgrade,id);
    }

    private static int FlightCost(GridPosition from,GridPosition to)
    {
        long dx=from.X-to.X,dy=from.Y-to.Y;
        return (int)Math.Ceiling(Math.Sqrt(.25+dx*dx+dy*dy)-.5)*10;
    }
    private CommandResult Fly(Ship ship,GridPosition destination)
    {
        if(!ship.CanMove || !Board.Contains(destination) || destination==ship.Position || FlightCost(ship.Position,destination)>ship.MovementRemainingUnits) return CommandResult.Rejected("Выберите другую клетку для перелёта.");
        int cost=FlightCost(ship.Position,destination); var start=ship.Position; int steps=Math.Max(Math.Abs(destination.X-start.X),Math.Abs(destination.Y-start.Y));
        var path=new List<GridPosition> { start }; var frames=new List<MovementFrame> { MovementFrame(ship) };
        for(int i=1;i<=steps;i++)
        {
            var cell=new GridPosition(Ship.Whole(start.X+(destination.X-start.X)*i/(double)steps),Ship.Whole(start.Y+(destination.Y-start.Y)*i/(double)steps));
            ship.Position=cell; UpdateVision(); path.Add(cell); frames.Add(MovementFrame(ship));
        }
        ship.HasMoved=true; ship.MovementSpentUnits+=cost;
        return new(true,"Воздушный шар: перелёт завершён.",CommandKind.Move,ship.Id,Path:path,Movement:frames);
    }
}
