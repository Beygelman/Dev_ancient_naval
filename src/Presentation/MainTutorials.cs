using System;
using System.Linq;
using DevAncientNaval.Presentation.UI;
using Godot;

namespace DevAncientNaval.Presentation;

public partial class Main
{
    internal TutorialHud Tutorial { get; private set; } = null!;
    internal TutorialAdvice TutorialHistory { get; private set; } = null!;
    private void InitializeTutorials()
    {
        Tutorial = new TutorialHud();
        AddChild(Tutorial);
        var args = OS.GetCmdlineUserArgs();
        bool tests = args.Any(a => a.EndsWith("-test"));
        string? save = tests && !args.Any(a => a.StartsWith("--save-file=")) ? null : _saveStore.Path;
        TutorialHistory = new TutorialAdvice(Tutorial, save);
        UiHints.Changed += RefreshTutorials;
        TreeExiting += () => UiHints.Changed -= RefreshTutorials;
    }
    internal void BeginTutorialVoyage(bool newGame)
    {
        if (TutorialHistory is null) return;
        TutorialHistory.Begin(Battle, newGame);
        RefreshTutorials();
    }
    private void RefreshTutorials()
    {
        if (TutorialHistory is null) return;
        bool allowed = !Busy && !_sessionLoading && !_endingStamp && _home?.IsOpen != true
            && _voyageWelcome?.IsOpen != true && _rewards?.IsOpen != true && _victory?.IsOpen != true
            && !Hud.MenuVisible && !Hud.TurnConfirmationVisible && !Hud.UpgradeVisible
            && !Battle.IsOver && !Battle.PlayerDefeated;
        TutorialHistory.Observe(Battle, allowed);
    }
    private void SuspendTutorials() => TutorialHistory?.Suspend();
}
