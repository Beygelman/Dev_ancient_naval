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
    private Control _root=null!, _radial=null!, _upgradeOverlay=null!;
    private Control _root=null!, _radial=null!, _upgradeOverlay=null!, _resourceRoot=null!;
    private HBoxContainer _metrics=null!;
    private PanelContainer _shipCard=null!, _notice=null!, _upgradePanel=null!;
    private Label _coins=null!, _coinCaption=null!, _turn=null!, _ship=null!, _details=null!, _message=null!, _banner=null!, _upgradeTitle=null!;
    private SectorButton _repair=null!, _yard=null!, _collect=null!, _radar=null!, _mortar=null!, _dock=null!;
    private SectorButton _repair=null!, _yard=null!, _radar=null!, _mortar=null!, _resource=null!;
    private Button _end=null!, _restart=null!;
    private readonly Dictionary<ShipClass,SectorButton> _build=new();
    private readonly Dictionary<SectorButton,Label> _badges=new();
    private readonly Dictionary<UpgradeChoice,Button> _choices=new();
    private int? _selectedId;
    private bool _hasRadial, _productionOpen;
    private OrderMode _mode;
    private float _noticeTime, _bannerTime;
    public string SelectionText { get; private set; } = "";
    public string StatusText { get; private set; } = "";
    public string ShipText => _ship.Text;
    public string MessageText => _message.Text;
    public string BannerText => _banner.Visible?_banner.Text:"";
    public Vector2 MenuPosition => _radial.Position;
    public bool UpgradeVisible => _upgradeOverlay.Visible;
    public event Action? EndTurnRequested,RepairRequested,RestartRequested,CollectRequested,RadarRequested,MortarRequested,DockRequested;
    public event Action? EndTurnRequested,RepairRequested,RestartRequested,ResourceRequested,RadarRequested,MortarRequested;
    public event Action<ShipClass>? BuildRequested;
    public event Action<UpgradeChoice>? UpgradeRequested;

    public override void _Ready()
    {
        _root=new Control { MouseFilter=Control.MouseFilterEnum.Ignore }; AddChild(_root);
        _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _metrics=new HBoxContainer { MouseFilter=Control.MouseFilterEnum.Stop };
        _metrics.AddThemeConstantOverride("separation",62); _root.AddChild(_metrics);
        var money=new VBoxContainer(); money.AddThemeConstantOverride("separation",1); _metrics.AddChild(money);
        _coinCaption=Label("Thors (+4)",15,true); money.AddChild(_coinCaption);
        _coins=Label("5",32,true); money.AddChild(_coins);
        var turns=new VBoxContainer(); turns.AddThemeConstantOverride("separation",1); _metrics.AddChild(turns);
        turns.AddChild(Label("Ход",15,true)); _turn=Label("1",32,true); turns.AddChild(_turn);
        _restart=TextButton("",()=>SetMenuVisible(true)); _restart.Name="Menu"; _restart.TooltipText="Меню"; _restart.CustomMinimumSize=new(48,48); _restart.Size=new(48,48); _root.AddChild(_restart);
        _restart.AddChild(new ActionGlyph { Symbol=ActionSymbol.Menu,Position=new(10,10),Size=new(28,28),MouseFilter=Control.MouseFilterEnum.Ignore });
        _end=TextButton("Завершить ход  →",()=>EndTurnRequested?.Invoke()); _end.Name="EndTurn"; _root.AddChild(_end);
        _banner=Label("",24,true); _root.AddChild(_banner); _banner.Hide();
        _shipCard=Panel(_root); var stats=new VBoxContainer(); stats.AddThemeConstantOverride("separation",4); _shipCard.AddChild(stats);
        _ship=Label("",18); stats.AddChild(_ship); _details=Label("",14); stats.AddChild(_details);
        _notice=Panel(_root); _notice.MouseFilter=Control.MouseFilterEnum.Ignore;
        _message=Label("",16,true); _notice.AddChild(_message); _notice.Hide();
        _radial=new Control { MouseFilter=Control.MouseFilterEnum.Ignore,Size=new(136,136) }; _root.AddChild(_radial);
        _repair=IconButton("ActionRepair",ActionSymbol.Repair,"Ремонт",()=>RepairRequested?.Invoke());
        _collect=IconButton("ActionCollect",ActionSymbol.Fishing,"Собрать рыбу за 2 Thors",()=>CollectRequested?.Invoke());
        _radar=IconButton("ActionRadar",ActionSymbol.Radar,"Купить радар",()=>RadarRequested?.Invoke());
        _mortar=IconButton("ActionMortar",ActionSymbol.Mortar,"Мортира",()=>MortarRequested?.Invoke());
        _dock=IconButton("ActionDock",ActionSymbol.Dock,"Рыбный док",()=>DockRequested?.Invoke());
        _yard=IconButton("ActionBuild",ActionSymbol.Build,"Верфь",()=> { _productionOpen=true; ApplyMenuVisibility(); });
        foreach(var (kind,symbol) in new[] { (ShipClass.Fishing,ActionSymbol.Fishing),(ShipClass.Garrison,ActionSymbol.Scout),
            (ShipClass.Invader,ActionSymbol.Standard),(ShipClass.Kolonel,ActionSymbol.Heavy),(ShipClass.Togus,ActionSymbol.Mortar) })
            _build[kind]=IconButton("Build"+kind,symbol,kind.ToString(),()=>BuildRequested?.Invoke(kind));

        _resourceRoot=new Control { MouseFilter=Control.MouseFilterEnum.Ignore,Size=new(136,136) }; _root.AddChild(_resourceRoot);
        _resource=IconButton("TileResource",ActionSymbol.Fishing,"",()=>ResourceRequested?.Invoke(),_resourceRoot);
        _resource.SetSector(0,1); _resource.GetChild<ActionGlyph>(0).Position=_resource.IconCenter-new Vector2(14,14);
        _badges[_resource].Position=new Vector2(48,-14); _resourceRoot.Hide();
        _upgradeOverlay=new Control { MouseFilter=Control.MouseFilterEnum.Stop }; _root.AddChild(_upgradeOverlay);
        _upgradeOverlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var shade=new ColorRect { Color=new Color(0.01f,0.035f,0.05f,0.5f),MouseFilter=Control.MouseFilterEnum.Stop };
        _upgradeOverlay.AddChild(shade); shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _upgradePanel=Panel(_upgradeOverlay); _upgradePanel.CustomMinimumSize=new(540,0);
        var column=new VBoxContainer(); column.AddThemeConstantOverride("separation",18); _upgradePanel.AddChild(column);
        _upgradeTitle=Label("",25,true); column.AddChild(_upgradeTitle);
        column.AddChild(Label("Выберите одно улучшение",17,true));
        foreach(var (choice,text) in new[] {
            (UpgradeChoice.Income,"+1 Thor к доходу за ход"),(UpgradeChoice.Mobility,"+1 клетка движения"),
            (UpgradeChoice.SecondAttack,"Вторая атака Mothership"),(UpgradeChoice.Balloon,"Воздушный шар · обзор 4 · движение 3"),
            (UpgradeChoice.Fortification,"+5 HP и +3 к урону контратаки"),(UpgradeChoice.Shipwright,"−25% стоимости кораблей") })
        {
            var button=TextButton(text,()=>UpgradeRequested?.Invoke(choice)); button.Name="Upgrade"+choice; column.AddChild(button); _choices[choice]=button;
        }
        BuildGameMenu(); _upgradeOverlay.Hide(); _radial.Hide(); _shipCard.Hide(); Layout();
    }

    public void UpdateBattle(BattleState battle,Ship? selected,bool busy,OrderMode mode)
    {
        if(_selectedId!=selected?.Id||busy||mode!=OrderMode.None) _productionOpen=false;
        if(_selectedId!=selected?.Id||busy||MenuVisible) _resourceRoot.Hide();
        _selectedId=selected?.Id; _mode=mode;
        _coinCaption.Text=$"{(battle.Credits(Side.Player)==1?"Thor":"Thors")} (+{battle.Income(Side.Player)})";
        _coins.Text=battle.Credits(Side.Player).ToString(); _turn.Text=battle.Round.ToString();
        StatusText=battle.IsOver?(battle.Winner==Side.Player?"ПОБЕДА":"ПОРАЖЕНИЕ"):$"Ход {battle.Round}";
        if(battle.IsOver) { _banner.Text=StatusText; _bannerTime=-1; _banner.Show(); }
        else if(_bannerTime<0) { _bannerTime=0; _banner.Hide(); }
        var pending=battle.PendingUpgrade(Side.Player);
        _upgradeOverlay.Visible=pending is not null&&!busy&&!battle.IsOver;
        if(pending is not null)
        {
            _upgradeTitle.Text=$"Mothership · уровень {pending.Level}";
            foreach(var (choice,button) in _choices) { button.Visible=battle.UpgradeOptions(pending.Id).Contains(choice); button.Disabled=busy; }
            _upgradePanel.ResetSize();
        }
        bool canAct=!busy&&!battle.IsOver&&battle.ActiveSide==Side.Player&&pending is null&&!MenuVisible;
        _end.Disabled=!canAct; _restart.Disabled=busy;
        _shipCard.Visible=selected is not null;
        if(selected is not null)
        {
            _ship.Text=selected.IsAirborne?"Воздушный шар":$"{selected.Definition.Name}{(selected.IsMothership?$" · ур. {selected.Level}":selected.IsVeteran?" ★ ВЕТЕРАН":"")}   {selected.Health:0}/{selected.MaxHealth:0} HP";
            _details.Text=selected.IsAirborne?$"Обзор {selected.VisualRange} · Ход {selected.MovementRemaining:0}/{selected.MovementAllowance} · неуязвим":$"Урон {selected.CurrentDamage:0} · Огонь {selected.Definition.AttackRange} · Обзор {selected.VisualRange} · Радар {selected.RadarRange}";
            if(selected.HasMortar) _details.Text+=$" · Мортира 4–{selected.MortarRange}: {selected.CurrentMortarDamage:0}";
            if(selected.IsStructure) _details.Text="Доход +1 · неподвижный рыбный док";
            _shipCard.TooltipText=selected.IsMothership?$"Ресурсы: {selected.Resources}/{selected.ResourcesRequired}":$"Потоплено: {selected.Kills}/3";
        }
        _hasRadial=canAct&&selected?.Owner==Side.Player&&!selected.IsAirborne&&!selected.IsStructure;
        Availability(_repair,_hasRadial&&selected!.CanRepair,"");
        Availability(_yard,_hasRadial&&selected!.IsMothership&&!selected.HasProduced,"");
        Availability(_collect,_hasRadial&&battle.CollectionCells(selected!.Id).Count>0&&battle.Credits(Side.Player)>=battle.CollectionCost(Side.Player),battle.CollectionCost(Side.Player).ToString());
        Availability(_radar,_hasRadial&&battle.RadarBlockReason(Side.Player,selected!.Id) is null,selected?.HasRadar==true?"✓":"2");
        Availability(_mortar,_hasRadial&&battle.MortarBlockReason(Side.Player,selected!.Id) is null,selected?.HasMortar==true?"✓":"10");
        Availability(_dock,_hasRadial&&battle.DockCells(selected!.Id).Count>0&&battle.Credits(Side.Player)>=battle.DockPrice(Side.Player),battle.DockPrice(Side.Player).ToString());
        _mortar.TooltipText=selected?.HasMortar==true?"Мортира установлена · мёртвая зона 3":$"Мортира · 10 Thors · { (selected is null?"":battle.MortarBlockReason(Side.Player,selected.Id)) }";
        _dock.TooltipText=$"Рыбный док · {battle.DockPrice(Side.Player)} Thors · +2 ресурса, +1 доход";
        _mortar.SetMeta("applicable",selected?.IsMothership==true); _dock.SetMeta("applicable",selected?.Definition.CollectionRange>0);
        _mortar.SetMeta("applicable",selected?.IsMothership==true);
        _repair.TooltipText=$"Ремонт: до +{battle.Rules.RepairAmount} HP";
        _radar.TooltipText=selected?.HasRadar==true?$"Радар установлен · радиус {selected.RadarRange}":"Купить радар · 2 Thors";
        _collect.TooltipText=$"Собрать рыбную клетку · {battle.CollectionCost(Side.Player)} Thors → 1 ресурс Mothership";
        _yard.SetMeta("applicable",selected?.IsMothership==true);
        _collect.SetMeta("applicable",selected?.Definition.CollectionRange>0);
        _radar.SetMeta("applicable",selected?.Definition.Class is ShipClass.Mothership or ShipClass.Kolonel);
        foreach(var (kind,button) in _build)
        {
            var definition=battle.Rules.Get(kind); var reason=selected is null?"Выберите Mothership":battle.BuildBlockReason(Side.Player,selected.Id,kind);
            Availability(button,_hasRadial&&reason is null,battle.BuildPrice(Side.Player,kind).ToString());
            button.TooltipText=$"{definition.Name} · {battle.BuildPrice(Side.Player,kind)} Thors"+(reason is null?"":$" · {reason}");
        }
        UpdateCreativeLabel(battle.Creative); ApplyMenuVisibility(); Layout();
    }
    public void CloseMenus() { _productionOpen=false; }
    public void CloseMenus() { _productionOpen=false; _resourceRoot.Hide(); }
    public void HideResource()=>_resourceRoot.Hide();
    public void ShowResource(bool dock,int price,bool affordable)
    {
        _productionOpen=false;
        var glyph=_resource.GetChild<ActionGlyph>(0); glyph.Symbol=dock?ActionSymbol.Dock:ActionSymbol.Fishing; glyph.QueueRedraw();
        _resource.TooltipText=(dock?"Рыбный док · +2 ресурса, +1 доход":"Собрать рыбу · +1 ресурс")+$" · {price} Thors"+(affordable?"":" · недостаточно Thors");
        Availability(_resource,affordable,price.ToString()); _resourceRoot.Show();
    }
    public void PositionResource(Vector2? tileScreen)
    {
        if(tileScreen is not { } point) { _resourceRoot.Hide(); return; }
        _resourceRoot.Position=point-SectorButton.Center;
    }
    private void ApplyMenuVisibility()
    {
        _repair.Visible=_hasRadial&&!_productionOpen&&_mode==OrderMode.None;
        foreach(var button in new[] { _yard,_collect,_radar,_mortar,_dock })
        foreach(var button in new[] { _yard,_radar,_mortar })
            button.Visible=_hasRadial&&!_productionOpen&&_mode==OrderMode.None&&button.GetMeta("applicable",false).AsBool();
        foreach(var button in _build.Values) button.Visible=_hasRadial&&_productionOpen;
        var sectors=_radial.GetChildren().OfType<SectorButton>().Where(b=>b.Visible).ToArray();
        for(int i=0;i<sectors.Length;i++)
        {
            var b=sectors[i]; b.SetSector(i,sectors.Length);
            b.GetChild<ActionGlyph>(0).Position=b.IconCenter-new Vector2(14,14);
            _badges[b].Position=SectorButton.Center+Vector2.FromAngle(b.CenterAngle)*73+new Vector2(-20,-7);
        }
        _radial.Visible=sectors.Length>0;
    }
    public void PositionActions(Vector2? shipScreen)
    {
        if(!_hasRadial||shipScreen is not { } point||_mode!=OrderMode.None) { _radial.Hide(); return; }
        var size=GetViewport().GetVisibleRect().Size;
        _radial.Visible=new Rect2(-20,-20,size.X+40,size.Y+40).HasPoint(point);
        var center=new Vector2(Math.Clamp(point.X,78,size.X-78),Math.Clamp(point.Y,152,size.Y-116));
        _radial.Position=center-SectorButton.Center;
    }
    public void ShowOpponentTurn() { _banner.Text="Ходит: Капитан Анат"; _bannerTime=2.4f; _banner.Show(); Layout(); }
    public void HideOpponentTurn() { _bannerTime=0; _banner.Hide(); }
    public void ShowMessage(string message) { _message.Text=message; _noticeTime=message.Length>0?3.8f:0; _notice.Visible=message.Length>0; Layout(); }
    public void ShowTile(BattleState battle,GridPosition? cell)
    {
        if(cell is not { } p) { SelectionText=""; return; }
        SelectionText=battle.Vision.KnownTerrain(Side.Player,p) switch { TerrainType.Land=>"Остров",TerrainType.Coast=>"Береговая вода",TerrainType.Water=>"Море",_=>"Неизведанные воды" };
        if(battle.Vision.State(Side.Player,p)==VisibilityState.RadarContact) SelectionText+=" · Радар: неизвестный корабль";
    }
    public override void _Process(double delta)
    {
        if(_noticeTime>0) { _noticeTime-=(float)delta; if(_noticeTime<=0) _notice.Hide(); }
        if(_bannerTime>0) { _bannerTime-=(float)delta; if(_bannerTime<=0) _banner.Hide(); }
        Layout();
    }
    private void Layout()
    {
        if(_root is null) return;
        var size=GetViewport().GetVisibleRect().Size;
        _metrics.Position=new((size.X-_metrics.Size.X)/2,16); _restart.Position=new(size.X-_restart.Size.X-18,16);
        _end.Position=new(size.X-_end.Size.X-18,size.Y-_end.Size.Y-18);
        _banner.Position=new((size.X-_banner.Size.X)/2,98); _shipCard.Position=new(18,size.Y-_shipCard.Size.Y-18);
        _notice.Position=new((size.X-_notice.Size.X)/2,size.Y-_notice.Size.Y-106);
        if(_upgradePanel is not null) _upgradePanel.Position=(size-_upgradePanel.Size)/2;
        if(_menuPanel is not null) _menuPanel.Position=(size-_menuPanel.Size)/2;
    }
    private SectorButton IconButton(string name,ActionSymbol symbol,string hint,Action pressed)
    private SectorButton IconButton(string name,ActionSymbol symbol,string hint,Action pressed,Control? parent=null)
    {
        var b=new SectorButton { Name=name,Size=new(136,136),FocusMode=Control.FocusModeEnum.None,TooltipText=hint };
        foreach(var state in new[] { "normal","hover","pressed","disabled","focus" }) b.AddThemeStyleboxOverride(state,new StyleBoxEmpty());
        var glyph=new ActionGlyph { Symbol=symbol,Size=new(28,28),MouseFilter=Control.MouseFilterEnum.Ignore }; b.AddChild(glyph);
        var badge=Label("",12,true); badge.Size=new(40,15); b.AddChild(badge); _badges[b]=badge;
        b.Pressed+=pressed; _radial.AddChild(b); return b;
        b.Pressed+=pressed; (parent??_radial).AddChild(b); return b;
    }
    private void Availability(SectorButton b,bool enabled,string badge)
    {
        b.Disabled=!enabled; _badges[b].Text=badge;
        foreach(var child in b.GetChildren().OfType<Control>()) child.Modulate=new Color(1,1,1,enabled?1:0.32f);
        b.QueueRedraw();
    }
    private static Button TextButton(string text,Action pressed)
    {
        var b=new Button { Text=text,CustomMinimumSize=new(112,48),FocusMode=Control.FocusModeEnum.None };
        b.AddThemeFontSizeOverride("font_size",17);
        foreach(var state in new[] { "normal","hover","pressed","disabled" })
            b.AddThemeStyleboxOverride(state,Style(new Color(0.08f,0.23f,0.29f,state=="hover"?0.82f:0.62f)));
        b.Pressed+=pressed; return b;
    }
    private static PanelContainer Panel(Control parent)
    {
        var p=new PanelContainer { MouseFilter=Control.MouseFilterEnum.Stop }; p.AddThemeStyleboxOverride("panel",Style(new Color(0.055f,0.16f,0.21f,0.7f))); parent.AddChild(p); return p;
    }
    private static Label Label(string text,int size,bool centered=false)
    {
        var l=new Label { Text=text,MouseFilter=Control.MouseFilterEnum.Ignore,HorizontalAlignment=centered?HorizontalAlignment.Center:HorizontalAlignment.Left };
        l.AddThemeFontSizeOverride("font_size",size); l.AddThemeColorOverride("font_color",new Color("deedf0")); return l;
    }
    private static StyleBoxFlat Style(Color color)=>new() { BgColor=color,ContentMarginLeft=16,ContentMarginRight=16,ContentMarginTop=12,ContentMarginBottom=12,
        CornerRadiusTopLeft=12,CornerRadiusTopRight=12,CornerRadiusBottomLeft=12,CornerRadiusBottomRight=12 };
}
