using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.World;
using DevAncientNaval.Presentation;
using DevAncientNaval.Presentation.UI;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Tests.Runtime;

/// <summary>Real native input through the rolling setup, settings, intro and saved voyage.</summary>
public partial class Ui0205Checks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;
    private void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("v020.5 UI: " + message);
        _checks++;
    }
    private static IEnumerable<Node> Nodes(Node node)
    {
        yield return node;
        foreach (var child in node.GetChildren()) foreach (var value in Nodes(child)) yield return value;
    }
    private Button HomeButton(string name) => Nodes(Game.Home).OfType<Button>().Single(button => button.Name == name);
    private async Task Frames(int count = 5)
    {
        for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }
    private void Click(Control control, Vector2? local = null)
    {
        var point = control.GetGlobalTransformWithCanvas() * (local ?? control.Size * .5f);
        // A physical click first moves the pointer into the current clipped
        // layout. Scrolling can leave the native hover cache on the old child.
        GetViewport().PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
        foreach (bool pressed in new[] { true, false })
            GetViewport().PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point,
                ButtonIndex = MouseButton.Left, Pressed = pressed, ButtonMask = pressed ? MouseButtonMask.Left : (MouseButtonMask)0 }, true);
    }
    private async Task Reveal(Button button)
    {
        var scroll = Nodes(Game.Home).OfType<ScrollContainer>().Single(node => node.Name == "VoyageSetupScroll");
        scroll.EnsureControlVisible(button); await Frames();
        var point = button.GetGlobalTransformWithCanvas() * (button.Size * .5f);
        var viewport = new Rect2(scroll.GetGlobalTransformWithCanvas().Origin, scroll.Size * UiScale.Value);
        Check(viewport.HasPoint(point), "the real choice is inside the parchment's clipped input area");
    }
    private async Task WaitFor(Func<bool> predicate, string reason)
    {
        ulong began = Time.GetTicksMsec();
        while (!predicate() && Time.GetTicksMsec() - began < 30_000) await Frames(1);
        Check(predicate(), reason);
    }
    private async Task Capture(string suffix)
    {
        string? file = OS.GetCmdlineUserArgs().FirstOrDefault(arg => arg.StartsWith("--capture="))?[10..];
        if (file is null || DisplayServer.GetName() == "headless") return;
        await Frames(); await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Check(GetViewport().GetTexture().GetImage().SavePng(file.Replace(".png", "-" + suffix + ".png")) == Error.Ok,
            "native capture " + suffix);
    }
    private async Task CheckSetup()
    {
        Check(Game.Home.IsOpen && !Game.BoardView.Visible, "title owns input before a voyage");
        Click(HomeButton("HomeSettings")); await Frames();
        await ToSignal(GetTree().CreateTimer(.5), SceneTreeTimer.SignalName.Timeout);
        var slider = Nodes(Game.Home).OfType<HSlider>().Single();
        Check(slider.IsVisibleInTree(), "home settings contains interface scale");
        Click(slider, new(slider.Size.X * .72f, slider.Size.Y * .5f)); await Frames();
        Check(UiScale.Value > 1 && UiScale.Value <= UiScale.Maximum, "native scaled settings slider changes the preference");
        Click(HomeButton("CloseHomeSettings")); await Frames();
        await ToSignal(GetTree().CreateTimer(.5), SceneTreeTimer.SignalName.Timeout);
        Check(!slider.IsVisibleInTree(), "scale and language are tucked inside settings");
        UiScale.Set(1, persist: false);
        Click(HomeButton("HomeNewGame")); await Frames();
        await ToSignal(GetTree().CreateTimer(.5), SceneTreeTimer.SignalName.Timeout);
        Check(Game.Home.OpponentCount == 1, "one burgundy rival is selected by default");
        var paper = Nodes(Game.Home).OfType<PanelContainer>().Single(node => node.Name == "VoyageSetupPaper");
        var scroll = Nodes(Game.Home).OfType<ScrollContainer>().Single(node => node.Name == "VoyageSetupScroll");
        Check(scroll.VerticalScrollMode == ScrollContainer.ScrollMode.ShowNever, "the rolling setup has no visible slider or scrollbar");
        Check(Nodes(paper).OfType<TextureRect>().Single(node => node.Name == "VoyageLogo").Texture is not null,
            "the logo is on the parchment above the nations");
        foreach (float scale in new[] { .8f, 1.25f })
        {
            UiScale.Set(scale, persist: false); await Frames();
            var viewport = UiScale.LogicalViewport(this);
            float rightMargin = viewport.X - paper.Position.X - paper.Size.X;
            Check(Math.Abs(paper.Position.Y - rightMargin) < 1 && Math.Abs(viewport.Y - paper.Position.Y - paper.Size.Y - rightMargin) < 1,
                "scaled setup follows equal top, bottom and right margins");
            foreach (string locale in new[] { "en", "uk", "nl" })
            {
                Language.Set(locale, persist: false); await Frames();
                foreach (FleetColor nation in Enum.GetValues<FleetColor>())
                {
                    var swatch = HomeButton("FleetColor" + nation);
                    var caption = swatch.GetNode<Label>("NationCaption");
                    Check(caption.Text == NationIdentity.Name(nation), "nation picker shows its localized name " + locale + " " + nation);
                    Check(locale == "en" || caption.Text != NationIdentity.SourceName(nation), "nation name has a real translation " + locale + " " + nation);
                    foreach (string state in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_focus_color", "font_disabled_color", "font_hover_pressed_color" })
                        Check(swatch.GetThemeColor(state).V < .65f, "painted nation uses dark text in " + state);
                }
                await Reveal(HomeButton("FleetColorRed")); Click(HomeButton("FleetColorRed")); await Frames();
                Check(Game.Home.SelectedColor == FleetColor.Red, "actual transformed red nation click " + locale);
                Check(HomeButton("StartBattle").Text == Language.Translate("Embark with the {0} nation").Replace("{0}", NationIdentity.Name(FleetColor.Red)),
                    "the voyage inscription names the selected nation in the active language");
            }
        }
        UiScale.Set(1, persist: false); Language.Set("en", persist: false); await Frames();
        foreach (int count in new[] { 4, 2, 1 })
        {
            var button = HomeButton("OpponentCount" + count); await Reveal(button); Click(button); await Frames();
            Check(Game.Home.OpponentCount == count, "rival figurine selects count " + count);
            for (int figure = 1; figure <= 4; figure++)
                Check(((PaintedVoyageChoice)HomeButton("OpponentCount" + figure)).Marked == (figure <= count),
                    "rivals are painted from left to right without an isolated selection");
        }
        await Reveal(HomeButton("MapSizeLake")); Click(HomeButton("MapSizeLake")); await Frames();
        Check(Game.Home.MapSize == MapSize.Lake && Game.Home.OpponentCount == 1, "map area is independent from the number of rivals");
        foreach (var difficulty in Enum.GetValues<AiDifficulty>())
        {
            var button = HomeButton("Difficulty" + difficulty); await Reveal(button); Click(button); await Frames();
            Check(Game.Home.Difficulty == difficulty, "painted difficulty pictogram " + difficulty);
        }
        foreach (var world in Enum.GetValues<WorldKind>())
        {
            var button = HomeButton("World" + world); await Reveal(button); Click(button); await Frames();
            Check(Game.Home.WorldKind == world, "etched terrain choice " + world);
        }
        await Capture("painted-worlds");
        scroll.ScrollVertical = 0; await Frames();
        var position = scroll.GetGlobalTransformWithCanvas() * (scroll.Size * .5f);
        GetViewport().PushInput(new InputEventMouseButton { Position = position, GlobalPosition = position,
            ButtonIndex = MouseButton.WheelDown, Pressed = true }, true);
        GetViewport().PushInput(new InputEventMouseButton { Position = position, GlobalPosition = position,
            ButtonIndex = MouseButton.WheelDown, Pressed = false }, true);
        await Frames();
        Check(scroll.ScrollVertical > 0, "a real mouse wheel unrolls the clipped parchment");
        await Capture("painted-nations");
    }
    private async Task CheckVoyage()
    {
        var start = HomeButton("StartBattle"); await Reveal(start);
        await Capture("embark-before-click");
        bool submitted = false;
        string rawInput = "";
        start.Pressed += () => submitted = true;
        start.GuiInput += input => { if (input is InputEventMouseButton button) rawInput += $"{button.ButtonIndex}:{button.Pressed};"; };
        var clickPoint = start.GetGlobalTransformWithCanvas() * (start.Size * .5f);
        GetViewport().PushInput(new InputEventMouseMotion { Position = clickPoint, GlobalPosition = clickPoint }, true);
        GetViewport().PushInput(new InputEventMouseButton { Position = clickPoint, GlobalPosition = clickPoint,
            ButtonIndex = MouseButton.Left, Pressed = true, ButtonMask = MouseButtonMask.Left }, true);
        await Frames(2);
        GetViewport().PushInput(new InputEventMouseButton { Position = clickPoint, GlobalPosition = clickPoint,
            ButtonIndex = MouseButton.Left, Pressed = false, ButtonMask = (MouseButtonMask)0 }, true);
        await Frames(1);
        Check(submitted, "a real mouse release reaches the embark inscription: disabled=" + start.Disabled
            + ", control=" + start.GetGlobalTransformWithCanvas().Origin + ", size=" + start.Size
            + ", hover=" + GetViewport().GuiGetHoveredControl()?.GetPath() + ", raw=" + rawInput
            + ", transition=" + Game.Home.Transitioning + ", task=" + Game.CurrentOrder.Status);
        Check(Game.Home.Transitioning && !Game.CurrentOrder.IsCompleted, "the handprint locks repeated voyage submissions: transition="
            + Game.Home.Transitioning + ", task=" + Game.CurrentOrder.Status + ", labels="
            + string.Join(" | ", Nodes(Game.Home).OfType<Label>().Where(node => node.IsVisibleInTree()).Select(node => node.Text)));
        await WaitFor(() => Nodes(Game.VoyageWelcome).OfType<Button>().Single(node => node.Name == "AcceptVoyage").IsVisibleInTree(),
            "descent finishes at the elders' welcome");
        Check(Game.Battle.Board.MapSize == MapSize.Lake && Game.Battle.PlayerColor == FleetColor.Red
            && Game.Battle.Difficulty == AiDifficulty.Admiral, "the four setup choices reach the actual battle");
        var mother = Game.Battle.Mothership(Side.Player)!;
        var focus = Game.BoardView.Projection.GridToWorld(mother.Position);
        Check(Game.MapCamera.Position.DistanceTo(focus) < 1 && Math.Abs(Game.MapCamera.Zoom.X - .75f) < .001,
            "the opening descent ends exactly at the player's flagship");
        Game.SelectCell(mother.Position);
        Check(Game.SelectedShipId is null && Game.VoyageWelcome.IsOpen, "orders remain blocked until the elders' parchment is accepted");
        await ToSignal(GetTree().CreateTimer(.4), SceneTreeTimer.SignalName.Timeout);
        await Capture("elders-welcome");
        Click(Nodes(Game.VoyageWelcome).OfType<Button>().Single(node => node.Name == "AcceptVoyage"));
        await Game.CurrentOrder; await Frames();
        Check(!Game.VoyageWelcome.IsOpen && !Game.Home.IsOpen && Game.Saves.Exists,
            "explicit embark acceptance opens play and saves the voyage once");
        Game.SelectCell(mother.Position); await Frames();
        Check(Game.SelectedShipId == mother.Id, "selection becomes available after the introduction");
        Game.Hud.SetMenuVisible(true); await Frames();
        await ToSignal(GetTree().CreateTimer(.5), SceneTreeTimer.SignalName.Timeout);
        var settings = Nodes(Game.Hud).OfType<Button>().Single(node => node.Name == "GameSettings");
        Click(settings); await Frames();
        Check(Nodes(Game.Hud).OfType<HSlider>().Single().IsVisibleInTree(), "in-game Settings houses the scale preference");
        Click(Nodes(Game.Hud).OfType<Button>().Single(node => node.Name == "CloseGameSettings")); await Frames();
        Check(!Nodes(Game.Hud).OfType<HSlider>().Single().IsVisibleInTree(), "returning to menu hides settings controls");
        Game.Hud.SetMenuVisible(false);
        await ToSignal(GetTree().CreateTimer(.5), SceneTreeTimer.SignalName.Timeout);
        string state = Game.Battle.SaveJson();
        Game.ShowHome(); await Frames(); Click(HomeButton("HomeContinue")); await Game.CurrentOrder; await Frames();
        Check(Game.Battle.SaveJson() == state && !Game.VoyageWelcome.IsOpen, "Continue preserves the exact voyage and never repeats the opening ceremony");
    }
    public override async void _Ready()
    {
        try
        {
            await Frames(); await CheckSetup(); await CheckVoyage();
            GD.Print($"PASS: {_checks} v020.5 painted setup, settings, scrolling, introduction and saved-voyage checks.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}
