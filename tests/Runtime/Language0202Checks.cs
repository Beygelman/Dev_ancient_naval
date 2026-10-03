using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Presentation;
using DevAncientNaval.Presentation.UI;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Tests.Runtime;

/// <summary>Exercises the native translation bridge and live menus with an isolated preference file.</summary>
public partial class Language0202Checks : Node
{
    public Main Game { get; set; } = null!;
    private int _checks;

    private void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        _checks++;
    }

    private static IEnumerable<Node> Descendants(Node parent)
    {
        yield return parent;
        foreach (Node child in parent.GetChildren())
            foreach (Node node in Descendants(child)) yield return node;
    }

    private static Button Find(Node parent, string name) => Descendants(parent).OfType<Button>().Single(b => b.Name == name);
    private async Task Frame() => await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    private async Task Settled() { await Frame(); await Frame(); }
    private string Rendered(Button button) => button.Tr(button.Text).ToString();

    private async Task Capture(string suffix)
    {
        string? arg = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--capture="));
        if (arg is null || DisplayServer.GetName() == "headless") return;
        await Settled();
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        string path = arg[10..].Replace(".png", "-" + suffix + ".png");
        Check(GetViewport().GetTexture().GetImage().SavePng(path) == Error.Ok, "Capture " + suffix);
    }

    private async Task Switch(Node surface, string locale)
    {
        var button = Find(surface, "Language_" + locale);
        Check(button.IsVisibleInTree(), "Language choice is reachable: " + locale);
        button.EmitSignal(BaseButton.SignalName.Pressed);
        await Settled();
        Check(Language.Current == locale, "Live locale changes: " + locale);
        foreach (var group in Descendants(Game).OfType<LanguageButtons>())
            foreach (Button choice in group.GetChildren())
            {
                bool active = choice.GetMeta("locale").AsString() == locale;
                Check(choice.Text.StartsWith("✓ ") == active, "Both selectors show the active locale");
                Check(choice.AutoTranslateMode == Control.AutoTranslateModeEnum.Disabled, "Language names stay native");
            }
    }

    private void CheckButton(Node surface, string name)
    {
        var button = Find(surface, name);
        Check(button.AutoTranslateMode != Control.AutoTranslateModeEnum.Disabled, "Native translation is enabled: " + name);
        string expected = Language.Translate(button.Text);
        Check(Rendered(button) == expected, "Godot renders the selected language: " + name);
        Check(Language.Current == "en" || expected != button.Text, "Visible action is translated: " + button.Text);
    }

    private void InsideViewport(Control control, string name)
    {
        Rect2 rect = control.GetGlobalRect();
        Vector2 size = GetViewport().GetVisibleRect().Size;
        Check(rect.Position.X >= -1 && rect.Position.Y >= -1 && rect.End.X <= size.X + 1 && rect.End.Y <= size.Y + 1,
            $"{name} stays inside {size}: {rect}");
    }

    public override async void _Ready()
    {
        try
        {
            await Settled();
            string saveArgument = OS.GetCmdlineUserArgs().Single(a => a.StartsWith("--save-file="));
            string savePath = Path.GetFullPath(saveArgument[12..]);
            Check(!OS.GetCmdlineUserArgs().Any(a => a.StartsWith("--language=")), "Persistence test does not override language on the command line");
            if (OS.GetCmdlineUserArgs().Contains("--resume-language-only"))
            {
                Check(Language.Current == "nl", "A fresh process reads the disposable language preference");
                Game.ShowHome();
                await Settled();
                CheckButton(Game.Home, "HomeNewGame");
                GD.Print($"PASS: {_checks} language preference process-restart checks.");
                GetTree().Quit();
                return;
            }
            Game.FastChecks = true;
            Game.ShowHome();
            await Settled();
            foreach (string locale in new[] { "en", "uk", "nl" })
            {
                await Switch(Game.Home, locale);
                foreach (string name in new[] { "HomeNewGame", "HomeExit" }) CheckButton(Game.Home, name);
                Check(System.IO.File.ReadAllText(savePath + ".language") == locale, "Only the disposable preference is persisted");
                Find(Game.Home, "HomeNewGame").EmitSignal(BaseButton.SignalName.Pressed);
                await Settled();
                var paper = Descendants(Game.Home).OfType<PanelContainer>().Single(n => n.Name == "VoyageSetupPaper");
                InsideViewport(paper, "Right-side voyage setup: " + locale);
                Check(paper.GetGlobalRect().GetCenter().X > GetViewport().GetVisibleRect().Size.X / 2,
                    "Voyage setup remains on the right");
                CheckButton(Game.Home, "StartBattle");
                CheckButton(Game.Home, "CancelColor");
                foreach (Button swatch in Descendants(paper).OfType<Button>().Where(b => b.Name.ToString().StartsWith("FleetColor")))
                    InsideViewport(swatch, "Color choice " + swatch.Name);
                await Capture("setup-" + locale);
                Find(Game.Home, "CancelColor").EmitSignal(BaseButton.SignalName.Pressed);
                await Settled();
            }

            Language.Set("en", persist: false);
            Language.Initialize();
            await Settled();
            Check(Language.Current == "nl", "Reload reads the saved preference without touching a normal user save");
            await Game.StartNewSession(FleetColor.Red);
            await Settled();
            string saved = Game.Battle.SaveJson();
            Game.Hud.SetMenuVisible(true);
            foreach (string locale in new[] { "en", "uk", "nl" })
            {
                await Switch(Game.Hud, locale);
                Check(Game.Hud.MenuVisible, "Changing language keeps the game menu open");
                foreach (string name in new[] { "NewGame", "Creative", "GodEye", "CloseMenu", "MainMenu", "ExitGame" })
                    CheckButton(Game.Hud, name);
                Check(Game.Battle.SaveJson() == saved, "Language switches preserve every rule, identifier and nation/town name");
                await Capture("menu-" + locale);
            }

            CheckCatalogsAndMessages();
            await CheckLore();
            await CheckMysticalUpgrades();
            Language.Set("nl");
            GD.Print($"PASS: {_checks} language bridge, native menus, viewport, preference, lore and dynamic-message checks.");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PushError(error.ToString());
            GetTree().Quit(1);
        }
    }

    private void CheckCatalogsAndMessages()
    {
        var catalogs = new[] { "uk", "nl" }.Select(locale => JsonSerializer.Deserialize<Dictionary<string, string>>(
            Godot.FileAccess.GetFileAsString($"res://data/localization/{locale}.json"))!).ToArray();
        Check(catalogs.All(c => c.Count >= 450), "Broad catalogs cover at least 450 messages each");
        Check(catalogs[0].Keys.ToHashSet().SetEquals(catalogs[1].Keys), "Both languages cover the same message catalog");
        foreach (var (locale, expected) in new[]
        {
            ("uk", new[] { "1 клітинка", "2 клітинки", "5 клітинок", "11 клітинок", "21 клітинка", "22 клітинки" }),
            ("nl", new[] { "1 tegel", "2 tegels", "5 tegels", "11 tegels", "21 tegels", "22 tegels" })
        })
        {
            int index = 0;
            foreach (int number in new[] { 1, 2, 5, 11, 21, 22 })
                Check(LocalizedMessages.Translate($"{number} tiles", locale) == expected[index++], "Grammatical tile plural: " + locale + number);
            Check(LocalizedMessages.Translate("4/15 HP", locale) == "4/15 HP", "Health numerals and unit stay exact");
            Check(LocalizedMessages.Translate("Akhet-Ra", locale) == "Akhet-Ra", "Latin city names stay exact");
            string captain = LocalizedMessages.Translate("Captain Harukaze's turn.", locale);
            Check(captain != "Captain Harukaze's turn." && captain.Contains("Harukaze"), "Captain name remains intact in translated turn status");
            string health = LocalizedMessages.Translate("Health 4/15", locale);
            Check(health != "Health 4/15" && health.EndsWith("4/15"), "Dynamic health label translates without changing numbers");
            foreach (string source in new[]
            {
                "Available at Mothership level 3.", "Double salvo: 8 damage", "Bomb: 7 direct damage, 2 splash. Ready again in 3 turns.",
                "Not enough Thors.", "Fired at a radar contact · outcome not visible", "Health", "Crew & economy", "Installed improvements",
                "Other nations are taking their turns", "Move the Balloon first. Its bomb recharges every 3 owner turns."
            })
            {
                string translated = LocalizedMessages.Translate(source, locale);
                Check(translated != source && !translated.Contains("[[plural:") && !Regex.IsMatch(translated, @"\{\d+\}"),
                    "Runtime message is fully rendered: " + locale + " / " + source);
                Check(Regex.Matches(source, @"\d+").Select(m => m.Value).SequenceEqual(Regex.Matches(translated, @"\d+").Select(m => m.Value)),
                    "Translated message retains numerical facts");
            }
        }
    }

    private async Task CheckLore()
    {
        Game.Hud.SetMenuVisible(false);
        Game.LoadScenario(LoreChecks.Example(Game.Battle.Rules));
        string saved = Game.Battle.SaveJson();
        var missing = new HashSet<string>();
        foreach (string locale in new[] { "uk", "nl" })
        {
            Language.Set(locale);
            foreach (var ship in Game.Battle.OwnShips(Side.Player))
            {
                var page = AncientLore.Ship(Game.Battle, ship);
                Check(Language.Translate(page.Summary) != page.Summary, "All ship counsel is translated: " + ship.Name);
                Check(Language.Translate(ship.Name) != ship.Name || ship.Name is "Kolonel" or "Granado", "Ship class display name is translated");
                foreach (var section in page.Sections)
                {
                    Check(Language.Translate(section.Title) != section.Title, "Lore section is translated: " + section.Title);
                    Check(Language.Translate(section.Title.ToUpperInvariant()) != section.Title.ToUpperInvariant(),
                        "Native uppercase lore heading is translated: " + section.Title);
                    foreach (var row in section.Rows)
                    {
                        Check(Language.Translate(row.Label) != row.Label || row.Label is "Radar" or "Bomb", "Lore field is translated: " + row.Label);
                        string translated = Language.Translate(row.Value);
                        if (row.Label == "Health") Check(translated == row.Value, "Lore preserves the exact HP fraction");
                        Check(!translated.Contains("[[plural:") && !Regex.IsMatch(translated, @"\{\d+\}"), "Lore has no unresolved template tokens");
                        if (translated == row.Value && Regex.IsMatch(row.Value, "[a-z]")) missing.Add(locale + ": " + row.Value);
                    }
                }
            }
            Game.Hud.ShowInformation(Game.Battle, Game.Battle.Mothership(Side.Player)!.Position);
            await Settled();
            var labels = Descendants(Game.Hud).OfType<Label>().Where(n => n.Name == "InformationFieldLabel").ToArray();
            Check(labels.Length >= 8 && labels.All(label => label.Tr(label.Text).ToString() == Language.Translate(label.Text)),
                "Native counsel fields use the active language");
            await Capture("lore-" + locale);
            Game.Hud.CloseMenus();
            Check(Game.Battle.SaveJson() == saved, "Reading translated counsel does not alter a voyage");
        }
        Check(missing.Count == 0, "Untranslated counsel: " + string.Join("\n", missing.OrderBy(s => s)));
    }

    private async Task CheckMysticalUpgrades()
    {
        var snapshot = Game.Battle.CaptureSnapshot();
        var mother = snapshot.Ships.First(ship => ship.Owner == Side.Player && ship.Kind == ShipClass.Mothership);
        mother.Level = 2;
        mother.PendingUpgradeLevel = 2;
        mother.Health = Game.Battle.Rules.Get(ShipClass.Mothership).MaxHealth;
        Game.LoadScenario(BattleState.LoadJson(BattleState.SerializeSnapshot(snapshot)));
        await Settled();
        foreach (string locale in new[] { "en", "uk", "nl" })
        {
            Language.Set(locale, persist: false);
            await Settled();
            Check(Game.Hud.UpgradeVisible, "Pending level choice is visible in " + locale);
            foreach (var button in Descendants(Game.Hud).OfType<MysticUpgradeButton>())
            {
                var choice = Enum.Parse<UpgradeChoice>(button.Name.ToString()[7..]);
                Check(button.Text == MysticUpgradeButton.Title(choice), "Upgrade face contains only its mystical name");
                Check(button.TooltipText == UpgradeDescriptions.Description(choice, Game.Battle.Rules),
                    "Hover counsel uses the current rules for " + choice);
                Check(locale == "en" || Rendered(button) != button.Text, "Mystical title is translated in " + locale);
                var tooltip = button._MakeCustomTooltip(button.TooltipText);
                var label = Descendants(tooltip).OfType<Label>().Single();
                Check(label.Text == Language.Translate(button.TooltipText), "Hover facts use the chosen language");
                Check(locale == "en" || label.Text != button.TooltipText, "Concrete upgrade effects are translated");
                Check(label.AutowrapMode == TextServer.AutowrapMode.WordSmart, "Long hover counsel wraps inside its parchment");
                tooltip.Free();
            }
            var mobility = Find(Game.Hud, "UpgradeMobility");
            Check(mobility.IsVisibleInTree(), "Level-two mystical mobility choice is reachable");
            var scroll = Descendants(Game.Hud).OfType<ScrollContainer>().Single(node => node.Name == "UpgradeScroll");
            // Translated headings reflow the bounded modal. Reveal the actual choice
            // inside its clipping viewport before hovering, then use the native canvas
            // transform so this continues to exercise real input at any UI scale.
            scroll.EnsureControlVisible(mobility);
            for (int frame = 0; frame < 5; frame++) await Frame();
            scroll.EnsureControlVisible(mobility);
            await Settled();
            var point = mobility.GetGlobalTransformWithCanvas() * (mobility.Size * .5f);
            var clip = new Rect2(scroll.GetGlobalTransformWithCanvas() * Vector2.Zero,
                scroll.Size * Game.Hud.Scale);
            Check(clip.HasPoint(point), $"Translated mobility hover is inside the actual scroll viewport in {locale}: pointer={point}, clip={clip}");
            GetViewport().PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
            await ToSignal(GetTree().CreateTimer(.7), SceneTreeTimer.SignalName.Timeout);
            if (DisplayServer.GetName() != "headless")
                Check(Descendants(GetTree().Root).OfType<Label>().Any(label => label.IsVisibleInTree()
                    && label.Text == Language.Translate(mobility.TooltipText)),
                    "A native mouse hover opens translated upgrade counsel in " + locale);
            await Capture("upgrade-hover-" + locale);
            GetViewport().PushInput(new InputEventMouseMotion { Position = new Vector2(5, 5), GlobalPosition = new Vector2(5, 5) }, true);
            await Settled();
        }
    }
}
