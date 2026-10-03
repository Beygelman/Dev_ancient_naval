using System;
using System.Linq;
using System.Threading.Tasks;
using DevAncientNaval.Core.AI;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.World;
using DevAncientNaval.Presentation.Camera;
using DevAncientNaval.Presentation.Input;
using DevAncientNaval.Presentation.Map;
using DevAncientNaval.Presentation.UI;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation;
public partial class Main
{
    public void SelectAtScreen(Vector2 screen)
    {
<<<<<<< Updated upstream
        if (_rewards?.IsOpen == true || _victory?.IsOpen == true || Busy || _home?.IsOpen == true || _sessionLoading || Hud.MenuVisible || Battle.PendingUpgrade(Side.Player)is not null)
=======
        if (Busy || _home?.IsOpen == true || _sessionLoading || Hud.MenuVisible || Battle.PendingUpgrade(Side.Player) is not null)
>>>>>>> Stashed changes
            return;
        var air = Battle.ObservedShips(Side.Player).Where(s => s.IsAirborne).FirstOrDefault(s => (GetViewport().GetCanvasTransform() * (BoardView.Projection.GridToWorld(s.Position) + new Vector2(0, -62))).DistanceTo(screen) < 24 * MapCamera.Zoom.X);
        if (air is not null && CanCommand && Selected is { Owner: Side.Player } attacker && Battle.CanAttack(attacker.Id, air.Id))
        {
<<<<<<< Updated upstream
            if (OfferSalvoChoice(attacker, air.Position, air.Id)) return;
            RunSafely(() => Perform(b => b.Attack(Side.Player, attacker.Id, air.Id), deferImpacts: true));
=======
            RunSafely(() => Perform(() => Battle.Attack(Side.Player, attacker.Id, air.Id)));
>>>>>>> Stashed changes
            return;
        }

        if (air is not null)
        {
            ClearMode();
            SelectedVillageId = null;
            SelectedShipId = air.Id;
            BoardView.Select(air.Position);
            Refresh();
            return;
        }

<<<<<<< Updated upstream
=======
        foreach (var village in Battle.ObservedVillages(Side.Player))
        {
            var flag = GetViewport().GetCanvasTransform() * BoardView.VillageFlagPosition(village.Position);
            if (CanCommand && Battle.CanCaptureVillage(Side.Player, village.Id) && flag.DistanceTo(screen) < Math.Max(12, 18 * MapCamera.Zoom.X))
            {
                ClearMode();
                SelectedShipId = null;
                SelectedVillageId = village.Id;
                BoardView.Select(village.Position);
                RunSafely(CaptureVillage);
                return;
            }
        }

>>>>>>> Stashed changes
        SelectCell(BoardView.Projection.WorldToGrid(BoardView.ToLocal(MapCamera.ScreenToWorld(screen))));
    }

    public void SelectCell(GridPosition cell)
    {
<<<<<<< Updated upstream
        using var trace = Diagnostics.PerformanceTrace.Measure("Selection.Dispatch");
        if (_rewards?.IsOpen == true || _victory?.IsOpen == true || Busy || _home?.IsOpen == true || _sessionLoading || Hud.MenuVisible || Battle.PendingUpgrade(Side.Player)is not null)
            return;
        _salvoCell = null;
        Hud.HideSalvoChoice();
=======
        if (Busy || _home?.IsOpen == true || _sessionLoading || Hud.MenuVisible || Battle.PendingUpgrade(Side.Player) is not null)
            return;
>>>>>>> Stashed changes
        if (!Battle.Board.Contains(cell))
        {
            CancelOrder();
            return;
        }

        BoardView.Select(cell);
        Hud.ShowTile(Battle, cell);
        var hit = Battle.ObservedAt(Side.Player, cell);
        var selected = Selected;
        _resourceCell = null;
        Hud.HideResource();
        if (CanCommand && Mode == OrderMode.Build && _building is { } villageKind && SelectedVillage is { } yard)
        {
            if (Battle.VillageSpawnCells(yard.Id).Contains(cell))
            {
<<<<<<< Updated upstream
                RunSafely(() => Perform(b => b.BuildFromVillage(Side.Player, yard.Id, villageKind, cell)));
=======
                RunSafely(() => Perform(() => Battle.BuildFromVillage(Side.Player, yard.Id, villageKind, cell)));
>>>>>>> Stashed changes
                return;
            }

            CancelOrder();
            return;
        }

        if (selected?.Owner == Side.Player && CanCommand)
        {
            if (Mode == OrderMode.Build && _building is { } kind)
            {
                if (Battle.SpawnCells(selected.Id).Contains(cell))
                {
                    int id = selected.Id;
<<<<<<< Updated upstream
                    RunSafely(() => Perform(b => b.Build(Side.Player, id, kind, cell)));
=======
                    RunSafely(() => Perform(() => Battle.Build(Side.Player, id, kind, cell)));
>>>>>>> Stashed changes
                    return;
                }

                CancelOrder();
                return;
            }

            if (Battle.TargetCells(selected.Id).Contains(cell))
            {
<<<<<<< Updated upstream
                if (OfferSalvoChoice(selected, cell)) return;
                int id = selected.Id;
                RunSafely(() => Perform(b => b.AttackAt(Side.Player, id, cell), deferImpacts: true));
=======
                int id = selected.Id;
                RunSafely(() => Perform(() => Battle.AttackAt(Side.Player, id, cell)));
>>>>>>> Stashed changes
                return;
            }
        }

<<<<<<< Updated upstream
        // A friendly model must remain selectable even on a resource school.
        if (Mode == OrderMode.None && hit?.Owner == Side.Player && hit.Id != selected?.Id)
        {
            ClearMode();
            SelectedVillageId = null;
            SelectedShipId = hit.Id;
            Refresh();
            return;
        }

=======
>>>>>>> Stashed changes
        // Resources belong to the fleet's collection reach, independent of selection.
        if (CanCommand && Mode == OrderMode.None && (Battle.CollectionCells(Side.Player).Contains(cell) || Battle.DockCells(Side.Player).Contains(cell)))
        {
            _resourceCell = cell;
            _resourceIsDock = Battle.DockCells(Side.Player).Contains(cell);
            int price = _resourceIsDock ? Battle.DockPrice(Side.Player) : Battle.CollectionCost(Side.Player);
            Refresh();
            Hud.ShowResource(_resourceIsDock, price, Battle.Credits(Side.Player) >= price);
            PositionActions();
            return;
        }

        if (selected?.Owner == Side.Player && CanCommand)
        {
            if ((hit is null || hit.IsAirborne || selected.IsAirborne) && SelectedRoutes()?.PathTo(cell).Count > 1)
            {
                int id = selected.Id;
<<<<<<< Updated upstream
                RunSafely(() => Perform(b => b.Move(Side.Player, id, cell)));
=======
                RunSafely(() => Perform(() => Battle.Move(Side.Player, id, cell)));
>>>>>>> Stashed changes
                return;
            }
        }

        ClearMode();
        SelectedShipId = hit?.Id;
        SelectedVillageId = hit is null ? Battle.ObservedVillages(Side.Player).FirstOrDefault(v => v.Position == cell)?.Id : null;
        Hud.ShowMessage("");
        Refresh();
    }

    private void PreviewAtScreen(Vector2 screen)
    {
        using var trace = DevAncientNaval.Presentation.Diagnostics.PerformanceTrace.Measure("Hover.Handler");
        if (!CanCommand || Selected is not { Owner: Side.Player } ship)
            return;
        var cell = BoardView.Projection.WorldToGrid(BoardView.ToLocal(MapCamera.ScreenToWorld(screen)));
        if (_previewCell == cell)
            return;
        _previewCell = cell;
        if (Mode == OrderMode.None)
        {
            BoardView.PreviewPath = SelectedRoutes()?.PathTo(cell) ?? Array.Empty<GridPosition>();
            BoardView.QueueRedraw();
        }

<<<<<<< Updated upstream
        if (Mode == OrderMode.None && Battle.ObservedAt(Side.Player, cell)is { } target && target.Owner != Side.Player && Battle.CanAttack(ship.Id, target.Id))
=======
        if (Mode == OrderMode.None && Battle.ObservedAt(Side.Player, cell) is { } target && target.Owner != Side.Player && Battle.CanAttack(ship.Id, target.Id))
>>>>>>> Stashed changes
            Hud.ShowCombatPreview(Math.Min(target.Health, Battle.Damage(ship, target)), Math.Min(ship.Health, Battle.PreviewCounterDamage(ship, target)));
    }

    public void CancelOrder()
    {
        if (Busy)
            return;
        ClearMode();
        SelectedShipId = null;
        SelectedVillageId = null;
        BoardView.Select(null);
        Hud.ShowTile(Battle, null);
        Hud.ShowMessage("");
        Refresh();
    }

    private void ClearMode()
    {
<<<<<<< Updated upstream
        _salvoCell = null;
        _salvoBattle = null;
=======
>>>>>>> Stashed changes
        Mode = OrderMode.None;
        _building = null;
        _resourceCell = null;
        _previewCell = null;
        Hud.CloseMenus();
        BoardView.PreviewPath = Array.Empty<GridPosition>();
    }
}
