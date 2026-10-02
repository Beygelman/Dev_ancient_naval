using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Presentation.UI;

namespace DevAncientNaval.Presentation;

public partial class Main
{
    private VictoryScreen? _victory;
    private BattleState? _celebratedBattle;
    private bool _outcomeOwnsInput;

    internal VictoryScreen? Victory => _victory;

    private void InitializeOutcome()
    {
        _victory = new VictoryScreen { Name = "VictoryScreen" };
        AddChild(_victory);
        _victory.HomeRequested += () =>
        {
            HideOutcome();
            ShowHome();
        };
        _victory.ExitRequested += ExitSession;
    }

    private void RefreshOutcome()
    {
        if (_victory is null)
            return;
        bool finished = Battle.Winner == Side.Player && !Busy && !_sessionLoading
            && Battle.PendingPresentation is null && _home?.IsOpen != true
            && BoardView.Visible;
        if (!finished)
        {
            HideOutcome();
            if (!ReferenceEquals(_celebratedBattle, Battle))
                _celebratedBattle = null;
            return;
        }
        if (ReferenceEquals(_celebratedBattle, Battle) && _victory.IsOpen)
            return;

        // Busy spans shell flight, flagship fracture and the remaining fleet's
        // sinking. Core victory alone is deliberately insufficient to open this.
        MapInput.CancelGesture();
        MapInput.SetProcessInput(false);
        MapInput.SetProcessUnhandledInput(false);
        Hud.SetProcessUnhandledInput(false);
        Hud.Hide();
        _outcomeOwnsInput = true;
        _celebratedBattle = Battle;
        _victory.ShowVictory(Battle.Statistics, Battle.Round);
    }

    private void HideOutcome()
    {
        _victory?.Close();
        if (!_outcomeOwnsInput)
            return;
        _outcomeOwnsInput = false;
        bool gameVisible = BoardView.Visible && _home?.IsOpen != true;
        MapInput.SetProcessInput(gameVisible);
        MapInput.SetProcessUnhandledInput(gameVisible);
        Hud.SetProcessUnhandledInput(gameVisible);
        Hud.Visible = gameVisible;
    }
}
