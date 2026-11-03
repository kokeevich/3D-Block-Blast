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
}
