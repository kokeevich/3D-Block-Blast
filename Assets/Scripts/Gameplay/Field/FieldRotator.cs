using UnityEngine;
using System.Collections;

public class FieldRotator : MonoBehaviour
{
    public RectTransform swipeZone;
    public float animationTime = 0.22f;      // длительность шага
    public float minSwipeScreenPercent = 0.03f; // минимальная длина свайпа

    private Canvas canvas;
    private bool isAnimating = false;
    private Vector2 startPos;
    private Quaternion targetRotation;

    void Start()
    {
        if (swipeZone == null) Debug.LogWarning("SwipeZone not assigned!");
        canvas = swipeZone != null ? swipeZone.GetComponentInParent<Canvas>() : null;
        SnapRotationTo90();
        targetRotation = transform.rotation;
    }

    void Update()
    {
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 12f);

        if (isAnimating) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (!IsPointerInZone(Input.mousePosition)) return;
            startPos = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (!IsPointerInZone(Input.mousePosition)) return;

            Vector2 rawDelta = (Vector2)Input.mousePosition - startPos;
            if (rawDelta.magnitude < Screen.height * minSwipeScreenPercent) return;

            if (Mathf.Abs(rawDelta.x) > Mathf.Abs(rawDelta.y))
            {
                // Горизонталь — вращаем по мировой up (фиксированно), влево/вправо
                if (rawDelta.x > 0f) StartRotationAroundWorldAxis(Vector3.up, -90f);
                else StartRotationAroundWorldAxis(Vector3.up, +90f);
            }
            else
            {
                // Вертикаль — вращаем вокруг мировой оси X (right)
                // Направление: если свайп вверх — +90, вниз — -90
                float angle = rawDelta.y > 0f ? +90f : -90f;

                // Вращение вокруг мировой оси X
                targetRotation = Quaternion.AngleAxis(angle, Vector3.right) * transform.rotation;

                StartCoroutine(AnimateAndSnap(targetRotation));
            }
        }
    }

    bool IsPointerInZone(Vector2 screenPos)
    {
        if (swipeZone == null || canvas == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(swipeZone, screenPos, canvas.worldCamera);
    }

    void StartRotationAroundWorldAxis(Vector3 axisWorld, float angleDeg)
    {
        if (isAnimating) return;

        axisWorld = axisWorld.normalized;
        targetRotation = Quaternion.AngleAxis(angleDeg, axisWorld) * transform.rotation;
        StartCoroutine(AnimateAndSnap(targetRotation));
    }

    IEnumerator AnimateAndSnap(Quaternion intendedTarget)
    {
        isAnimating = true;

        Quaternion start = transform.rotation;
        Quaternion end = intendedTarget;

        float t = 0f;
        while (t < animationTime)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / animationTime);
            transform.rotation = Quaternion.Slerp(start, end, p);
            yield return null;
        }

        transform.rotation = end;
        SnapRotationTo90();
        targetRotation = transform.rotation;
        isAnimating = false;
    }

    Vector3 GetClosestWorldAxis(Vector3 v)
    {
        Vector3 n = v.normalized;
        Vector3[] candidates = new Vector3[]
        {
            Vector3.right, -Vector3.right,
            Vector3.up, -Vector3.up,
            Vector3.forward, -Vector3.forward
        };

        float best = -1f;
        Vector3 bestAxis = Vector3.right;
        foreach (var c in candidates)
        {
            float d = Mathf.Abs(Vector3.Dot(n, c));
            if (d > best)
            {
                best = d;
                bestAxis = (Vector3.Dot(n, c) >= 0f) ? c : -c;
            }
        }
        return bestAxis.normalized;
    }

    void SnapRotationTo90()
    {
        Quaternion bestQ = transform.rotation;
        float bestDot = -1f;

        for (int x = 0; x < 4; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                for (int z = 0; z < 4; z++)
                {
                    Quaternion q = Quaternion.Euler(x * 90f, y * 90f, z * 90f);
                    float score = Mathf.Abs(Quaternion.Dot(transform.rotation, q));
                    if (score > bestDot)
                    {
                        bestDot = score;
                        bestQ = q;
                    }
                }
            }
        }

        transform.rotation = bestQ;
    }

    Vector3 WorldToScreenDelta(Vector3 worldPos, Vector3 dir)
    {
        Camera cam = (canvas != null && canvas.worldCamera != null) ? canvas.worldCamera : Camera.main;
        Vector3 a = cam.WorldToScreenPoint(worldPos);
        Vector3 b = cam.WorldToScreenPoint(worldPos + dir);
        return b - a;
    }
}