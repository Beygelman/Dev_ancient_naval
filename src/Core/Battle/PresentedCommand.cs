using DevAncientNaval.Core.Units;

namespace DevAncientNaval.Core.Battle;
/// <summary>A deterministic command calculated on an isolated aggregate. The live
/// battle advances only at presentation impact boundaries; simulation callers can
/// continue using the immediate command facade.</summary>
public sealed class PresentedCommand
{
    private readonly BattleState _owner;
    private readonly BattleSave _final;
    private readonly Dictionary<string, BattleSave> _frames;
    private readonly HashSet<string> _resolved = new();
    public CommandResult Result { get; }
    public IReadOnlyList<ShipSnapshot> DestroyedShips { get; }
    public bool Complete { get; private set; }

    internal PresentedCommand(BattleState owner, CommandResult result, BattleSave final, Dictionary<string, BattleSave> frames, BattleSave initial)
    {
        _owner = owner;
        Result = result;
        _final = final;
        _frames = frames;
        DestroyedShips = initial.Ships.Where(s => !final.Ships.Any(t => t.Id == s.Id)).Select(s => ShipSnapshot.From(s.Restore(owner.Rules))).ToArray();
    }

    public void Impact(string key)
    {
        if (Complete || !_resolved.Add(key) || !_frames.TryGetValue(key, out var frame))
            return;
        _owner.ApplyPresentationFrame(frame, preserveFollowers: true);
    }

    public void Finish()
    {
        if (Complete)
            return;
        _owner.ApplyPresentationFrame(_final, preserveFollowers: false);
        Complete = true;
        _owner.ReleasePresentation(this);
    }
}

public sealed partial class BattleState
{
    private Action<string>? _captureImpact;
    public PresentedCommand? PendingPresentation { get; private set; }

    private void RecordImpact(string key) => _captureImpact?.Invoke(key);
    public PresentedCommand Prepare(Func<BattleState, CommandResult> command)
    {
        if (PendingPresentation is not null)
            throw new InvalidOperationException("Finish the current presentation before preparing another command.");
        var initial = CreateSnapshot();
        var simulation = new BattleState(Board, Rules);
        simulation.ApplyPresentationFrame(initial, false);
        var frames = new Dictionary<string, BattleSave>();
        simulation._captureImpact = key => frames[key] = simulation.CreateSnapshot(initial.Board);
        var result = command(simulation);
        var prepared = new PresentedCommand(this, result, simulation.CreateSnapshot(initial.Board), frames, initial);
        PendingPresentation = prepared;
        if (!result.Success || frames.Count == 0)
            prepared.Finish();
        return prepared;
    }

    internal void ApplyPresentationFrame(BattleSave saved, bool preserveFollowers)
    {
        // A flagship hit removes it at impact. Its other ships stay until the
        // flagship's visible wreck has submerged, then the presentation releases them.
        var followers = preserveFollowers ? _ships.Where(s => !s.IsMothership && !saved.Ships.Any(t => t.Id == s.Id) && _ships.Any(m => m.Owner == s.Owner && m.IsMothership && !saved.Ships.Any(t => t.Id == m.Id))).ToArray() : Array.Empty<Ship>();
        _treasuries.Clear();
        _whirlpools.Clear();
        _forbidden.Clear();
        _captureWaits.Clear();
        _treasuryWaits.Clear();
        _pirateHomes.Clear();
        _treasuryOutcomes.Clear();
        RestoreEntities(saved);
        _ships.AddRange(followers);
        RestoreProgress(saved);
        Vision.Restore(saved.Vision, _factions);
        UpdateVision();
    }

    public void CompleteFleetCollapse(Side side)
    {
        if (Mothership(side)is null)
            EliminateFaction(side);
        UpdateVision();
    }

    internal void ReleasePresentation(PresentedCommand command)
    {
        if (ReferenceEquals(PendingPresentation, command))
            PendingPresentation = null;
    }
}
