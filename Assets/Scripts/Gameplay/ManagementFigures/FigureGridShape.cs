using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class FigureGridShape : MonoBehaviour
{
    [SerializeField] private float cubeSize = 0.5f;

    public Vector3Int[] cells;
    public Vector3Int anchorCell;
    public Vector3 pivotOffset;

#if UNITY_EDITOR
    [ContextMenu("Auto Generate Cells From Children")]
    private void GenerateFromChildren()
    {
        List<Vector3Int> result = new();

        foreach (Transform child in transform)
        {
            Vector3 local = child.localPosition / cubeSize;

            Vector3Int cell = new Vector3Int(
                Mathf.RoundToInt(local.x),
                Mathf.RoundToInt(local.y),
                Mathf.RoundToInt(local.z)
            );

            if (!result.Contains(cell))
                result.Add(cell);
        }

        cells = result.ToArray();

        CalculateAnchor();
        CalculatePivotOffset();

        EditorUtility.SetDirty(this);
        Debug.Log($"[FigureGridShape] Generated {cells.Length} cells for {name}");
    }
#endif

    private void Awake()
    {
        if (cells != null && cells.Length > 0)
        {
            CalculateAnchor();
            CalculatePivotOffset();
        }
    }

    private void CalculateAnchor()
    {
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
        if (cells == null || cells.Length == 0) return cells;
        if (rotation == Quaternion.identity) return (Vector3Int[])cells.Clone();

        var result = new Vector3Int[cells.Length];
        for (int i = 0; i < cells.Length; i++)
        {
            Vector3 local = (Vector3)(cells[i] - anchorCell) * cubeSize;
            Vector3 rotated = rotation * local;
            Vector3Int newCell = anchorCell + new Vector3Int(
                Mathf.RoundToInt(rotated.x / cubeSize),
                Mathf.RoundToInt(rotated.y / cubeSize),
                Mathf.RoundToInt(rotated.z / cubeSize));
            result[i] = newCell;
        }
        return result;
    }
}
