using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneClearer : MonoBehaviour
{
    [SerializeField] private Field3D field;
    [SerializeField] private float popDuration = 0.25f;
    [SerializeField] private float popScale = 1.4f;

    void Start()
    {
        if (field == null) field = GetComponent<Field3D>();
    }

    public void SetField(Field3D f) { field = f; }

    // Keep the component name so existing scene references remain valid.
    public IEnumerator ClearLinesWithAnimation(BoardModel.ClearResult result)
    {
        if (field == null || result == null || result.Cells.Count == 0) yield break;
        var blocks = new List<Transform>(result.Cells.Count);
        foreach (var c in result.Cells)
        {
            var block = field.GetCell(c.x, c.y, c.z);
            if (block != null) blocks.Add(block);
        }

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

        field.ClearLines(result);
    }
}
