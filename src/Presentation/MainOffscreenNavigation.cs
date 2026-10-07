using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Presentation.UI;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation;

public partial class Main
{
    private OffscreenNavigationHud _navigationHud = null!;
    private BattleState? _navigationBattle;
    private readonly Dictionary<int, double> _ownProtectedHealth = new();
    private readonly Dictionary<int, (GridPosition Cell, int Round)> _ownAttackAlerts = new();
    private readonly List<OffscreenNavigationTarget> _navigationTargets = new();
    private int _sanctuaryRound = -1;
    internal OffscreenNavigationHud NavigationHud => _navigationHud;
    internal int TurnSanctuaryPulseCount { get; private set; }

    private void InitializeOffscreenNavigation()
    {
        _navigationHud = new OffscreenNavigationHud { Name = "OffscreenNavigation", CanInteract = () => CanNavigateChart };
        _navigationHud.TargetRequested += target => RunSafely(() => FocusNavigationTarget(target));
        _navigationHud.MothershipRequested += () => RunSafely(FocusOwnMothership);
        AddChild(_navigationHud);
    }

    private bool CanNavigateChart => CanCommand && Hud.Visible && BoardView.Visible
        && Battle.PendingUpgrade(Side.Player) is null;

    private void RefreshOffscreenNavigation()
    {
        if (_navigationHud is null) return;
        if (!ReferenceEquals(_navigationBattle, Battle))
        {
            _navigationBattle = Battle;
            _ownProtectedHealth.Clear();
            _ownAttackAlerts.Clear();
            _sanctuaryRound = _sessionLoading && Battle.ActiveSide != Side.Player ? Battle.TurnSerial : -1;
        }

        // Only committed, owned HP is observed. The future prepared salvo result
        // must never create an alert while its projectiles are still in flight.
        if (Battle.PendingPresentation is null)
        {
            var protectedObjects = Battle.Villages.Where(v => v.Owner == Side.Player)
                .Select(v => (Key: -v.Id, Cell: v.Position, Health: v.Health))
                .Concat(Battle.OwnShips(Side.Player).Where(s => s.IsMothership)
                    .Select(s => (Key: s.Id, Cell: s.Position, Health: s.Health))).ToArray();
            var present = new HashSet<int>();
            foreach (var item in protectedObjects)
            {
                present.Add(item.Key);
                if (_ownProtectedHealth.TryGetValue(item.Key, out var before) && item.Health < before)
                    _ownAttackAlerts[item.Key] = (item.Cell, Battle.Round);
                _ownProtectedHealth[item.Key] = item.Health;
                if (_ownAttackAlerts.TryGetValue(item.Key, out var alert))
                    _ownAttackAlerts[item.Key] = (item.Cell, alert.Round);
            }
            foreach (int key in _ownProtectedHealth.Keys.Where(k => !present.Contains(k)).ToArray())
            { _ownProtectedHealth.Remove(key); _ownAttackAlerts.Remove(key); }
            foreach (int key in _ownAttackAlerts.Where(a => Battle.Round > a.Value.Round + 1).Select(a => a.Key).ToArray())
                _ownAttackAlerts.Remove(key);
        }

        _navigationTargets.Clear();
        if (CanCommand)
        {
            foreach (var town in Battle.ObservedVillages(Side.Player).Where(v => Battle.CanCaptureVillage(Side.Player, v.Id)))
                _navigationTargets.Add(new(town.Id, town.Position, NavigationMarkerKind.Capture));
            foreach (var ship in Battle.OwnShips(Side.Player).Where(s => Battle.Vision.IsVisible(Side.Player, s.Position)
                && Battle.CanLootTreasury(Side.Player, s.Id)))
                _navigationTargets.Add(new(ship.Id, ship.Position, NavigationMarkerKind.Treasury));
        }
        foreach (var (id, alert) in _ownAttackAlerts)
            _navigationTargets.Add(new(id, alert.Cell, NavigationMarkerKind.OwnAttack));
        bool visible = CanNavigateChart;
        _navigationHud.SetTargets(_navigationTargets, Battle.PlayerColor, visible,
            Battle.Mothership(Side.Player) is not null);

        // A visual ceremony, once per stable opponent turn, with no reward or save
        // mutation. Repeated refreshes, camera pans and modals cannot restart it.
        if (Hud.Visible && BoardView.Visible && _home?.IsOpen != true && Battle.ActiveSide != Side.Player
            && !Battle.IsOver && !Battle.PlayerDefeated && Battle.PendingPresentation is null
            && _sanctuaryRound != Battle.TurnSerial)
        {
            _sanctuaryRound = Battle.TurnSerial;
            BoardView.RefreshDepthObjects(Fleet);
            BoardView.PulseOwnedSanctuaries(Side.Player);
            Fleet.PulseOwnedSanctuaries(Side.Player);
            TurnSanctuaryPulseCount++;
        }
    }

    private void PositionNavigationMarkers()
    {
        if (_navigationHud is null) return;
        _navigationHud.PositionMarkers(cell => GetViewport().GetCanvasTransform() * BoardView.ToGlobal(
            Battle.ObservedVillages(Side.Player).FirstOrDefault(v => v.Position == cell) is { } town
                ? BoardView.VillageWorldAnchor(town) : BoardView.Projection.GridToWorld(cell)));
    }

    private async Task FocusNavigationTarget(OffscreenNavigationTarget target)
    {
        if (!CanNavigateChart || !_navigationTargets.Contains(target)) return;
        if (target.Kind == NavigationMarkerKind.Capture && !Battle.CanCaptureVillage(Side.Player, target.Id)) return;
        if (target.Kind == NavigationMarkerKind.Treasury && !Battle.CanLootTreasury(Side.Player, target.Id)) return;
        if (target.Kind == NavigationMarkerKind.OwnAttack)
        {
            if (!_ownAttackAlerts.Remove(target.Id)) return;
        }
        await FocusChartCell(target.Cell);
    }

    private Task FocusOwnMothership() => CanNavigateChart && Battle.Mothership(Side.Player) is { } mother
        ? FocusChartCell(mother.Position) : Task.CompletedTask;

    private async Task FocusChartCell(GridPosition cell)
    {
        MapInput.CancelGesture();
        ClearMode();
        Refresh();
        var point = Battle.ObservedVillages(Side.Player).FirstOrDefault(v => v.Position == cell) is { } town
            ? BoardView.VillageWorldAnchor(town) : BoardView.Projection.GridToWorld(cell);
        await MapCamera.FocusAsync(BoardView.ToGlobal(point), .25);
    }
}
