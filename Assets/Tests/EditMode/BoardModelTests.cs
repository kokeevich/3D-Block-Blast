using NUnit.Framework;
using Cell = BoardModel.Cell;
using Plane = BoardModel.Plane;
using Axis = BoardModel.PlaneAxis;

public class BoardModelTests
{
    private BoardModel board;
    private static readonly Cell[] Single = { new Cell(0, 0, 0) };

    [SetUp] public void SetUp() => board = new BoardModel();

    [Test] public void NewBoardHas512FreeCells()
    {
        Assert.AreEqual(512, board.CountFreeCells());
        Assert.AreEqual(512, board.GetFreeCells().Count);
    }

    [Test] public void PlacementUsesAnchorAndOffsets()
    {
        var shape = new[] { new Cell(0, 0, 0), new Cell(1, 0, 1) };
        Assert.IsTrue(board.TryPlaceShape(new Cell(3, 2, 4), shape));
        Assert.IsTrue(board.IsOccupied(3, 2, 4));
        Assert.IsTrue(board.IsOccupied(4, 2, 5));
        Assert.IsTrue(board.IsFree(0, 0, 0));
        Assert.AreEqual(510, board.CountFreeCells());
    }

    [TestCase(-1, 0, 0)] [TestCase(0, 8, 0)] [TestCase(0, 0, 8)]
    public void OutsidePlacementDoesNotMutateBoard(int x, int y, int z)
    {
        Assert.IsFalse(board.TryPlaceShape(new Cell(x, y, z), Single));
        Assert.AreEqual(512, board.CountFreeCells());
    }

    [Test] public void OverlapRejectsWholeShape()
    {
        board.TryPlaceShape(new Cell(2, 0, 0), Single);
        Assert.IsFalse(board.TryPlaceShape(new Cell(1, 0, 0),
            new[] { new Cell(0, 0, 0), new Cell(1, 0, 0) }));
        Assert.IsTrue(board.IsFree(1, 0, 0));
        Assert.AreEqual(511, board.CountFreeCells());
    }

    [Test] public void DuplicateAndEmptyShapesAreRejected()
    {
        Assert.IsFalse(board.TryPlaceShape(Cell.zero, new[] { Cell.zero, Cell.zero }));
        Assert.IsFalse(board.TryPlaceShape(Cell.zero, null));
        Assert.IsFalse(board.TryPlaceShape(Cell.zero, new Cell[0]));
        Assert.AreEqual(512, board.CountFreeCells());
    }

    [TestCase(Axis.XY)] [TestCase(Axis.XZ)] [TestCase(Axis.YZ)]
    public void FullPlaneIsDetectedAndClearedWithoutMovingOtherCells(Axis axis)
    {
        for (int a = 0; a < 8; a++)
        for (int b = 0; b < 8; b++)
        {
            var c = axis == Axis.XY ? new Cell(a, b, 3) :
                axis == Axis.XZ ? new Cell(a, 3, b) : new Cell(3, a, b);
            board.SetOccupied(c.x, c.y, c.z, true);
        }
        board.SetOccupied(0, 0, 0, true);
        var plane = new Plane(axis, 3);
        Assert.IsTrue(board.IsPlaneFull(plane));
        Assert.AreEqual(1, board.GetCompletedPlanes().Count);
        board.ClearPlane(plane);
        Assert.IsFalse(board.IsPlaneFull(plane));
        Assert.IsTrue(board.IsOccupied(0, 0, 0));
        Assert.AreEqual(511, board.CountFreeCells());
    }

    [Test] public void MissingCellPreventsPlaneCompletion()
    {
        for (int x = 0; x < 8; x++)
        for (int y = 0; y < 8; y++) board.SetOccupied(x, y, 0, true);
        board.ClearCell(7, 7, 0);
        Assert.IsFalse(board.IsPlaneFull(new Plane(Axis.XY, 0)));
        Assert.IsEmpty(board.GetCompletedPlanes());
    }

    [Test] public void FullBoardHasNoMoveAndClearingRestoresMove()
    {
        for (int x = 0; x < 8; x++)
        for (int y = 0; y < 8; y++)
        for (int z = 0; z < 8; z++) board.SetOccupied(x, y, z, true);
        Assert.IsFalse(board.CanPlaceAnywhere(Single));
        board.ClearCell(6, 5, 4);
        Assert.IsTrue(board.TryFindPlacement(Single, out var anchor));
        Assert.AreEqual(new Cell(6, 5, 4), anchor);
        Assert.IsFalse(board.CanPlaceAnywhere(new[] { Cell.zero, new Cell(1, 0, 0) }));
    }

    [Test] public void IntersectingPlanesCanBeClearedFromOneSnapshot()
    {
        for (int a = 0; a < 8; a++)
        for (int b = 0; b < 8; b++)
        {
            board.SetOccupied(a, b, 3, true);
            board.SetOccupied(a, 3, b, true);
        }
        var planes = board.GetCompletedPlanes();
        Assert.AreEqual(2, planes.Count);
        Assert.AreEqual(392, board.CountFreeCells());
        foreach (var plane in planes) board.ClearPlane(plane);
        Assert.AreEqual(512, board.CountFreeCells());
    }

    [Test] public void InvalidPlaneIsIgnored()
    {
        board.SetOccupied(0, 0, 0, true);
        board.ClearPlane(new Plane(Axis.XY, -1));
        Assert.IsFalse(board.IsPlaneFull(new Plane((Axis)99, 0)));
        Assert.AreEqual(511, board.CountFreeCells());
    }
    [TestCase(0)] [TestCase(1)] [TestCase(2)]
    public void FullLineClearsOnEveryAxis(int axis)
    {
        for (int t = 0; t < 8; t++)
            board.SetOccupied(axis == 0 ? t : 2, axis == 1 ? t : 3, axis == 2 ? t : 4, true);
        board.SetOccupied(7, 7, 7, true);
        var result = board.GetCompletedLines();
        Assert.AreEqual(1, result.LinesCleared);
        Assert.AreEqual(8, result.Cells.Count);
        Assert.AreEqual(0, result.PlanesCompleted);
        Assert.AreEqual(13, result.Score(3));
        board.ClearLines(result);
        Assert.AreEqual(511, board.CountFreeCells());
        Assert.IsTrue(board.IsOccupied(7, 7, 7));
        Assert.AreEqual(0, board.GetCompletedLines().LinesCleared);
    }

    [Test] public void SevenCellsDoNotClear()
    {
        for (int x = 0; x < 7; x++) board.SetOccupied(x, 2, 3, true);
        var result = board.GetCompletedLines();
        Assert.AreEqual(0, result.LinesCleared);
        Assert.IsEmpty(result.Cells);
        Assert.AreEqual(4, result.Score(4));
    }

    [Test] public void ThreeIntersectingLinesRemoveSharedCellOnce()
    {
        for (int t = 0; t < 8; t++)
        {
            board.SetOccupied(t, 3, 4, true);
            board.SetOccupied(2, t, 4, true);
            board.SetOccupied(2, 3, t, true);
        }
        var result = board.GetCompletedLines();
        Assert.AreEqual(3, result.LinesCleared);
        Assert.AreEqual(22, result.Cells.Count);
        Assert.AreEqual(10, result.ComboBonus);
        Assert.AreEqual(42, result.Score(2));
        board.ClearLines(result);
        Assert.AreEqual(512, board.CountFreeCells());
        board.ClearLines(result);
        Assert.AreEqual(512, board.CountFreeCells());
    }

    [Test] public void ParallelLinesClearTogether()
    {
        for (int t = 0; t < 8; t++)
        {
            board.SetOccupied(t, 0, 0, true);
            board.SetOccupied(t, 7, 7, true);
        }
        var result = board.GetCompletedLines();
        Assert.AreEqual(2, result.LinesCleared);
        Assert.AreEqual(16, result.Cells.Count);
        Assert.AreEqual(5, result.ComboBonus);
        board.ClearLines(result);
        Assert.AreEqual(512, board.CountFreeCells());
    }

    [TestCase(Axis.XY)] [TestCase(Axis.XZ)] [TestCase(Axis.YZ)]
    public void FullPlaneAwardsBonusBeforeLinesAreRemoved(Axis axis)
    {
        for (int a = 0; a < 8; a++)
        for (int b = 0; b < 8; b++)
            board.SetOccupied(axis == Axis.YZ ? 3 : a,
                axis == Axis.XZ ? 3 : axis == Axis.YZ ? a : b,
                axis == Axis.XY ? 3 : b, true);
        var result = board.GetCompletedLines();
        Assert.AreEqual(16, result.LinesCleared);
        Assert.AreEqual(1, result.PlanesCompleted);
        Assert.AreEqual(64, result.Cells.Count);
        Assert.AreEqual(100, result.PlaneBonus);
        Assert.AreEqual(339, result.Score(4));
        board.ClearLines(result);
        Assert.AreEqual(512, board.CountFreeCells());
        Assert.AreEqual(1, result.PlanesCompleted); // snapshot survives clearing
    }

    [Test] public void FullCubeCountsAllLinesAndPlanesWithoutDuplicateCells()
    {
        for (int x = 0; x < 8; x++)
        for (int y = 0; y < 8; y++)
        for (int z = 0; z < 8; z++) board.SetOccupied(x, y, z, true);
        var result = board.GetCompletedLines();
        Assert.AreEqual(192, result.LinesCleared);
        Assert.AreEqual(24, result.PlanesCompleted);
        Assert.AreEqual(512, result.Cells.Count);
        board.ClearLines(result);
        Assert.AreEqual(512, board.CountFreeCells());
        Assert.AreEqual(0, board.GetCompletedLines().Score(0));
    }
}
