using System;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Units;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation.UI;

// A bounded native sheet keeps all slot choices readable on touch screens.
public partial class CityInfrastructureHud : CanvasLayer
{
    private Control _root = null!;
    private PanelContainer _paper = null!;
    private VBoxContainer _body = null!;
    private Button _close = null!;
    private ScrollContainer _scroll = null!;
    public event Action<int, CityFacility>? ConfigureRequested;
    internal bool IsOpen => _root?.Visible == true;
    private static string T(string text) => Language.Translate(text);
    public override void _Ready()
    {
        Layer = 35;
        _root = new Control { Name = "InfrastructureOverlay", MouseFilter = Control.MouseFilterEnum.Stop,
            MouseForcePassScrollEvents = false };
        AddChild(_root);
        _paper = new PanelContainer { Name = "InfrastructurePaper", MouseForcePassScrollEvents = false };
        _paper.AddThemeStyleboxOverride("panel", PapyrusStyle.Panel());
        _root.AddChild(_paper);
        _body = new VBoxContainer();
        _body.AddThemeConstantOverride("separation", 9);
        _close = new Button { Name = "InfrastructureClose", Text = T("Close"), CustomMinimumSize = new(0, 42) };
        PapyrusStyle.Button(_close);
        _close.Pressed += Close;
        _scroll = PapyrusModal.WrapWithFooter(_paper, _body, _close, "InfrastructureScroll");
        UiScale.Bind(this, _root, Layout);
        Close();
    }
    private Label Text(string value, int size = 14)
    {
        var label = new Label { Text = value, AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", PapyrusStyle.Ink);
        return label;
    }
    internal static string FacilityName(CityFacility kind) => kind switch
    {
        CityFacility.TradeYard => "Trade yard", CityFacility.RepairDock => "Repair dock",
        CityFacility.NavalArsenal => "Naval arsenal", _ => "Empty"
    };
    internal void ShowCity(BattleState battle, Village town)
    {
        _close.Text = T("Close");
        foreach (var child in _body.GetChildren()) { _body.RemoveChild(child); child.QueueFree(); }
        _body.AddChild(Text(T("City infrastructure"), 21));
        _body.AddChild(Text(town.Name));
        _body.AddChild(Text(T("One facility per slot. Replacing a facility costs extra. At level 5, improve one installed facility.")));
        _body.AddChild(Text(T("Trade needs a connected port. Dock service consumes the ship's actions and one city work turn.")));
        _body.AddChild(Text(string.Format(T("Trade +{0} income · Dock +{1} HP · Arsenal +{2} capacity; improvement doubles one bonus."),
            battle.Rules.Infrastructure.TradeIncome, battle.Rules.Infrastructure.DockRepair, battle.Rules.Infrastructure.ArsenalCapacity)));
        for (int slot = 0; slot < 2; slot++)
        {
            int index = slot;
            bool locked = slot >= battle.CityFacilitySlots(town);
            var installed = BattleState.CityFacilityAt(town, slot);
            _body.AddChild(Text(string.Format(T("Slot {0}: {1}"), slot + 1,
                locked ? string.Format(T("Unlocks at level {0}"), slot == 0 ? 2 : 4)
                    : T(FacilityName(installed)) + (town.ImprovedFacility == installed && installed != CityFacility.None ? " ★" : "")), 16));
            if (locked) continue;
            foreach (var kind in new[] { CityFacility.TradeYard, CityFacility.RepairDock, CityFacility.NavalArsenal })
            {
                var selectedKind = kind;
                bool improve = kind == installed;
                int price = battle.CityFacilityPrice(Side.Player, town.Id, slot, kind);
                string? reason = battle.CityFacilityBlockReason(Side.Player, town.Id, slot, kind);
                string verb = improve ? "Improve" : installed == CityFacility.None ? "Install" : "Replace with";
                var button = new Button { Name = $"Facility_{slot}_{kind}",
                    Text = T(verb) + " · " + T(FacilityName(kind)) + $" · {price} " + T("Thors"),
                    Disabled = reason is not null, CustomMinimumSize = new(0, 44),
                    TooltipText = reason is null ? "" : T(reason) };
                PapyrusStyle.Button(button, 14);
                button.Pressed += () => { Close(); ConfigureRequested?.Invoke(index, selectedKind); };
                _body.AddChild(button);
            }
        }
        _root.Show();
        Layout();
    }
    internal void Close() { if (_root is not null) _root.Hide(); }
    private void Layout()
    {
        if (_root is null || _paper is null) return;
        var size = UiScale.LogicalViewport(_root);
        _root.Size = size;
        PapyrusModal.LayoutWithFooter(_paper, _scroll, _body, _close, size, 430, .83f);
    }
    public override void _UnhandledInput(InputEvent input)
    {
        if (IsOpen && input.IsActionPressed("ui_cancel")) { Close(); GetViewport().SetInputAsHandled(); }
    }
}

