using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.Vision;
using System.Linq;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation.UI;

public partial class DebugHud
{
    private PanelContainer _informationPanel = null!;
    private Label _informationTitle = null!;
    private ScrollContainer _informationScroll = null!;
    private VBoxContainer _informationBody = null!;
    private SectorButton _information = null!;
    private GridPosition? _inspectionCell;
    private BattleState? _informationBattle;
    private LorePage _lorePage = LorePage.Empty;
    private string _loreTitle = "", _loreText = "", _renderedLore = "";
    public bool InformationVisible => _informationPanel?.Visible == true;
    public string InformationText => _loreText;

    private void BuildInformation()
    {
        _information = IconButton("ActionInformation", ActionSymbol.Information, "Read the chart", OpenInformation);
        _informationPanel = Panel(_root);
        _informationPanel.Name = "InformationScroll";
        _informationPanel.Resized += () =>
        {
            _layoutSizes = null;
            Layout();
        };
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 10);
        _informationPanel.AddChild(column);
        var heading = new HBoxContainer();
        column.AddChild(heading);
        _informationTitle = Label("", 20);
        _informationTitle.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        heading.AddChild(_informationTitle);
        var close = TextButton("×", () => _informationPanel.Hide());
        close.Name = "CloseInformation";
        close.CustomMinimumSize = new(34, 30);
        heading.AddChild(close);
        _informationScroll = new ScrollContainer
        {
            Name = "InformationContentScroll",
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        column.AddChild(_informationScroll);
        _informationBody = new VBoxContainer
        {
            Name = "InformationSections",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        _informationBody.AddThemeConstantOverride("separation", 14);
        _informationScroll.AddChild(_informationBody);
        _informationBody.MinimumSizeChanged += () =>
        {
            _layoutSizes = null;
            Layout();
        };
        _informationPanel.Hide();
    }

    private void SetLore(string title, LorePage page)
    {
        _loreTitle = title;
        _lorePage = page;
        _loreText = page.PlainText;
    }

    private void UpdateInformation(BattleState battle, Ship? selected, Village? village)
    {
        _informationBattle = battle;
        if (selected is not null)
            SetLore(selected.Name, AncientLore.Ship(battle, selected));
        else if (village is not null)
            SetLore(village.Name, AncientLore.Village(battle, village));
        else if (_inspectionCell is { } cell && battle.Board.Contains(cell)
            && (battle.Vision.IsRadarContact(Side.Player, cell)
                || battle.Vision.IsVisible(Side.Player, cell) && (battle.TreasuryAt(cell) is not null
                    || battle.Shoals.Contains(cell) || battle.FishSpots.Contains(cell))))
        {
            if (battle.Vision.State(Side.Player, cell) == VisibilityState.RadarContact)
                SetLore("Distant contact", new("A mark on the chart tells you where, never who; bring a lookout nearer.", System.Array.Empty<LoreSection>()));
            else
            {
                var lore = AncientLore.Cell(battle, cell);
                SetLore(lore?.Title ?? "", lore?.Page ?? LorePage.Empty);
            }
            _ship.Text = _loreTitle;
            _health.Text = "";
            _details.Text = "Read the chart for the keeper's counsel.";
            _shipCard.Show();
        }
        else
        {
            SetLore("", LorePage.Empty);
            _shipCard.Hide();
        }

        _information.SetMeta("applicable", _loreText.Length > 0);
        Availability(_information, _loreText.Length > 0, "Info");
        if (_informationPanel.Visible)
        {
            RenderInformation();
            if (_loreText.Length == 0)
                _informationPanel.Hide();
        }
    }

    private void RenderInformation()
    {
        _informationTitle.Text = _loreTitle;
        string key = _loreTitle + "\n" + _loreText;
        if (_renderedLore == key)
            return;
        bool sameObject = _informationBody.GetMeta("object", "").AsString() == _loreTitle;
        _renderedLore = key;
        _informationBody.SetMeta("object", _loreTitle);
        foreach (var child in _informationBody.GetChildren())
        {
            _informationBody.RemoveChild(child);
            child.QueueFree();
        }
        var summary = WrappedLoreLabel(_lorePage.Summary, 15);
        summary.Name = "InformationSummary";
        _informationBody.AddChild(summary);
        foreach (var section in _lorePage.Sections)
        {
            var group = new VBoxContainer();
            group.AddThemeConstantOverride("separation", 5);
            _informationBody.AddChild(group);
            var title = Label(section.Title.ToUpperInvariant(), 13);
            title.Name = "InformationSectionHeading";
            title.AddThemeColorOverride("font_color", PapyrusStyle.FaintInk);
            group.AddChild(title);
            var divider = new HSeparator();
            divider.AddThemeStyleboxOverride("separator", new StyleBoxLine { Color = new Color(PapyrusStyle.Bronze, .45f), Thickness = 1 });
            group.AddChild(divider);
            foreach (var row in section.Rows)
            {
                var fields = new HBoxContainer();
                fields.AddThemeConstantOverride("separation", 12);
                group.AddChild(fields);
                var label = WrappedLoreLabel(row.Label, 14);
                label.Name = "InformationFieldLabel";
                label.SizeFlagsHorizontal = Control.SizeFlags.Fill;
                label.CustomMinimumSize = new(112, 0);
                label.AddThemeColorOverride("font_color", PapyrusStyle.FaintInk);
                fields.AddChild(label);
                var value = WrappedLoreLabel(row.Value, 14);
                value.Name = "InformationFieldValue";
                fields.AddChild(value);
            }
        }
        if (!sameObject)
            _informationScroll.ScrollVertical = 0;
    }

    private static Label WrappedLoreLabel(string text, int fontSize)
    {
        var label = Label(text, fontSize);
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        return label;
    }

    private void LayoutInformation(Vector2 viewport)
    {
        float width = Mathf.Clamp(viewport.X - 36, 280, 408);
        float ceiling = Mathf.Clamp(viewport.Y - 196, 220, 500);
        float headingHeight = (_informationTitle?.GetParent() as Control)?.GetCombinedMinimumSize().Y ?? 48;
        float margins = _informationPanel.GetThemeStylebox("panel").GetMinimumSize().Y;
        float contentHeight = (_informationBody?.GetCombinedMinimumSize().Y ?? 0) + headingHeight + margins + 12;
        float height = Mathf.Clamp(contentHeight, 220, ceiling);
        _informationPanel.Size = new(width, height);
        _informationPanel.Position = new(viewport.X - width - 18, (viewport.Y - height) / 2);
    }

    private void OpenInformation()
    {
        if (_loreText.Length == 0)
            return;
        RenderInformation();
        _informationPanel.Visible = !_informationPanel.Visible;
        _layoutSizes = null;
        Layout();
    }

    public void ShowInformation(BattleState battle, GridPosition cell)
    {
        ShowTile(battle, cell);
        UpdateInformation(battle, battle.ObservedAt(Side.Player, cell), battle.ObservedVillages(Side.Player).FirstOrDefault(v => v.Position == cell));
        OpenInformation();
    }
}
