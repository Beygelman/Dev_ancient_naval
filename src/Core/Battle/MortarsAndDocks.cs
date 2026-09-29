using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.Vision;
using DevAncientNaval.Core.World;

namespace DevAncientNaval.Core.Battle;
public sealed partial class BattleState
{
    public const int MortarPrice=10;
    private readonly HashSet<GridPosition> _shoals=new();
    public IReadOnlyCollection<GridPosition> Shoals=>_shoals.ToArray();
    public IEnumerable<GridPosition> KnownShoals(Side side)=>_shoals.Where(p=>Vision.IsVisible(side,p));
    private void InitializeShoals(int seed)
    {
        var random=new Random(seed^0x5367);
        var water=Board.Tiles.Where(t=>t.Terrain!=TerrainType.Land && At(t.Position) is null).Select(t=>t.Position).OrderBy(_=>random.Next()).ToArray();
        foreach(var mother in Ships.Where(s=>s.IsMothership))
        {
            var near=water.Where(p=>BattleVision.InRadius(p,mother.Position,2)).ToArray();
            if(near.Length>0) _shoals.Add(near[0]);
        }
        foreach(var p in water) { if(_shoals.Count>=10) break; if(_shoals.All(q=>!BattleVision.InRadius(p,q,2))) _shoals.Add(p); }
    }
    private void GrantResources(Ship mother,int amount)
    {
        if(mother.Level>=4) return;
        mother.Resources+=amount;
        if(mother.Resources<mother.ResourcesRequired) return;
        int needed=mother.ResourcesRequired; double oldMax=mother.MaxHealth;
        mother.Resources-=needed; mother.Level++; mother.Health+=mother.MaxHealth-oldMax;
        mother.PendingUpgradeLevel=mother.Level;
        if(mother.Level==4) mother.Resources=0;
        RegisterShipIncome(mother);
    }
    public string? MortarBlockReason(Side requester,int id)
    {
        var error=ValidateActor(requester,id,out var ship); if(error is not null) return error;
        if(!ship!.IsMothership) return "Мортиру можно установить на Mothership.";
        if(ship.HasMortar) return "Мортира установлена.";
        if(!ship.HasRadar) return "Сначала установите радар.";
        if(Credits(requester)<MortarPrice) return "Для мортиры нужно 10 Thors.";
        return null;
    }
    public CommandResult BuyMortar(Side requester,int id)
    {
        var error=MortarBlockReason(requester,id); if(error is not null) return CommandResult.Rejected(error);
        _credits[(int)requester]-=MortarPrice; Find(id)!.HasMortar=true;
        return new(true,"Мортира установлена · огонь за пределами 3 клеток",CommandKind.Upgrade,id);
    }
    public int DockPrice(Side side)=>Creative&&side==Side.Player?0:Rules.Get(ShipClass.FishingDock).Price;
    public IReadOnlyCollection<GridPosition> DockCells(int id)
    {
        var ship=Find(id);
        if(ship is null||IsOver||ship.Owner!=ActiveSide||ship.Definition.CollectionRange==0||PendingUpgrade(ship.Owner) is not null) return Array.Empty<GridPosition>();
        return _shoals.Where(p=>Vision.IsVisible(ship.Owner,p)&&At(p) is null&&BattleVision.InRadius(p,ship.Position,ship.Definition.CollectionRange)).ToArray();
    }
    public CommandResult BuildDock(Side requester,int id,GridPosition cell)
    {
        var error=ValidateActor(requester,id,out _); if(error is not null) return CommandResult.Rejected(error);
        if(!DockCells(id).Contains(cell)) return CommandResult.Rejected("Выберите свободный косяк в радиусе сбора.");
        if(Credits(requester)<DockPrice(requester)) return CommandResult.Rejected("Недостаточно Thors для дока.");
        var mother=Mothership(requester)!;
        _credits[(int)requester]-=DockPrice(requester); _shoals.Remove(cell); _fish.Remove(cell);
        var dock=new Ship(_nextId++,requester,Rules.Get(ShipClass.FishingDock),cell);
        _ships.Add(dock); RegisterShipIncome(dock); GrantResources(mother,2); UpdateVision();
        return new(true,"Рыбный док: +2 ресурса и +1 доход",CommandKind.Dock,id,dock.Id,2);
    }
}
