using System;
using System.Collections.Generic;
using System.Linq;
using Cell=BoardModel.Cell;

// A repeatable smoke simulation, not a solver or a forecast of human game length.
public static class Program
{
    private sealed class Move { public int Slot; public Cell Anchor; public Cell[] Cells; public double Value; }
    private static readonly Cell[][][] variants=BalanceRules.Catalog.Select(p =>
        Enumerable.Range(0,24).Select(r => ShapeRules.Rotate(p.Cells,r)).ToArray()).ToArray();
    public static void Main(string[] args)
    {
        if (args.Contains("--paired")) { RunPaired(); return; }
        Console.WriteLine("agent,seed,moves,lines,occupied,game_over");
        foreach (bool greedy in new[] { false,true })
        {
            var lengths=new List<int>(); int ended=0;
            for (int seed=0;seed<12;seed++)
            {
                var result=Run(seed,greedy); lengths.Add(result.moves); if (result.ended) ended++;
                Console.WriteLine($"{(greedy ? "clear-seeking" : "sampled-random")},{seed},{result.moves},{result.lines},{result.occupied},{result.ended}");
            }
            lengths.Sort();
            Console.WriteLine($"# {(greedy ? "clear-seeking" : "sampled-random")}: median={(lengths[5]+lengths[6])/2.0}, range={lengths[0]}..{lengths[11]}, ended={ended}/12");
        }
    }
    private static (int moves,int lines,int occupied,bool ended) Run(int seed,bool greedy)
    {
        var generation=new Random(seed+900); var actions=new Random(seed+100);
        var board=new BoardModel(); List<ShapeOffer> offers=null; int moves=0,lines=0;
        while (moves<240)
        {
            if (offers==null || offers.All(o => o==null)) offers=BalanceRules.GenerateOffers(board,moves,generation);
            Move best=FindMove(board,offers,actions,greedy);
            if (best==null) return (moves,lines,512-board.CountFreeCells(),true);
            if (!board.TryPlaceShape(best.Anchor,best.Cells)) throw new Exception("Agent selected an invalid move.");
            var clear=board.GetCompletedLines(); lines+=clear.LinesCleared; board.ClearLines(clear);
            offers[best.Slot]=null; moves++;
        }
        return (moves,lines,512-board.CountFreeCells(),false);
    }
    private static Move FindMove(BoardModel board,List<ShapeOffer> offers,Random actions,bool greedy)
    {
            Move best=null;
            for (int slot=0;slot<offers.Count;slot++)
            {
                var offer=offers[slot];
                if (offer==null || !offer.TryFit(board,out var fitted,out var anchor)) continue;
                var candidates=new List<Move> { new Move { Slot=slot,Anchor=anchor,Cells=fitted.Cells } };
                // Include a fitting fallback plus up to eight random valid poses.
                for (int attempt=0;attempt<180 && candidates.Count<9;attempt++)
                {
                    var cells=variants[offer.CatalogIndex][actions.Next(24)];
                    var pos=new Cell(actions.Next(8),actions.Next(8),actions.Next(8));
                    if (board.CanPlaceShape(pos,cells)) candidates.Add(new Move { Slot=slot,Anchor=pos,Cells=cells });
                }
                foreach (var candidate in candidates)
                {
                    candidate.Value=actions.NextDouble();
                    if (greedy)
                    {
                        var probe=Clone(board); probe.TryPlaceShape(candidate.Anchor,candidate.Cells);
                        candidate.Value+=probe.GetCompletedLines().Cells.Count*100;
                        foreach (var c in candidate.Cells)
                        {
                            int x=c.x+candidate.Anchor.x,y=c.y+candidate.Anchor.y,z=c.z+candidate.Anchor.z;
                            foreach (var n in new[] { new Cell(x+1,y,z),new Cell(x-1,y,z),new Cell(x,y+1,z),
                                new Cell(x,y-1,z),new Cell(x,y,z+1),new Cell(x,y,z-1) })
                                if (board.IsOccupied(n.x,n.y,n.z)) candidate.Value+=2;
                        }
                    }
                    if (best==null || candidate.Value>best.Value) best=candidate;
                }
            }
        return best;
    }
    private static void RunPaired()
    {
        Console.WriteLine("agent,seed,sequence_id,score,moves,lines,peak_percent,stop");
        for (int seed=0;seed<12;seed++)
        {
            // Freeze the draws before either agent sees a board. Runtime rescue
            // is intentionally excluded here: it would change offers based on
            // each agent's board, defeating an identical-sequence comparison.
            var random=new Random(seed+900);
            var tape=Enumerable.Range(0,80).Select(set => BalanceRules.GenerateOffers(new BoardModel(),set*3,random)).ToArray();
            uint checksum=2166136261;
            foreach (var set in tape) foreach (var offer in set)
                checksum=unchecked((checksum^(uint)(offer.CatalogIndex*24+offer.Orientation))*16777619);
            foreach (bool manual in new[] { false,true })
            {
                var result=Replay(tape,seed,manual);
                Console.WriteLine(FormattableString.Invariant($"{(manual ? "clear-seeking" : "confirm-default")},{seed},{checksum:X8},{result.score},{result.stats.Moves},{result.stats.Lines},{result.stats.PeakPercent:0.0},{result.stop}"));
            }
        }
    }
    private static (int score,RunStatistics stats,string stop) Replay(List<ShapeOffer>[] tape,int seed,bool manual)
    {
        var board=new BoardModel(); var stats=new RunStatistics(); var actions=new Random(seed+100);
        var frame=new PlacementFrame(new PlacementFrame.Direction(0,0,-1),
            new PlacementFrame.Direction(1,0,0),new PlacementFrame.Direction(0,1,0));
        List<ShapeOffer> offers=null; int next=0,score=0;
        while (stats.Moves<240)
        {
            if (offers==null || offers.All(o => o==null))
            {
                if (next>=tape.Length) return (score,stats,"cap");
                offers=tape[next++].ToList();
            }
            if (!OfferRules.AnyFits(board,offers)) return (score,stats,"game_over");
            Move move=null;
            if (manual) move=FindMove(board,offers,actions,true);
            else
                for (int slot=0;slot<offers.Count;slot++)
                {
                    if (offers[slot]==null) continue;
                    var cells=offers[slot].Cells; var anchor=frame.StartAnchor(cells);
                    if (board.CanPlaceShape(anchor,cells)) { move=new Move { Slot=slot,Anchor=anchor,Cells=cells }; break; }
                }
            // A red preview with legal moves elsewhere is NOT a loss.
            if (move==null) return (score,stats,"needs_manual_move");
            if (!board.TryPlaceShape(move.Anchor,move.Cells)) throw new Exception("Invalid replay move.");
            var clear=board.GetCompletedLines(); score+=clear.Score(move.Cells.Length); board.ClearLines(clear);
            stats.RecordPlacement(clear,512-board.CountFreeCells()); offers[move.Slot]=null;
        }
        return (score,stats,"cap");
    }

    private static BoardModel Clone(BoardModel source)
    {
        var copy=new BoardModel();
        for (int x=0;x<8;x++) for (int y=0;y<8;y++) for (int z=0;z<8;z++)
            if (source.IsOccupied(x,y,z)) copy.SetOccupied(x,y,z,true);
        return copy;
    }
}
