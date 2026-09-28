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
    private Control _root=null!, _radial=null!;
    private HBoxContainer _metrics=null!;
    private PanelContainer _shipCard=null!, _notice=null!;
    private Label _coins=null!, _coinCaption=null!, _turn=null!, _ship=null!, _details=null!, _message=null!, _banner=null!;
    private Button _move=null!, _attack=null!, _repair=null!, _yard=null!, _cancel=null!, _end=null!, _restart=null!;
    private readonly Dictionary<ShipClass,Button> _build=new();
    private readonly Dictionary<Button,Label> _badges=new();
    private int? _selectedId;
    private bool _hasRadial, _productionOpen;
    private OrderMode _mode;
    private float _noticeTime, _bannerTime;
    public string SelectionText { get; private set; } = "";
    public string StatusText { get; private set; } = "";
    public string ShipText => _ship.Text;
    public string MessageText => _message.Text;
    public string BannerText => _banner.Visible ? _banner.Text : "";
    public Vector2 MenuPosition => _radial.Position;
    public event Action? MoveRequested, AttackRequested, CancelRequested, EndTurnRequested, RepairRequested, RestartRequested;
    public event Action<ShipClass>? BuildRequested;

    public override void _Ready()
    {
        _root=new Control { MouseFilter=Control.MouseFilterEnum.Ignore }; AddChild(_root);
        _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _metrics=new HBoxContainer { MouseFilter=Control.MouseFilterEnum.Stop };
        _metrics.AddThemeConstantOverride("separation",62); _root.AddChild(_metrics);
        var money=new VBoxContainer(); money.AddThemeConstantOverride("separation",1); _metrics.AddChild(money);
        _coinCaption=Label("Монеты (+25)",15,true); money.AddChild(_coinCaption);
        _coins=Label("120",32,true); money.AddChild(_coins);
        var turns=new VBoxContainer(); turns.AddThemeConstantOverride("separation",1); _metrics.AddChild(turns);
        turns.AddChild(Label("Ход",15,true)); _turn=Label("1",32,true); turns.AddChild(_turn);

        _restart=TextButton("Заново",()=>RestartRequested?.Invoke()); _restart.Name="Restart"; _root.AddChild(_restart);
        _end=TextButton("Завершить ход  →",()=>EndTurnRequested?.Invoke()); _end.Name="EndTurn"; _root.AddChild(_end);
        _end.AddThemeStyleboxOverride("normal",Style(new Color("214858"),14));

        _banner=Label("",24,true); _banner.MouseFilter=Control.MouseFilterEnum.Ignore; _root.AddChild(_banner); _banner.Hide();
        _shipCard=new PanelContainer { MouseFilter=Control.MouseFilterEnum.Stop };
        _shipCard.AddThemeStyleboxOverride("panel",Style(new Color("102b38"),12)); _root.AddChild(_shipCard);
        var stats=new VBoxContainer(); stats.AddThemeConstantOverride("separation",4); _shipCard.AddChild(stats);
        _ship=Label("",18); stats.AddChild(_ship); _details=Label("",14); stats.AddChild(_details);
        _notice=new PanelContainer { MouseFilter=Control.MouseFilterEnum.Ignore };
        _notice.AddThemeStyleboxOverride("panel",Style(new Color("102b38"),10)); _root.AddChild(_notice);
        _message=Label("",16,true); _notice.AddChild(_message); _notice.Hide();

        _radial=new Control { MouseFilter=Control.MouseFilterEnum.Ignore, Size=new Vector2(240,220) }; _root.AddChild(_radial);
        _move=IconButton("ActionMove",ActionSymbol.Move,new(-78,-26),"Движение",()=>MoveRequested?.Invoke());
        _attack=IconButton("ActionAttack",ActionSymbol.Attack,new(0,-82),"Атака",()=>AttackRequested?.Invoke());
        _repair=IconButton("ActionRepair",ActionSymbol.Repair,new(78,-26),"Ремонт",()=>RepairRequested?.Invoke());
        _yard=IconButton("ActionBuild",ActionSymbol.Build,new(66,53),"Построить корабль",()=> { _productionOpen=true; ApplyMenuVisibility(); });
        _cancel=IconButton("ActionClose",ActionSymbol.Close,new(-66,53),"Снять выбор / отменить режим",()=> {
            if(_productionOpen) { _productionOpen=false; ApplyMenuVisibility(); } else CancelRequested?.Invoke();
        });
        var classes=new[] { ShipClass.Fishing,ShipClass.Garrison,ShipClass.Invader,ShipClass.Kolonel };
        var symbols=new[] { ActionSymbol.Fishing,ActionSymbol.Scout,ActionSymbol.Standard,ActionSymbol.Heavy };
        var positions=new[] { new Vector2(-78,-26),new Vector2(0,-82),new Vector2(78,-26),new Vector2(66,53) };
        for(int i=0;i<classes.Length;i++)
        {
            var kind=classes[i]; _build[kind]=IconButton("Build"+kind,symbols[i],positions[i],kind.ToString(),()=>BuildRequested?.Invoke(kind));
        }
        _radial.Hide(); _shipCard.Hide();
        Layout();
    }

    public void UpdateBattle(BattleState battle,Ship? selected,bool busy,OrderMode mode)
    {
        if(_selectedId!=selected?.Id || busy || mode!=OrderMode.None) _productionOpen=false;
        _selectedId=selected?.Id; _mode=mode;
        _coinCaption.Text=$"Монеты (+{battle.Income(Side.Player)})";
        _coins.Text=battle.Credits(Side.Player).ToString(); _turn.Text=battle.Round.ToString();
        StatusText=battle.IsOver ? battle.Winner==Side.Player?"ПОБЕДА":"ПОРАЖЕНИЕ" : $"Ход {battle.Round}";
        if(battle.IsOver)
        {
            _banner.Text=StatusText; _bannerTime=-1; _banner.Show();
        }
        else if(_bannerTime<0) { _bannerTime=0; _banner.Hide(); }
        bool canAct=!busy&&!battle.IsOver&&battle.ActiveSide==Side.Player;
        _end.Disabled=!canAct; _restart.Disabled=busy;
        _shipCard.Visible=selected is not null;
        if(selected is not null)
        {
            _ship.Text=$"{selected.Definition.Name}{(selected.IsVeteran?" ★ ВЕТЕРАН":"")}   {selected.Health:0.##}/{selected.MaxHealth:0.##} HP";
            _details.Text=$"Урон {selected.CurrentDamage:0.##} · Броня {selected.Definition.Armor} · Обзор {selected.Definition.VisualRange} · Радар {selected.Definition.RadarRange}";
            _shipCard.TooltipText=$"Победы: {selected.Kills}/3 · Берег ×{selected.Definition.CoastMovementCost}" +
                (selected.HealthRatio<0.25?" · Повреждение: движение −1":"")+
                (selected.HasProduced?" · После постройки движение закрыто":"");
        }
        _hasRadial=canAct&&selected?.Owner==Side.Player;
        Availability(_move,_hasRadial&&selected!.CanMove,selected?.CanMove==true?selected.MovementRemaining.ToString("0.#"):"0");
        Availability(_attack,_hasRadial&&selected!.AttacksRemaining>0,(selected?.AttacksRemaining??0).ToString());
        Availability(_repair,_hasRadial&&selected!.CanRepair,"+");
        Availability(_yard,_hasRadial&&selected!.Definition.Class==ShipClass.Mothership&&!selected.HasProduced,"");
        _repair.TooltipText=$"Ремонт: до +{battle.Rules.RepairAmount} HP вместо движения и атак";
        _move.TooltipText=selected?.HasProduced==true?"Движение закрыто после постройки":"Движение: выберите клетку";
        _attack.TooltipText=selected?.IsArmed==false?"У корабля нет оружия":"Атака: выберите видимую цель";
        foreach(var (kind,button) in _build)
        {
            var definition=battle.Rules.Get(kind);
            var reason=selected is null?"Выберите Mothership":battle.BuildBlockReason(Side.Player,selected.Id,kind);
            Availability(button,_hasRadial&&reason is null,definition.Price.ToString());
            button.TooltipText=$"{definition.Name} · {definition.Price} монет"+(reason is null?"":$" · {reason}");
        }
        _yard.SetMeta("mother",selected?.Definition.Class==ShipClass.Mothership);
        ApplyMenuVisibility(); Layout();
    }

    private void ApplyMenuVisibility()
    {
        _move.Visible=_attack.Visible=_repair.Visible=_hasRadial&&!_productionOpen&&_mode==OrderMode.None;
        _yard.Visible=_hasRadial&&!_productionOpen&&_mode==OrderMode.None&&_yard.GetMeta("mother",false).AsBool();
        foreach(var button in _build.Values) button.Visible=_hasRadial&&_productionOpen;
        _cancel.Visible=_hasRadial;
        _radial.Visible=_hasRadial;
    }

    public void PositionActions(Vector2? shipScreen)
    {
        if(!_hasRadial||shipScreen is not { } point) { _radial.Hide(); return; }
        var size=GetViewport().GetVisibleRect().Size;
        _radial.Visible=new Rect2(-30,-30,size.X+60,size.Y+60).HasPoint(point);
        // Keep every small button reachable when the selected ship approaches the edge.
        var center=new Vector2(Math.Clamp(point.X,118,size.X-118),Math.Clamp(point.Y,174,size.Y-156));
        _radial.Position=center-new Vector2(120,110);
    }

    public void ShowOpponentTurn()
    {
        _banner.Text="Ходит: Капитан Анат"; _bannerTime=2.4f; _banner.Show(); Layout();
    }
    public void HideOpponentTurn() { _bannerTime=0; _banner.Hide(); }
    public void ShowMessage(string message)
    {
        _message.Text=message; _noticeTime=message.Length>0?3.8f:0; _notice.Visible=message.Length>0; Layout();
    }
    public void ShowTile(BattleState battle,GridPosition? cell)
    {
        // Terrain inspection stays available internally, with no coordinate/debug overlay.
        if(cell is not { } p) { SelectionText=""; return; }
        SelectionText=battle.Vision.KnownTerrain(Side.Player,p) switch
        { TerrainType.Land=>"Остров",TerrainType.Coast=>"Береговая вода",TerrainType.Water=>"Море",_=>"Неизведанные воды" };
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
        _metrics.Position=new((size.X-_metrics.Size.X)/2,16);
        _restart.Position=new(size.X-_restart.Size.X-18,16);
        _end.Position=new(size.X-_end.Size.X-18,size.Y-_end.Size.Y-18);
        _banner.Position=new((size.X-_banner.Size.X)/2,98);
        _shipCard.Position=new(18,size.Y-_shipCard.Size.Y-18);
        _notice.Position=new((size.X-_notice.Size.X)/2,size.Y-_notice.Size.Y-106);
    }

    private Button IconButton(string name,ActionSymbol symbol,Vector2 offset,string hint,Action pressed)
    {
        var b=new Button { Name=name,Size=new Vector2(50,50),CustomMinimumSize=new Vector2(50,50),
            Position=new Vector2(120,110)+offset-new Vector2(25,25),FocusMode=Control.FocusModeEnum.None,TooltipText=hint };
        b.AddThemeStyleboxOverride("normal",Style(new Color("173b49"),25,1));
        b.AddThemeStyleboxOverride("hover",Style(new Color("326778"),25,2));
        b.AddThemeStyleboxOverride("pressed",Style(new Color("418196"),25,2));
        b.AddThemeStyleboxOverride("disabled",Style(new Color("122c37"),25,1));
        var glyph=new ActionGlyph { Symbol=symbol,Position=new Vector2(9,5),Size=new Vector2(32,32),MouseFilter=Control.MouseFilterEnum.Ignore };
        b.AddChild(glyph);
        var badge=Label("",12,true); badge.Position=new(5,33); badge.Size=new(40,15); b.AddChild(badge); _badges[b]=badge;
        b.Pressed+=pressed; _radial.AddChild(b); return b;
    }
    private void Availability(Button b,bool enabled,string badge)
    {
        b.Disabled=!enabled; _badges[b].Text=badge;
        foreach(var child in b.GetChildren().OfType<Control>()) child.Modulate=new Color(1,1,1,enabled?1:0.3f);
    }
    private static Button TextButton(string text,Action pressed)
    {
        var b=new Button { Text=text,CustomMinimumSize=new(112,48),FocusMode=Control.FocusModeEnum.None };
        b.AddThemeFontSizeOverride("font_size",17);
        b.AddThemeStyleboxOverride("normal",Style(new Color("132b36"),12));
        b.AddThemeStyleboxOverride("hover",Style(new Color("26505e"),12));
        b.AddThemeStyleboxOverride("pressed",Style(new Color("376b7a"),12));
        b.Pressed+=pressed; return b;
    }
    private static Label Label(string text,int size,bool centered=false)
    {
        var l=new Label { Text=text,MouseFilter=Control.MouseFilterEnum.Ignore,HorizontalAlignment=centered?HorizontalAlignment.Center:HorizontalAlignment.Left };
        l.AddThemeFontSizeOverride("font_size",size); l.AddThemeColorOverride("font_color",new Color("deedf0")); return l;
    }
    private static StyleBoxFlat Style(Color color,int radius,int border=0)=>new()
    {
        BgColor=color,ContentMarginLeft=14,ContentMarginRight=14,ContentMarginTop=9,ContentMarginBottom=9,
        CornerRadiusTopLeft=radius,CornerRadiusTopRight=radius,CornerRadiusBottomLeft=radius,CornerRadiusBottomRight=radius,
        BorderWidthLeft=border,BorderWidthRight=border,BorderWidthTop=border,BorderWidthBottom=border,BorderColor=new Color("6bafbd")
    };
}
