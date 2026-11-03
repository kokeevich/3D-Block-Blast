using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Cell = BoardModel.Cell;

public class ShapeRulesTests
{
    private static readonly Cell[] Bar = { new Cell(0,0,0), new Cell(1,0,0), new Cell(2,0,0) };
    private static BoardModel Corridor(int length)
    {
        var board = new BoardModel();
        for (int x = 0; x < 8; x++)
        for (int y = 0; y < 8; y++)
        for (int z = 0; z < 8; z++) board.SetOccupied(x, y, z, true);
        for (int z = 0; z < length; z++) board.ClearCell(0, 0, z);
        return board;
    }

    [Test] public void CubeHas24DistinctProperOrientations()
    {
        var keys = new HashSet<string>();
        foreach (var o in ShapeRules.Orientations)
        {
            var cross = new Cell(o.Right.y * o.Up.z - o.Right.z * o.Up.y,
                o.Right.z * o.Up.x - o.Right.x * o.Up.z, o.Right.x * o.Up.y - o.Right.y * o.Up.x);
            Assert.AreEqual(cross, o.Forward); // right-handed, never mirrored
            keys.Add($"{o.Right.x},{o.Right.y},{o.Right.z}/{o.Up.x},{o.Up.y},{o.Up.z}");
        }
        Assert.AreEqual(24, keys.Count);
    }

    [Test] public void EveryRotationPreservesShapeAndNormalizesBounds()
    {
        var shape = new[] { new Cell(0,0,0), new Cell(1,0,0), new Cell(0,2,0), new Cell(0,0,3) };
        var variants = new HashSet<string>();
        for (int i = 0; i < 24; i++)
        {
            var rotated = ShapeRules.Rotate(shape, i);
            Assert.AreEqual(4, rotated.Distinct().Count());
            Assert.AreEqual(0, rotated.Min(c => c.x));
            Assert.AreEqual(0, rotated.Min(c => c.y));
            Assert.AreEqual(0, rotated.Min(c => c.z));
            Assert.AreEqual(1, SquaredDistance(rotated[0], rotated[1]));
            Assert.AreEqual(4, SquaredDistance(rotated[0], rotated[2]));
            Assert.AreEqual(9, SquaredDistance(rotated[0], rotated[3]));
            variants.Add(string.Join(";", rotated.Select(c => $"{c.x},{c.y},{c.z}").OrderBy(c => c)));
        }
        Assert.AreEqual(24, variants.Count);
        Assert.AreEqual(new Cell(0,2,0), shape[2]);
    }
    private static int SquaredDistance(Cell a, Cell b) =>
        (a.x-b.x)*(a.x-b.x) + (a.y-b.y)*(a.y-b.y) + (a.z-b.z)*(a.z-b.z);

    [TestCase(0)] [TestCase(1)] [TestCase(2)]
    public void FourQuarterTurnsReturnToOriginalOrientation(int axis)
    {
        for (int start = 0; start < 24; start++)
        {
            int rotation = start;
            for (int n = 0; n < 4; n++) rotation = ShapeRules.RotateOrientation(rotation, axis);
            Assert.AreEqual(start, rotation);
        }
    }

    [Test] public void OfferCanFitAfterRotationEvenWhenShownOrientationCannot()
    {
        var board = Corridor(3);
        var offer = new ShapeOffer(7, Bar, 0);
        Assert.IsFalse(board.CanPlaceAnywhere(offer.Cells));
        Assert.IsTrue(offer.TryFit(board, out var fitted, out var anchor));
        Assert.AreEqual(7, fitted.CatalogIndex);
        Assert.IsTrue(board.CanPlaceShape(anchor, fitted.Cells));
        Assert.AreEqual(0, offer.Orientation); // checking does not change the shown offer
    }

    [Test] public void PlayableSetKeepsItsConcreteOffers()
    {
        var offers = new List<ShapeOffer> { new ShapeOffer(5, Bar, 0) };
        var original = offers[0];
        Assert.IsTrue(OfferRules.EnsurePlayable(new BoardModel(), offers, new ShapeOffer[0]));
        Assert.AreSame(original, offers[0]);
    }

    [Test] public void ExhaustiveCatalogFallbackReplacesAnImpossibleOffer()
    {
        var board = Corridor(3);
        var tooLarge = new ShapeOffer(0, new[] { new Cell(0,0,0), new Cell(1,0,0), new Cell(2,0,0), new Cell(3,0,0) }, 0);
        var fits = new ShapeOffer(1, Bar, 0);
        var offers = new List<ShapeOffer> { tooLarge, tooLarge, tooLarge };
        Assert.IsTrue(OfferRules.EnsurePlayable(board, offers, new[] { tooLarge, fits }));
        Assert.AreEqual(1, offers[0].CatalogIndex);
        Assert.IsTrue(board.CanPlaceAnywhere(offers[0].Cells));
        Assert.AreSame(tooLarge, offers[1]);
    }

    [Test] public void NoMoveMeansAllOfferedRotationsFail()
    {
        var board = Corridor(2);
        Assert.IsFalse(OfferRules.AnyFits(board, new[] { new ShapeOffer(0, Bar, 0) }));
        var single = new ShapeOffer(1, new[] { new Cell(0,0,0) }, 0);
        Assert.IsTrue(OfferRules.AnyFits(board, new[] { new ShapeOffer(0, Bar, 0), single }));
        Assert.IsFalse(OfferRules.AnyFits(board, new ShapeOffer[0]));
    }

    [Test] public void FullBoardDoesNotReceiveAPretendPlayableSet()
    {
        var offer = new ShapeOffer(0, Bar, 0);
        Assert.IsFalse(OfferRules.EnsurePlayable(Corridor(0), new List<ShapeOffer> { offer }, new[] { offer }));
    }

    [Test] public void OfferOwnsCopiesOfItsGeometry()
    {
        var cells = new[] { new Cell(0,0,0), new Cell(1,0,0) };
        var offer = new ShapeOffer(0, cells, 0);
        cells[1] = new Cell(7,7,7);
        var result = offer.Cells;
        result[1] = new Cell(6,6,6);
        Assert.AreEqual(new Cell(1,0,0), offer.Cells[1]);
    }
}
