using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class FigureGridShape : MonoBehaviour
{
    [SerializeField] private float cubeSize = 0.5f;

    [Header("Runtime Visuals")]
    [SerializeField] private bool rebuildVisualsFromCells = true;
    [SerializeField, Range(0.5f, 1f)] private float blockFill = 0.9f;

    public Vector3Int[] cells;
    public Vector3Int anchorCell;
    public Vector3 pivotOffset;

    private bool runtimeInitialized;
    public void ConfigurePreview(Vector3Int[] logicalCells)
    { cells=logicalCells; rebuildVisualsFromCells=false; }

#if UNITY_EDITOR
    [ContextMenu("Auto Generate Cells From Children")]
    private void GenerateFromChildren()
    {
        cells = CalculateNormalizedCellsFromChildren();

        CalculateAnchor();
        CalculatePivotOffset();

        EditorUtility.SetDirty(this);
        Debug.Log($"[FigureGridShape] Generated {cells.Length} cells for {name}");
    }
#endif

    private void Awake()
    {
        cells = CalculateNormalizedCellsFromChildren();
        CalculateAnchor();
        CalculatePivotOffset();
        runtimeInitialized = true;

        if (rebuildVisualsFromCells)
            RebuildRuntimeVisuals();
    }

    /// <summary>
    /// Возвращает нормализованную форму без зависимости от pivot префаба.
    /// Для prefab asset Awake не вызывается, поэтому форма вычисляется напрямую
    /// по дочерним блокам. Для игрового экземпляра используется кэш Awake.
    /// </summary>
    public Vector3Int[] GetNormalizedCells()
    {
        Vector3Int[] source = runtimeInitialized
            ? cells
            : CalculateNormalizedCellsFromChildren();

        return source != null ? (Vector3Int[])source.Clone() : new Vector3Int[0];
    }

    private Vector3Int[] CalculateNormalizedCellsFromChildren()
    {
        var children = new List<Transform>();
        foreach (Transform child in transform)
            children.Add(child);

        if (children.Count == 0)
            return cells != null ? RemoveDuplicatesAndNormalize(cells) : new Vector3Int[0];

        Vector3 min = children[0].localPosition;
        foreach (var child in children)
            min = Vector3.Min(min, child.localPosition);

        float step = Mathf.Max(0.0001f, cubeSize);
        var result = new List<Vector3Int>(children.Count);
        foreach (var child in children)
        {
            Vector3 offset = (child.localPosition - min) / step;
            var cell = new Vector3Int(
                Mathf.RoundToInt(offset.x),
                Mathf.RoundToInt(offset.y),
                Mathf.RoundToInt(offset.z));

            if (!result.Contains(cell))
                result.Add(cell);
        }

        SortCells(result);
        return result.ToArray();
    }

    private static Vector3Int[] RemoveDuplicatesAndNormalize(Vector3Int[] source)
    {
        if (source == null || source.Length == 0)
            return new Vector3Int[0];

        Vector3Int min = source[0];
        foreach (var cell in source)
            min = Vector3Int.Min(min, cell);

        var result = new List<Vector3Int>(source.Length);
        foreach (var cell in source)
        {
            Vector3Int normalized = cell - min;
            if (!result.Contains(normalized))
                result.Add(normalized);
        }

        SortCells(result);
        return result.ToArray();
    }

    private static void SortCells(List<Vector3Int> values)
    {
        values.Sort((a, b) =>
        {
            int y = a.y.CompareTo(b.y);
            if (y != 0) return y;
            int x = a.x.CompareTo(b.x);
            return x != 0 ? x : a.z.CompareTo(b.z);
        });
    }

    private void RebuildRuntimeVisuals()
    {
        Material material = null;
        var sourceRenderer = GetComponentInChildren<MeshRenderer>(true);
        if (sourceRenderer != null)
            material = sourceRenderer.sharedMaterial;

        var authoredChildren = new List<GameObject>();
        foreach (Transform child in transform)
            authoredChildren.Add(child.gameObject);

        foreach (var child in authoredChildren)
            child.SetActive(false);

        var container = new GameObject("RuntimeCells");
        container.transform.SetParent(transform, false);

        foreach (var cell in cells)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = $"Cell_{cell.x}_{cell.y}_{cell.z}";
            block.transform.SetParent(container.transform, false);
            block.transform.localPosition = (Vector3)cell * cubeSize;
            block.transform.localScale = Vector3.one * cubeSize * blockFill;

            var renderer = block.GetComponent<MeshRenderer>();
            if (renderer != null && material != null)
                renderer.sharedMaterial = material;

            var collider = block.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);
        }

        var rootCollider = GetComponent<Collider>();
        if (rootCollider != null)
            rootCollider.enabled = false;

        transform.localScale = Vector3.one;
    }

    private void CalculateAnchor()
    {
        if (cells == null || cells.Length == 0)
        {
            anchorCell = Vector3Int.zero;
            return;
        }

        anchorCell = cells[0];

        foreach (var cell in cells)
        {
            if (cell.y < anchorCell.y ||
               (cell.y == anchorCell.y && cell.x < anchorCell.x) ||
               (cell.y == anchorCell.y && cell.x == anchorCell.x && cell.z < anchorCell.z))
            {
                anchorCell = cell;
            }
        }
    }

    private static Vector3Int FindAnchor(Vector3Int[] source)
    {
        if (source == null || source.Length == 0)
            return Vector3Int.zero;

        Vector3Int result = source[0];
        foreach (var cell in source)
        {
            if (cell.y < result.y ||
               (cell.y == result.y && cell.x < result.x) ||
               (cell.y == result.y && cell.x == result.x && cell.z < result.z))
            {
                result = cell;
            }
        }
        return result;
    }

    private void CalculatePivotOffset()
    {
        // pivotOffset = положение anchor-клетки в локальных координатах фигуры
        pivotOffset = (Vector3)anchorCell * cubeSize;
    }

    private void OnDrawGizmosSelected()
    {
        if (cells == null) return;

        Gizmos.color = Color.cyan;

        foreach (var cell in cells)
        {
            Vector3 local = ((Vector3)cell * cubeSize) - pivotOffset;

            Gizmos.DrawWireCube(
                transform.position + transform.rotation * local,
                Vector3.one * cubeSize * 0.95f
            );
        }
    }

    /// <summary>
    /// Возвращает копию cells, повёрнутую в соответствии с указанной кватернионом.
    /// Учитывает, что anchor-клетка должна остаться на месте — поворот делается относительно неё.
    /// </summary>
    public Vector3Int[] GetRotatedCells(Quaternion rotation)
    {
        Vector3Int[] source = GetNormalizedCells();
        if (source.Length == 0) return source;
        if (rotation == Quaternion.identity) return source;

        Vector3Int rotationAnchor = FindAnchor(source);

        var result = new Vector3Int[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            Vector3 local = (Vector3)(source[i] - rotationAnchor) * cubeSize;
            Vector3 rotated = rotation * local;
            Vector3Int newCell = rotationAnchor + new Vector3Int(
                Mathf.RoundToInt(rotated.x / cubeSize),
                Mathf.RoundToInt(rotated.y / cubeSize),
                Mathf.RoundToInt(rotated.z / cubeSize));
            result[i] = newCell;
        }
        return result;
    }
}
