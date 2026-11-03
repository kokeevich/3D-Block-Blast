using System;
using System.Collections.Generic;
using Cell = BoardModel.Cell;

/// <summary>The 24 proper cube rotations; mirrored shapes are never introduced.</summary>
public static class ShapeRules
{
    public struct Orientation
    {
        public Cell Right, Up, Forward;
        public Orientation(Cell right, Cell up)
        {
            Right = right; Up = up;
            Forward = new Cell(right.y * up.z - right.z * up.y,
                right.z * up.x - right.x * up.z, right.x * up.y - right.y * up.x);
        }
    }

    private static readonly Orientation[] orientations = BuildOrientations();
    public static IReadOnlyList<Orientation> Orientations => Array.AsReadOnly(orientations);

    private static Orientation[] BuildOrientations()
    {
        var axes = new[] { new Cell(1,0,0), new Cell(0,1,0), new Cell(0,0,1),
            new Cell(-1,0,0), new Cell(0,-1,0), new Cell(0,0,-1) };
        var result = new List<Orientation>();
        foreach (var right in axes)
        foreach (var up in axes)
            if (right.x * up.x + right.y * up.y + right.z * up.z == 0)
                result.Add(new Orientation(right, up));
        return result.ToArray();
    }

    public static int RotateOrientation(int index, int axis)
    {
        if (axis < 0 || axis > 2) throw new ArgumentOutOfRangeException(nameof(axis));
        var o = orientations[index];
        Cell right = QuarterTurn(o.Right, axis), up = QuarterTurn(o.Up, axis);
        for (int i = 0; i < orientations.Length; i++)
            if (orientations[i].Right.Equals(right) && orientations[i].Up.Equals(up)) return i;
        throw new InvalidOperationException("Rotation left the cube orientation group.");
    }
    private static Cell QuarterTurn(Cell c, int axis) => axis == 0 ? new Cell(c.x, -c.z, c.y) :
        axis == 1 ? new Cell(c.z, c.y, -c.x) : new Cell(-c.y, c.x, c.z);

    public static Cell[] Rotate(Cell[] shape, int index)
    {
        if (shape == null || shape.Length == 0) return new Cell[0];
        var o = orientations[index];
        var result = new Cell[shape.Length];
        int minX = int.MaxValue, minY = int.MaxValue, minZ = int.MaxValue;
        for (int i = 0; i < shape.Length; i++)
        {
            var c = shape[i];
            var r = new Cell(c.x * o.Right.x + c.y * o.Up.x + c.z * o.Forward.x,
                c.x * o.Right.y + c.y * o.Up.y + c.z * o.Forward.y,
                c.x * o.Right.z + c.y * o.Up.z + c.z * o.Forward.z);
            result[i] = r;
            minX = Math.Min(minX, r.x); minY = Math.Min(minY, r.y); minZ = Math.Min(minZ, r.z);
        }
        for (int i = 0; i < result.Length; i++)
            result[i] = new Cell(result[i].x - minX, result[i].y - minY, result[i].z - minZ);
        return result;
    }

    public static bool TryFindPlacement(BoardModel board, Cell[] shape, int preferred,
        out int orientation, out Cell anchor)
    {
        orientation = preferred;
        if (board.TryFindPlacement(Rotate(shape, preferred), out anchor)) return true;
        for (int i = 0; i < orientations.Length; i++)
            if (i != preferred && board.TryFindPlacement(Rotate(shape, i), out anchor))
            { orientation = i; return true; }
        return false;
    }
}

/// <summary>A concrete catalog entry with the orientation shown to the player.</summary>
public sealed class ShapeOffer
{
    public int CatalogIndex { get; }
    public int Orientation { get; }
    private readonly Cell[] shape;
    public ShapeOffer(int catalogIndex, Cell[] cells, int orientation)
    {
        CatalogIndex = catalogIndex; Orientation = orientation;
        shape = cells == null ? new Cell[0] : (Cell[])cells.Clone();
    }
    public Cell[] Cells => ShapeRules.Rotate(shape, Orientation);
    public bool TryFit(BoardModel board, out ShapeOffer fitted, out Cell anchor)
    {
        bool found = ShapeRules.TryFindPlacement(board, shape, Orientation, out var rotation, out anchor);
        fitted = found ? new ShapeOffer(CatalogIndex, shape, rotation) : null;
        return found;
    }
}

public static class OfferRules
{
    public static bool AnyFits(BoardModel board, IEnumerable<ShapeOffer> offers)
    {
        foreach (var offer in offers)
            if (offer != null && offer.TryFit(board, out _, out _)) return true;
        return false;
    }

    // Exhaustive fallback over the real catalog, not a random retry budget.
    public static bool EnsurePlayable(BoardModel board, IList<ShapeOffer> offers, IEnumerable<ShapeOffer> catalog)
    {
        if (AnyFits(board, offers)) return true;
        if (offers.Count == 0) return false;
        foreach (var candidate in catalog)
            if (candidate != null && candidate.TryFit(board, out var fitted, out _))
            { offers[0] = fitted; return true; }
        return false;
    }
}
