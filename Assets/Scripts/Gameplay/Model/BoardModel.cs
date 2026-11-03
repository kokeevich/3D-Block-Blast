using System;
using System.Collections.Generic;

/// <summary>Engine-independent occupancy, placement and plane-clearing rules.</summary>
public class BoardModel
{
    public const int Size = 8;

    public enum PlaneAxis
    {
        XY, // по Z (горизонтальная плоскость на высоте z)
        XZ, // по Y (вертикальная плоскость вдоль Y)
        YZ  // по X (вертикальная плоскость вдоль X)
    }

    [Serializable]
    public struct Plane
    {
        public PlaneAxis axis;
        public int index;

        public Plane(PlaneAxis a, int i) { axis = a; index = i; }
    }

    // Occupancy is independent of scene objects and their lifetime.
    private readonly bool[,,] cells = new bool[Size, Size, Size];

    public bool IsInside(int x, int y, int z)
    {
        return x >= 0 && x < Size &&
               y >= 0 && y < Size &&
               z >= 0 && z < Size;
    }

    public bool IsFree(int x, int y, int z)
    {
        return IsInside(x, y, z) && !cells[x, y, z];
    }

    public bool IsOccupied(int x, int y, int z)
    {
        return IsInside(x, y, z) && cells[x, y, z];
    }

    public void SetOccupied(int x, int y, int z, bool occupied)
    {
        if (IsInside(x, y, z)) cells[x, y, z] = occupied;
    }

    public void ClearCell(int x, int y, int z)
    {
        if (IsInside(x, y, z))
            cells[x, y, z] = false;
    }

    public int CountFreeCells()
    {
        int count = 0;
        for (int x = 0; x < Size; x++)
            for (int y = 0; y < Size; y++)
                for (int z = 0; z < Size; z++)
                    if (!cells[x, y, z]) count++;
        return count;
    }

    /// <summary>
    /// Проверяет, можно ли разместить набор клеток (в локальных координатах) с указанной опорной точкой.
    /// anchorPos — позиция "anchor"-ячейки фигуры в координатах сетки.
    /// cells — массив смещений от anchor в координатах сетки.
    /// </summary>
    public bool CanPlaceShape(Cell anchorPos, Cell[] shapeCells)
    {
        if (shapeCells == null || shapeCells.Length == 0) return false;

        var unique = new HashSet<Cell>();
        for (int i = 0; i < shapeCells.Length; i++)
        {
            if (!unique.Add(shapeCells[i])) return false;
            int x = anchorPos.x + shapeCells[i].x;
            int y = anchorPos.y + shapeCells[i].y;
            int z = anchorPos.z + shapeCells[i].z;

            if (!IsInside(x, y, z)) return false;
            if (cells[x, y, z]) return false;
        }
        return true;
    }

    public bool TryPlaceShape(Cell anchor, Cell[] shape)
    {
        if (!CanPlaceShape(anchor, shape)) return false;
        foreach (var c in shape) cells[anchor.x + c.x, anchor.y + c.y, anchor.z + c.z] = true;
        return true;
    }

    private static bool ValidPlane(Plane p) => p.index >= 0 && p.index < Size &&
        p.axis >= PlaneAxis.XY && p.axis <= PlaneAxis.YZ;

    public struct Cell : IEquatable<Cell>
    {
        public readonly int x, y, z;
        public Cell(int x, int y, int z) { this.x = x; this.y = y; this.z = z; }
        public static Cell zero => new Cell(0, 0, 0);
        public bool Equals(Cell other) => x == other.x && y == other.y && z == other.z;
        public override bool Equals(object other) => other is Cell && Equals((Cell)other);
        public override int GetHashCode() => (x * 397 ^ y) * 397 ^ z;
    }

    /// <summary>
    /// Удаляет все блоки (если есть) с указанной плоскости и помечает её пустой.
    /// </summary>
    public void ClearPlane(Plane plane)
    {
        if (!ValidPlane(plane)) return;
        for (int a = 0; a < Size; a++)
        for (int b = 0; b < Size; b++)
        {
            int x = 0, y = 0, z = 0;
            switch (plane.axis)
            {
                case PlaneAxis.XY: x = a; y = b; z = plane.index; break;
                case PlaneAxis.XZ: x = a; y = plane.index; z = b; break;
                case PlaneAxis.YZ: x = plane.index; y = a; z = b; break;
            }

            cells[x, y, z] = false;
        }
    }

    /// <summary>
    /// Проверяет, заполнена ли плоскость целиком.
    /// </summary>
    public bool IsPlaneFull(Plane plane)
    {
        if (!ValidPlane(plane)) return false;
        for (int a = 0; a < Size; a++)
        for (int b = 0; b < Size; b++)
        {
            int x = 0, y = 0, z = 0;
            switch (plane.axis)
            {
                case PlaneAxis.XY: x = a; y = b; z = plane.index; break;
                case PlaneAxis.XZ: x = a; y = plane.index; z = b; break;
                case PlaneAxis.YZ: x = plane.index; y = a; z = b; break;
            }
            if (!cells[x, y, z]) return false;
        }
        return true;
    }

    /// <summary>
    /// Собирает все полностью заполненные плоскости.
    /// </summary>
    public List<Plane> GetCompletedPlanes()
    {
        var result = new List<Plane>();
        for (int i = 0; i < Size; i++)
        {
            if (IsPlaneFull(new Plane(PlaneAxis.XY, i))) result.Add(new Plane(PlaneAxis.XY, i));
            if (IsPlaneFull(new Plane(PlaneAxis.XZ, i))) result.Add(new Plane(PlaneAxis.XZ, i));
            if (IsPlaneFull(new Plane(PlaneAxis.YZ, i))) result.Add(new Plane(PlaneAxis.YZ, i));
        }
        return result;
    }

    // A snapshot must be collected before any deletion: intersecting lines and
    // plane bonuses all refer to the same board state.
    public sealed class ClearResult
    {
        public int LinesCleared { get; }
        public int PlanesCompleted { get; }
        public IReadOnlyList<Cell> Cells { get; }
        public int ComboBonus => Math.Max(0, LinesCleared - 1) * 5;
        public int PlaneBonus => PlanesCompleted * 100;

        internal ClearResult(int lines, int planes, List<Cell> cells)
        {
            LinesCleared = lines;
            PlanesCompleted = planes;
            Cells = cells.AsReadOnly();
        }

        public int Score(int placedCells, int pointsPerBlock = 1) =>
            placedCells * pointsPerBlock + LinesCleared * 10 + ComboBonus + PlaneBonus;
    }

    public ClearResult GetCompletedLines()
    {
        var unique = new HashSet<Cell>();
        var clearedCells = new List<Cell>();
        int lines = 0;
        for (int axis = 0; axis < 3; axis++)
        for (int a = 0; a < Size; a++)
        for (int b = 0; b < Size; b++)
        {
            bool full = true;
            for (int t = 0; t < Size; t++)
            {
                var c = LineCell(axis, a, b, t);
                if (!IsOccupied(c.x, c.y, c.z)) { full = false; break; }
            }
            if (!full) continue;
            lines++;
            for (int t = 0; t < Size; t++)
            {
                var c = LineCell(axis, a, b, t);
                if (unique.Add(c)) clearedCells.Add(c);
            }
        }
        return new ClearResult(lines, GetCompletedPlanes().Count, clearedCells);
    }

    private static Cell LineCell(int axis, int a, int b, int t) =>
        axis == 0 ? new Cell(t, a, b) :
        axis == 1 ? new Cell(a, t, b) : new Cell(a, b, t);

    public void ClearLines(ClearResult result)
    {
        if (result == null) return;
        foreach (var c in result.Cells) ClearCell(c.x, c.y, c.z);
    }

    /// <summary>
    /// Ищет ЛЮБУЮ валидную позицию для фигуры на поле.
    /// Возвращает true, если нашлась anchor-позиция.
    /// </summary>
    public bool TryFindPlacement(Cell[] shapeCells, out Cell anchorPos)
    {
        anchorPos = Cell.zero;
        if (shapeCells == null || shapeCells.Length == 0) return false;

        for (int x = 0; x < Size; x++)
            for (int y = 0; y < Size; y++)
                for (int z = 0; z < Size; z++)
                {
                    var pos = new Cell(x, y, z);
                    if (CanPlaceShape(pos, shapeCells))
                    {
                        anchorPos = pos;
                        return true;
                    }
                }
        return false;
    }

    /// <summary>
    /// Можно ли разместить фигуру хоть где-то на поле.
    /// </summary>
    public bool CanPlaceAnywhere(Cell[] shapeCells)
    {
        return TryFindPlacement(shapeCells, out _);
    }

    /// <summary>
    /// Получить все свободные клетки.
    /// </summary>
    public List<Cell> GetFreeCells()
    {
        var list = new List<Cell>();
        for (int x = 0; x < Size; x++)
            for (int y = 0; y < Size; y++)
                for (int z = 0; z < Size; z++)
                    if (!cells[x, y, z]) list.Add(new Cell(x, y, z));
        return list;
    }

}
