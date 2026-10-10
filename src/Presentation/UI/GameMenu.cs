using System;
using System.Threading.Tasks;
using Godot;

namespace DevAncientNaval.Presentation.UI;
public partial class DebugHud
{
    private Control _menuOverlay = null!;
    private PanelContainer _menuPanel = null!;
    private ScrollContainer _menuScroll = null!;
    private VBoxContainer _menuBody = null!;
    private VBoxContainer _menuMain = null!, _menuSettings = null!;
    private Button _godEyeButton = null!, _creativeButton = null!;
    private BrushPaperButton _returnVoyage = null!;
    private bool _menuClosing;
    private int _menuRevision;
    public bool MenuVisible => _menuOverlay?.Visible == true;
    public bool InstantPaperAnimations { get; set; }
    public event Action? CreativeRequested, GodEyeRequested, ExitRequested, MenuChanged, HomeRequested;

    private void BuildGameMenu()
    {
        _menuOverlay = new Control { Name = "GameMenu", MouseFilter = Control.MouseFilterEnum.Stop };
        _root.AddChild(_menuOverlay);
        _menuOverlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var shade = new ColorRect { Color = new Color(.01f, .035f, .05f, .55f), MouseFilter = Control.MouseFilterEnum.Stop };
        _menuOverlay.AddChild(shade);
        shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var version = GameIdentity.Footer("VoyageVersionSignature");
        version.AddThemeFontSizeOverride("font_size", 13);
        version.AddThemeColorOverride("font_color", new Color("e5edde"));
        _menuOverlay.AddChild(version);
        version.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        version.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomWide);
        version.OffsetLeft = 14; version.OffsetTop = -48;
        version.OffsetRight = -14; version.OffsetBottom = -8;
        _menuPanel = new RollingModalPaper { Name = "GameMenuPaper", CustomMinimumSize = new(340, 0) };
        _menuOverlay.AddChild(_menuPanel);
        _menuBody = new VBoxContainer();
        _menuScroll = PapyrusModal.Wrap(_menuPanel, _menuBody, "GameMenuScroll");
        _menuScroll.VerticalScrollMode = ScrollContainer.ScrollMode.ShowNever;
        _menuMain = new VBoxContainer();
        _menuMain.AddThemeConstantOverride("separation", 13);
        _menuBody.AddChild(_menuMain);
        void Add(string name, string title, Action action)
        {
            var button = MenuBrush(name, title, action);
            _menuMain.AddChild(button);
        }
        Add("NewGame", "New game", async () =>
        {
            await CloseMenuPaper();
            RestartRequested?.Invoke();
        });
        Add("GameSettings", "Settings", () =>
        {
            _menuMain.Hide();
            _menuSettings.Show();
            Layout();
        });
        var modes = new HBoxContainer { Name = "VoyageSpecialModes", Alignment = BoxContainer.AlignmentMode.Center };
        modes.AddThemeConstantOverride("separation", 30);
        _menuMain.AddChild(modes);
        _creativeButton = MenuIcon("Creative", ActionSymbol.Build, () => CreativeRequested?.Invoke());
        _godEyeButton = MenuIcon("GodEye", ActionSymbol.Radar, () => GodEyeRequested?.Invoke());
        modes.AddChild(_creativeButton);
        modes.AddChild(_godEyeButton);
        _returnVoyage = MenuBrush("CloseMenu", "Return to the voyage", async () =>
        {
            await _returnVoyage.StampAsync(InstantPaperAnimations);
            await CloseMenuPaper();
        });
        _returnVoyage.Underline = true;
        _menuMain.AddChild(_returnVoyage);
        Add("MainMenu", "Return to title", async () =>
        {
            await CloseMenuPaper();
            HomeRequested?.Invoke();
        });
        Add("ExitGame", "Exit game", () => ExitRequested?.Invoke());
        _menuSettings = new VBoxContainer { Name = "GameSettingsBody" };
        _menuSettings.AddThemeConstantOverride("separation", 12);
        _menuBody.AddChild(_menuSettings);
        _menuSettings.AddChild(Label("Settings", 25, true));
        _menuSettings.AddChild(new LanguageButtons());
        _menuSettings.AddChild(new UiScaleSlider());
        _menuSettings.AddChild(new UiHintsToggle());
        _menuSettings.AddChild(MenuBrush("CloseGameSettings", "Back", () =>
        {
            _menuSettings.Hide();
            _menuMain.Show();
            Layout();
        }));
        _menuSettings.Hide();
        _menuOverlay.Hide();
    }
    private static BrushPaperButton MenuBrush(string name, string text, Action action)
    {
        var button = new BrushPaperButton { Name = name, Text = text,
            CustomMinimumSize = new(0, 45), AutowrapMode = TextServer.AutowrapMode.WordSmart };
        button.Pressed += action;
        return button;
    }
    private static BrushPaperButton MenuIcon(string name, ActionSymbol symbol, Action action)
    {
        var button = MenuBrush(name, "", action);
        button.CustomMinimumSize = new(76, 58);
        button.AddChild(new ActionGlyph { Symbol = symbol, Position = new(23, 12), Size = new(30, 32),
            MouseFilter = Control.MouseFilterEnum.Ignore });
        return button;
    }
    public void SetMenuVisible(bool visible)
    {
        if (visible && _restart.Disabled) return;
        if (!visible)
        {
            _ = CloseMenuPaper();
            return;
        }
        if (MenuVisible && !_menuClosing) return;
        _menuRevision++;
        _menuClosing = false;
        CloseReadyActionsMenu();
        _menuOverlay.Show();
        _menuMain.Show();
        _menuSettings.Hide();
        _returnVoyage.ResetStamp();
        _returnVoyage.Ink = _namedBattle is null ? PaintedVoyageChoice.Burgundy
            : DevAncientNaval.Presentation.Map.FleetPalette.For(_namedBattle, DevAncientNaval.Core.Units.Side.Player).Darkened(.25f);
        MenuChanged?.Invoke();
        Layout();
        _ = ((RollingModalPaper)_menuPanel).OpenAsync(InstantPaperAnimations);
    }
    private async Task CloseMenuPaper()
    {
        if (!MenuVisible || _menuClosing) return;
        _menuClosing = true;
        int revision = ++_menuRevision;
        await ((RollingModalPaper)_menuPanel).FoldAsync(InstantPaperAnimations);
        if (revision != _menuRevision || !IsInsideTree()) return;
        _menuOverlay.Hide();
        _menuClosing = false;
        MenuChanged?.Invoke();
        Layout();
    }
    private void UpdateGodEyeLabel(bool enabled, bool victory)
    {
        _godEyeButton.Text = "";
        _godEyeButton.TooltipText = (enabled || victory ? "God’s eye: on ✓" : "God’s eye: off") + "\nSee the whole sea without fog";
        _godEyeButton.Disabled = victory;
        ((BrushPaperButton)_godEyeButton).Selected = enabled || victory;
        _godEyeButton.QueueRedraw();
    }
    private void UpdateCreativeLabel(bool enabled)
    {
        if (_creativeButton is null) return;
        _creativeButton.Text = "";
        _creativeButton.TooltipText = (enabled ? "Creative: on ✓" : "Creative: off") + "\nFree ships, docks and fish collection";
        ((BrushPaperButton)_creativeButton).Selected = enabled;
        _creativeButton.QueueRedraw();
    }
    public override void _UnhandledInput(InputEvent input)
    {
        if (input is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape }) return;
        if (TurnConfirmationVisible) CloseTurnConfirmation();
        else if (ReadyActionsMenuVisible) CloseReadyActionsMenu();
        else SetMenuVisible(!MenuVisible);
        GetViewport().SetInputAsHandled();
    }
}
