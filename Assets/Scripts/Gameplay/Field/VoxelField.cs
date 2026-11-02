using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Лёгкая визуализация сетки поля. Старое поведение создавало 512 Cube-объектов,
/// даже когда они были невидимы. Теперь вся сетка строится одним Mesh.
/// </summary>
public class VoxelField : MonoBehaviour
{
    [SerializeField] private int size = Field3D.Size;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private bool visible = true;
    [SerializeField] private Color lineColor = new Color(0.35f, 0.75f, 1f, 0.32f);

    private Mesh gridMesh;
    private MeshRenderer gridRenderer;
    private Material runtimeMaterial;

    private void Start()
    {
        var field = GetComponent<Field3D>();
        if (field != null)
        {
            size = Field3D.Size;
            cellSize = field.CellSize;
        }

        HideLegacyFieldModel();
        GenerateGrid();
        SetVisibility(visible);
    }

    private void HideLegacyFieldModel()
    {
        if (transform.parent == null) return;

        var legacyRenderers = transform.parent.GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in legacyRenderers)
        {
            if (renderer != null && !renderer.transform.IsChildOf(transform))
                renderer.enabled = false;
        }
    }

    private void GenerateGrid()
    {
        var container = new GameObject("GridVisualization");
        container.transform.SetParent(transform, false);

        var filter = container.AddComponent<MeshFilter>();
        gridRenderer = container.AddComponent<MeshRenderer>();

        float half = size * cellSize * 0.5f;
        var vertices = new List<Vector3>((size + 1) * (size + 1) * 6);
        var indices = new List<int>((size + 1) * (size + 1) * 6);

        for (int a = 0; a <= size; a++)
        for (int b = 0; b <= size; b++)
        {
            float p1 = -half + a * cellSize;
            float p2 = -half + b * cellSize;

            AddLine(vertices, indices,
                new Vector3(-half, p1, p2), new Vector3(half, p1, p2));
            AddLine(vertices, indices,
                new Vector3(p1, -half, p2), new Vector3(p1, half, p2));
            AddLine(vertices, indices,
                new Vector3(p1, p2, -half), new Vector3(p1, p2, half));
        }

        gridMesh = new Mesh { name = "Grid8x8x8" };
        gridMesh.SetVertices(vertices);
        gridMesh.SetIndices(indices, MeshTopology.Lines, 0);
        gridMesh.RecalculateBounds();
        filter.sharedMesh = gridMesh;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Standard");

        runtimeMaterial = new Material(shader)
        {
            name = "Runtime Grid Material",
            color = lineColor
        };
        gridRenderer.sharedMaterial = runtimeMaterial;
    }

    private static void AddLine(
        List<Vector3> vertices,
        List<int> indices,
        Vector3 start,
        Vector3 end)
    {
        int index = vertices.Count;
        vertices.Add(start);
        vertices.Add(end);
        indices.Add(index);
        indices.Add(index + 1);
    }

    public void SetVisibility(bool isVisible)
    {
        visible = isVisible;
        if (gridRenderer != null)
            gridRenderer.enabled = visible;
    }

    private void OnDestroy()
    {
        if (gridMesh != null) Destroy(gridMesh);
        if (runtimeMaterial != null) Destroy(runtimeMaterial);
    }
}
