using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ShapePlacer : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private ActiveFigureController figureController;
    [SerializeField] private Field3D field;
    [SerializeField] private PlaneClearer planeClearer;
    [SerializeField] private Transform blocksParent;
    [SerializeField] private GameObject blockPrefab;
    [SerializeField] private float placeTapTime = 0.25f;
    [SerializeField] private float placeTapMaxDistance = 25f;

    public delegate void ShapePlacedHandler(int cellsCount, int planesCleared, int comboBonus);
    public event ShapePlacedHandler OnShapePlaced;

    private Vector2 downPos;
    private float downTime;
    private bool tracking;
    private bool busyClearing;

    void Start()
    {
        if (figureController == null)
            figureController = FindObjectOfType<ActiveFigureController>();
        if (field == null && figureController != null)
        {
            var t = figureController.FieldTransform;
            if (t != null) field = t.GetComponent<Field3D>();
        }
        if (field == null)
            field = FindObjectOfType<Field3D>();
        if (planeClearer == null)
            planeClearer = FindObjectOfType<PlaneClearer>();
    }

    public void SetField(Field3D f) { field = f; }
    public void SetFigureController(ActiveFigureController c) { figureController = c; }
    public void SetPlaneClearer(PlaneClearer c) { planeClearer = c; }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (busyClearing) return;
        downPos = eventData.position;
        downTime = Time.unscaledTime;
        tracking = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!tracking) return;
        tracking = false;
        if (busyClearing) return;

        Vector2 delta = eventData.position - downPos;
        float distance = delta.magnitude;
        float elapsed = Time.unscaledTime - downTime;

        if (distance > placeTapMaxDistance) return;
        if (elapsed > placeTapTime) return;

        TryPlaceActive();
    }

    public void TryPlaceActive()
    {
        if (busyClearing) return;
        if (figureController == null || field == null) return;
        if (!figureController.HasActiveFigure) return;
        if (!figureController.CanPlaceAtCurrent()) return;

        var shape = figureController.ActiveShape;
        var figureGo = figureController.ActiveFigure;
        if (shape == null || figureGo == null) return;

        // Получаем клетки с учётом поворота
        Vector3Int[] rotatedCells = figureController.GetRotatedCells();
        if (rotatedCells == null) return;

        // LockActiveFigure сбрасывает состояние контроллера, поэтому все данные,
        // необходимые для размещения, нужно сохранить до блокировки.
        Vector3Int placementPosition = figureController.CurrentGridPosition;
        float cellSize = figureController.CellSize;

        // Снимаем активную фигуру
        figureController.LockActiveFigure();

        // Цвет блоков берём из фигуры
        Color blockColor = Color.white;
        var rnd = figureGo.GetComponentInChildren<MeshRenderer>();
        if (rnd != null && rnd.material != null)
        {
            if (rnd.material.HasProperty("_BaseColor"))
                blockColor = rnd.material.GetColor("_BaseColor");
            else if (rnd.material.HasProperty("_Color"))
                blockColor = rnd.material.GetColor("_Color");
        }

        // Размещаем блоки на поле
        var blocks = field.PlaceShape(
            placementPosition,
            rotatedCells,
            blockPrefab,
            blocksParent != null ? blocksParent : field.transform,
            cellSize,
            blockColor);

        if (blocks.Count == 0)
        {
            Destroy(figureGo);
            return;
        }

        // На поле создаются отдельные постоянные блоки. Исходный объект был
        // только управляемым предпросмотром и после установки больше не нужен.
        Destroy(figureGo);

        // Анимация появления
        StartCoroutine(AnimateSpawn(blocks));

        // Проверяем заполненные плоскости и очищаем их с анимацией
        var planes = field.GetCompletedPlanes();
        int cellsCount = blocks.Count;
        StartCoroutine(ResolvePlanesAndNotify(planes, cellsCount));
    }

    private IEnumerator ResolvePlanesAndNotify(List<Field3D.Plane> planes, int cellsCount)
    {
        int cleared = 0;
        int comboBonus = 0;

        if (planes != null && planes.Count > 0 && planeClearer != null)
        {
            busyClearing = true;
            int bonusFromClearer = 0;
            int clearedFromClearer = 0;

            // Подпишемся на событие, чтобы получить точные значения
            System.Action<int, int> handler = null;
            handler = (c, b) => { clearedFromClearer = c; bonusFromClearer = b; };
            planeClearer.OnPlanesCleared += handler;

            yield return StartCoroutine(planeClearer.ClearPlanesWithAnimation(planes));

            planeClearer.OnPlanesCleared -= handler;
            cleared = clearedFromClearer;
            comboBonus = bonusFromClearer;
            busyClearing = false;
        }
        else if (planes != null && planes.Count > 0)
        {
            // Нет clearer — очищаем напрямую
            cleared = planes.Count;
            comboBonus = cleared >= 2 ? (cleared - 1) * 5 : 0;
            foreach (var p in planes) field.ClearPlane(p);
        }

        OnShapePlaced?.Invoke(cellsCount, cleared, comboBonus);
    }

    private IEnumerator AnimateSpawn(List<GameObject> blocks)
    {
        float duration = 0.18f;
        var scales = new List<Vector3>(blocks.Count);
        for (int i = 0; i < blocks.Count; i++)
        {
            if (blocks[i] == null) { scales.Add(Vector3.one); continue; }
            scales.Add(blocks[i].transform.localScale);
            blocks[i].transform.localScale = Vector3.zero;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            float e = Mathf.SmoothStep(0f, 1f, p);
            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i] != null)
                    blocks[i].transform.localScale = scales[i] * e;
            }
            yield return null;
        }

        for (int i = 0; i < blocks.Count; i++)
        {
            if (blocks[i] != null) blocks[i].transform.localScale = scales[i];
        }
    }
}
