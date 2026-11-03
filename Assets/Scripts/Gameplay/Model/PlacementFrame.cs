using System;
using Cell = BoardModel.Cell;

/// <summary>Camera-relative placement on a fixed nearest face, without scene dependencies.</summary>
public sealed class PlacementFrame
{
    public struct Direction
    {
        public float X, Y, Z;
        public Direction(float x, float y, float z) { X = x; Y = y; Z = z; }
        public float this[int axis] => axis == 0 ? X : axis == 1 ? Y : Z;
    }
    public int DepthAxis { get; }
    public int NearSign { get; }
    public int HorizontalAxis { get; }
    public int HorizontalSign { get; }
    public int VerticalAxis { get; }
    public int VerticalSign { get; }

    public PlacementFrame(Direction toCamera, Direction cameraRight, Direction cameraUp)
    {
        int depth = 0;
        for (int a = 1; a < 3; a++)
            if (Math.Abs(toCamera[a]) > Math.Abs(toCamera[depth])) depth = a;
        DepthAxis = depth;
        NearSign = toCamera[depth] >= 0 ? 1 : -1;
        int first = (depth + 1) % 3, second = (depth + 2) % 3;
        HorizontalAxis = Math.Abs(cameraRight[first]) >= Math.Abs(cameraRight[second]) ? first : second;
        VerticalAxis = HorizontalAxis == first ? second : first;
        HorizontalSign = cameraRight[HorizontalAxis] >= 0 ? 1 : -1;
        VerticalSign = cameraUp[VerticalAxis] >= 0 ? 1 : -1;
    }

    public int Depth(Cell anchor) => NearSign > 0 ? 7 - Coordinate(anchor, DepthAxis) : Coordinate(anchor, DepthAxis);
    public Cell AtDepth(Cell anchor, int depth) => WithCoordinate(anchor, DepthAxis,
        NearSign > 0 ? 7 - Clamp(depth) : Clamp(depth));
    public Cell Move(Cell anchor, int horizontal, int vertical)
    {
        anchor = WithCoordinate(anchor, HorizontalAxis, Clamp(Coordinate(anchor, HorizontalAxis) + horizontal * HorizontalSign));
        return WithCoordinate(anchor, VerticalAxis, Clamp(Coordinate(anchor, VerticalAxis) + vertical * VerticalSign));
    }
    public static int Coordinate(Cell c, int axis) => axis == 0 ? c.x : axis == 1 ? c.y : c.z;
    public static Cell WithCoordinate(Cell c, int axis, int value) =>
        axis == 0 ? new Cell(value, c.y, c.z) : axis == 1 ? new Cell(c.x, value, c.z) : new Cell(c.x, c.y, value);
    private static int Clamp(int value) => Math.Max(0, Math.Min(BoardModel.Size - 1, value));
}
