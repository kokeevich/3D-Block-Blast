using System;

/// <summary>Successful moves only; peak occupancy includes cells about to be cleared.</summary>
public sealed class RunStatistics
{
    public int Moves { get; private set; }
    public int Lines { get; private set; }
    public int PeakOccupied { get; private set; }
    public double PeakPercent => 100.0*PeakOccupied/(BoardModel.Size*BoardModel.Size*BoardModel.Size);
    public void Restore(int moves,int lines,int peak)
    { Moves=moves; Lines=lines; PeakOccupied=peak; }
    public void RecordPlacement(BoardModel.ClearResult clear,int occupiedAfterClear)
    {
        if (clear==null) throw new ArgumentNullException(nameof(clear));
        int beforeClear=occupiedAfterClear+clear.Cells.Count;
        if (occupiedAfterClear<0 || beforeClear>BoardModel.Size*BoardModel.Size*BoardModel.Size)
            throw new ArgumentOutOfRangeException(nameof(occupiedAfterClear));
        Moves++; Lines+=clear.LinesCleared;
        PeakOccupied=Math.Max(PeakOccupied,beforeClear);
    }
}
