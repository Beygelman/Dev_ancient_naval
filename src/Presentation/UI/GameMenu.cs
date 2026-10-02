using System;
using Godot;

namespace DevAncientNaval.Presentation.UI;
public partial class DebugHud
{
    private Control _menuOverlay = null !;
    private PanelContainer _menuPanel = null !;
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
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 12);
        _menuPanel.AddChild(column);
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
        _creativeButton = TextButton("Creative: off", () => CreativeRequested?.Invoke());
        _creativeButton.Name = "Creative";
        column.AddChild(_creativeButton);
        column.AddChild(Label("Free ships, docks and fish collection", 14, true));
        _godEyeButton = TextButton("God’s eye: off", () => GodEyeRequested?.Invoke());
        _godEyeButton.Name = "GodEye";
        column.AddChild(_godEyeButton);
        column.AddChild(Label("See the whole sea without fog", 14, true));
        Add("CloseMenu", "Return to game", () => SetMenuVisible(false));
        Add("MainMenu", "Save and return to title", () =>
        {
            SetMenuVisible(false);
            HomeRequested?.Invoke();
        });
        Add("ExitGame", "Exit game", () => ExitRequested?.Invoke());
        _menuOverlay.Hide();
    }

    public void SetMenuVisible(bool visible)
    {
        if (visible && _restart.Disabled)
            return;
        _menuOverlay.Visible = visible;
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
            SetMenuVisible(!MenuVisible);
            GetViewport().SetInputAsHandled();
        }
    }
}
