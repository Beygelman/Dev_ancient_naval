using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation.UI;
public partial class DebugHud
{
    private ActionPapyrus _claimPapyrus = null!, _treasuryPapyrus = null!;
    private readonly Dictionary<(bool Capture, int Id), ActionPapyrus> _stories = new();
    private readonly Dictionary<(bool Capture, int Id), GridPosition> _storyCells = new();
    private BattleState? _storyBattle;
    internal ActionPapyrus ClaimPapyrus => _claimPapyrus;
    internal ActionPapyrus TreasuryPapyrus => _treasuryPapyrus;
    public event Action<int>? CaptureStoryRequested, TreasuryStoryRequested;
    private ActionPapyrus MakeStory(bool capture, string name)
    {
        var scroll = new ActionPapyrus
        {
            Name = name,
            ArtworkPath = capture ? "res://assets/ui/harbor-claim-0.20.png" : "res://assets/ui/treasury-awakening-0.20.png",
            TooltipText = capture ? "Claim this harbor" : "Awaken the sunken treasury"
        };
        _root.AddChild(scroll);
        scroll.Pressed += () =>
        {
            var entry = _stories.FirstOrDefault(pair => ReferenceEquals(pair.Value, scroll));
            if (entry.Value is null) return;
            if (capture) CaptureStoryRequested?.Invoke(entry.Key.Id);
            else TreasuryStoryRequested?.Invoke(entry.Key.Id);
        };
        return scroll;
    }
    private void BuildActionStories()
    {
        _claimPapyrus = MakeStory(true, "HarborClaimPapyrus");
        _treasuryPapyrus = MakeStory(false, "TreasuryAwakeningPapyrus");
    }
    private void UpdateActionStories(BattleState battle, Ship? ship, Village? village, bool canAct)
    {
        if (!ReferenceEquals(_storyBattle, battle))
        {
            foreach (var scroll in _stories.Values)
            {
                scroll.SetReady(false);
                if (scroll != _claimPapyrus && scroll != _treasuryPapyrus) scroll.QueueFree();
            }
            _stories.Clear();
            _storyCells.Clear();
            _storyBattle = battle;
        }
        foreach (var scroll in _stories.Values) scroll.ActivationEnabled = canAct;
        // Temporary order/menu/opponent locks do not restart an opened banner.
        if (!canAct && !battle.IsOver && !battle.PlayerDefeated) return;
        var ready = new Dictionary<(bool Capture, int Id), GridPosition>();
        if (canAct)
        {
            foreach (var town in battle.Villages.Where(v => battle.CanCaptureVillage(Side.Player, v.Id)))
                ready[(true, town.Id)] = town.Position;
            foreach (var vessel in battle.OwnShips(Side.Player).Where(s => battle.CanLootTreasury(Side.Player, s.Id)))
                ready[(false, vessel.Id)] = vessel.Position;
        }
        foreach (var key in _stories.Keys.ToArray())
        {
            if (ready.ContainsKey(key) || _stories[key].IsConsuming) continue;
            var old = _stories[key];
            old.SetReady(false);
            _stories.Remove(key);
            _storyCells.Remove(key);
            if (old != _claimPapyrus && old != _treasuryPapyrus) old.QueueFree();
        }
        foreach (var (key, cell) in ready)
        {
            if (!_stories.TryGetValue(key, out var scroll))
            {
                var primary = key.Capture ? _claimPapyrus : _treasuryPapyrus;
                scroll = _stories.Values.Contains(primary) ? MakeStory(key.Capture, "ReadyStory" + key.Id) : primary;
                _stories.Add(key, scroll);
            }
            _storyCells[key] = cell;
            scroll.ActivationEnabled = canAct;
            scroll.SetReady(true);
        }
    }
    internal Task ConsumeStory(bool capture, int id) => _stories.TryGetValue((capture, id), out var scroll) ? scroll.ConsumeAsync() : Task.CompletedTask;
    public void PositionStories(Func<GridPosition, Vector2> screen)
    {
        foreach (var (key, cell) in _storyCells)
        {
            var scroll = _stories[key];
            var point = screen(cell);
            scroll.SetOnScreen(GetViewport().GetVisibleRect().Grow(30).HasPoint(point));
            scroll.Position = ClampWorldUi(UiScale.ScreenToUi(point) - new Vector2(scroll.Size.X / 2, 195 / UiScale.Value), scroll.Size);
        }
    }
    private Vector2 ClampWorldUi(Vector2 position, Vector2 size)
    {
        var viewport = UiScale.LogicalViewport(this);
        return new(Mathf.Clamp(position.X, 8, Math.Max(8, viewport.X - size.X - 8)),
            Mathf.Clamp(position.Y, 90, Math.Max(90, viewport.Y - size.Y - 70)));
    }
}
