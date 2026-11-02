using UnityEngine;

public class Grid3DGenerator : MonoBehaviour
{
    [SerializeField] private int gridSize = 8;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private float lineThickness = 0.04f;
    [SerializeField] private Material lineMaterial;

    private float HalfSize => gridSize * cellSize * 0.5f;

    private void Start()
    {
        Generate();
    }
    
    private void Generate()
    {
        for (int i = 0; i <= gridSize; i++)
        {
            float pos = -HalfSize + i * cellSize;

            // X lines
            CreateLine(
                new Vector3(0, pos, pos),
                new Vector3(gridSize * cellSize, lineThickness, lineThickness)
            );
            CreateLine(
                new Vector3(pos, 0, pos),
                new Vector3(lineThickness, gridSize * cellSize, lineThickness)
            );
            CreateLine(
                new Vector3(pos, pos, 0),
                new Vector3(lineThickness, lineThickness, gridSize * cellSize)
            );
        }
    }

    private void CreateLine(Vector3 localPos, Vector3 scale)
    {
        GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
        line.transform.SetParent(transform, false);
        line.transform.localPosition = localPos;
        line.transform.localScale = scale;

        if (lineMaterial != null)
            line.GetComponent<MeshRenderer>().material = lineMaterial;

        Destroy(line.GetComponent<BoxCollider>());
    }
}
