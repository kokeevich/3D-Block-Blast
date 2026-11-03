using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public class BalanceAndTurnTests
{
    [TestCase(100,10,1,-1)] [TestCase(-100,10,1,1)]
    [TestCase(10,100,0,1)] [TestCase(10,-100,0,-1)]
    public void SwipeChoosesOneWorldAxis(float x,float y,int expectedAxis,int expectedDirection)
    {
        Assert.IsTrue(BoardTurnRules.TryGetTurn(x,y,20,out int axis,out int direction));
        Assert.AreEqual(expectedAxis,axis); Assert.AreEqual(expectedDirection,direction);
    }
    [Test] public void SmallMovementDoesNotTurnTheBoard()
    { Assert.IsFalse(BoardTurnRules.TryGetTurn(10,10,20,out _,out _)); }
    [Test] public void QuarterTurnsAndTheirInversesStayInThe24GridPoses()
    {
        for (int start=0;start<24;start++)
        for (int axis=0;axis<2;axis++)
        {
            int next=BoardTurnRules.Apply(start,axis,1);
            Assert.AreNotEqual(start,next);
            Assert.AreEqual(start,BoardTurnRules.Apply(next,axis,-1));
            int pose=start;
            for (int i=0;i<4;i++) pose=BoardTurnRules.Apply(pose,axis,1);
            Assert.AreEqual(start,pose);
        }
    }
    [TestCase(0,3,8)] [TestCase(8,3,8)] [TestCase(9,5,12)]
    [TestCase(26,5,12)] [TestCase(27,7,16)] [TestCase(100,7,16)]
    public void GeneratedSetsFollowDifficultyAndStayPlayableOnAnEmptyBoard(int moves,int min,int max)
    {
        var board=new BoardModel(); var random=new Random(815);
        for (int set=0;set<40;set++)
        {
            var offers=BalanceRules.GenerateOffers(board,moves,random);
            Assert.AreEqual(3,offers.Count);
            Assert.AreEqual(3,offers.Select(o => o.CatalogIndex).Distinct().Count());
            foreach (var offer in offers) Assert.That(offer.Cells.Length,Is.InRange(min,max));
            Assert.IsTrue(OfferRules.AnyFits(board,offers));
        }
    }
    [Test] public void CatalogGeometryIsConnectedUniqueAndFitsAllRotations()
    {
        var board=new BoardModel();
        foreach (var piece in BalanceRules.Catalog)
        {
            var cells=piece.Cells; var all=new HashSet<BoardModel.Cell>(cells);
            Assert.AreEqual(cells.Length,all.Count,piece.Name);
            var seen=new HashSet<BoardModel.Cell> { cells[0] }; var queue=new Queue<BoardModel.Cell>(); queue.Enqueue(cells[0]);
            while (queue.Count>0)
            {
                var c=queue.Dequeue();
                foreach (var d in new[] { new BoardModel.Cell(1,0,0),new BoardModel.Cell(-1,0,0),
                    new BoardModel.Cell(0,1,0),new BoardModel.Cell(0,-1,0),new BoardModel.Cell(0,0,1),new BoardModel.Cell(0,0,-1) })
                {
                    var next=new BoardModel.Cell(c.x+d.x,c.y+d.y,c.z+d.z);
                    if (all.Contains(next) && seen.Add(next)) queue.Enqueue(next);
                }
            }
            Assert.AreEqual(cells.Length,seen.Count,piece.Name);
            for (int r=0;r<24;r++) Assert.IsTrue(board.CanPlaceAnywhere(ShapeRules.Rotate(cells,r)),piece.Name);
        }
    }
    [TestCase(0)] [TestCase(9)] [TestCase(27)]
    public void ScatteredFreeCellsCanActuallyEndTheParty(int moves)
    {
        var board=new BoardModel();
        for (int x=0;x<8;x++) for (int y=0;y<8;y++) for (int z=0;z<8;z++)
            board.SetOccupied(x,y,z,true);
        // 64 isolated holes, but no connected three-cell (or larger) placement.
        for (int x=0;x<8;x+=2) for (int y=0;y<8;y+=2) for (int z=0;z<8;z+=2) board.ClearCell(x,y,z);
        Assert.AreEqual(64,board.CountFreeCells());
        var offers=BalanceRules.GenerateOffers(board,moves,new Random(33));
        Assert.IsFalse(OfferRules.AnyFits(board,offers));
    }
    [Test] public void FallbackNeverBringsBackRetiredTinyPieces()
    {
        var board=new BoardModel();
        for (int x=0;x<8;x++) for (int y=0;y<8;y++) for (int z=0;z<8;z++) board.SetOccupied(x,y,z,true);
        for (int z=0;z<3;z++) board.ClearCell(0,0,z);
        Assert.IsTrue(OfferRules.AnyFits(board,BalanceRules.GenerateOffers(board,0,new Random(1))));
        Assert.IsFalse(OfferRules.AnyFits(board,BalanceRules.GenerateOffers(board,27,new Random(1))));
    }
    [Test] public void DifficultyIncreasesExpectedPieceSize()
    {
        double[] averages=new double[3];
        int[] moves={ 0,9,27 };
        for (int stage=0;stage<3;stage++)
        {
            double weight=0;
            foreach (var piece in BalanceRules.Catalog)
            { float w=BalanceRules.Weight(piece.Count,moves[stage]); weight+=w; averages[stage]+=w*piece.Count; }
            averages[stage]/=weight;
        }
        Assert.Greater(averages[1],averages[0]+1);
        Assert.Greater(averages[2],averages[1]+1);
    }
    [Test] public void SeededGenerationCanBeReproduced()
    {
        var a=BalanceRules.GenerateOffers(new BoardModel(),27,new Random(42));
        var b=BalanceRules.GenerateOffers(new BoardModel(),27,new Random(42));
        CollectionAssert.AreEqual(a.Select(o => o.CatalogIndex),b.Select(o => o.CatalogIndex));
        CollectionAssert.AreEqual(a.Select(o => o.Orientation),b.Select(o => o.Orientation));
    }
}
