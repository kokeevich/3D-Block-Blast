using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShapePlacer : MonoBehaviour
{
    [SerializeField] private ActiveFigureController figureController;
    [SerializeField] private Field3D field;
    [SerializeField] private PlaneClearer planeClearer;
    [SerializeField] private Transform blocksParent;
    [SerializeField] private GameObject blockPrefab;

    public delegate void ShapePlacedHandler(int cellsCount, BoardModel.ClearResult clearResult);
    public event ShapePlacedHandler OnShapePlaced;

    private bool busyClearing;
    private GameManager game;
    private BoardModel.ClearResult pending;
    private int pendingCells;
    public int PendingScore(int pointsPerBlock) => pending!=null ? pending.Score(pendingCells,pointsPerBlock) : 0;

    public BoardModel.ClearResult PendingClear => pending;
    public int PendingCells => pendingCells;
    public void RestoreBlock(RunSnapshot.Block b)
    {
        int v=b.rgba;
        Color color=new Color32((byte)(v>>24),(byte)(v>>16),(byte)(v>>8),(byte)v);
        field.PlaceShape(new Vector3Int(b.x,b.y,b.z),new[] { Vector3Int.zero },blockPrefab,
            blocksParent!=null ? blocksParent : field.transform,color);
    }

    public bool IsResolving => busyClearing;

    void Start()
    {
        game=FindObjectOfType<GameManager>();
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

    public void TryPlaceActive()
    {
        if (busyClearing || (game!=null && !game.CanPlace)) return;
        if (figureController == null || field == null) return;
        if (!figureController.HasActiveFigure) return;
        if (!figureController.CanPlaceAtCurrent()) { GameFeedback.Play(GameFeedback.Cue.Invalid); return; }

        var shape = figureController.ActiveShape;
        var figureGo = figureController.ActiveFigure;
        if (shape == null || figureGo == null) return;

        // Получаем клетки с учётом поворота
        Vector3Int[] rotatedCells = figureController.GetRotatedCells();
        if (rotatedCells == null) return;

        // LockActiveFigure сбрасывает состояние контроллера, поэтому все данные,
        // необходимые для размещения, нужно сохранить до блокировки.
        Vector3Int placementPosition = figureController.CurrentGridPosition;

        Color blockColor = figureController.ActiveColor;
        figureController.LockActiveFigure();

        // Размещаем блоки на поле
        var blocks = field.PlaceShape(
            placementPosition,
            rotatedCells,
            blockPrefab,
            blocksParent != null ? blocksParent : field.transform,
            blockColor);

        if (blocks.Count == 0)
        {
            Destroy(figureGo);
            return;
        }

        // На поле создаются отдельные постоянные блоки. Исходный объект был
        // только управляемым предпросмотром и после установки больше не нужен.
        Destroy(figureGo);

        // Capture all lines and plane bonuses before removing any cell.
        var result = field.GetCompletedLines();
        pending=result; pendingCells=blocks.Count;
        busyClearing = true;
        GameFeedback.Play(GameFeedback.Cue.Place);
        if (game!=null) game.SaveSession();
        StartCoroutine(ResolveLinesAndNotify(result, blocks));
    }

    private IEnumerator ResolveLinesAndNotify(BoardModel.ClearResult result, List<GameObject> blocks)
    {
        try
        {
            // Run sequentially so spawn and clear never animate the same scale.
            yield return StartCoroutine(AnimateSpawn(blocks));
            if (result.Cells.Count > 0)
            {
                GameFeedback.Play(result.LinesCleared>=2 || result.PlanesCompleted>0 ? GameFeedback.Cue.Combo : GameFeedback.Cue.Clear);
                if (planeClearer != null)
                {
                    planeClearer.SetField(field);
                    yield return StartCoroutine(planeClearer.ClearLinesWithAnimation(result));
                }
                else field.ClearLines(result);
            }
            pending=null; pendingCells=0;
            OnShapePlaced?.Invoke(blocks.Count, result);
        }
        finally
        {
            busyClearing = false;
            pending=null; pendingCells=0;
        }
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
