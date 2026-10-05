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

/// <summary>Native checkbox inputs must reach the real generated voyage, at all UI scales/languages.</summary>
public partial class Pirates0208Checks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;
    private void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Pirates0208: " + message);
        _checks++;
    }
    private static IEnumerable<Node> Nodes(Node node)
    {
        yield return node;
        foreach (Node child in node.GetChildren())
            foreach (Node descendant in Nodes(child)) yield return descendant;
    }
    private Button Button(string name) => Nodes(Game.Home).OfType<Button>().Single(n => n.Name == name);
    private async Task Frames(int count = 5)
    {
        for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }
    private void Click(Control control)
    {
        var point = control.GetGlobalTransformWithCanvas() * (control.Size * .5f);
        GetViewport().PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
        foreach (bool down in new[] { true, false })
            GetViewport().PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point,
                ButtonIndex = MouseButton.Left, Pressed = down,
                ButtonMask = down ? MouseButtonMask.Left : (MouseButtonMask)0 }, true);
    }
    private async Task Reveal(Button button)
    {
        await Frames(); // native containers settle after a locale/CanvasLayer scale change
        Nodes(Game.Home).OfType<ScrollContainer>().Single(n => n.Name == "VoyageSetupScroll").EnsureControlVisible(button);
        await Frames();
    }
    public override async void _Ready()
    {
        try
        {
            await Frames(); Game.FastChecks = true; Game.ShowHome(); Game.Home.ShowColors();
            await ToSignal(GetTree().CreateTimer(.4), SceneTreeTimer.SignalName.Timeout);
            var checkbox = (CheckBox)Button("IncludePirates");
            Check(Game.Home.IncludePirates, "pirates remain enabled by default");
            foreach (float scale in new[] { .8f, 1.25f })
            {
                UiScale.Set(scale, persist: false);
                foreach (string locale in new[] { "en", "uk", "nl" })
                {
                    Language.Set(locale, persist: false); await Reveal(checkbox);
                    Check(checkbox.GetGlobalTransformWithCanvas().Scale.IsEqualApprox(new Vector2(scale, scale)),
                        "checkbox retains its natural scale at " + locale);
                    bool previous = Game.Home.IncludePirates;
                    Click(checkbox); await Frames();
                    Check(Game.Home.IncludePirates != previous, "native checkbox toggles in " + locale + " hover=" + GetViewport().GuiGetHoveredControl()?.GetPath() + " value=" + Game.Home.IncludePirates);
                    Check(checkbox.Text == "Include pirates" && checkbox.Tr(checkbox.Text).ToString() == Language.Translate(checkbox.Text),
                        "checkbox uses the complete active language catalog " + locale);
                    if (locale != "en") Check(Language.Translate(checkbox.Text) != checkbox.Text, "localized pirate choice " + locale);
                }
            }
            UiScale.Set(1, persist: false); Language.Set("en", persist: false);
            await Reveal(Button("MapSizeLake")); Click(Button("MapSizeLake")); await Frames();
            await Reveal(checkbox);
            if (Game.Home.IncludePirates) { Click(checkbox); await Frames(); }
            string? capture = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--capture="))?[10..];
            if (capture is not null && DisplayServer.GetName() != "headless")
            {
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                Check(GetViewport().GetTexture().GetImage().SavePng(capture.Replace(".png", "-setup.png")) == Error.Ok,
                    "native setup screenshot");
                await Frames(1);
            }
            var start = Button("StartBattle"); await Reveal(start); Click(start);
            await Frames(1); await Game.CurrentOrder; await Frames();
            Check(Game.Battle.Board.MapSize == MapSize.Lake, "selected area reaches real voyage");
            Check(!Game.Battle.PiratesEnabled && Game.Battle.Ships.All(s => s.Owner != Side.Pirates)
                && Game.Battle.Villages.All(v => v.Owner != Side.Pirates), "unchecked option removes all pirate fleets and towns");
            var loaded = BattleState.LoadJson(Game.Battle.SaveJson());
            Check(!loaded.PiratesEnabled && loaded.SaveJson() == Game.Battle.SaveJson(), "disabled policy survives exact Continue snapshot");
            Game.VoyageWelcome.Close();
            GD.Print($"PASS: {_checks} v020.8 native pirate checkbox, language, scale, generation and resume checks.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}
