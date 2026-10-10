using System;
using System.Linq;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.Vision;
using DevAncientNaval.Presentation.Map;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation.UI;

public partial class DebugHud
{
    // The old basic card is retained as a private layout scaffold; this card owns visible information.
    private ActionGlyph _objectIcon = null!;
    private PanelContainer _informationPanel = null!;
    private Label _informationTitle = null!, _informationLevel = null!, _informationSummary = null!;
    private ScrollContainer _informationScroll = null!;
    private VBoxContainer _informationBody = null!, _informationColumn = null!, _informationContent = null!;
    private ObjectWaxSeal _objectSeal = null!;
    private ObjectCardOrnament _objectOrnament = null!;
    private GridPosition? _inspectionCell;
    private BattleState? _informationBattle;
    private LorePage _lorePage = LorePage.Empty;
    private string _loreTitle = "", _loreText = "", _renderedLore = "", _informationKey = "";
    private bool _layingOutInformation;
    public bool InformationVisible => _informationPanel?.Visible == true;
    public string InformationText => _loreText;

    private void BuildInformation()
    {
        _informationPanel = Panel(_root);
        _informationPanel.Name = "InformationScroll";
        _informationPanel.MouseForcePassScrollEvents = false;
        _informationPanel.CustomMinimumSize = new(352, 0);
        var style = PapyrusStyle.Panel();
        style.ContentMarginLeft = 43;
        style.ContentMarginRight = 12;
        style.ContentMarginTop = 12;
        style.ContentMarginBottom = 12;
        _informationPanel.AddThemeStyleboxOverride("panel", style);
        var column = _informationColumn = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 7);
        _informationPanel.AddChild(column);
        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 7);
        column.AddChild(header);
        _objectSeal = new ObjectWaxSeal { Name = "SelectedObjectWaxSeal", CustomMinimumSize = new(62, 64),
            MouseFilter = Control.MouseFilterEnum.Ignore };
        header.AddChild(_objectSeal);
        var titles = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        titles.AddThemeConstantOverride("separation", 2);
        header.AddChild(titles);
        _informationTitle = WrappedLoreLabel("", 20);
        _informationTitle.Name = "SelectedObjectName";
        titles.AddChild(_informationTitle);
        _informationLevel = WrappedLoreLabel("", 11);
        _informationLevel.Name = "SelectedObjectLevel";
        _informationLevel.AddThemeColorOverride("font_color", PapyrusStyle.FaintInk);
        titles.AddChild(_informationLevel);
        _informationSummary = WrappedLoreLabel("", 13);
        _informationSummary.Name = "InformationSummary";
        column.AddChild(_informationSummary);
        _informationScroll = new ScrollContainer { Name = "InformationContentScroll",
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.ShowNever,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, MouseForcePassScrollEvents = false };
        column.AddChild(_informationScroll);
        _informationContent = new VBoxContainer { Name = "InformationScrollableContent",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _informationContent.AddThemeConstantOverride("separation", 7);
        _informationScroll.AddChild(_informationContent);
        _informationBody = new VBoxContainer { Name = "InformationSections",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _informationBody.AddThemeConstantOverride("separation", 9);
        _informationContent.AddChild(_informationBody);
        _objectOrnament = new ObjectCardOrnament { Name = "ObjectNationOrnament",
            MouseFilter = Control.MouseFilterEnum.Ignore, ShowBehindParent = false };
        _informationPanel.AddChild(_objectOrnament);
        // An overlay ornament must never contribute a minimum or absorb wheel input.
        _objectOrnament.SetAsTopLevel(false);
        _informationPanel.Resized += () => { _layoutSizes = null; Layout(); };
        // Wrapped text can temporarily promote the native panel's minimum before
        // its width settles. Reapply the bounded height when that minimum shrinks.
        _informationPanel.MinimumSizeChanged += () => { _layoutSizes = null; Layout(); };
        _informationPanel.Hide();
    }

    private void SetLore(string title, LorePage page)
    {
        _loreTitle = title;
        _lorePage = new(page.Summary, page.Sections.Select(section => new LoreSection(section.Title,
            section.Rows.Where(row => row.Label != "Level").OrderBy(row => FactPriority(row.Label)).ToArray()))
            .Where(section => section.Rows.Count > 0).ToArray());
        _loreText = _lorePage.PlainText;
    }

    private void UpdateInformation(BattleState battle, Ship? selected, Village? village)
    {
        _informationBattle = battle;
        string objectKey = "";
        Side? owner = null;
        bool identified = false;
        _informationLevel.Text = "";
        _objectSeal.Symbol = ActionSymbol.Treasure;
        if (selected is not null)
        {
            SetLore(selected.Name, AncientLore.Ship(battle, selected));
            objectKey = "ship:" + selected.Id;
            owner = selected.Owner;
            identified = true;
            _objectSeal.Symbol = NavalGlyphArt.Symbol(selected.Definition.Class);
            _informationLevel.Text = $"Level {selected.Level}" + (selected.IsVeteran ? " · Veteran" : "");
        }
        else if (village is not null)
        {
            SetLore(village.Name, AncientLore.Village(battle, village));
            objectKey = "town:" + village.Id;
            owner = village.Owner;
            identified = true;
            _objectSeal.Symbol = ActionSymbol.City;
            _informationLevel.Text = $"Level {village.Level}";
        }
        else if (_inspectionCell is { } cell && battle.Board.Contains(cell)
            && (battle.Vision.IsRadarContact(Side.Player, cell)
                || battle.Vision.IsVisible(Side.Player, cell) && (battle.TreasuryAt(cell) is { IsCollected: false }
                    || battle.Shoals.Contains(cell) || battle.FishSpots.Contains(cell))))
        {
            objectKey = "cell:" + cell;
            if (battle.Vision.State(Side.Player, cell) == VisibilityState.RadarContact)
                SetLore("Distant contact", new("A mark on the chart tells you where, never who; bring a lookout nearer.", Array.Empty<LoreSection>()));
            else
            {
                var lore = AncientLore.Cell(battle, cell);
                SetLore(lore?.Title ?? "", lore?.Page ?? LorePage.Empty);
                _objectSeal.Symbol = battle.TreasuryAt(cell) is { IsCollected: false } ? ActionSymbol.Treasure : ActionSymbol.Fishing;
            }
        }
        else SetLore("", LorePage.Empty);

        _shipCard.Hide();
        _informationPanel.Visible = _loreText.Length > 0;
        if (!_informationPanel.Visible) return;
        // Styling uses the observed owner only; radar contacts have neutral wax and no faction ornament.
        _objectSeal.Wax = identified ? FleetPalette.For(battle, owner) : PapyrusStyle.Bronze;
        _objectOrnament.Nation = identified && owner is not null && owner != Side.Pirates ? battle.ColorFor(owner.Value) : null;
        _objectOrnament.Ink = identified ? FleetPalette.For(battle, owner).Darkened(.33f) : PapyrusStyle.FaintInk;
        _objectOrnament.Visible = identified;
        _objectSeal.QueueRedraw();
        _objectOrnament.QueueRedraw();
        if (_informationKey != objectKey)
        {
            _informationKey = objectKey;
            _informationScroll.ScrollVertical = 0;
        }
        RenderInformation();
        LayoutInformation(UiScale.LogicalViewport(this));
    }

    private void RenderInformation()
    {
        _informationTitle.Text = _loreTitle;
        _informationSummary.Text = _lorePage.Summary;
        string key = _loreTitle + "\n" + _loreText;
        if (_renderedLore == key) return;
        _renderedLore = key;
        foreach (var child in _informationBody.GetChildren())
        {
            _informationBody.RemoveChild(child);
            child.QueueFree();
        }
        foreach (var section in _lorePage.Sections)
        {
            var group = new VBoxContainer();
            group.AddThemeConstantOverride("separation", 4);
            _informationBody.AddChild(group);
            var title = WrappedLoreLabel(section.Title.ToUpperInvariant(), 11);
            title.Name = "InformationSectionHeading";
            title.AddThemeColorOverride("font_color", PapyrusStyle.FaintInk);
            group.AddChild(title);
            var facts = new GridContainer { Name = "InformationFactColumns", Columns = 2,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            facts.AddThemeConstantOverride("h_separation", 10);
            facts.AddThemeConstantOverride("v_separation", 7);
            group.AddChild(facts);
            foreach (var row in section.Rows)
            {
                var field = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
                field.AddThemeConstantOverride("separation", 1);
                facts.AddChild(field);
                var heading = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
                heading.AddThemeConstantOverride("separation", 5);
                field.AddChild(heading);
                heading.AddChild(new ActionGlyph { Name = "InformationStatGlyph", Symbol = FactSymbol(row.Label),
                    CustomMinimumSize = new(23, 23), InkScale = .65f, MouseFilter = Control.MouseFilterEnum.Ignore });
                var label = WrappedLoreLabel(row.Label, 11);
                label.Name = "InformationFieldLabel";
                label.AddThemeColorOverride("font_color", PapyrusStyle.FaintInk);
                heading.AddChild(label);
                var value = WrappedLoreLabel(row.Value, 12);
                value.Name = "InformationFieldValue";
                field.AddChild(value);
            }
        }
    }

    private static ActionSymbol FactSymbol(string label) => label switch
    {
        "Health" or "Vulnerability" => ActionSymbol.Health,
        "Sight" or "Lookout" or "Low profile" => ActionSymbol.Sight,
        "Movement" or "Propulsion" or "Level-4 keel" or "Trade lanes" or "Order" => ActionSymbol.Move,
        "Income" or "Settlement income" or "Trade income" or "Cost" or "Bounty" or "Trade stores" => ActionSymbol.Income,
        "Radar" or "Radar targets" => ActionSymbol.Radar,
        "Mortar" or "Blast" or "Against towns" => ActionSymbol.Mortar,
        "Cannons" or "Shots" or "Counterfire" or "Automatic guns" or "Double salvo" or "Heavy shot" or "Wall-piercing guns" => ActionSymbol.Attack,
        "Bomb" or "Anti-air" => ActionSymbol.Bomb,
        "Repair" or "Passive repair" or "Reinforced hull" => ActionSymbol.Repair,
        "Port" or "Local shipyard" => ActionSymbol.Dock,
        "Shipyard" or "Shipwright" or "Fishing Dock" => ActionSymbol.Build,
        "Resources" or "Collection" or "Reach" => ActionSymbol.Fishing,
        "Progress cells" or "Veterancy" or "Veteran" or "Veteran reach" or "Recharge" or "Readiness" => ActionSymbol.Progress,
        "Outpost" => ActionSymbol.Fortify,
        _ => ActionSymbol.Flag
    };

    private static int FactPriority(string label) => label switch
    {
        "Health" => 0, "Movement" => 1, "Cannons" or "Mortar" or "Bomb" => 2,
        "Shots" or "Automatic guns" or "Income" or "Settlement income" => 3,
        "Resources" => 4, "Sight" => 5, "Radar" => 6,
        "Progress cells" or "Radar targets" or "Vulnerability" => 30,
        _ => 15
    };

    private static Label WrappedLoreLabel(string text, int fontSize)
    {
        var label = Label(text, fontSize);
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        return label;
    }

    private void LayoutInformation(Vector2 viewport)
    {
        if (_informationPanel is null || _layingOutInformation) return;
        _layingOutInformation = true;
        // On portrait safe areas the name stays fixed while counsel
        // and facts share one readable scroll viewport. Rotation restores the
        // usual fixed summary without rebuilding or changing any text.
        var summaryParent = viewport.Y > viewport.X
            ? _informationContent : _informationColumn;
        if (_informationSummary.GetParent() != summaryParent)
        {
            _informationSummary.Reparent(summaryParent);
            summaryParent.MoveChild(_informationSummary, summaryParent == _informationContent ? 0 : 1);
        }
        float width = Math.Min(352, Math.Max(1, viewport.X - 30));
        float height = Math.Min(286, Math.Max(160, viewport.Y * .42f));
        if (viewport.Y > viewport.X)
        {
            height = Math.Min(180, Math.Max(120, viewport.Y * .20f));
            float dockTop = _radial.IsVisibleInTree()
                ? _radial.Position.Y + _radial.RingCenter.Y * _radial.Scale.Y - 18
                : viewport.Y - 18;
            var minimumPortrait = new Vector2(width, 0);
            if (_informationPanel.CustomMinimumSize != minimumPortrait)
                _informationPanel.CustomMinimumSize = minimumPortrait;
            if (_informationPanel.Size != new Vector2(width, height)) _informationPanel.Size = new(width, height);
            var dockPosition = new Vector2((viewport.X - width) / 2, dockTop - _informationPanel.Size.Y);
            if (_informationPanel.Position != dockPosition)
            {
                _informationPanel.Position = dockPosition;
                _objectOrnament.QueueRedraw();
            }
            _layingOutInformation = false;
            return;
        }
        // On narrow/touch views reserve the bottom-center compass footprint.
        // Wide views retain the established lower-left baseline.
        float bottom = 14 + (14 + width > viewport.X / 2 - 40 ? 80 : 0);
        // The nation relic is taller than the compass. Reserve its actual footprint
        // when portrait counsel extends into the right-hand end-turn column.
        if (14 + width > _end.Position.X - 12)
            bottom = Math.Max(bottom, viewport.Y - _end.Position.Y + 12);
        var bounds = new Rect2(14, viewport.Y - height - bottom, width, height);
        var minimum = new Vector2(width, 0);
        if (_informationPanel.CustomMinimumSize != minimum) _informationPanel.CustomMinimumSize = minimum;
        if (_informationPanel.Size != bounds.Size) _informationPanel.Size = bounds.Size;
        var position = new Vector2(bounds.Position.X, bounds.End.Y - _informationPanel.Size.Y);
        bool moved = _informationPanel.Position != position;
        if (moved) _informationPanel.Position = position;
        // PanelContainer sizes children automatically: the decorative child is drawn
        // only in its left inset, so its full host size cannot cover text or controls.
        if (moved) _objectOrnament.QueueRedraw();
        _layingOutInformation = false;
    }

    private void AvoidPortraitCommandOverlap()
    {
        if (_informationPanel?.IsVisibleInTree() == true)
            LayoutInformation(UiScale.LogicalViewport(this));
    }

    public void ShowInformation(BattleState battle, GridPosition cell)
    {
        ShowTile(battle, cell);
        UpdateInformation(battle, battle.ObservedAt(Side.Player, cell),
            battle.ObservedVillages(Side.Player).FirstOrDefault(v => v.Position == cell));
    }
}
