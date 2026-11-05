using System;

/// <summary>One natural break per finished game; cooldown counts actual presentations, not loads.</summary>
public sealed class AdExposureRules
{
    public const int CooldownSeconds=120;
    public int Losses { get; private set; }
    public long LastShown { get; private set; }
    public AdExposureRules(int losses=0,long lastShown=0)
    { Losses=Math.Max(0,losses); LastShown=Math.Max(0,lastShown); }
    public void RecordLoss() { if (Losses<int.MaxValue) Losses++; }
    public bool CanShow(long now) => Losses>=2 && now>=LastShown && now-LastShown>=CooldownSeconds;
    public void RecordShown(long now) { LastShown=Math.Max(LastShown,now); }
}
