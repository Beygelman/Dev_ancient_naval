using System;
using Godot;

namespace DevAncientNaval.Presentation.UI;
public partial class DebugHud
{
    private Control _menuOverlay = null !;
    private PanelContainer _menuPanel = null !;
    private ScrollContainer _menuScroll = null!;
    private VBoxContainer _menuBody = null!;
    private VBoxContainer _menuMain = null!, _menuSettings = null!;
    private Button _godEyeButton = null !;
    private Button _creativeButton = null !;
    public bool MenuVisible => _menuOverlay?.Visible == true;

    public event Action? CreativeRequested, GodEyeRequested, ExitRequested, MenuChanged, HomeRequested;
    private void BuildGameMenu()
    {
        _menuOverlay = new Control
        {
            Name = "GameMenu",
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        _root.AddChild(_menuOverlay);
        _menuOverlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var shade = new ColorRect
        {
            Color = new Color(.01f, .035f, .05f, .55f),
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        _menuOverlay.AddChild(shade);
        shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _menuPanel = Panel(_menuOverlay);
        _menuPanel.CustomMinimumSize = new(340, 0);
        _menuBody = new VBoxContainer();
        _menuScroll = PapyrusModal.Wrap(_menuPanel, _menuBody, "GameMenuScroll");
        var column = new VBoxContainer();
        _menuMain = column;
        column.AddThemeConstantOverride("separation", 12);
        _menuBody.AddChild(column);
        column.AddChild(Label("Menu", 25, true));
        void Add(string name, string title, Action action)
        {
            var button = TextButton(title, action);
            button.Name = name;
            column.AddChild(button);
        }

        Add("NewGame", "New game", () =>
        {
            SetMenuVisible(false);
            RestartRequested?.Invoke();
        });
        Add("GameSettings", "Settings", () =>
        {
            _menuMain.Hide();
            _menuSettings.Show();
            Layout();
        });
        _creativeButton = TextButton("Creative: off", () => CreativeRequested?.Invoke());
        _creativeButton.Name = "Creative";
        column.AddChild(_creativeButton);
        var freeHint = Label("Free ships, docks and fish collection", 14, true);
        freeHint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        column.AddChild(freeHint);
        _godEyeButton = TextButton("God’s eye: off", () => GodEyeRequested?.Invoke());
        _godEyeButton.Name = "GodEye";
        column.AddChild(_godEyeButton);
        var sightHint = Label("See the whole sea without fog", 14, true);
        sightHint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        column.AddChild(sightHint);
        Add("CloseMenu", "Return to game", () => SetMenuVisible(false));
        Add("MainMenu", "Return to title", () =>
        {
            SetMenuVisible(false);
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
        var back = TextButton("Back", () =>
        {
            _menuSettings.Hide();
            _menuMain.Show();
            Layout();
        });
        back.Name = "CloseGameSettings";
        _menuSettings.AddChild(back);
        _menuSettings.Hide();
        _menuOverlay.Hide();
    }

    public void SetMenuVisible(bool visible)
    {
        if (visible && _restart.Disabled)
            return;
        _menuOverlay.Visible = visible;
        if (visible) { _menuMain.Show(); _menuSettings.Hide(); }
        MenuChanged?.Invoke();
        Layout();
    }

    private void UpdateGodEyeLabel(bool enabled, bool victory)
    {
        _godEyeButton.Text = victory ? "God’s eye: on · Victory" : enabled ? "God’s eye: on ✓" : "God’s eye: off";
        _godEyeButton.Disabled = victory;
    }

    private void UpdateCreativeLabel(bool enabled)
    {
        if (_creativeButton is not null)
            _creativeButton.Text = enabled ? "Creative: on ✓" : "Creative: off";
    }

    public override void _UnhandledInput(InputEvent input)
    {
        if (input is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
        {
            if (TurnConfirmationVisible) CloseTurnConfirmation();
            else SetMenuVisible(!MenuVisible);
            GetViewport().SetInputAsHandled();
        }
    }
}
