using System;
using System.Linq;
using System.Threading.Tasks;
using DevAncientNaval.Core.AI;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using DevAncientNaval.Presentation.UI;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation;
public partial class Main
{
    private StartScreen _home = null !;
    private SaveStore _saveStore = null !;
    private bool _saveEnabled, _sessionStarted, _sessionLoading;
    internal StartScreen Home => _home;
    internal SaveStore Saves => _saveStore;

    private void InitializeSession()
    {
        var args = OS.GetCmdlineUserArgs();
        bool tests = args.Any(a => a.EndsWith("-test"));
        bool menuTest = args.Contains("--menu-test") || args.Contains("--ui0205-test");
        bool victoryTest = (args.Contains("--victory-test") || args.Contains("--world-mode-test")) && args.Any(a => a.StartsWith("--save-file="));
        string save = args.FirstOrDefault(a => a.StartsWith("--save-file="))?[12..] ?? ProjectSettings.GlobalizePath("user://last_battle.json");
        bool performanceSave = (args.Contains("--performance-test") || args.Contains("--ui0203-test")) && args.Any(a => a.StartsWith("--save-file="));
        _saveStore = new SaveStore(save);
        _saveEnabled = (!tests || menuTest || performanceSave || victoryTest) && !_mapPreview;
        if (menuTest && !args.Any(a => a.StartsWith("--save-file=")))
            _saveEnabled = false;
        _home = new StartScreen
        {
            Name = "StartScreen"
        };
        AddChild(_home);
        _voyageWelcome = new VoyageWelcome { Name = "VoyageWelcome" };
        AddChild(_voyageWelcome);
        _home.StartRequested += color => RunSafely(() => StartNewSession(color));
        _home.ContinueRequested += () => RunSafely(ContinueSession);
        _home.ExitRequested += ExitSession;
        GetTree().AutoAcceptQuit = false;
        if (tests && !menuTest || _mapPreview)
        {
            _home.Hide();
            _sessionStarted = true;
        }
        else
            ShowHome();
    }

    private void SetBattleVisible(bool visible)
    {
        BoardView.Visible = visible;
        Fleet.Visible = visible;
        Ambience.Visible = visible;
        Hud.Visible = visible;
        Fleet.SetProcess(visible);
        Ambience.SetProcess(visible);
        Hud.SetProcess(visible);
        Hud.SetProcessUnhandledInput(visible);
        MapInput.SetProcessInput(visible);
        MapInput.SetProcessUnhandledInput(visible);
        MapInput.CancelGesture();
    }

    internal void ShowHome()
    {
        if (_sessionLoading || Busy)
            return;
        SaveSession();
        _rewards?.Close();
        _voyageWelcome.Close();
        HideOutcome();
        SetBattleVisible(false);
        _home.ShowHome(_saveStore.HasUnfinishedVoyage());
    }

    private void ShowColorSelection()
    {
        ShowHome();
        _home.ShowColors();
    }

    internal async Task StartNewSession(FleetColor color)
    {
        if (_sessionLoading)
            return;
        _sessionLoading = true;
        try
        {
            _home.SetNotice("Charting a new sea…");
            int opponents = _home.OpponentCount;
            var kind = _home.WorldKind;
            var size = _home.MapSize;
            var collapse = _home.CloseForVoyage(FastChecks);
            var battle = await Task.Run(() => SkirmishSetup.Create(PrototypeBoard.Create(opponentCount: opponents, kind: kind, mapSize: size), _rules, opponents));
            battle.SetPlayerColor(color);
            battle.SetDifficulty(_home.Difficulty);
            await collapse;
            SetBattleVisible(true);
            LoadScenario(battle);
            await DescendToFlagship(color);
            _home.Hide();
            _home.CompleteVoyage();
            _sessionStarted = true;
            await SaveSessionAsync(newGame: true);
            Hud.ShowPlayerTurn();
            Hud.ShowMessage("Your voyage begins. Explore, collect resources and protect your Mothership.");
        }
        catch (Exception e)
        {
            _voyageWelcome.Close();
            SetBattleVisible(false);
            _home.ShowHome(_saveStore.Exists, "Could not start the battle: " + e.Message);
        }
        finally
        {
            _sessionLoading = false;
            Refresh();
        }
    }

    internal async Task ContinueSession()
    {
        if (_sessionLoading)
            return;
        _sessionLoading = true;
        try
        {
            var saved = await Task.Run(_saveStore.Read);
            if (saved.Battle.IsOver || saved.Battle.PlayerDefeated)
            {
                _saveStore.Delete();
                _home.ShowHome(false);
                return;
            }
            _home.Hide();
            SetBattleVisible(true);
            LoadScenario(saved.Battle);
            _sessionStarted = true;
            MapCamera.Position = saved.Camera;
            MapCamera.Zoom = Vector2.One * Mathf.Clamp(saved.Zoom, Camera.MapCamera.MinZoom, Camera.MapCamera.MaxZoom);
            MapCamera.ForceUpdateScroll();
            Hud.ShowMessage(saved.Backup ? "Recovered the last intact backup." : "Saved voyage continued.");
            if (Battle.ActiveSide != Side.Player && !Battle.IsOver && !Battle.PlayerDefeated)
                await RunOpponents();
        }
        catch (Exception e)
        {
            SetBattleVisible(false);
            _home.ShowHome(_saveStore.Exists, e.Message);
        }
        finally
        {
            _sessionLoading = false;
            Refresh();
        }
    }

    internal void SaveSession()
    {
        if (!_saveEnabled || !_sessionStarted)
            return;
        try
        {
            if (Battle.IsOver || Battle.PlayerDefeated) _saveStore.Delete();
            else _saveStore.Write(Battle, MapCamera.Position, MapCamera.Zoom.X);
        }
        catch (Exception e)when (e is System.IO.IOException or UnauthorizedAccessException)
        {
            Hud.ShowMessage("Could not save this turn: " + e.Message);
            GD.PushWarning(e.Message);
        }
    }

    private void ExitSession()
    {
        Battle.PendingPresentation?.Finish();
        SaveSession();
        GetTree().Quit();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
            ExitSession();
    }

    private async Task RunOpponents()
    {
        Busy = true;
        Refresh();
        try
        {
            Hud.ShowOpponentTurn(Battle.ActiveSide);
            if (!FastChecks)
                await ToSignal(GetTree().CreateTimer(.8), SceneTreeTimer.SignalName.Timeout);
            for (int commands = 0; commands < 2048 && Battle.ActiveSide != Side.Player && !Battle.IsOver && !Battle.PlayerDefeated; commands++)
            {
                var before = Battle.ActiveSide;
                var presentation = Battle.Prepare(SimpleOpponent.Step);
                var result = presentation.Result;
                if (!result.Success)
                    throw new InvalidOperationException(result.Message);
                InvalidateGameplayPresentation();
                Refresh();
                Hud.ShowMessage("");
                if (before != Battle.ActiveSide && Battle.ActiveSide != Side.Player)
                    Hud.ShowOpponentTurn(Battle.ActiveSide);
                if (!FastChecks)
                {
                    await Fleet.Animate(result, presentation: presentation);
                    await ToSignal(GetTree().CreateTimer(.05), SceneTreeTimer.SignalName.Timeout);
                }

                presentation.Finish();
                await PresentHeavenlyAssistance(result);
                await PresentEncounters();
                await SaveSessionAsync();
                if (before != Battle.ActiveSide && Battle.ActiveSide != Side.Player)
                    Hud.ShowOpponentTurn(Battle.ActiveSide);
            }

            if (Battle.ActiveSide != Side.Player && !Battle.IsOver && !Battle.PlayerDefeated)
                throw new InvalidOperationException("The opponent did not finish its turn. The battle has been saved.");
        }
        finally
        {
            await SaveSessionAsync();
            Busy = false;
            Hud.HideOpponentTurn();
            if (Battle.ActiveSide == Side.Player && !Battle.IsOver && !Battle.PlayerDefeated)
                Hud.ShowPlayerTurn();
            Refresh();
        }
    }
}
