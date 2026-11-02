using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelField : MonoBehaviour
{
    public int size = 8;             // 8x8x8 поле
    public float cellSize = 1f;      // размер одной клетки
    public bool visible = true;       // можно выключать поле

    private GameObject[,,] cells;

    void Start()
    {
        GenerateField();
        SetVisibility(visible);
    }

    void GenerateField()
    {
        cells = new GameObject[size, size, size];

        // Смещение для центрирования
        float offset = (size - 1) * cellSize * 0.5f;

        // Создаём контейнер для кубов — он НЕ вращается, только передаёт локальную систему координат
        GameObject container = new GameObject("VoxelFieldContainer");
        container.transform.parent = transform; // родитель — GameFieldRoot
        container.transform.localPosition = Vector3.zero;
        container.transform.localRotation = Quaternion.identity; // важно: без поворота!
        container.transform.localScale = Vector3.one;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int z = 0; z < size; z++)
                {
                    GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.transform.parent = container.transform; // 👈 теперь родитель — контейнер

                    cube.transform.localScale = Vector3.one * cellSize;
                    cube.transform.localPosition = new Vector3(
                        x * cellSize - offset,
                        y * cellSize - offset,
                        z * cellSize - offset
                    );

                    cube.name = $"Cell_{x}_{y}_{z}";
                    cells[x, y, z] = cube;
                }
            }
        }
    }

    public void SetVisibility(bool isVisible)
    {
        foreach (var cube in cells)
        {
            cube.GetComponent<MeshRenderer>().enabled = isVisible;
        }
    }
}