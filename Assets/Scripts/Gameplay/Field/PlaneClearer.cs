using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneClearer : MonoBehaviour
{
    [SerializeField] private Field3D field;
    [SerializeField] private float popDuration = 0.25f;
    [SerializeField] private float popScale = 1.4f;

    [Header("Combo Flash")]
    [SerializeField] private float comboFlashDelay = 0.05f;

    public System.Action<int, int> OnPlanesCleared; // (clearedCount, comboBonus)

    void Start()
    {
        if (field == null) field = GetComponent<Field3D>();
    }

    public void SetField(Field3D f) { field = f; }

    public IEnumerator ClearPlanesWithAnimation(List<Field3D.Plane> planes)
    {
        if (field == null) yield break;
        if (planes == null || planes.Count == 0) yield break;

        int cleared = planes.Count;
        int comboBonus = cleared >= 2 ? (cleared - 1) * 5 : 0;

        // Соберём все блоки, которые нужно анимировать
        var blocks = CollectBlocks(planes);

        // Анимация: пульсация + исчезновение
        var scales = new List<Vector3>(blocks.Count);
        for (int i = 0; i < blocks.Count; i++)
        {
            scales.Add(blocks[i] != null ? blocks[i].localScale : Vector3.one);
        }

        float t = 0f;
        while (t < popDuration * 0.5f)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / (popDuration * 0.5f));
            float e = Mathf.SmoothStep(0f, 1f, p);
            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i] != null)
                    blocks[i].localScale = scales[i] * Mathf.Lerp(1f, popScale, e);
            }
            yield return null;
        }

        t = 0f;
        while (t < popDuration * 0.5f)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / (popDuration * 0.5f));
            float e = 1f - Mathf.SmoothStep(0f, 1f, p);
            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i] != null)
                    blocks[i].localScale = scales[i] * popScale * e;
            }
            yield return null;
        }

        // Очищаем плоскости (уничтожаем блоки)
        for (int i = 0; i < planes.Count; i++)
            field.ClearPlane(planes[i]);

        OnPlanesCleared?.Invoke(cleared, comboBonus);
    }

    private List<Transform> CollectBlocks(List<Field3D.Plane> planes)
    {
        var list = new List<Transform>();
        for (int pi = 0; pi < planes.Count; pi++)
        {
            var plane = planes[pi];
            for (int a = 0; a < Field3D.Size; a++)
            for (int b = 0; b < Field3D.Size; b++)
            {
                int x = 0, y = 0, z = 0;
                switch (plane.axis)
                {
                    case Field3D.PlaneAxis.XY: x = a; y = b; z = plane.index; break;
                    case Field3D.PlaneAxis.XZ: x = a; y = plane.index; z = b; break;
                    case Field3D.PlaneAxis.YZ: x = plane.index; y = a; z = b; break;
                }
                var t = field.GetCell(x, y, z);
                if (t != null) list.Add(t);
            }
        }
        return list;
    }
}
