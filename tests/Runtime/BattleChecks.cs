using System;
using System.Linq;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using DevAncientNaval.Core.Vision;
using DevAncientNaval.Presentation;
using DevAncientNaval.Presentation.UI;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Tests.Runtime;
public partial class BattleChecks : Node
{
    public Main Game { get; set; } = null !;

    private int _checks;
<<<<<<< Updated upstream
    private void Check(bool ok, string name)
    {
        if (!ok)
            throw new Exception(name);
        _checks++;
    }

=======
    private void Check(bool ok,string name) { if(!ok) throw new Exception(name); _checks++; }
>>>>>>> Stashed changes
    private async Task Frame()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        // Real input must wait for the scroll's deliberately disabled hit areas.
        if (Descendants(Game.Hud).OfType<RadialPapyrus>().Any(s => s.IsVisibleInTree() && s.IsProcessing()))
            await ToSignal(GetTree().CreateTimer(.30), SceneTreeTimer.SignalName.Timeout);
    }
<<<<<<< Updated upstream

=======
>>>>>>> Stashed changes
    private static System.Collections.Generic.IEnumerable<Node> Descendants(Node root)
    {
        yield return root;
        foreach (var child in root.GetChildren())
            foreach (var item in Descendants(child))
                yield return item;
    }

    private Button Button(string name) => Descendants(Game.Hud).OfType<Button>().Single(b => b.Name == name);
    private Vector2 ClickAt(Button b) => b is SectorButton s ? s.GetGlobalTransform() * s.IconCenter : b.GetGlobalRect().GetCenter();
    private void Click(Button b)
    {
        var p = ClickAt(b);
        GetViewport().PushInput(new InputEventMouseButton { Position = p, GlobalPosition = p, ButtonIndex = MouseButton.Left, Pressed = true }, true);
        GetViewport().PushInput(new InputEventMouseButton { Position = p, GlobalPosition = p, ButtonIndex = MouseButton.Left, Pressed = false }, true);
    }

    private void Tap(Vector2 p)
    {
        GetViewport().PushInput(new InputEventScreenTouch { Index = 0, Position = p, Pressed = true }, true);
        GetViewport().PushInput(new InputEventScreenTouch { Index = 0, Position = p, Pressed = false }, true);
    }

    private Vector2 Screen(GridPosition p) => GetViewport().GetCanvasTransform() * Game.BoardView.Projection.GridToWorld(p);
    private async Task Capture(string suffix)
    {
        var arg = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--capture="));
        if (arg is null || DisplayServer.GetName() == "headless")
            return;
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Check(GetViewport().GetTexture().GetImage().SavePng(arg[10..].Replace(".png", suffix + ".png")) == Error.Ok, "Screenshot " + suffix);
    }

    private async Task BuyResource(GridPosition cell, bool touch = false)
    {
        Game.SelectCell(cell);
        await Frame();
        Check(Button("TileResource").IsVisibleInTree(), $"Tile opens resource sector: cell {cell}, selected {Game.SelectedShipId}, level {Game.Battle.Mothership(Side.Player)!.Level}, pending {Game.Battle.PendingUpgrade(Side.Player)?.Level}, sites {string.Join(';', Game.BoardView.Collection)}");
        if (touch)
            Tap(ClickAt(Button("TileResource")));
        else
            Click(Button("TileResource"));
        await Game.CurrentOrder;
        await Frame();
    }

    public override async void _Ready()
    {
        try
        {
            await Frame();
            await Run();
            Game.Restart();
            var anchor = Game.Battle.Mothership(Side.Player)!.Position;
            Game.SelectCell(anchor);
            await Game.BuyRadar();
            Game.Hud.ShowMessage("");
            Game.MapCamera.ZoomAt(Screen(anchor), 1.8f);
            await Frame();
            await Capture("");
            foreach (var tile in Game.Battle.Board.Tiles)
                Game.Battle.Vision.RevealCombat(Side.Player, tile.Position);
            Game.Battle.Vision.Recompute(Game.Battle.Ships, 1);
            Game.CancelOrder();
            Game.MapCamera.FitBoard();
            Game.Refresh();
            await Capture("-archipelago");
<<<<<<< Updated upstream
            GD.Print($"PASS: {_checks} naval runtime checks (legacy-save controls, scroll input, upgrades, stealth animation, hexagon).");
            GetTree().Quit();
        }
        catch (Exception e)
        {
            GD.PushError(e.ToString());
            GetTree().Quit(1);
=======
            GD.Print($"PASS: {_checks} naval runtime checks (legacy-save controls, scroll input, upgrades, stealth animation, hexagon)."); GetTree().Quit();
>>>>>>> Stashed changes
        }
    }

    private async Task Run()
    {
        // Keep released-save rules under regression while the new economy is
        // checked separately by the faction/economy and current-menu suites.
<<<<<<< Updated upstream
        var defaults = BattleRules.FromJson(FileAccess.GetFileAsString("res://tests/CoreChecks/Fixtures/balance-0.14.1.json"));
        Game.LoadScenario(SkirmishSetup.Create(PrototypeBoard.Create(731), defaults));
        var rules = new BattleRules
        {
            StartingCredits = 80,
            IncomePerMothership = defaults.IncomePerMothership,
            RepairAmount = defaults.RepairAmount,
            FleetLimit = defaults.FleetLimit,
            Ships = defaults.Ships
        };
        Click(Button("Menu"));
        await Frame();
        Check(Game.Hud.MenuVisible && Button("NewGame").IsVisibleInTree() && Button("ExitGame").IsVisibleInTree(), "Menu exposes new game and exit");
        var beforeSelection = Game.SelectedShipId;
        Tap(new Vector2(40, 180));
        Check(Game.SelectedShipId == beforeSelection && Game.Hud.MenuVisible, "Menu blocks map input");
        Click(Button("Creative"));
        Check(Game.Battle.Creative && Game.Battle.BuildPrice(Side.Player, ShipClass.Kolonel) == 0, "Creative toggle");
        await Capture("-menu");
        Click(Button("Creative"));
        Click(Button("CloseMenu"));
        Check(!Game.Hud.MenuVisible && !Game.Battle.Creative, "Menu closes and creative off");
        Check(Game.Battle.Ships.Count == 8 && Game.Battle.Credits(Side.Player) == 5 && Game.Battle.Income(Side.Player) == 4, "Starting fleet and economy");
        Check(!Descendants(Game.Hud).OfType<Button>().Any(b => new[] { "ActionMove", "ActionAttack", "ActionClose", "ActionCollect", "ActionDock" }.Contains(b.Name.ToString())), "No obsolete ship actions");
        var garrison = Game.Battle.OwnShips(Side.Player).Single(s => s.Definition.Class == ShipClass.Garrison);
        Game.SelectCell(garrison.Position);
        await Frame();
        var before = Game.Hud.MenuPosition;
        Game.MapCamera.Pan(new(40, 12));
        await Frame();
        Check(Game.Hud.MenuPosition.DistanceTo(before) > 5, "Ship actions follow the hull while the camera moves");
        var resources = Game.Battle.CollectionCells(Side.Player).Concat(Game.Battle.DockCells(Side.Player)).ToHashSet();
        var destination = Game.Battle.Reachable(garrison.Id).First(p => p.Value == 10 && p.Key != garrison.Position && !resources.Contains(p.Key) &&
            !Descendants(Game.Hud).OfType<SectorButton>().Any(s => s.IsVisibleInTree() && s._HasPoint(s.GetGlobalTransform().AffineInverse() * Screen(p.Key)))).Key;
        Tap(Screen(destination));
        var order = Game.CurrentOrder;
        Check(Game.Busy, "Movement starts on tile click");
        await order;
        Check(garrison.Position == destination && !Game.Busy, "Direct movement complete");
        Game.SelectCell(new(0, 0));
        Check(Game.SelectedShipId is null, "Pentagon exterior deselects");
        Game.FastChecks = true;
        var mother = Game.Battle.Mothership(Side.Player)!;
        Game.SelectCell(mother.Position);
        await ToSignal(GetTree().CreateTimer(.3), SceneTreeTimer.SignalName.Timeout);
        await Frame();
        var sectors = Descendants(Game.Hud).OfType<SectorButton>().Where(s => s.IsVisibleInTree()).ToArray();
        Check(sectors.Length == 5 && sectors.All(s => Mathf.IsEqualApprox(s.Sweep, SectorButton.SectorStep) && s._HasPoint(s.IconCenter) && !s._HasPoint(SectorButton.Center)), "Five readable papyrus sectors, including information, leave the map center transparent");
        int money = Game.Battle.Credits(Side.Player);
        Click(Button("ActionRadar"));
        await Game.CurrentOrder;
        Check(mother.HasRadar && Game.Battle.Credits(Side.Player) == money - 2, "Radar purchase");
        Click(Button("ActionBuild"));
        await Frame();
        Check(!Button("BuildFishing").Disabled && Button("BuildInvader").Disabled && Button("BuildKolonel").Disabled && Button("BuildTogus").Disabled, "Starting yard unlocks only fishing and garrison");
        await Capture("-shipyard");
        Click(Button("BuildFishing"));
        var spawn = Game.Battle.SpawnCells(mother.Id).First();
        Game.SelectCell(spawn);
        await Game.CurrentOrder;
        Check(Game.Battle.At(spawn)!.Definition.Class == ShipClass.Fishing && !mother.CanMove, "Build works and locks mother movement");
        var fish = (
            from y in Enumerable.Range(3, 5)from x in Enumerable.Range(3, 5)let p = new GridPosition(x, y)
            where p != new GridPosition(5, 5) && BattleVision.InRadius(p, new(5, 5), 2)select p).Take(14).ToArray();
        Game.LoadScenario(new BattleState(new GameBoard(20, 20, p => p == new GridPosition(7, 7) ? TerrainType.Land : TerrainType.Water), rules, new[] { (Side.Player, ShipClass.Mothership, new GridPosition(5, 5)), (Side.Enemy, ShipClass.Mothership, new GridPosition(18, 18)), (Side.Player, ShipClass.Fishing, new GridPosition(8, 8)) }, fish));
        mother = Game.Battle.Find(1)!;
        Game.SelectCell(mother.Position);
        await Frame();
        Check(Game.BoardView.Collection.Count == 14 && Game.BoardView.DockSites.Count == 0 && Game.BoardView.AttackArea.Count == 0, "Available fish icons, no attack fill or level-one docks");
        Game.SelectCell(fish[0]);
        await Frame();
        Check(Game.Battle.Credits(Side.Player) == 80 && mother.Resources == 0, "Selecting fish does not spend currency");
        Game.CancelOrder();
        await Frame();
        Check(!Button("TileResource").IsVisibleInTree() && Game.Battle.Credits(Side.Player) == 80, "Deselect dismisses purchase without spending");
        Check(Game.SelectedShipId is null, "Resource collection starts without a selected ship");
        Game.SelectCell(fish[0]);
        await Frame();
        Check(Button("TileResource").IsVisibleInTree(), "Available resource opens without selecting its nearby collector");
        await Capture("-resource");
        Tap(ClickAt(Button("TileResource")));
        await Game.CurrentOrder;
        await Frame();
        Check(mother.Resources == 1 && Game.Battle.Credits(Side.Player) == 78 && !Button("TileResource").IsVisibleInTree(), "Touch buys resource once and closes sector");
        await BuyResource(fish[1]);
        Check(Game.Hud.UpgradeVisible && mother.Level == 2, "Level two automatic modal");
        await Capture("-level2");
        var position = mother.Position;
        Tap(Screen(new(5, 7)));
        Check(mother.Position == position, "Modal blocks movement");
        Click(Button("UpgradeMobility"));
        await Game.CurrentOrder;
        Check(!Game.Hud.UpgradeVisible && mother.MovementAllowance == 2 && Game.BoardView.DockSites.Count > 0, "Level two unlocks docks");
        foreach (var cell in fish.Skip(2).Take(3))
            await BuyResource(cell);
        Check(mother.Level == 3 && Game.Hud.UpgradeVisible, "Level three");
        Click(Button("UpgradeRestoration"));
        await Game.CurrentOrder;
        Check(mother.MaxHealth == 35, "Level three reinforced hull adds five HP");
        foreach (var cell in fish.Skip(5).Take(4))
            await BuyResource(cell);
        Check(mother.Level == 4 && Game.Hud.UpgradeVisible, "Level four");
        Click(Button("UpgradeBalloon"));
        await Game.CurrentOrder;
        Check(mother.MaxHealth == 40 && Game.Battle.BuildBlockReason(Side.Player, 1, ShipClass.Kolonel)is null && Game.Battle.MortarBlockReason(Side.Player, 1)is not null, "Level four heavy unlock; mortar locked");
        var balloon = Game.Battle.OwnShips(Side.Player).Single(s => s.IsAirborne);
        Game.SelectAtScreen(GetViewport().GetCanvasTransform() * (Game.BoardView.Projection.GridToWorld(balloon.Position) + new Vector2(0, -62)));
        Check(Game.SelectedShipId == balloon.Id, "Balloon selected above ship");
        Game.SelectCell(new(7, 7));
        await Game.CurrentOrder;
        Check(balloon.Position == new GridPosition(7, 7) && balloon.MovementRemainingUnits == 20, "Balloon crosses land with one-point diagonals");
        await Capture("-balloon");
        Game.SelectCell(mother.Position);
        foreach (var cell in fish.Skip(9))
            await BuyResource(cell);
        Check(mother.Level == 5 && mother.MaxHealth == 45 && Game.Hud.UpgradeVisible && mother.ResourcesRequired == 0, "Level five presents the final upgrade choice");
        Click(Button("UpgradeShipwright"));
        await Game.CurrentOrder;
        Check(!Game.Hud.UpgradeVisible && Game.Battle.BuildPrice(Side.Player, ShipClass.Kolonel) == 6, "Level five shipbuilding discount applies");
        Game.SelectCell(mother.Position);
        await Frame();
        Click(Button("ActionRadar"));
        await Game.CurrentOrder;
        await Frame();
        money = Game.Battle.Credits(Side.Player);
        Check(!Button("ActionMortar").Disabled, "Level five radar enables mortar");
        Click(Button("ActionMortar"));
        await Game.CurrentOrder;
        Check(mother.HasMortar && Game.Battle.Credits(Side.Player) == money - 10 && mother.CurrentMortarDamage == 8, "Mortar cost and damage");
        var site = Game.Battle.DockCells(1).First();
        await BuyResource(site);
        Check(Game.Battle.At(site)is { IsStructure: true } && Game.Battle.Income(Side.Player) == 13, "Tile dock purchase and income at level five");
        await Capture("-dock");
        Click(Button("ActionBuild"));
        await Frame();
        Check(!Button("BuildTogus").Disabled, "Togus unlocked at five");
        Click(Button("BuildTogus"));
        spawn = Game.Battle.SpawnCells(1).First();
        Game.SelectCell(spawn);
        await Game.CurrentOrder;
        Check(Game.Battle.At(spawn)is { HasMortar: true, HasRadar: true }, "Built Togus includes artillery and radar");
        await Frame();
        await Capture("-togus");
        Game.LoadScenario(new BattleState(new GameBoard(20, 20, _ => TerrainType.Water), rules, new[] { (Side.Player, ShipClass.Mothership, new GridPosition(0, 0)), (Side.Enemy, ShipClass.Mothership, new GridPosition(19, 19)), (Side.Player, ShipClass.Kolonel, new GridPosition(7, 7)), (Side.Enemy, ShipClass.Kolonel, new GridPosition(9, 7)) }, Array.Empty<GridPosition>()));
        Game.FastChecks = false;
        Game.SelectCell(new(7, 7));
        Check(Game.BoardView.Targets.Contains(new GridPosition(9, 7)), "Selectable targets remain outlined");
        Game.SelectCell(new(9, 7));
        if (Game.Battle.Rules.DoubleSalvo)
        {
            Check(Game.Hud.SalvoChoiceVisible, "Two-shot target opens the choice parchment");
            await Frame();
            Click(Button("SingleShot"));
        }
        order = Game.CurrentOrder;
        Check(!Game.Fleet.TurningForShot && Game.Fleet.ProjectilePosition is null, "Camera leads the broadside and projectile");
        for (int frame = 0; frame < 120 && !Game.Fleet.TurningForShot && !order.IsCompleted; frame++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Check(Game.Fleet.TurningForShot && Game.Fleet.ProjectilePosition is null, "Broadside begins after the camera arrives and before any projectile");
        await WaitForProjectile(order);
        Check(Game.Fleet.ProjectilePosition is not null && !Game.Fleet.TurningForShot, "Cannon projectile follows the completed broadside turn");
        await order;
        Check(Math.Abs(Game.Fleet.DeckAngle(3)) > .01, "Completed cannon salvo preserves its broadside heading");
        Check(Game.Battle.Find(3)!.Health < 15 && Game.Battle.Find(4)!.Health < 15, "Counterattack");
        Game.Battle.EndTurn(Side.Player);
        Game.Battle.EndTurn(Side.Enemy);
        Game.Refresh();
        await Frame();
        double hp = Game.Battle.Find(3)!.Health;
        Click(Button("ActionRepair"));
        await Game.CurrentOrder;
        Check(Game.Battle.Find(3)!.Health > hp, "Repair sector works");
        Game.LoadScenario(new BattleState(new GameBoard(20, 20, _ => TerrainType.Water), rules, new[] { (Side.Player, ShipClass.Mothership, new GridPosition(0, 0)), (Side.Enemy, ShipClass.Mothership, new GridPosition(19, 19)), (Side.Player, ShipClass.Togus, new GridPosition(5, 5)), (Side.Enemy, ShipClass.Kolonel, new GridPosition(10, 5)) }, Array.Empty<GridPosition>()));
        Game.SelectCell(new(5, 5));
        Check(Game.Battle.FindObserved(Side.Player, 4)is null && Game.BoardView.Targets.Contains(new GridPosition(10, 5)), "Radar target anonymous before shot");
        Game.SelectCell(new(10, 5));
        order = Game.CurrentOrder;
        await WaitForProjectile(order);
        Check(Game.Fleet.ProjectilePosition is not null && !Game.Fleet.AnimatedShipIds.Contains(4) && Game.Fleet.CombatFeedback.Length == 0, "Animation never inserts hidden target or damage text");
        await Capture("-mortar");
        await order;
        Check(Game.Battle.Find(4)!.Health == 7 && Game.Battle.FindObserved(Side.Player, 4)is null && !Game.Hud.MessageText.Contains("8") && !Game.Hud.MessageText.Contains("Kolonel"), "Hit applies without revealing class or damage");
        Game.FastChecks = true;
        await Game.EndPlayerTurn();
        Check(Game.Battle.ActiveSide == Side.Player && !Game.Busy, "Opponent returns control");
        await VillagesAndBalloon(rules);
        await Release014Battle(rules);
        int seed = Game.Battle.Board.Seed;
        Game.Restart();
        Check(Game.Battle.Board.Seed != seed && Game.Battle.Board.Boundary.Count == 6 && Game.Battle.Ships.Count == 8, "Restart randomizes hexagon and resets game");
    }

    private async Task WaitForProjectile(Task order)
    {
        for (int frame = 0; frame < 240; frame++)
        {
            if (Game.Fleet.ProjectilePosition is not null && !Game.Fleet.TurningForShot)
                return;
            if (order.IsCompleted)
                throw new Exception("The attack completed without an observable projectile phase.");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        throw new Exception("The attack did not enter its projectile phase.");
=======
        var defaults=BattleRules.FromJson(FileAccess.GetFileAsString("res://tests/CoreChecks/Fixtures/balance-0.14.1.json"));
        Game.LoadScenario(SkirmishSetup.Create(Game.Battle.Board, defaults));
        var rules=new BattleRules { StartingCredits=80,IncomePerMothership=defaults.IncomePerMothership,RepairAmount=defaults.RepairAmount,FleetLimit=defaults.FleetLimit,Ships=defaults.Ships };
        Click(Button("Menu")); await Frame();
        Check(Game.Hud.MenuVisible&&Button("NewGame").IsVisibleInTree()&&Button("ExitGame").IsVisibleInTree(),"Menu exposes new game and exit");
        var beforeSelection=Game.SelectedShipId; Tap(new Vector2(40,180));
        Check(Game.SelectedShipId==beforeSelection&&Game.Hud.MenuVisible,"Menu blocks map input");
        Click(Button("Creative")); Check(Game.Battle.Creative&&Game.Battle.BuildPrice(Side.Player,ShipClass.Kolonel)==0,"Creative toggle");
        await Capture("-menu"); Click(Button("Creative")); Click(Button("CloseMenu"));
        Check(!Game.Hud.MenuVisible&&!Game.Battle.Creative,"Menu closes and creative off");
        Check(Game.Battle.Ships.Count==8&&Game.Battle.Credits(Side.Player)==5&&Game.Battle.Income(Side.Player)==4,"Starting fleet and economy");
        Check(!Descendants(Game.Hud).OfType<Button>().Any(b=>new[] {"ActionMove","ActionAttack","ActionClose","ActionCollect","ActionDock"}.Contains(b.Name.ToString())),"No obsolete ship actions");
        var garrison=Game.Battle.OwnShips(Side.Player).Single(s=>s.Definition.Class==ShipClass.Garrison);
        Game.SelectCell(garrison.Position); await Frame();
        var before=Game.Hud.MenuPosition; Game.MapCamera.Pan(new(40,12)); await Frame();
        Check(Game.Hud.MenuPosition.DistanceTo(before)>15,"Ship sectors follow camera");
        var destination=Game.Battle.Reachable(garrison.Id).First(p=>p.Value==10 && p.Key!=garrison.Position).Key;
        Tap(Screen(destination)); var order=Game.CurrentOrder; Check(Game.Busy,"Movement starts on tile click"); await order;
        Check(garrison.Position==destination&&!Game.Busy,"Direct movement complete");
        Game.SelectCell(new(0,0)); Check(Game.SelectedShipId is null,"Pentagon exterior deselects");
        Game.FastChecks=true; var mother=Game.Battle.Mothership(Side.Player)!; Game.SelectCell(mother.Position); await Frame();
        var sectors=Descendants(Game.Hud).OfType<SectorButton>().Where(s=>s.IsVisibleInTree()).ToArray();
        Check(sectors.Length==5&&sectors.All(s=>Mathf.IsEqualApprox(s.Sweep,SectorButton.SectorStep)&&s._HasPoint(s.IconCenter)&&!s._HasPoint(SectorButton.Center)),"Five readable papyrus sectors, including information, leave the map center transparent");
        int money=Game.Battle.Credits(Side.Player); Click(Button("ActionRadar")); await Game.CurrentOrder;
        Check(mother.HasRadar&&Game.Battle.Credits(Side.Player)==money-2,"Radar purchase");
        Click(Button("ActionBuild")); await Frame();
        Check(!Button("BuildFishing").Disabled&&Button("BuildInvader").Disabled&&Button("BuildKolonel").Disabled&&Button("BuildTogus").Disabled,"Starting yard unlocks only fishing and garrison");
        await Capture("-shipyard"); Click(Button("BuildFishing")); var spawn=Game.Battle.SpawnCells(mother.Id).First(); Game.SelectCell(spawn); await Game.CurrentOrder;
        Check(Game.Battle.At(spawn)!.Definition.Class==ShipClass.Fishing&&!mother.CanMove,"Build works and locks mother movement");

        var fish=(from y in Enumerable.Range(3,5) from x in Enumerable.Range(3,5) let p=new GridPosition(x,y) where p!=new GridPosition(5,5)&&BattleVision.InRadius(p,new(5,5),2) select p).Take(14).ToArray();
        Game.LoadScenario(new BattleState(new GameBoard(20,20,p=>p==new GridPosition(7,7)?TerrainType.Land:TerrainType.Water),rules,new[] {
            (Side.Player,ShipClass.Mothership,new GridPosition(5,5)),(Side.Enemy,ShipClass.Mothership,new GridPosition(18,18)),
            (Side.Player,ShipClass.Fishing,new GridPosition(8,8)) },fish));
        mother=Game.Battle.Find(1)!; Game.SelectCell(mother.Position); await Frame();
        Check(Game.BoardView.Collection.Count==14&&Game.BoardView.DockSites.Count==0&&Game.BoardView.AttackArea.Count==0,"Available fish icons, no attack fill or level-one docks");
        Game.SelectCell(fish[0]); await Frame();
        Check(Game.Battle.Credits(Side.Player)==80&&mother.Resources==0,"Selecting fish does not spend currency");
        Game.CancelOrder(); await Frame();
        Check(!Button("TileResource").IsVisibleInTree()&&Game.Battle.Credits(Side.Player)==80,"Deselect dismisses purchase without spending");
        Check(Game.SelectedShipId is null,"Resource collection starts without a selected ship");
        Game.SelectCell(fish[0]); await Frame();
        Check(Button("TileResource").IsVisibleInTree(),"Available resource opens without selecting its nearby collector");
        await Capture("-resource");
        Tap(ClickAt(Button("TileResource"))); await Game.CurrentOrder; await Frame();
        Check(mother.Resources==1&&Game.Battle.Credits(Side.Player)==78&&!Button("TileResource").IsVisibleInTree(),"Touch buys resource once and closes sector");
        await BuyResource(fish[1]); Check(Game.Hud.UpgradeVisible&&mother.Level==2,"Level two automatic modal"); await Capture("-level2");
        var position=mother.Position; Tap(Screen(new(5,7))); Check(mother.Position==position,"Modal blocks movement");
        Click(Button("UpgradeMobility")); await Game.CurrentOrder;
        Check(!Game.Hud.UpgradeVisible&&mother.MovementAllowance==2&&Game.BoardView.DockSites.Count>0,"Level two unlocks docks");
        foreach(var cell in fish.Skip(2).Take(3)) await BuyResource(cell);
        Check(mother.Level==3&&Game.Hud.UpgradeVisible,"Level three"); Click(Button("UpgradeRestoration")); await Game.CurrentOrder;
        Check(mother.MaxHealth==35,"Level three reinforced hull adds five HP");
        foreach(var cell in fish.Skip(5).Take(4)) await BuyResource(cell);
        Check(mother.Level==4&&Game.Hud.UpgradeVisible,"Level four"); Click(Button("UpgradeBalloon")); await Game.CurrentOrder;
        Check(mother.MaxHealth==40&&Game.Battle.BuildBlockReason(Side.Player,1,ShipClass.Kolonel) is null&&Game.Battle.MortarBlockReason(Side.Player,1) is not null,"Level four heavy unlock; mortar locked");
        var balloon=Game.Battle.OwnShips(Side.Player).Single(s=>s.IsAirborne);
        Game.SelectAtScreen(GetViewport().GetCanvasTransform()*(Game.BoardView.Projection.GridToWorld(balloon.Position)+new Vector2(0,-62)));
        Check(Game.SelectedShipId==balloon.Id,"Balloon selected above ship"); Game.SelectCell(new(7,7)); await Game.CurrentOrder;
        Check(balloon.Position==new GridPosition(7,7)&&balloon.MovementRemainingUnits==20,"Balloon crosses land with one-point diagonals");
        await Capture("-balloon"); Game.SelectCell(mother.Position);
        foreach(var cell in fish.Skip(9)) await BuyResource(cell);
        Check(mother.Level==5&&mother.MaxHealth==45&&Game.Hud.UpgradeVisible&&mother.ResourcesRequired==0,"Level five presents the final upgrade choice");
        Click(Button("UpgradeShipwright")); await Game.CurrentOrder;
        Check(!Game.Hud.UpgradeVisible&&Game.Battle.BuildPrice(Side.Player,ShipClass.Kolonel)==6,"Level five shipbuilding discount applies");
        Game.SelectCell(mother.Position);
        await Frame(); Click(Button("ActionRadar")); await Game.CurrentOrder; await Frame(); money=Game.Battle.Credits(Side.Player);
        Check(!Button("ActionMortar").Disabled,"Level five radar enables mortar"); Click(Button("ActionMortar")); await Game.CurrentOrder;
        Check(mother.HasMortar&&Game.Battle.Credits(Side.Player)==money-10&&mother.CurrentMortarDamage==8,"Mortar cost and damage");
        var site=Game.Battle.DockCells(1).First(); await BuyResource(site);
        Check(Game.Battle.At(site) is { IsStructure:true }&&Game.Battle.Income(Side.Player)==13,"Tile dock purchase and income at level five");
        await Capture("-dock"); Click(Button("ActionBuild")); await Frame(); Check(!Button("BuildTogus").Disabled,"Togus unlocked at five");
        Click(Button("BuildTogus")); spawn=Game.Battle.SpawnCells(1).First(); Game.SelectCell(spawn); await Game.CurrentOrder;
        Check(Game.Battle.At(spawn) is { HasMortar:true,HasRadar:true },"Built Togus includes artillery and radar"); await Frame(); await Capture("-togus");

        Game.LoadScenario(new BattleState(new GameBoard(20,20,_=>TerrainType.Water),rules,new[] {
            (Side.Player,ShipClass.Mothership,new GridPosition(0,0)),(Side.Enemy,ShipClass.Mothership,new GridPosition(19,19)),
            (Side.Player,ShipClass.Kolonel,new GridPosition(7,7)),(Side.Enemy,ShipClass.Kolonel,new GridPosition(9,7)) },Array.Empty<GridPosition>()));
        Game.FastChecks=false; Game.SelectCell(new(7,7)); Check(Game.BoardView.Targets.Contains(new GridPosition(9,7)),"Selectable targets remain outlined");
        Game.SelectCell(new(9,7)); order=Game.CurrentOrder; await ToSignal(GetTree().CreateTimer(.12),SceneTreeTimer.SignalName.Timeout);
        Check(Game.Fleet.TurningForShot&&Game.Fleet.ProjectilePosition is null&&Math.Abs(Game.Fleet.DeckAngle(3))>.01,"Cannon vessel turns its broadside before firing");
        await ToSignal(GetTree().CreateTimer(.35),SceneTreeTimer.SignalName.Timeout);
        Check(Game.Fleet.ProjectilePosition is not null&&!Game.Fleet.TurningForShot,"Cannon projectile follows the completed broadside turn"); await order;
        Check(Game.Battle.Find(3)!.Health<15&&Game.Battle.Find(4)!.Health<15,"Counterattack");
        Game.Battle.EndTurn(Side.Player); Game.Battle.EndTurn(Side.Enemy); Game.Refresh(); await Frame();
        double hp=Game.Battle.Find(3)!.Health; Click(Button("ActionRepair")); await Game.CurrentOrder;
        Check(Game.Battle.Find(3)!.Health>hp,"Repair sector works");

        Game.LoadScenario(new BattleState(new GameBoard(20,20,_=>TerrainType.Water),rules,new[] {
            (Side.Player,ShipClass.Mothership,new GridPosition(0,0)),(Side.Enemy,ShipClass.Mothership,new GridPosition(19,19)),
            (Side.Player,ShipClass.Togus,new GridPosition(5,5)),(Side.Enemy,ShipClass.Kolonel,new GridPosition(10,5)) },Array.Empty<GridPosition>()));
        Game.SelectCell(new(5,5)); Check(Game.Battle.FindObserved(Side.Player,4) is null&&Game.BoardView.Targets.Contains(new GridPosition(10,5)),"Radar target anonymous before shot");
        Game.SelectCell(new(10,5)); order=Game.CurrentOrder;
        await ToSignal(GetTree().CreateTimer(.35),SceneTreeTimer.SignalName.Timeout);
        Check(Game.Fleet.ProjectilePosition is not null&&!Game.Fleet.AnimatedShipIds.Contains(4)&&Game.Fleet.CombatFeedback.Length==0,"Animation never inserts hidden target or damage text");
        await Capture("-mortar"); await order;
        Check(Game.Battle.Find(4)!.Health==7&&Game.Battle.FindObserved(Side.Player,4) is null&&!Game.Hud.MessageText.Contains("8")&&!Game.Hud.MessageText.Contains("Kolonel"),"Hit applies without revealing class or damage");
        Game.FastChecks=true; await Game.EndPlayerTurn(); Check(Game.Battle.ActiveSide==Side.Player&&!Game.Busy,"Opponent returns control");
        await VillagesAndBalloon(rules);
        await Release014Battle(rules);
        int seed=Game.Battle.Board.Seed; Game.Restart(); Check(Game.Battle.Board.Seed!=seed&&Game.Battle.Board.Boundary.Count==6&&Game.Battle.Ships.Count==8,"Restart randomizes hexagon and resets game");
>>>>>>> Stashed changes
    }

    private async Task VillagesAndBalloon(BattleRules rules)
    {
<<<<<<< Updated upstream
        var location = new GridPosition(9, 8);
        Game.LoadScenario(new BattleState(new GameBoard(20, 20, p => p == location ? TerrainType.Land : TerrainType.Water), rules, new[] { (Side.Player, ShipClass.Mothership, new GridPosition(1, 1)), (Side.Enemy, ShipClass.Mothership, new GridPosition(18, 18)), (Side.Player, ShipClass.Garrison, new GridPosition(8, 8)), (Side.Enemy, ShipClass.Kolonel, new GridPosition(11, 8)) }, Array.Empty<GridPosition>(), villageSpots: new[] { location }));
        Game.FastChecks = true;
        var village = Game.Battle.Villages.Single();
        Game.MapCamera.ZoomAt(Screen(location), 1.8f);
        Game.SelectCell(location);
        await Frame();
        Check(Game.SelectedVillageId == village.Id && !Button("ActionCapture").IsVisibleInTree() && !Game.Hud.ClaimPapyrus.Visible, "A living neutral town has no ready claim scroll");
        await Capture("-village-neutral");
        Game.Battle.AttackVillage(Side.Player, 3, village.Id);
        Game.Battle.EndTurn(Side.Player);
        Game.Battle.EndTurn(Side.Enemy);
        Game.Battle.AttackVillage(Side.Player, 3, village.Id);
        Game.Refresh();
        await Frame();
        Check(!Game.Hud.ClaimPapyrus.Visible, "Zero HP alone does not reveal a claim scroll");
        Game.Battle.EndTurn(Side.Player);
        Game.Battle.EndTurn(Side.Enemy);
        Game.Refresh();
        await Frame();
        Check(Game.Hud.ClaimPapyrus.Visible, "Claim scroll appears after holding until next turn");
        Game.CancelOrder();
        await Frame();
        await ToSignal(GetTree().CreateTimer(.42), SceneTreeTimer.SignalName.Timeout);
        Tap(Game.Hud.ClaimPapyrus.GlobalPosition + Game.Hud.ClaimPapyrus.Size / 2);
        await Game.CurrentOrder;
        await Frame();
        Check(village.Owner == Side.Player && Game.SelectedVillageId == village.Id && Button("ActionBuild").IsVisibleInTree(), "Tapping the hovering scroll captures and opens the town controls");
        Check(Game.Battle.Find(3)!.IsExhausted, "Capturing ship has no remaining actions");
        await Capture("-village-captured");
        Click(Button("ActionFortify"));
        await Game.CurrentOrder;
        await Frame();
        Check(village.IsFortified && Button("ActionFortify").Disabled, "Fortification can be bought once through its action");
        await Capture("-village-fortified");
        Click(Button("ActionBuild"));
        await Frame();
        Check(!Button("BuildFishing").Disabled && Button("BuildGarrison").Disabled && Button("BuildInvader").Disabled, "Village starts with fishing; Brig unlocks at level two");
        Click(Button("BuildFishing"));
        var spawn = Game.Battle.VillageSpawnCells(village.Id).First();
        Game.SelectCell(spawn);
        await Game.CurrentOrder;
        Check(Game.Battle.At(spawn)!.Definition.Class == ShipClass.Fishing && village.HasProduced, "Village shipyard places the selected ship on adjacent water");
        for (int turn = 0; turn < 8; turn++)
        {
            Game.Battle.EndTurn(Side.Player);
            Game.Battle.EndTurn(Side.Enemy);
        }

        Game.CancelOrder();
        Game.SelectCell(location);
        await Frame();
        Check(village.Level == 5 && village.MaxHealth == 25, "Village grows automatically without upgrade dialogs");
        Click(Button("ActionBuild"));
        await Frame();
        Check(!Button("BuildFishing").Disabled && !Button("BuildGarrison").Disabled && !Button("BuildInvader").Disabled && !Button("BuildKolonel").Disabled && !Button("BuildTogus").Disabled, "Level-five village offers all five ship types");
        await Capture("-village-level5");
        Game.Battle.EndTurn(Side.Player);
        Check(Game.Battle.AttackVillage(Side.Enemy, 4, village.Id).Success, "Repair fixture damages fortified village");
        Game.Battle.EndTurn(Side.Enemy);
        Game.CancelOrder();
        Game.SelectCell(location);
        await Frame();
        double damaged = village.Health;
        Check(Button("ActionRepair").IsVisibleInTree() && !Button("ActionRepair").Disabled, "Village exposes active repair");
        Click(Button("ActionRepair"));
        await Game.CurrentOrder;
        await Frame();
        Check(village.Health == Math.Min(village.MaxHealth, damaged + 5) && Button("ActionRepair").Disabled, "Village repair works through its sector and cannot repeat");
        Game.LoadScenario(new BattleState(new GameBoard(20, 20, _ => TerrainType.Water), rules, new[] { (Side.Player, ShipClass.Mothership, new GridPosition(1, 1)), (Side.Enemy, ShipClass.Mothership, new GridPosition(18, 18)), (Side.Player, ShipClass.Balloon, new GridPosition(8, 8)), (Side.Enemy, ShipClass.Kolonel, new GridPosition(9, 8)) }, Array.Empty<GridPosition>()));
        Game.MapCamera.ZoomAt(Screen(new(8, 8)), 1.8f);
        Game.SelectAtScreen(GetViewport().GetCanvasTransform() * (Game.BoardView.Projection.GridToWorld(new(8, 8)) + new Vector2(0, -62)));
        await Frame();
        Check(Game.SelectedShipId == 3 && Button("ActionBomb").Disabled, "Balloon bomb waits for movement");
        Game.SelectCell(new(9, 8));
        await Game.CurrentOrder;
        await Frame();
        Check(!Button("ActionBomb").Disabled, "Balloon can bomb after moving over a ship");
        await Capture("-bomb-ready");
        Game.FastChecks = false;
        Click(Button("ActionBomb"));
        var bombOrder = Game.CurrentOrder;
        await WaitForProjectile(bombOrder);
        Check(Game.Fleet.ProjectilePosition is not null, "Bomb visibly falls after the camera arrives");
        await Capture("-bomb-falling");
        await bombOrder;
        await Frame();
        Check(Game.Battle.Find(3)!.BombUsed && Game.Battle.Find(4)!.Health == 9 && Button("ActionBomb").Disabled, "Bomb button deals six direct damage and starts its cooldown");
        await Capture("-bomb-used");
        Game.FastChecks = true;
=======
        var location=new GridPosition(9,8);
        Game.LoadScenario(new BattleState(new GameBoard(20,20,p=>p==location?TerrainType.Land:TerrainType.Water),rules,new[] {
            (Side.Player,ShipClass.Mothership,new GridPosition(1,1)),
            (Side.Enemy,ShipClass.Mothership,new GridPosition(18,18)),
            (Side.Player,ShipClass.Garrison,new GridPosition(8,8)),
            (Side.Enemy,ShipClass.Kolonel,new GridPosition(11,8))
        },Array.Empty<GridPosition>(),villageSpots:new[]{location}));
        Game.FastChecks=true;
        var village=Game.Battle.Villages.Single();
        Game.MapCamera.ZoomAt(Screen(location),1.8f);
        Game.SelectCell(location); await Frame();
        Check(Game.SelectedVillageId==village.Id&&Button("ActionCapture").IsVisibleInTree()&&Button("ActionCapture").Disabled,
            "Neutral town card requires defeated defenses");
        await Capture("-village-neutral");
        Game.Battle.AttackVillage(Side.Player,3,village.Id);
        Game.Battle.EndTurn(Side.Player); Game.Battle.EndTurn(Side.Enemy);
        Game.Battle.AttackVillage(Side.Player,3,village.Id); Game.Refresh(); await Frame();
        Check(Button("ActionCapture").Disabled,"Zero HP alone does not allow immediate capture");
        Game.Battle.EndTurn(Side.Player); Game.Battle.EndTurn(Side.Enemy); Game.Refresh(); await Frame();
        Check(!Button("ActionCapture").Disabled,"Capture action becomes available after holding until next turn");
        Game.CancelOrder(); await Frame();
        Tap(GetViewport().GetCanvasTransform()*Game.BoardView.VillageFlagPosition(location));
        await Game.CurrentOrder; await Frame();
        Check(village.Owner==Side.Player&&Game.SelectedVillageId==village.Id&&Button("ActionBuild").IsVisibleInTree(),
            "Tapping the village flag captures and opens its shipyard controls");
        Check(Game.Battle.Find(3)!.IsExhausted,"Capturing ship has no remaining actions");
        await Capture("-village-captured");
        Click(Button("ActionFortify")); await Game.CurrentOrder; await Frame();
        Check(village.IsFortified&&Button("ActionFortify").Disabled,"Fortification can be bought once through its action");
        await Capture("-village-fortified");
        Click(Button("ActionBuild")); await Frame();
        Check(!Button("BuildFishing").Disabled&&Button("BuildGarrison").Disabled&&Button("BuildInvader").Disabled,
            "Village starts with fishing; Brig unlocks at level two");
        Click(Button("BuildFishing"));
        var spawn=Game.Battle.VillageSpawnCells(village.Id).First(); Game.SelectCell(spawn); await Game.CurrentOrder;
        Check(Game.Battle.At(spawn)!.Definition.Class==ShipClass.Fishing&&village.HasProduced,
            "Village shipyard places the selected ship on adjacent water");
        for(int turn=0;turn<8;turn++) { Game.Battle.EndTurn(Side.Player); Game.Battle.EndTurn(Side.Enemy); }
        Game.CancelOrder(); Game.SelectCell(location); await Frame();
        Check(village.Level==5&&village.MaxHealth==25,"Village grows automatically without upgrade dialogs");
        Click(Button("ActionBuild")); await Frame();
        Check(!Button("BuildFishing").Disabled&&!Button("BuildGarrison").Disabled&&!Button("BuildInvader").Disabled&&!Button("BuildKolonel").Disabled&&!Button("BuildTogus").Disabled,
            "Level-five village offers all five ship types");
        await Capture("-village-level5");
        Game.Battle.EndTurn(Side.Player);
        Check(Game.Battle.AttackVillage(Side.Enemy,4,village.Id).Success,"Repair fixture damages fortified village");
        Game.Battle.EndTurn(Side.Enemy);Game.CancelOrder();Game.SelectCell(location);await Frame();
        double damaged=village.Health;
        Check(Button("ActionRepair").IsVisibleInTree()&&!Button("ActionRepair").Disabled,"Village exposes active repair");
        Click(Button("ActionRepair"));await Game.CurrentOrder;await Frame();
        Check(village.Health==Math.Min(village.MaxHealth,damaged+5)&&Button("ActionRepair").Disabled,"Village repair works through its sector and cannot repeat");

        Game.LoadScenario(new BattleState(new GameBoard(20,20,_=>TerrainType.Water),rules,new[] {
            (Side.Player,ShipClass.Mothership,new GridPosition(1,1)),
            (Side.Enemy,ShipClass.Mothership,new GridPosition(18,18)),
            (Side.Player,ShipClass.Balloon,new GridPosition(8,8)),
            (Side.Enemy,ShipClass.Kolonel,new GridPosition(9,8))
        },Array.Empty<GridPosition>()));
        Game.MapCamera.ZoomAt(Screen(new(8,8)),1.8f);
        Game.SelectAtScreen(GetViewport().GetCanvasTransform()*(Game.BoardView.Projection.GridToWorld(new(8,8))+new Vector2(0,-62)));
        await Frame();
        Check(Game.SelectedShipId==3&&Button("ActionBomb").Disabled,"Balloon bomb waits for movement");
        Game.SelectCell(new(9,8)); await Game.CurrentOrder; await Frame();
        Check(!Button("ActionBomb").Disabled,"Balloon can bomb after moving over a ship");
        await Capture("-bomb-ready");
        Game.FastChecks=false;
        Click(Button("ActionBomb")); var bombOrder=Game.CurrentOrder;
        await ToSignal(GetTree().CreateTimer(.15),SceneTreeTimer.SignalName.Timeout);
        Check(Game.Fleet.ProjectilePosition is not null,"Bomb visibly falls from the balloon");
        await Capture("-bomb-falling");
        await bombOrder; await Frame();
        Check(Game.Battle.Find(3)!.BombUsed&&Game.Battle.Find(4)!.Health==9&&Button("ActionBomb").Disabled,
            "Bomb button deals six direct damage and starts its cooldown");
        await Capture("-bomb-used");
        Game.FastChecks=true;
>>>>>>> Stashed changes
    }
}

