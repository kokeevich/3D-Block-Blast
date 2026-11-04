using System;
using NUnit.Framework;
using Cell=BoardModel.Cell;
using Direction=PlacementFrame.Direction;

public class ManualPlacementTests
{
    [TestCase(0,1)] [TestCase(0,-1)] [TestCase(1,1)] [TestCase(1,-1)] [TestCase(2,1)] [TestCase(2,-1)]
    public void InitialPreviewIsInsideTheBoardForAllFacesAndRotations(int axis,int sign)
    {
        var normal=axis==0 ? new Direction(sign,0,0) : axis==1 ? new Direction(0,sign,0) : new Direction(0,0,sign);
        var frame=new PlacementFrame(normal,new Direction(1,0,0),new Direction(0,1,0));
        foreach (var piece in BalanceRules.Catalog)
        for (int rotation=0;rotation<24;rotation++)
        {
            var cells=ShapeRules.Rotate(piece.Cells,rotation);
            Assert.IsTrue(new BoardModel().CanPlaceShape(frame.StartAnchor(cells),cells),piece.Name);
        }
    }
    [Test] public void OccupiedDefaultPositionDoesNotMoveThePreviewOrMeanGameOver()
    {
        var frame=new PlacementFrame(new Direction(0,0,-1),new Direction(1,0,0),new Direction(0,1,0));
        var shape=new[] { new Cell(0,0,0),new Cell(1,0,0),new Cell(2,0,0) };
        var anchor=frame.StartAnchor(shape);
        var board=new BoardModel(); board.TryPlaceShape(anchor,shape);
        Assert.AreEqual(anchor,frame.StartAnchor(shape));
        Assert.IsFalse(board.CanPlaceShape(anchor,shape));
        Assert.IsTrue(OfferRules.AnyFits(board,new[] { new ShapeOffer(0,shape,0) }));
    }
    [Test] public void StatisticsIncludeThePeakBeforeIntersectingLinesDisappear()
    {
        var board=new BoardModel();
        for (int t=0;t<8;t++) { board.SetOccupied(t,3,4,true); board.SetOccupied(2,t,4,true); board.SetOccupied(2,3,t,true); }
        var clear=board.GetCompletedLines(); board.ClearLines(clear);
        var stats=new RunStatistics(); stats.RecordPlacement(clear,512-board.CountFreeCells());
        Assert.AreEqual(1,stats.Moves); Assert.AreEqual(3,stats.Lines);
        Assert.AreEqual(22,stats.PeakOccupied); Assert.AreEqual(22*100.0/512,stats.PeakPercent,0.001);
        board.TryPlaceShape(new Cell(0,0,0),new[] { new Cell(0,0,0) });
        stats.RecordPlacement(board.GetCompletedLines(),1);
        Assert.AreEqual(2,stats.Moves); Assert.AreEqual(3,stats.Lines); Assert.AreEqual(22,stats.PeakOccupied);
    }
    [Test] public void CompleteBoardCanRecordA100PercentPeakAfterItIsCleared()
    {
        var board=new BoardModel();
        for (int x=0;x<8;x++) for (int y=0;y<8;y++) for (int z=0;z<8;z++) board.SetOccupied(x,y,z,true);
        var clear=board.GetCompletedLines(); board.ClearLines(clear);
        var stats=new RunStatistics(); stats.RecordPlacement(clear,0);
        Assert.AreEqual(100,stats.PeakPercent); Assert.AreEqual(192,stats.Lines);
    }
    [Test] public void RejectedStatisticsDoNotAddAMove()
    {
        var stats=new RunStatistics();
        Assert.Throws<ArgumentNullException>(() => stats.RecordPlacement(null,0));
        Assert.AreEqual(0,stats.Moves);
    }
}
