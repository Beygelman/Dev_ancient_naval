namespace DevAncientNaval.Core.Battle;
public enum AiDifficulty
{
    Boatswain,
    Captain,
    Admiral
}

public sealed partial class BattleState
{
    public bool GodEye { get; private set; }
    public bool FullMapVisible => GodEye || Winner == Units.Side.Player;
    public AiDifficulty Difficulty { get; private set; } = AiDifficulty.Captain;
    public bool PiratesEnabled { get; private set; } = true;

    public void SetGodEye(bool enabled)
    {
        GodEye = enabled;
        UpdateVision();
    }

    public void SetDifficulty(AiDifficulty difficulty)
    {
        if (!Enum.IsDefined(difficulty))
            throw new ArgumentException("Invalid difficulty.");
        Difficulty = difficulty;
    }
}
