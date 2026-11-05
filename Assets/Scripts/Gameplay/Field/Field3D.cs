using System;
using System.Collections.Generic;
using UnityEngine;

public class Field3D : MonoBehaviour
{
    public const int Size = BoardModel.Size;

    // GameFieldRoot keeps the former world scale (0.5) directly on its root.
    // A local cell size of 1 therefore still produces 0.5-unit world blocks.
    [SerializeField] private float cellSize = 1f;

    public float CellSize => cellSize;
    public BoardModel Model => board;

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

    private readonly Dictionary<int,Stack<GameObject>> pools=new Dictionary<int,Stack<GameObject>>();
    private readonly Dictionary<GameObject,int> poolKeys=new Dictionary<GameObject,int>();
    private Transform poolRoot;
    private MaterialPropertyBlock colorProperties;
    public int Revision => board.Revision;
    public int PooledBlockCount
    { get { int count=0; foreach (var pool in pools.Values) count+=pool.Count; return count; } }
    private GameObject RentBlock(GameObject prefab,Transform parent)
    {
        int key=prefab!=null ? prefab.GetInstanceID() : 0;
        if (!pools.TryGetValue(key,out var pool)) pools[key]=pool=new Stack<GameObject>();
        GameObject block=null;
        while (pool.Count>0 && block==null) block=pool.Pop();
        if (block==null)
        {
            block=prefab!=null ? Instantiate(prefab) : GameObject.CreatePrimitive(PrimitiveType.Cube);
            foreach (var collider in block.GetComponentsInChildren<Collider>(true))
            { collider.enabled=false; Destroy(collider); }
            poolKeys[block]=key;
        }
        block.transform.SetParent(parent!=null ? parent : transform,false);
        block.SetActive(true); return block;
    }
    private void ReturnBlock(Transform block)
    {
        if (block==null) return;
        if (!poolKeys.TryGetValue(block.gameObject,out int key)) { Destroy(block.gameObject); return; }
        if (poolRoot==null)
        {
            poolRoot=new GameObject("PooledBlocks").transform; poolRoot.SetParent(transform,false);
            poolRoot.gameObject.SetActive(false);
        }
        // ClearCell has already restored any translucent material through OnCellChanged.
        foreach (var renderer in block.GetComponentsInChildren<Renderer>(true)) renderer.SetPropertyBlock(null);
        block.gameObject.SetActive(false); block.SetParent(poolRoot,false);
        pools[key].Push(block.gameObject);
    }

    private readonly Color[,,] colors=new Color[Size,Size,Size];
    public Color CellColor(int x,int y,int z) => colors[x,y,z];

    private readonly BoardModel board = new BoardModel();

    public event Action<Plane> OnPlaneCleared;
    public event Action<Vector3Int,Transform> OnCellChanged;

    public bool IsInside(int x, int y, int z) => board.IsInside(x, y, z);
    public bool IsFree(int x, int y, int z) => board.IsFree(x, y, z);
    public bool IsOccupied(int x, int y, int z) => board.IsOccupied(x, y, z);

    public Transform GetCell(int x, int y, int z)
    {
        if (!IsInside(x, y, z)) return null;
        return cells[x, y, z];
    }

    public void SetCell(int x, int y, int z, Transform block)
    {
        if (!IsInside(x, y, z)) return;
        cells[x, y, z] = block;
        board.SetOccupied(x, y, z, block != null);
        OnCellChanged?.Invoke(new Vector3Int(x,y,z),block);
    }

    public void ClearCell(int x, int y, int z)
    {
        if (!IsInside(x, y, z)) return;
        cells[x, y, z] = null;
        board.ClearCell(x, y, z);
        OnCellChanged?.Invoke(new Vector3Int(x,y,z),null);
    }

    public int CountFreeCells() => board.CountFreeCells();

    /// <summary>
    /// Проверяет, можно ли разместить набор клеток (в локальных координатах) с указанной опорной точкой.
    /// anchorPos — позиция "anchor"-ячейки фигуры в координатах сетки.
    /// cells — массив смещений от anchor в координатах сетки.
    /// </summary>
    public bool CanPlaceShape(Vector3Int anchorPos, Vector3Int[] shapeCells)
    {
        return board.CanPlaceShape(ToCell(anchorPos), ToCells(shapeCells));
    }

    private static BoardModel.Cell ToCell(Vector3Int c) => new BoardModel.Cell(c.x, c.y, c.z);
    private static BoardModel.Cell[] ToCells(Vector3Int[] shape)
    {
        if (shape == null) return null;
        return Array.ConvertAll(shape, ToCell);
    }
    private static BoardModel.Plane ToPlane(Plane p) =>
        new BoardModel.Plane((BoardModel.PlaneAxis)p.axis, p.index);

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
        var created = new List<GameObject>();
        if (!CanPlaceShape(anchorPos, shapeCells)) return created;

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

            GameObject block=RentBlock(blockPrefab,blocksParent);
            block.name=$"Block_{x}_{y}_{z}";

            Vector3 local = GridToLocal(new Vector3Int(x, y, z));

            block.transform.localPosition = local;
            block.transform.localRotation = Quaternion.identity;
            block.transform.localScale = Vector3.one * cellSize * 0.95f;

            // Применим цвет/материал, если задан
            if (color.a > 0f)
            {
                var r = block.GetComponent<MeshRenderer>();
                if (r != null)
                {
                    if (sharedMat!=null) r.sharedMaterial=sharedMat;
                    if (colorProperties==null) colorProperties=new MaterialPropertyBlock();
                    colorProperties.Clear();
                    colorProperties.SetColor("_Color",color);
                    colorProperties.SetColor("_BaseColor",color);
                    r.SetPropertyBlock(colorProperties);
                }
            }

            colors[x,y,z]=color;
            SetCell(x, y, z, block.transform);
            created.Add(block);
        }

        return created;
    }

    /// <summary>
    /// Удаляет все блоки (если есть) с указанной плоскости и помечает её пустой.
    /// </summary>
    public void ClearPlane(Plane plane)
    {
        if (plane.index < 0 || plane.index >= Size ||
            plane.axis < PlaneAxis.XY || plane.axis > PlaneAxis.YZ) return;
        board.ClearPlane(ToPlane(plane));
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
            ClearCell(x,y,z);
            ReturnBlock(t);
        }
    }

    /// <summary>
    /// Проверяет, заполнена ли плоскость целиком.
    /// </summary>
    public bool IsPlaneFull(Plane plane)
    {
        return board.IsPlaneFull(ToPlane(plane));
    }

    /// <summary>
    /// Собирает все полностью заполненные плоскости.
    /// </summary>
    public List<Plane> GetCompletedPlanes()
    {
        return board.GetCompletedPlanes().ConvertAll(p => new Plane((PlaneAxis)p.axis, p.index));
    }

    /// <summary>
    /// Ищет ЛЮБУЮ валидную позицию для фигуры на поле.
    /// Возвращает true, если нашлась anchor-позиция.
    /// </summary>
    public bool TryFindPlacement(Vector3Int[] shapeCells, out Vector3Int anchorPos)
    {
        bool found = board.TryFindPlacement(ToCells(shapeCells), out var anchor);
        anchorPos = new Vector3Int(anchor.x, anchor.y, anchor.z);
        return found;
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
        return board.GetFreeCells().ConvertAll(c => new Vector3Int(c.x, c.y, c.z));
    }

    public BoardModel.ClearResult GetCompletedLines() => board.GetCompletedLines();

    public void ClearLines(BoardModel.ClearResult result)
    {
        if (result == null) return;
        foreach (var c in result.Cells)
        {
            var block = GetCell(c.x, c.y, c.z);
            ClearCell(c.x, c.y, c.z);
            ReturnBlock(block);
        }
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
