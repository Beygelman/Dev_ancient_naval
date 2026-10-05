using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Presentation;
using DevAncientNaval.Presentation.UI;
using Godot;

namespace DevAncientNaval.Tests.Runtime;

/// <summary>Native clipping, unchanged text transforms, fixed footer inputs and ceremonial pendants.</summary>
public partial class Scroll0208Checks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;

    private void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException("Scroll0208: " + message);
        _checks++;
    }

    private static IEnumerable<Node> Nodes(Node root)
    {
        yield return root;
        foreach (Node child in root.GetChildren())
            foreach (Node descendant in Nodes(child)) yield return descendant;
    }

    private async Task Frames(int count = 3)
    {
        for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private async Task Delay(double seconds) => await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
    private static Vector2 Center(Control control) => control.GetGlobalTransformWithCanvas() * (control.Size * .5f);

    private void Click(Control control)
    {
        Vector2 at = Center(control);
        GetViewport().PushInput(new InputEventMouseMotion { Position = at, GlobalPosition = at }, true);
        foreach (bool pressed in new[] { true, false })
            GetViewport().PushInput(new InputEventMouseButton
            {
                Position = at,
                GlobalPosition = at,
                ButtonIndex = MouseButton.Left,
                Pressed = pressed,
                ButtonMask = pressed ? MouseButtonMask.Left : (MouseButtonMask)0
            }, true);
    }

    private void Wheel(Control control)
    {
        Vector2 at = Center(control);
        GetViewport().PushInput(new InputEventMouseMotion { Position = at, GlobalPosition = at }, true);
        foreach (bool pressed in new[] { true, false })
            GetViewport().PushInput(new InputEventMouseButton
            {
                Position = at,
                GlobalPosition = at,
                ButtonIndex = MouseButton.WheelDown,
                Pressed = pressed
            }, true);
    }

    private async Task Capture(string suffix)
    {
        string? target = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--capture="))?[10..];
        if (target is null || DisplayServer.GetName() == "headless") return;
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        using var image = GetViewport().GetTexture().GetImage();
        Check(image.SavePng(target.Replace(".png", "-" + suffix + ".png")) == Error.Ok, "capture " + suffix);
        await Frames(1);
    }

    private async Task PhysicalReveal()
    {
        var layer = new CanvasLayer { Layer = 86 };
        AddChild(layer);
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Stop, Size = GetViewport().GetVisibleRect().Size };
        layer.AddChild(root);
        var shade = new ColorRect { Color = new Color("14232e"), Size = root.Size, MouseFilter = Control.MouseFilterEnum.Stop };
        root.AddChild(shade);
        var paper = new RollingModalPaper { Name = "PhysicalRevealFixture", Size = new(380, 360) };
        root.AddChild(paper);
        paper.Position = (root.Size - paper.Size) * .5f;
        var body = new VBoxContainer();
        var scroll = PapyrusModal.Wrap(paper, body, "PhysicalContentScroll");
        scroll.VerticalScrollMode = ScrollContainer.ScrollMode.ShowNever;
        scroll.CustomMinimumSize = new(0, 336);
        int presses = 0;
        var button = new Button { Text = "Full size text remains full size", CustomMinimumSize = new(0, 60) };
        var bright = new StyleBoxFlat { BgColor = new Color("db21b2") };
        button.AddThemeStyleboxOverride("normal", bright);
        button.Pressed += () => presses++;
        body.AddChild(button);
        body.AddChild(new Label { Text = "Only the reveal viewport changes height.", CustomMinimumSize = new(0, 100) });
        await Frames();
        // A repeated menu opening resets its outer PanelContainer before requesting
        // the same cached layout. The reveal layer deliberately has no Control child
        // minimum, so this must restore geometry even when the text has not changed.
        PapyrusModal.Layout(paper, scroll, body, root.Size);
        await Frames();
        Vector2 laidOut = paper.Size;
        Vector2 laidContent = paper.ContentSurface.Size;
        paper.ResetSize();
        Check(paper.Size.Y < laidOut.Y, "reset removes the outer sheet's previous height in a repeat-open fixture");
        PapyrusModal.Layout(paper, scroll, body, root.Size);
        await Frames();
        Check(paper.Size == laidOut && paper.ContentSurface.Size == laidContent
            && paper.ContentSurface.Size.Y == paper.Size.Y,
            $"an identical cached layout restores a reset paper and its native full-size content; "
            + $"original={laidOut}, originalInner={laidContent}, paper={paper.Size}, inner={paper.ContentSurface.Size}, "
            + $"innerMin={paper.ContentSurface.GetCombinedMinimumSize()}, scroll={scroll.Size}, "
            + $"scrollMin={scroll.GetCombinedMinimumSize()}, bodyMin={body.GetCombinedMinimumSize()}");
        scroll.CustomMinimumSize = new(0, 336);
        paper.Size = new(380, 360);
        paper.Position = (root.Size - paper.Size) * .5f;
        await Frames();
        await paper.OpenAsync(true);
        Vector2 original = Center(button);
        Vector2 originalSize = button.Size;
        float openRadius = paper.RollRadius;
        var opening = paper.OpenAsync();
        await Delay(.11);
        Check(paper.IsAnimating && paper.RollProgress is > 0 and < .5f, "a real tween exposes only a central strip at first");
        Check(paper.Scale == Vector2.One && paper.ContentSurface.Scale == Vector2.One,
            "neither paper nor content is squeezed during opening");
        Check(paper.ContentSurface.Size == paper.Size && button.Size == originalSize && Center(button).DistanceTo(original) < .01f,
            "content size and world position stay fixed behind the moving rolls");
        Check(paper.ContentViewport.ClipContents && paper.ContentViewport.Size.Y < paper.Size.Y * .5f,
            "the central strip is a native clipping viewport");
        Check(paper.RollRadius > openRadius, "wound paper has visibly thicker rolls");
        Click(button);
        Check(presses == 0, "a clipped-away button cannot receive a native click");
        if (DisplayServer.GetName() != "headless")
        {
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using var rendered = GetViewport().GetTexture().GetImage();
            var hiddenPoint = original - new Vector2(0, 16);
            Color pixel = rendered.GetPixel((int)hiddenPoint.X, (int)hiddenPoint.Y);
            Check(pixel.R < .22f && pixel.B < .25f, "the renderer hides ink and magenta plate behind closed paper");
            await Frames(1);
        }
        await opening;
        Click(button);
        await Frames(1);
        Check(presses == 1 && paper.RollProgress == 1, "the same native click reaches the fully revealed command");
        await Capture("full-scale-paper");
        var closing = paper.FoldAsync();
        await Delay(.29);
        Check(paper.Scale == Vector2.One && button.Size == originalSize && Center(button).DistanceTo(original) < .01f,
            "closing winds paper without changing the text transform");
        Check(paper.RollRadius > openRadius && paper.RollProgress < .5f, "closing cylinders gain thickness as the sheet retracts");
        var interrupted = paper.OpenAsync();
        await closing;
        Check(!interrupted.IsCompleted, "interrupted folding releases its waiter without completing the new opening");
        await interrupted;
        Check(paper.RollProgress == 1 && !paper.IsAnimating, "reopened paper ends in a stable fully expanded state");
        scroll.CustomMinimumSize = new(0, 256);
        paper.Size = new(420, 280);
        await Frames();
        Check(paper.ContentSurface.Size == paper.Size && paper.ContentViewport.Size == paper.Size,
            "resize preserves matching full-size content and reveal bounds");
        Check(ScrollRollArt.Radius(new(420, 560), 0) > ScrollRollArt.Radius(new(210, 220), 0),
            "a longer and larger sheet stores naturally thicker rolls");
        await paper.FoldAsync(true);
        Check(paper.ContentViewport.Size.Y == 0 && paper.Scale == Vector2.One,
            "instant folding hides all content while retaining its normal scale");
        Check(Math.Abs(ScrollRollArt.LowerRollY(paper.Size, 0) - ScrollRollArt.UpperRollY(paper.Size, 0)
            - paper.RollRadius * 2) < .01f, "fully closed rolls meet as distinct solid cylinders, rather than overlapping");
        root.QueueFree();
        layer.QueueFree();
        await Frames();
    }

    private async Task CeremonyGallery()
    {
        var layer = new CanvasLayer { Layer = 86 };
        AddChild(layer);
        var root = new Control { Size = GetViewport().GetVisibleRect().Size, Theme = PapyrusStyle.ChartTheme(),
            MouseFilter = Control.MouseFilterEnum.Stop };
        layer.AddChild(root);
        root.AddChild(new ColorRect { Color = new Color("14232e"), Size = root.Size, MouseFilter = Control.MouseFilterEnum.Stop });
        int index = 0;
        foreach (FleetColor nation in Enum.GetValues<FleetColor>())
        {
            var paper = new RollingModalPaper { Name = "Ceremonial" + nation, Nation = nation,
                CeremonialDecorations = true, Size = new(210, 214) };
            root.AddChild(paper);
            paper.Position = new(root.Size.X * .5f - 370 + index % 3 * 260,
                root.Size.Y * .5f - 260 + index / 3 * 272);
            var body = new VBoxContainer();
            var scroll = PapyrusModal.Wrap(paper, body, "GalleryScroll" + nation);
            scroll.CustomMinimumSize = new(0, 190);
            body.AddChild(new NationOrnament { Color = nation, CustomMinimumSize = new(0, 80) });
            body.AddChild(new Label { Text = NationIdentity.Name(nation),
                HorizontalAlignment = HorizontalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart });
            await paper.OpenAsync(true);
            var beads = Nodes(paper).OfType<NationBeads>().Single();
            Check(beads.Nation == nation && beads.IsProcessing() && beads.IsVisibleInTree(),
                "ceremonial ornament belongs to " + nation);
            Vector2 at = beads.PendantPosition;
            await Delay(.04);
            Check(beads.PendantPosition != at, "projected hanging ornament swings in depth for " + nation);
            index++;
        }
        await Frames();
        await Capture("nation-pendants");
        root.Hide();
        await Frames();
        Check(Nodes(root).OfType<NationBeads>().All(beads => !beads.IsProcessing()),
            "hidden parchment decorations stop cosmetic animation");
        root.QueueFree();
        layer.QueueFree();
        await Frames();
    }

    private async Task FixedFooterRelayout()
    {
        var layer = new CanvasLayer { Layer = 86 };
        AddChild(layer);
        var root = new Control { Size = GetViewport().GetVisibleRect().Size, Theme = PapyrusStyle.ChartTheme(),
            MouseFilter = Control.MouseFilterEnum.Stop };
        layer.AddChild(root);
        root.AddChild(new ColorRect { Color = new Color("14232e"), Size = root.Size,
            MouseFilter = Control.MouseFilterEnum.Stop });
        var paper = new RollingModalPaper { Name = "LateFooterFixture" };
        root.AddChild(paper);
        var body = new VBoxContainer();
        for (int row = 0; row < 18; row++)
            body.AddChild(new Label { Text = "Long voyage ledger row " + row, CustomMinimumSize = new(0, 36) });
        var footer = new VBoxContainer();
        var scroll = PapyrusModal.WrapWithFooter(paper, body, footer, "LateFooterScroll");
        PapyrusModal.LayoutWithFooter(paper, scroll, body, footer, root.Size);
        await paper.OpenAsync(true);
        await Frames();
        int pressed = 0;
        BrushPaperButton? last = null;
        for (int i = 0; i < 3; i++)
        {
            last = new BrushPaperButton { Text = "Footer action " + i, CustomMinimumSize = new(0, 44) };
            last.Pressed += () => pressed++;
            footer.AddChild(last);
        }
        await Frames();
        // Let native containers expand to the stale scroll minimum first, then
        // lower that minimum without changing the outer sheet's height at all.
        Vector2 unchangedOuter = paper.Size;
        Vector2 staleInner = paper.ContentSurface.Size;
        PapyrusModal.LayoutWithFooter(paper, scroll, body, footer, root.Size);
        await Frames(8);
        Check(paper.Size == unchangedOuter && paper.ContentSurface.Size == paper.Size,
            $"late footer minima shrink the inner native container at unchanged outer size; "
            + $"original={unchangedOuter}, staleInner={staleInner}, paper={paper.Size}, inner={paper.ContentSurface.Size}, "
            + $"innerMin={paper.ContentSurface.GetCombinedMinimumSize()}, scroll={scroll.Size}, "
            + $"scrollMin={scroll.GetCombinedMinimumSize()}, bodyMin={body.GetCombinedMinimumSize()}, "
            + $"footer={footer.Size}, footerMin={footer.GetCombinedMinimumSize()}, viewport={root.Size}");
        bool FooterFits() => footer.GetGlobalTransformWithCanvas().Origin.Y + footer.Size.Y
            <= paper.GetGlobalTransformWithCanvas().Origin.Y + paper.Size.Y - 11;
        Check(FooterFits(), "every fixed footer action remains above the lower roll after late layout");
        Click(last!);
        await Frames();
        Check(pressed == 1, "the last previously clipped footer action receives actual native input");
        Vector2 footerPosition = footer.GetGlobalTransformWithCanvas().Origin;
        Vector2 zoom = Game.MapCamera.Zoom;
        Wheel(scroll);
        await Frames();
        Check(scroll.ScrollVertical > 0 && footer.GetGlobalTransformWithCanvas().Origin == footerPosition && FooterFits(),
            "native body scrolling retains the footer at its fixed visible position");
        Check(Game.MapCamera.Zoom == zoom, "footer fixture scrolling cannot zoom the underlying sea");
        paper.ResetSize();
        PapyrusModal.LayoutWithFooter(paper, scroll, body, footer, root.Size);
        await paper.OpenAsync(true);
        await Frames(8);
        Check(paper.ContentSurface.Size == paper.Size && FooterFits(),
            "repeat opening restores full-size body and footer bounds after ResetSize");
        var reducedViewport = new Vector2(root.Size.X, root.Size.Y - 80);
        PapyrusModal.LayoutWithFooter(paper, scroll, body, footer, reducedViewport);
        await Frames(8);
        Check(paper.Size.Y <= reducedViewport.Y * .6f + 1 && paper.ContentSurface.Size == paper.Size && FooterFits(),
            "resize reflows the ledger and keeps fixed actions inside the sixty-percent sheet");
        await Capture("fixed-footer-reflow");
        layer.QueueFree();
        await Frames();
    }

    private async Task RealWelcomeAndRewards()
    {
        string saved = Game.Battle.SaveJson();
        var welcome = new VoyageWelcome { InstantAnimations = true };
        AddChild(welcome);
        foreach (string locale in new[] { "en", "uk", "nl" })
        {
            Language.Set(locale, persist: false);
            UiScale.Set(locale == "uk" ? 1.25f : .8f, persist: false);
            var accepted = welcome.Welcome(FleetColor.Red);
            await Frames();
            var paper = Nodes(welcome).OfType<RollingModalPaper>().Single();
            var sail = Nodes(welcome).OfType<Button>().Single(button => button.Name == "AcceptVoyage");
            Check(paper.CeremonialDecorations && paper.Nation == FleetColor.Red && paper.Scale == Vector2.One,
                "elders parchment adopts the faction rods and beads in " + locale);
            Check(paper.Size.Y <= UiScale.LogicalViewport(this).Y * .6f + 1 && sail.IsVisibleInTree(),
                "ceremony and fixed acceptance footer remain bounded in " + locale);
            Click(sail);
            await accepted;
            Check(!welcome.IsOpen, "native acceptance folds and dismisses the " + locale + " welcome");
        }
        welcome.QueueFree();
        Language.Set("en", persist: false);
        UiScale.Set(1, persist: false);
        var reward = new RewardPapyrusHud { Nation = FleetColor.Purple };
        AddChild(reward);
        int claims = 0;
        string? identity = null;
        reward.ClaimRequested += id => { identity = id; claims++; reward.Close(); };
        reward.ShowNation("physical-nation", "Argent Veil", new Color("ba9cdf"));
        var claim = Nodes(reward).OfType<BrushPaperButton>().Single(button => button.Name == "ClaimReward");
        var rewardPaper = Nodes(reward).OfType<RollingModalPaper>().Single();
        claim.EmitSignal(BaseButton.SignalName.Pressed);
        Check(claims == 0 && claim.Disabled, "a folded receipt cannot be submitted before its opening");
        await Delay(.5);
        Check(!claim.Disabled && rewardPaper.CeremonialDecorations && rewardPaper.Nation == FleetColor.Purple,
            "encounter receipt opens with the player's ceremonial necklace");
        await Capture("encounter-paper");
        Click(claim);
        await Frames(3);
        Check(claims == 0 && claim.Disabled && claim.StampProgress > 0,
            "actual reward click paints a hand before submitting its receipt");
        await Delay(.8);
        Check(claims == 1 && identity == "physical-nation" && !reward.IsOpen,
            "one receipt is submitted only after its physical paper has folded");
        reward.InstantAnimations = true;
        reward.ShowHeavenly("physical-heaven", 2);
        await Frames();
        Check(rewardPaper.CeremonialDecorations && rewardPaper.Scale == Vector2.One,
            "blessings use the exact same shared ceremonial sheet");
        Click(claim);
        await Frames();
        Check(claims == 2 && identity == "physical-heaven", "instant-mode blessing preserves exact receipt identity");
        Check(Game.Battle.SaveJson() == saved, "scroll browsing and callbacks alone cannot alter battle currency");
        reward.QueueFree();
        await Frames();
    }

    public override async void _Ready()
    {
        try
        {
            Game.FastChecks = true;
            Game.Home.Hide();
            Game.Tutorial.Clear();
            Game.Hud.CloseMenus();
            UiScale.Set(1, persist: false);
            Language.Set("en", persist: false);
            await Frames();
            await PhysicalReveal();
            await FixedFooterRelayout();
            await CeremonyGallery();
            await RealWelcomeAndRewards();
            GD.Print($"PASS: {_checks} v020.8 native physical papyrus, clip input, proportional rolls, pendants and receipt checks.");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PushError(error.ToString());
            GetTree().Quit(1);
        }
    }
}
