using System;
using System.Collections.Generic;
using UnityEngine;

public class Field3D : MonoBehaviour
{
    public const int Size = 8;

    // GameFieldRoot находится внутри старого FBX с масштабом 0.5.
    // Локальный шаг 1.0 поэтому соответствует мировому размеру блока 0.5.
    [SerializeField] private float cellSize = 1f;

    public float CellSize => cellSize;

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

    // Каждая клетка хранит либо null (пусто), либо ссылку на трансформ блока.
    private Transform[,,] cells = new Transform[Size, Size, Size];

    public event Action<Plane> OnPlaneCleared;

    public bool IsInside(int x, int y, int z)
    {
        return x >= 0 && x < Size &&
               y >= 0 && y < Size &&
               z >= 0 && z < Size;
    }

    public bool IsFree(int x, int y, int z)
    {
        return IsInside(x, y, z) && cells[x, y, z] == null;
    }

    public bool IsOccupied(int x, int y, int z)
    {
        return IsInside(x, y, z) && cells[x, y, z] != null;
    }

    public Transform GetCell(int x, int y, int z)
    {
        if (!IsInside(x, y, z)) return null;
        return cells[x, y, z];
    }

    public void SetCell(int x, int y, int z, Transform block)
    {
        if (IsInside(x, y, z))
            cells[x, y, z] = block;
    }

    public void ClearCell(int x, int y, int z)
    {
        if (IsInside(x, y, z))
            cells[x, y, z] = null;
    }

    public int CountFreeCells()
    {
        int count = 0;
        for (int x = 0; x < Size; x++)
            for (int y = 0; y < Size; y++)
                for (int z = 0; z < Size; z++)
                    if (cells[x, y, z] == null) count++;
        return count;
    }

    /// <summary>
    /// Проверяет, можно ли разместить набор клеток (в локальных координатах) с указанной опорной точкой.
    /// anchorPos — позиция "anchor"-ячейки фигуры в координатах сетки.
    /// cells — массив смещений от anchor в координатах сетки.
    /// </summary>
    public bool CanPlaceShape(Vector3Int anchorPos, Vector3Int[] shapeCells)
    {
        if (shapeCells == null || shapeCells.Length == 0) return false;

        for (int i = 0; i < shapeCells.Length; i++)
        {
            int x = anchorPos.x + shapeCells[i].x;
            int y = anchorPos.y + shapeCells[i].y;
            int z = anchorPos.z + shapeCells[i].z;

            if (!IsInside(x, y, z)) return false;
            if (cells[x, y, z] != null) return false;
        }
        return true;
    }

    /// <summary>
    /// Размещает фигуру на поле, привязывая к cellSize-выровненным локальным координатам.
    /// blocksParent — родитель, в который будут добавлены блоки (обычно сам Field3D или его контейнер).
    /// </summary>
    public List<GameObject> PlaceShape(
        Vector3Int anchorPos,
        Vector3Int[] shapeCells,
        GameObject blockPrefab,
        Transform blocksParent,
        Color color)
    {
        var created = new List<GameObject>(shapeCells.Length);
        if (shapeCells == null || shapeCells.Length == 0) return created;

        // Один материал/цвет на всю фигуру
        Material sharedMat = null;
        if (blockPrefab != null)
        {
            var rend = blockPrefab.GetComponentInChildren<MeshRenderer>();
            if (rend != null) sharedMat = rend.sharedMaterial;
        }

        for (int i = 0; i < shapeCells.Length; i++)
        {
            int x = anchorPos.x + shapeCells[i].x;
            int y = anchorPos.y + shapeCells[i].y;
            int z = anchorPos.z + shapeCells[i].z;

            if (!IsInside(x, y, z) || cells[x, y, z] != null)
            {
                // Откатываем то, что успели создать
                for (int j = 0; j < created.Count; j++)
                    if (created[j] != null) Destroy(created[j]);
                created.Clear();
                return created;
            }

            GameObject block;
            if (blockPrefab != null)
            {
                block = Instantiate(blockPrefab, blocksParent);
                block.name = $"Block_{x}_{y}_{z}";
            }
            else
            {
                block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                block.name = $"Block_{x}_{y}_{z}";
                if (blocksParent != null) block.transform.SetParent(blocksParent, false);
            }

            Vector3 local = GridToLocal(new Vector3Int(x, y, z));

            block.transform.localPosition = local;
            block.transform.localRotation = Quaternion.identity;
            block.transform.localScale = Vector3.one * cellSize * 0.95f;

            // Уберём коллайдер — для блоков на поле он не нужен
            var col = block.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // Применим цвет/материал, если задан
            if (color.a > 0f)
            {
                var r = block.GetComponent<MeshRenderer>();
                if (r != null)
                {
                    // Создаём инстанс-материал, чтобы цвет не утекал на префаб
                    if (sharedMat != null)
                        r.material = new Material(sharedMat);
                    r.material.color = color;
                }
            }

            cells[x, y, z] = block.transform;
            created.Add(block);
        }

        return created;
    }

    /// <summary>
    /// Удаляет все блоки (если есть) с указанной плоскости и помечает её пустой.
    /// </summary>
    public void ClearPlane(Plane plane)
    {
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

            var t = cells[x, y, z];
            cells[x, y, z] = null;
            if (t != null) Destroy(t.gameObject);
        }
    }

    /// <summary>
    /// Проверяет, заполнена ли плоскость целиком.
    /// </summary>
    public bool IsPlaneFull(Plane plane)
    {
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
            if (cells[x, y, z] == null) return false;
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

    /// <summary>
    /// Ищет ЛЮБУЮ валидную позицию для фигуры на поле.
    /// Возвращает true, если нашлась anchor-позиция.
    /// </summary>
    public bool TryFindPlacement(Vector3Int[] shapeCells, out Vector3Int anchorPos)
    {
        anchorPos = Vector3Int.zero;
        if (shapeCells == null || shapeCells.Length == 0) return false;

        for (int x = 0; x < Size; x++)
            for (int y = 0; y < Size; y++)
                for (int z = 0; z < Size; z++)
                {
                    var pos = new Vector3Int(x, y, z);
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
    public bool CanPlaceAnywhere(Vector3Int[] shapeCells)
    {
        return TryFindPlacement(shapeCells, out _);
    }

    /// <summary>
    /// Получить все свободные клетки.
    /// </summary>
    public List<Vector3Int> GetFreeCells()
    {
        var list = new List<Vector3Int>();
        for (int x = 0; x < Size; x++)
            for (int y = 0; y < Size; y++)
                for (int z = 0; z < Size; z++)
                    if (cells[x, y, z] == null) list.Add(new Vector3Int(x, y, z));
        return list;
    }

    public Vector3Int WorldToGrid(Vector3 worldPosition)
    {
        return LocalToGrid(transform.InverseTransformPoint(worldPosition));
    }

    public Vector3Int LocalToGrid(Vector3 localPosition)
    {
        float offset = (Size - 1) * cellSize * 0.5f;
        return new Vector3Int(
            Mathf.RoundToInt((localPosition.x + offset) / cellSize),
            Mathf.RoundToInt((localPosition.y + offset) / cellSize),
            Mathf.RoundToInt((localPosition.z + offset) / cellSize));
    }

    public Vector3 GridToLocal(Vector3Int cell)
    {
        float offset = (Size - 1) * cellSize * 0.5f;
        return new Vector3(
            cell.x * cellSize - offset,
            cell.y * cellSize - offset,
            cell.z * cellSize - offset);
    }

    public Vector3 GridToWorld(Vector3Int cell)
    {
        return transform.TransformPoint(GridToLocal(cell));
    }
}
