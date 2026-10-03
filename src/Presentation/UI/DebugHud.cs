using System;
using System.Collections.Generic;
using System.Linq;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Vision;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation.UI;
public partial class DebugHud : CanvasLayer
{
    private Control _root = null !, _upgradeOverlay = null !;
    private ScrollContainer _upgradeScroll = null!;
    private VBoxContainer _upgradeBody = null!;
    private RadialPapyrus _radial = null !, _resourceRoot = null !;
    private PanelContainer _metricsPaper = null !;
    private PanelContainer _nationPaper = null!;
    private Label _nationLabel = null!;
    private SectorButton _resourceInformation = null !;
    private readonly List<SectorButton> _actionSectors = new(12);
    private readonly List<SectorButton> _visibleSectors = new(12);
    private readonly List<SectorButton> _radialSlots = new(13);
    private SectorButton _villageUpgrade = null!, _upgradeGap = null!;
    public event Action? VillageUpgradeRequested;
    private readonly List<Vector2> _worldTargetHitPoints = new(16);
    private int _lastSectorCount;
    private bool _unfoldActions;
    private HBoxContainer _metrics = null !;
    private PanelContainer _shipCard = null !, _notice = null !, _upgradePanel = null !;
    private Label _coins = null !, _coinCaption = null !, _turn = null !, _ship = null !, _details = null !, _message = null !, _banner = null !, _upgradeTitle = null !;
    private Label _fleetUsage = null!;
    private SectorButton _scuttle = null!;
    public Func<Ship, bool>? CanScuttleShip { get; set; }
    public event Action? ScuttleRequested;
    private Label _health = null !, _damagePreview = null !, _counterPreview = null !;
    private HBoxContainer _combatPreview = null !;
    private SectorButton _repair = null !, _yard = null !, _radar = null !, _mortar = null !, _resource = null !, _bomb = null !, _capture = null !, _fortify = null !, _port = null !, _loot = null !;
    private Button _end = null !, _restart = null !;
    private readonly Dictionary<ShipClass, SectorButton> _build = new();
    private readonly Dictionary<SectorButton, Label> _badges = new();
    private readonly Dictionary<UpgradeChoice, Button> _choices = new();
    private int? _selectedId;
    private int? _selectedVillageId;
    private bool _hasRadial, _productionOpen;
    private OrderMode _mode;
    private float _noticeTime, _bannerTime;
    private (Vector2 Viewport, Vector2 Metrics, Vector2 Restart, Vector2 End, Vector2 Banner, Vector2 Ship, Vector2 Notice, Vector2 Upgrade, Vector2 Menu)? _layoutSizes;
    public string SelectionText { get; private set; } = "";
    public string StatusText { get; private set; } = "";
    public string ShipText => _ship.Text;
    public string MessageText => _message.Text;
    public string BannerText => _banner.Visible ? _banner.Text : "";
    public Vector2 MenuPosition => _radial.Position;
    public bool UpgradeVisible => _upgradeOverlay.Visible;

    public event Action? EndTurnRequested, RepairRequested, RestartRequested, ResourceRequested, RadarRequested, MortarRequested;
    public event Action? BombRequested, CaptureRequested, FortifyRequested, PortRequested, LootRequested;
    public event Action<ShipClass>? BuildRequested;
    public event Action<UpgradeChoice>? UpgradeRequested;
    public override void _Ready()
    {
        _root = new Control
        {
            Theme = PapyrusStyle.ChartTheme(),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        AddChild(_root);
        _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _metricsPaper = Panel(_root);
        _metrics = new HBoxContainer
        {
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        _metrics.AddThemeConstantOverride("separation", 24);
        _metricsPaper.AddChild(_metrics);
        var money = new VBoxContainer();
        money.AddThemeConstantOverride("separation", 1);
        _metrics.AddChild(money);
        _coinCaption = Label("Thors (+4)", 12, true);
        money.AddChild(_coinCaption);
        _coins = Label("5", 23, true);
        var coinRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        coinRow.AddThemeConstantOverride("separation", 6);
        money.AddChild(coinRow);
        coinRow.AddChild(new CoinIcon { Name = "ThorCoin", CustomMinimumSize = new(18, 23) });
        coinRow.AddChild(_coins);
        var turns = new VBoxContainer();
        turns.AddThemeConstantOverride("separation", 1);
        _metrics.AddChild(turns);
        turns.AddChild(Label("Turn", 12, true));
        _turn = Label("1", 23, true);
        turns.AddChild(_turn);
        var fleet = new VBoxContainer();
        fleet.AddThemeConstantOverride("separation", 1);
        _metrics.AddChild(fleet);
        fleet.AddChild(Label("Fleet", 12, true));
        _fleetUsage = Label("0/0", 23, true);
        _fleetUsage.Name = "FleetUsage";
        fleet.AddChild(_fleetUsage);
        var compact = PapyrusStyle.Panel();
        compact.ContentMarginTop = compact.ContentMarginBottom = 7;
        compact.ContentMarginLeft = compact.ContentMarginRight = 11;
        _metricsPaper.AddThemeStyleboxOverride("panel", compact);
        _nationPaper = Panel(_root);
        _nationPaper.Name = "ActingNation";
        _nationPaper.MouseFilter = Control.MouseFilterEnum.Ignore;
        _nationLabel = Label("Your turn", 12, true);
        _nationPaper.AddChild(_nationLabel);
        _nationPaper.Hide();
        _restart = TextButton("", () => SetMenuVisible(true));
        _restart.Name = "Menu";
        _restart.TooltipText = "Menu";
        _restart.CustomMinimumSize = new(48, 48);
        _restart.Size = new(48, 48);
        _root.AddChild(_restart);
        _restart.AddChild(new ActionGlyph { Symbol = ActionSymbol.Menu, Position = new(10, 10), Size = new(28, 28), MouseFilter = Control.MouseFilterEnum.Ignore });
        _end = TextButton("End turn  →", () => EndTurnRequested?.Invoke());
        _end.Name = "EndTurn";
        _end.TooltipText = "End turn · Space";
        _root.AddChild(_end);
        _banner = Label("", 24, true);
        _root.AddChild(_banner);
        _banner.Hide();
        _shipCard = Panel(_root);
        var stats = new VBoxContainer();
        stats.AddThemeConstantOverride("separation", 4);
        _shipCard.AddChild(stats);
        _ship = Label("", 18);
        stats.AddChild(_ship);
        _health = Label("", 15);
        _health.AddThemeColorOverride("font_color", PapyrusStyle.Health);
        stats.AddChild(_health);
        _details = Label("", 14);
        stats.AddChild(_details);
        _notice = Panel(_root);
        _notice.MouseFilter = Control.MouseFilterEnum.Ignore;
        var notices = new VBoxContainer();
        _notice.AddChild(notices);
        _message = Label("", 16, true);
        notices.AddChild(_message);
        _notice.Hide();
        _combatPreview = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        notices.AddChild(_combatPreview);
        _combatPreview.AddThemeConstantOverride("separation", 18);
        _damagePreview = Label("", 16);
        _counterPreview = Label("", 16);
        _damagePreview.AddThemeColorOverride("font_color", new Color("b13c2e"));
        _counterPreview.AddThemeColorOverride("font_color", new Color("a17b12"));
        _combatPreview.AddChild(_damagePreview);
        _combatPreview.AddChild(_counterPreview);
        _combatPreview.Hide();
        _radial = new RadialPapyrus
        {
            Name = "ActionPapyrus",
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Size = new(248, 248)
        };
        _root.AddChild(_radial);
        BuildInformation();
        _repair = IconButton("ActionRepair", ActionSymbol.Repair, "Repair", () => RepairRequested?.Invoke());
        _scuttle = IconButton("ActionScuttle", ActionSymbol.Scuttle, "Scuttle · No refund", () => ScuttleRequested?.Invoke());
        _radar = IconButton("ActionRadar", ActionSymbol.Radar, "Install radar", () => RadarRequested?.Invoke());
        _mortar = IconButton("ActionMortar", ActionSymbol.Mortar, "Mortar", () => MortarRequested?.Invoke());
        _loot = IconButton("ActionLoot", ActionSymbol.Treasure, "Plunder treasury", () => LootRequested?.Invoke());
        _bomb = IconButton("ActionBomb", ActionSymbol.Bomb, "Drop bomb", () => BombRequested?.Invoke());
        _capture = IconButton("ActionCapture", ActionSymbol.Flag, "Capture village", () => CaptureRequested?.Invoke());
        _fortify = IconButton("ActionFortify", ActionSymbol.Fortify, "Fortify village", () => FortifyRequested?.Invoke());
        _port = IconButton("ActionPort", ActionSymbol.Dock, "Build port", () => PortRequested?.Invoke());
        _yard = IconButton("ActionBuild", ActionSymbol.Build, "Shipyard", () =>
        {
            _productionOpen = true;
            ApplyMenuVisibility();
        });
        foreach (var(kind, symbol)in new[]
        {
            (ShipClass.Fishing, ActionSymbol.Support),
            (ShipClass.Garrison, ActionSymbol.Scout),
            (ShipClass.Invader, ActionSymbol.Standard),
            (ShipClass.Kolonel, ActionSymbol.Heavy),
            (ShipClass.Togus, ActionSymbol.Mortar),
            (ShipClass.CannonTower, ActionSymbol.Tower),
            (ShipClass.Lighthouse, ActionSymbol.Lighthouse)
        }

        )
            _build[kind] = IconButton("Build" + kind, symbol, kind.ToString(), () => BuildRequested?.Invoke(kind));
        _resourceRoot = new RadialPapyrus
        {
            Name = "ResourcePapyrus",
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Size = new(248, 248)
        };
        _root.AddChild(_resourceRoot);
        _resource = IconButton("TileResource", ActionSymbol.Fishing, "", () => ResourceRequested?.Invoke(), _resourceRoot);
        _resourceInformation = IconButton("ResourceInformation", ActionSymbol.Information, "Read the chart", OpenInformation, _resourceRoot);
        _resource.SetSector(1, 2);
        _resourceInformation.SetSector(0, 2);
        Availability(_resourceInformation, true, "Info");
        _resourceRoot.Configure(new[] { _resourceInformation, _resource }, false);
        _resourceRoot.Hide();
        _upgradeOverlay = new Control
        {
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        _root.AddChild(_upgradeOverlay);
        _upgradeOverlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var shade = new ColorRect
        {
            Color = new Color(0.01f, 0.035f, 0.05f, 0.5f),
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        _upgradeOverlay.AddChild(shade);
        shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _upgradePanel = Panel(_upgradeOverlay);
        _upgradePanel.Name = "UpgradePapyrus";
        _upgradePanel.CustomMinimumSize = new(PapyrusModal.Width, 0);
        var column = new VBoxContainer();
        _upgradeBody = column;
        column.AddThemeConstantOverride("separation", 18);
        _upgradeScroll = PapyrusModal.Wrap(_upgradePanel, column, "UpgradeScroll");
        _upgradeTitle = Label("", 20, true);
        _upgradeTitle.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _upgradeTitle.CustomMinimumSize = Vector2.Zero;
        column.AddChild(_upgradeTitle);
        var upgradePrompt = Label("Choose one upgrade", 17, true);
        upgradePrompt.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        column.AddChild(upgradePrompt);
        foreach (var choice in new[]
        {
            UpgradeChoice.Mobility,
            UpgradeChoice.FishingBoat,
            UpgradeChoice.Vision,
            UpgradeChoice.Restoration,
            UpgradeChoice.Balloon,
            UpgradeChoice.SecondAttack,
            UpgradeChoice.Shipwright,
            UpgradeChoice.Firepower
        }

        )
        {
            var button = new MysticUpgradeButton { Text = MysticUpgradeButton.Title(choice),
                AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new(0, 48), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            PapyrusStyle.Button(button, 16);
            button.Pressed += () => UpgradeRequested?.Invoke(choice);
            button.TooltipText = UpgradeDescriptions.Description(choice);
            button.Name = "Upgrade" + choice;
            var group = new VBoxContainer();
            group.AddThemeConstantOverride("separation", 3);
            group.AddChild(button);
            column.AddChild(group);
            _choices[choice] = button;
        }

        BuildGameMenu();
        _villageUpgrade = IconButton("ActionVillageUpgrade", ActionSymbol.Upgrade, "Upgrade town", () => VillageUpgradeRequested?.Invoke());
        _upgradeGap = new SectorButton { Name = "TownArcGap", Visible = false, MouseFilter = Control.MouseFilterEnum.Ignore };
        _radial.AddChild(_upgradeGap);
        BuildActionStories();
        BuildSalvoChoice();
        BuildHeavenlyAssistance();
        _upgradeOverlay.Hide();
        _radial.Hide();
        _shipCard.Hide();
        UiScale.Bind(this, _root, () => { _layoutSizes = null; Layout(); });
        Layout();
    }

    private BattleState? _namedBattle;
    public void UpdateBattle(BattleState battle, Ship? selected, bool busy, OrderMode mode, Village? village = null)
    {
        using var trace = DevAncientNaval.Presentation.Diagnostics.PerformanceTrace.Measure("Hud.UpdateBattle");
        _namedBattle = battle;
        _unfoldActions |= _selectedId != selected?.Id || _selectedVillageId != village?.Id;
        if (_unfoldActions)
            _informationPanel.Hide();
        if (_selectedId != selected?.Id || _selectedVillageId != village?.Id || busy || mode != OrderMode.None)
            _productionOpen = false;
        if (_selectedId != selected?.Id || _selectedVillageId != village?.Id || busy || MenuVisible)
            _resourceRoot.Hide();
        _selectedId = selected?.Id;
        _selectedVillageId = village?.Id;
        _mode = mode;
        _coinCaption.Text = $"{(battle.Credits(Side.Player) == 1 ? "Thor" : "Thors")} (+{battle.Income(Side.Player)})";
        _metricsPaper.TooltipText = $"Income {battle.GrossIncome(Side.Player)} − fleet upkeep {battle.Upkeep(Side.Player)} = {battle.Income(Side.Player)} Thors each turn";
        _coins.Text = battle.Credits(Side.Player).ToString();
        _turn.Text = battle.Round.ToString();
        _fleetUsage.Text = $"{battle.FleetUsed(Side.Player)}/{battle.FleetCapacity(Side.Player)}";
        bool finished = battle.IsOver || battle.PlayerDefeated;
        StatusText = finished ? (battle.IsDraw ? "DRAW" : battle.Winner == Side.Player ? "VICTORY" : "DEFEAT") : $"Turn {battle.Round}";
        if (finished)
        {
            _banner.Text = StatusText;
            _bannerTime = -1;
            _banner.Show();
        }
        else if (_bannerTime == -1)
        {
            _bannerTime = 0;
            _banner.Hide();
        }

        var pending = battle.PendingUpgrade(Side.Player);
        _upgradeOverlay.Visible = pending is not null && !busy && !finished;
        if (pending is not null)
        {
            _upgradeTitle.Text = $"Mothership · level {pending.Level}";
            foreach (var(choice, button)in _choices)
            {
                button.Text = MysticUpgradeButton.Title(choice);
                button.TooltipText = UpgradeDescriptions.Description(choice, battle.Rules);
                button.Visible = battle.UpgradeOptions(pending.Id).Contains(choice);
                ((Control)button.GetParent()).Visible = button.Visible;
                button.Disabled = busy;
            }

            _upgradePanel.ResetSize();
        }

        bool canAct = !busy && !finished && battle.ActiveSide == Side.Player && pending is null && !MenuVisible;
        _end.Disabled = !canAct;
        _restart.Disabled = busy;
        _shipCard.Visible = selected is not null || village is not null;
        if (selected is not null)
        {
            _health.AddThemeColorOverride("font_color", selected.Owner == Side.Player ? PapyrusStyle.Health : PapyrusStyle.EnemyHealth);
            _ship.Text = selected.IsAirborne ? $"{selected.Name} · Persistent" : $"{selected.Name}{(selected.IsMothership ? $" · level {selected.Level}" : selected.IsVeteran ? " ★ VETERAN" : "")}";
            _health.Text = $"Health {selected.Health:0.##}/{selected.MaxHealth:0.##}";
            _details.Text = selected.IsAirborne ? $"Vision {selected.VisualRange} · Move {selected.MovementRemaining:0}/{selected.MovementAllowance} · 1 HP · Flagship/Kolonel guns can hit within {battle.Rules.Balloon.AntiAirRange} tiles\n{(selected.BombCooldown > 0 ? $"Bomb ready in {selected.BombCooldown} turn(s)" : $"Bomb ready: {battle.Rules.Balloon.BombDamage} direct + {battle.Rules.Balloon.SplashDamage} splash")}" : $"Damage {selected.CurrentDamage + selected.ShotDamageBonus:0} · Range {selected.CannonRange} · Vision {selected.VisualRange} · Radar {selected.RadarRange}";
            if (selected.HasMortar && selected.Definition.Class != ShipClass.AncientGun)
                _details.Text += $" · Mortar {battle.Rules.Mortar.DeadZone + 1}–{selected.MortarRange}: {selected.CurrentMortarDamage + selected.ShotDamageBonus:0}";
            if (selected.Definition.Class == ShipClass.FishingDock)
                _details.Text = $"Income +{selected.Definition.IncomePerTurn} · Stationary Fishing Dock";
            if (selected.Definition.Class is ShipClass.AncientGun or ShipClass.CannonTower)
                _details.Text = $"Damage {selected.CurrentDamage:0} · Range {selected.AttackRange} · Radar {selected.RadarRange} · Sight {selected.VisualRange}";
            if (!selected.IsAirborne && !selected.IsStructure)
                _details.Text += $"\nMove {selected.MovementRemaining:0}/{selected.MovementAllowance} · Shots {selected.AttacksRemaining}";
            _shipCard.TooltipText = selected.IsMothership ? (selected.Level == 5 ? "Maximum level" : $"Resources: {selected.Resources}/{selected.ResourcesRequired}") : $"Ships sunk: {selected.Kills}/3";
        }

        bool ownShip = canAct && selected?.Owner == Side.Player;
        bool ownVillage = canAct && village?.Owner == Side.Player;
        bool fishingBuilder = selected?.Definition.Class == ShipClass.Fishing &&
            (battle.Rules.FishingCannonTowers || battle.Rules.FishingLighthouses && battle.Rules.LighthousesEnabled);
        _hasRadial = selected is not null || village is not null;
        Availability(_repair, ownShip && selected!.CanRepair || ownVillage && battle.CanRepairVillage(Side.Player, village!.Id), $"+{battle.Rules.RepairAmount}");
        _repair.SetMeta("applicable", (selected is { Owner: Side.Player, IsAirborne: false } && selected.Definition.Class != ShipClass.AncientGun) || village?.Owner == Side.Player);
        _scuttle.SetMeta("applicable", selected is { Owner: Side.Player, IsMothership: false });
        Availability(_scuttle, ownShip && selected is not null && CanScuttleShip?.Invoke(selected) == true, "");
        Availability(_yard, (ownShip && (selected!.IsMothership || fishingBuilder) && !selected.HasProduced) || (ownVillage && !village!.HasProduced), "");
        Availability(_radar, ownShip && battle.RadarBlockReason(Side.Player, selected!.Id)is null, selected?.HasRadar == true ? "✓" : selected?.Definition.RadarPrice.ToString() ?? "");
        Availability(_mortar, ownShip && battle.MortarBlockReason(Side.Player, selected!.Id)is null, selected?.HasMortar == true ? "✓" : battle.Rules.Mortar.PurchasePrice.ToString());
        _mortar.TooltipText = selected?.HasMortar == true ? $"Mortar installed · minimum range {battle.Rules.Mortar.DeadZone + 1}" : $"Mortar · {battle.Rules.Mortar.PurchasePrice} Thors · {(selected is null ? "" : battle.MortarBlockReason(Side.Player, selected.Id))}";
        _mortar.SetMeta("applicable", selected?.Owner == Side.Player && selected?.IsMothership == true);
        _repair.TooltipText = $"Repair: up to +{battle.Rules.RepairAmount} HP · R";
        _radar.TooltipText = selected?.HasRadar == true ? $"Radar installed · range {selected.RadarRange}" : $"Install radar · {selected?.Definition.RadarPrice} Thors";
        _yard.SetMeta("applicable", selected?.Owner == Side.Player && (selected.IsMothership || fishingBuilder) || village?.Owner == Side.Player);
        _radar.SetMeta("applicable", selected?.Owner == Side.Player && selected?.Definition.Class is ShipClass.Mothership or ShipClass.Kolonel or ShipClass.CannonTower or ShipClass.Lighthouse);
        _bomb.SetMeta("applicable", selected?.Owner == Side.Player && selected?.IsAirborne == true);
        Availability(_bomb, ownShip && battle.CanDropBomb(selected!.Id), selected?.BombCooldown > 0 ? selected.BombCooldown.ToString() : $"{battle.Rules.Balloon.BombDamage}+{battle.Rules.Balloon.SplashDamage}");
        _bomb.TooltipText = selected?.BombCooldown > 0 ? $"Bomb recharging: {selected.BombCooldown} turns" : $"Move, then bomb: {battle.Rules.Balloon.BombDamage} direct + {battle.Rules.Balloon.SplashDamage} to adjacent cells, including allies. Recharges in {battle.Rules.Balloon.CooldownTurns} turns.";
        _loot.SetMeta("applicable", false);
        Availability(_loot, ownShip && battle.CanLootTreasury(Side.Player, selected!.Id), ownShip && battle.CanLootTreasury(Side.Player, selected!.Id) ? "Loot" : "Wait");
        _loot.TooltipText = $"Remain here until next turn. Discoveries: tower {battle.Rules.Treasury.AncientGunWeight}%, {battle.Rules.Treasury.CurrencyReward} Thors {battle.Rules.Treasury.CurrencyWeight}%, ancient Balloon {battle.Rules.Treasury.AncientBalloonWeight}%, {battle.Rules.Treasury.ResourceReward} resources {battle.Rules.Treasury.ResourcesWeight}%, deadly whirlpool {battle.Rules.Treasury.WhirlpoolWeight}%.";
        _radar.Cost = selected?.HasRadar == true ? null : selected?.Definition.RadarPrice;
        _mortar.Cost = selected?.HasMortar == true ? null : battle.MortarPrice;
        UpdateVillage(battle, village, canAct);
        UpdateInformation(battle, selected, village);
        _hasRadial |= _loreText.Length > 0;
        foreach (var(kind, button)in _build)
        {
            var definition = battle.Rules.Get(kind);
            var reason = village is not null ? battle.VillageBuildBlockReason(Side.Player, village.Id, kind) : selected is null ? "Select a Mothership" : battle.BuildBlockReason(Side.Player, selected.Id, kind);
            button.SetMeta("applicable", (village is null || kind != ShipClass.CannonTower)
                && (!fishingBuilder || kind == ShipClass.Lighthouse || kind == ShipClass.CannonTower && battle.Rules.FishingCannonTowers)
                && (kind != ShipClass.Lighthouse || battle.Rules.LighthousesEnabled && (village is null || !battle.Rules.FishingLighthouses)));
            int price = village is null ? battle.BuildPrice(Side.Player, kind) : battle.VillageBuildPrice(village.Id, kind);
            button.Cost = price;
            Availability(button, _hasRadial && reason is null, price.ToString());
            button.TooltipText = $"{definition.Name} · {price} Thors" + (reason is null ? "" : $" · {reason}");
        }

        UpdateCreativeLabel(battle.Creative);
        UpdateGodEyeLabel(battle.FullMapVisible, battle.Winner == Side.Player);
        UpdateActionStories(battle, selected, village, canAct);
        ApplyMenuVisibility();
        Layout();
    }

    public void CloseMenus()
    {
        HideSalvoChoice();
        _productionOpen = false;
        _resourceRoot.Hide();
        _informationPanel.Hide();
    }

    public void HideResource() => _resourceRoot.Hide();
    public void ShowResource(bool dock, int price, bool affordable)
    {
        _productionOpen = false;
        if (_informationBattle is { } battle && _inspectionCell is { } cell)
        {
            var lore = AncientLore.Cell(battle, cell);
            SetLore(lore?.Title ?? "", lore?.Page ?? LorePage.Empty);
        }
        var glyph = _resource.GetChild<ActionGlyph>(0);
        glyph.Symbol = dock ? ActionSymbol.Dock : ActionSymbol.Fishing;
        glyph.QueueRedraw();
        _resource.TooltipText = (dock ? $"Fishing Dock · +{_informationBattle?.Rules.DockResourceReward} resources, +{_informationBattle?.Rules.Get(ShipClass.FishingDock).IncomePerTurn} income" : "Collect resource shoal · +1 resource") + $" · {price} Thors" + (affordable ? "" : " · not enough Thors");
        _resource.Cost = price;
        Availability(_resource, affordable, price.ToString());
        _resourceRoot.Configure(new[] { _resourceInformation, _resource }, true);
        _resourceRoot.Show();
    }

    public void PositionResource(Vector2? tileScreen)
    {
        if (tileScreen is not { } point)
        {
            _resourceRoot.Hide();
            return;
        }

        _resourceRoot.Show();
        _resourceRoot.Position = ClampWorldUi(UiScale.ScreenToUi(point) - SectorButton.Center + new Vector2(0, 20), _resourceRoot.Size);
    }

    private void ApplyMenuVisibility()
    {
        foreach (var button in new[]
        {
            _information,
            _repair,
            _scuttle,
            _yard,
            _radar,
            _mortar,
            _bomb,
            _capture,
            _fortify,
            _port,
            _loot,
            _villageUpgrade
        }

        )
            button.Visible = _hasRadial && !_productionOpen && _mode == OrderMode.None && button.GetMeta("applicable", false).AsBool();
        foreach (var button in _build.Values)
            button.Visible = _hasRadial && _productionOpen && button.GetMeta("applicable", true).AsBool();
        _information.Visible = _hasRadial && _mode == OrderMode.None && _loreText.Length > 0;
        _visibleSectors.Clear();
        foreach (var button in _actionSectors)
            if (button.Visible)
                _visibleSectors.Add(button);
        _radialSlots.Clear();
        _radialSlots.AddRange(_visibleSectors);
        if (_villageUpgrade.Visible && _radialSlots.Remove(_villageUpgrade))
        {
            // An even number of actual actions gets one empty end slot. The town
            // upgrade always occupies the middle wedge directly below its town.
            if (_visibleSectors.Count % 2 == 0) _radialSlots.Add(_upgradeGap);
            _radialSlots.Insert(_radialSlots.Count / 2, _villageUpgrade);
        }
        for (int i = 0; i < _radialSlots.Count; i++)
        {
            _radialSlots[i].SetSector(i, _radialSlots.Count);
        }

        _radial.Configure(_radialSlots, _unfoldActions || _lastSectorCount != _visibleSectors.Count);
        _lastSectorCount = _visibleSectors.Count;
        _unfoldActions = false;
        _radial.Visible = _visibleSectors.Count > 0;
    }

    public void PositionActions(Vector2? shipScreen, float progressOffset = 39)
    {
        if (!_hasRadial || shipScreen is not { } point || _mode != OrderMode.None)
        {
            _radial.Hide();
            return;
        }

        _radial.Visible = _visibleSectors.Count > 0;
        // Keep the ring's center attached to the selected object. Larger objects reserve
        // enough space inside the parchment for their hull and progress cells.
        point = UiScale.ScreenToUi(point);
        progressOffset /= UiScale.Value;
        float scale = Mathf.Clamp((progressOffset + 10) / SectorButton.Inner, 1, 1.9f);
        _radial.Scale = Vector2.One * scale;
        var origin = point;
        var viewport = UiScale.LogicalViewport(this);
        float margin = SectorButton.Outer * scale + 9;
        origin.X = Mathf.Clamp(origin.X, margin, Math.Max(margin, viewport.X - margin));
        origin.Y = Mathf.Clamp(origin.Y, margin, Math.Max(margin, viewport.Y - margin));
        _radial.Position = origin - SectorButton.Center * scale;
    }

    internal void SetActionTargetHitExclusions(IReadOnlyList<Vector2> screenPoints)
    {
        _worldTargetHitPoints.Clear();
        var toLocal = _radial.GetGlobalTransformWithCanvas().AffineInverse();
        foreach (var point in screenPoints)
            _worldTargetHitPoints.Add(toLocal * point);
        float localRadius = 15 / Math.Max(.1f, _radial.Scale.X * UiScale.Value);
        foreach (var command in _actionSectors)
            command.SetWorldTargetHitExclusions(_worldTargetHitPoints, localRadius);
    }

    public void ShowOpponentTurn(Side side = Side.Enemy)
    {
        bool known = _namedBattle?.HasMet(side) == true;
        _nationLabel.Text = known ? $"{_namedBattle!.FactionName(side)}'s turn" : "Other nations are taking their turns";
        StyleNation(known ? Map.FleetPalette.For(_namedBattle!, side) : PapyrusStyle.Paper.Darkened(.14f));
        _banner.Hide();
        _notice.Hide();
        Layout();
    }

    public void ShowPlayerTurn()
    {
        _nationLabel.Text = "Your turn";
        StyleNation(_namedBattle is null ? PapyrusStyle.Paper : Map.FleetPalette.For(_namedBattle, Side.Player));
        Layout();
    }

    public void HideOpponentTurn()
    {
        _bannerTime = 0;
        _banner.Hide();
        _nationPaper.Hide();
    }
    private void StyleNation(Color color)
    {
        var style = PapyrusStyle.Panel();
        style.BgColor = new Color(color, .92f);
        style.ContentMarginTop = style.ContentMarginBottom = 4;
        style.ContentMarginLeft = style.ContentMarginRight = 9;
        _nationPaper.AddThemeStyleboxOverride("panel", style);
        _nationPaper.ResetSize();
        _nationPaper.Show();
    }

    public void ShowMessage(string message)
    {
        if (_namedBattle is { ActiveSide: not Side.Player }) message = "";
        _combatPreview.Hide();
        _message.Show();
        _message.Text = message;
        _noticeTime = message.Length > 0 ? 3.8f : 0;
        _notice.Visible = message.Length > 0;
        Layout();
    }

    public void ShowCombatPreview(double damage, double counter)
    {
        _message.Hide();
        _combatPreview.Show();
        _damagePreview.Text = $"Damage {damage:0.##}";
        _counterPreview.Text = $"−{counter:0}";
        _noticeTime = 3.8f;
        _notice.Show();
        Layout();
    }

    public void ShowTile(BattleState battle, GridPosition? cell)
    {
        if (_inspectionCell != cell)
        {
            _unfoldActions = true;
            if (_informationPanel is not null)
                _informationPanel.Hide();
        }

        _inspectionCell = cell;
        if (cell is not { } p)
        {
            SelectionText = "";
            return;
        }

        SelectionText = battle.Vision.KnownTerrain(Side.Player, p) switch
        {
            TerrainType.Land => "Island",
            TerrainType.Coast => "Coastal water",
            TerrainType.Water => "Sea",
            _ => "Uncharted waters"
        };
        if (battle.TreasuryAt(p)is not null && battle.Vision.IsVisible(Side.Player, p))
            SelectionText += " · Ancient treasury";
        if (battle.IsForbidden(p) && battle.Vision.IsExplored(Side.Player, p))
            SelectionText += " · Whirlpool: impassable";
        if (battle.Vision.State(Side.Player, p) == VisibilityState.RadarContact)
            SelectionText += " · Radar: unknown ship";
    }

    public override void _Process(double delta)
    {
        PositionHeavenlyAssistance();
        if (_noticeTime > 0)
        {
            _noticeTime -= (float)delta;
            if (_noticeTime <= 0)
                _notice.Hide();
        }

        if (_bannerTime > 0)
        {
            _bannerTime -= (float)delta;
            if (_bannerTime <= 0)
                _banner.Hide();
        }

        Layout();
        _nationPaper.Position = new((UiScale.LogicalViewport(this).X - _nationPaper.Size.X) / 2, _metricsPaper.Position.Y + _metricsPaper.Size.Y + 3);
    }

    private void Layout()
    {
        if (_root is null)
            return;
        var size = UiScale.LogicalViewport(this);
        if (_upgradePanel is not null && _upgradeScroll is not null)
            PapyrusModal.Layout(_upgradePanel, _upgradeScroll, _upgradeBody, size);
        if (_menuPanel is not null && _menuScroll is not null)
            PapyrusModal.Layout(_menuPanel, _menuScroll, _menuBody, size);
        var sizes = (size, _metricsPaper.Size, _restart.Size, _end.Size, _banner.Size, _shipCard.Size, _notice.Size, _upgradePanel?.Size ?? Vector2.Zero, _menuPanel?.Size ?? Vector2.Zero);
        if (_layoutSizes == sizes)
            return;
        _layoutSizes = sizes;
        _metricsPaper.Position = new((size.X - _metricsPaper.Size.X) / 2, 12);
        _restart.Position = new(size.X - _restart.Size.X - 18, 16);
        _end.Position = new(size.X - _end.Size.X - 18, size.Y - _end.Size.Y - 18);
        _banner.Position = new((size.X - _banner.Size.X) / 2, (size.Y - _banner.Size.Y) / 2);
        _shipCard.Position = new(18, size.Y - _shipCard.Size.Y - 18);
        float noticeBottom = _shipCard.Visible ? Math.Max(106, _shipCard.Size.Y + 36) : 106;
        _notice.Position = new((size.X - _notice.Size.X) / 2, size.Y - _notice.Size.Y - noticeBottom);
        if (_upgradePanel is not null)
            _upgradePanel.Position = (size - _upgradePanel.Size) / 2;
        if (_menuPanel is not null)
            _menuPanel.Position = (size - _menuPanel.Size) / 2;
        if (_informationPanel is not null)
            LayoutInformation(size);
    }

    private SectorButton IconButton(string name, ActionSymbol symbol, string hint, Action pressed, Control? parent = null)
    {
        var b = new SectorButton
        {
            Name = name,
            Size = new(248, 248),
            FocusMode = Control.FocusModeEnum.None,
            TooltipText = hint
        };
        foreach (var state in new[]
        {
            "normal",
            "hover",
            "pressed",
            "disabled",
            "focus"
        }

        )
            b.AddThemeStyleboxOverride(state, new StyleBoxEmpty());
        var glyph = new ActionGlyph
        {
            Symbol = symbol,
            Size = new(32, 32),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        b.AddChild(glyph);
        var badge = Label("", 12, true);
        badge.Size = new(40, 15);
        b.AddChild(badge);
        _badges[b] = badge;
        b.AttachInk(glyph, badge);
        if (parent is null)
            _actionSectors.Add(b);
        b.Pressed += pressed;
        (parent ?? _radial).AddChild(b);
        return b;
    }

    private void Availability(SectorButton b, bool enabled, string badge)
    {
        b.Disabled = !enabled;
        _badges[b].Text = badge;
        var opacity = new Color(1, 1, 1, enabled ? 1 : .52f);
        b.GetChild<ActionGlyph>(0).Modulate = opacity;
        _badges[b].Modulate = opacity;
        b.QueueRedraw();
    }

    private static Button TextButton(string text, Action pressed)
    {
        var b = new Button
        {
            Text = text,
            CustomMinimumSize = new(112, 48),
            FocusMode = Control.FocusModeEnum.None
        };
        PapyrusStyle.Button(b);
        b.Pressed += pressed;
        return b;
    }

    private static PanelContainer Panel(Control parent)
    {
        var p = new PanelContainer
        {
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        p.AddThemeStyleboxOverride("panel", PapyrusStyle.Panel());
        PapyrusGrain.Apply(p);
        parent.AddChild(p);
        return p;
    }

    private static Label Label(string text, int size, bool centered = false)
    {
        var l = new Label
        {
            Text = text,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            HorizontalAlignment = centered ? HorizontalAlignment.Center : HorizontalAlignment.Left
        };
        l.AddThemeFontSizeOverride("font_size", size);
        l.AddThemeColorOverride("font_color", PapyrusStyle.Ink);
        return l;
    }
}
