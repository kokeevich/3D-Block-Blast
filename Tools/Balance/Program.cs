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
    public static void Main()
    {
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
            if (best==null) return (moves,lines,512-board.CountFreeCells(),true);
            if (!board.TryPlaceShape(best.Anchor,best.Cells)) throw new Exception("Agent selected an invalid move.");
            var clear=board.GetCompletedLines(); lines+=clear.LinesCleared; board.ClearLines(clear);
            offers[best.Slot]=null; moves++;
        }
        return (moves,lines,512-board.CountFreeCells(),false);
    }
    private static BoardModel Clone(BoardModel source)
    {
        var copy=new BoardModel();
        for (int x=0;x<8;x++) for (int y=0;y<8;y++) for (int z=0;z<8;z++)
            if (source.IsOccupied(x,y,z)) copy.SetOccupied(x,y,z,true);
        return copy;
    }
}
