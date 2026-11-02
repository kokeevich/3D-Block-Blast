using System.Collections.Generic;
using UnityEngine;

public class ActiveFigureController : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private float cellSize = 0.5f;
    [SerializeField] private Transform fieldTransform;
    [SerializeField] private Field3D field;

    [Header("Visuals")]
    [SerializeField] private Color validColor = new Color(0.4f, 0.95f, 0.4f, 1f);
    [SerializeField] private Color invalidColor = new Color(0.95f, 0.4f, 0.4f, 1f);
    [SerializeField] private Color defaultColor = new Color(0.85f, 0.85f, 0.9f, 1f);

    private GameObject activeFigure;
    private FigureGridShape gridShape;
    private Vector3Int currentGridPosition;
    private Quaternion figureLocalRotation = Quaternion.identity;

    // Кэш материалов для подсветки валидности
    private List<MeshRenderer> renderers = new List<MeshRenderer>();
    private List<Color> originalColors = new List<Color>();
    private bool validityHighlighted = false;

    public Vector3Int CurrentGridPosition => currentGridPosition;
    public bool HasActiveFigure => activeFigure != null;
    public GameObject ActiveFigure => activeFigure;
    public FigureGridShape ActiveShape => gridShape;
    public float CellSize => field != null ? field.CellSize : cellSize;
    public Transform FieldTransform => fieldTransform;

    public void SetField(Field3D newField)
    {
        field = newField;
    }

    public void SetActiveFigure(GameObject figure, Vector3Int startGridPos)
    {
        ClearActiveFigure();

        activeFigure = figure;
        gridShape = figure != null ? figure.GetComponent<FigureGridShape>() : null;
        currentGridPosition = startGridPos;
        figureLocalRotation = figure != null && fieldTransform != null
            ? Quaternion.Inverse(fieldTransform.rotation) * figure.transform.rotation
            : Quaternion.identity;

        CacheRenderers();
        ResetRendererColors();
        UpdateWorldPosition();
        SnapToNearestCell();
        UpdateValidityHighlight();
    }

    public void ClearActiveFigure()
    {
        if (activeFigure != null)
        {
            Destroy(activeFigure);
        }

        activeFigure = null;
        gridShape = null;
        currentGridPosition = Vector3Int.zero;
        renderers.Clear();
        originalColors.Clear();
        validityHighlighted = false;
        figureLocalRotation = Quaternion.identity;
    }

    public void LockActiveFigure()
    {
        // Не уничтожаем — оставляем как "лок на экране", очищаем ссылки
        activeFigure = null;
        gridShape = null;
        currentGridPosition = Vector3Int.zero;
        renderers.Clear();
        originalColors.Clear();
        validityHighlighted = false;
        figureLocalRotation = Quaternion.identity;
    }

    public void Move(MoveAxis axis, int direction)
    {
        if (activeFigure == null)
            return;

        Vector3Int offset = axis switch
        {
            MoveAxis.X => Vector3Int.right * direction,
            MoveAxis.Y => Vector3Int.up * direction,
            MoveAxis.Z => Vector3Int.forward * direction,
            _ => Vector3Int.zero
        };

        Vector3Int targetPos = currentGridPosition + offset;

        if (!CanMoveTo(targetPos))
            return;

        currentGridPosition = targetPos;
        UpdateWorldPosition();
        UpdateValidityHighlight();
    }

    /// <summary>
    /// Возвращает клетки фигуры, повёрнутые в соответствии с текущим поворотом объекта.
    /// </summary>
    public Vector3Int[] GetRotatedCells()
    {
        if (activeFigure == null || gridShape == null) return null;
        return gridShape.GetRotatedCells(figureLocalRotation);
    }

    private void LateUpdate()
    {
        if (activeFigure != null)
            UpdateWorldPosition();
    }

    /// <summary>
    /// Проверяет, можно ли поставить фигуру в текущей позиции на поле.
    /// </summary>
    public bool CanPlaceAtCurrent()
    {
        if (activeFigure == null || gridShape == null || field == null) return false;
        return field.CanPlaceShape(currentGridPosition, GetRotatedCells());
    }

    /// <summary>
    /// Пытается переместить фигуру в любую свободную позицию.
    /// Возвращает true, если нашлась.
    /// </summary>
    public bool TryFindAnyPlacement()
    {
        if (activeFigure == null || gridShape == null || field == null) return false;
        if (field.TryFindPlacement(GetRotatedCells(), out var pos))
        {
            currentGridPosition = pos;
            UpdateWorldPosition();
            UpdateValidityHighlight();
            return true;
        }
        return false;
    }

    private void UpdateValidityHighlight()
    {
        if (activeFigure == null) return;
        bool canPlace = CanPlaceAtCurrent();
        ApplyColor(canPlace ? validColor : invalidColor);
        validityHighlighted = canPlace;
    }

    private void ApplyColor(Color c)
    {
        for (int i = 0; i < renderers.Count; i++)
        {
            var r = renderers[i];
            if (r == null) continue;
            // Используем MaterialPropertyBlock, чтобы не плодить инстансы
            var block = new MaterialPropertyBlock();
            r.GetPropertyBlock(block);
            // Стандартный шейдер использует _BaseColor (URP) или _Color (Built-in)
            block.SetColor("_BaseColor", c);
            block.SetColor("_Color", c);
            r.SetPropertyBlock(block);
        }
    }

    private void ResetRendererColors()
    {
        for (int i = 0; i < renderers.Count; i++)
        {
            var r = renderers[i];
            if (r == null) continue;
            var block = new MaterialPropertyBlock();
            r.GetPropertyBlock(block);
            block.SetColor("_BaseColor", originalColors[i]);
            block.SetColor("_Color", originalColors[i]);
            r.SetPropertyBlock(block);
        }
    }

    private void CacheRenderers()
    {
        renderers.Clear();
        originalColors.Clear();
        if (activeFigure == null) return;

        var rends = activeFigure.GetComponentsInChildren<MeshRenderer>(true);
        foreach (var r in rends)
        {
            renderers.Add(r);
            // Получаем оригинальный цвет из инстанса материала, если он есть, иначе из sharedMaterial
            Color c = defaultColor;
            if (r.material != null && r.material.HasProperty("_BaseColor"))
                c = r.material.GetColor("_BaseColor");
            else if (r.material != null && r.material.HasProperty("_Color"))
                c = r.material.GetColor("_Color");
            originalColors.Add(c);
        }
    }

    private void UpdateWorldPosition()
    {
        if (fieldTransform == null)
        {
            Debug.LogError("FieldTransform is not assigned!");
            return;
        }

        Vector3 worldGridPos = field != null
            ? field.GridToWorld(currentGridPosition)
            : fieldTransform.TransformPoint((Vector3)currentGridPosition * cellSize);

        Vector3 shapePivot = Vector3.zero;
        if (gridShape != null)
            shapePivot = fieldTransform.rotation * gridShape.pivotOffset;

        activeFigure.transform.position = worldGridPos - shapePivot;
        activeFigure.transform.rotation = fieldTransform.rotation * figureLocalRotation;
    }

    public void SnapToNearestCell()
    {
        if (activeFigure == null || gridShape == null)
            return;

        Vector3 anchorWorld =
            activeFigure.transform.position +
            fieldTransform.rotation * gridShape.pivotOffset;

        Vector3 local =
            Quaternion.Inverse(fieldTransform.rotation) *
            (anchorWorld - fieldTransform.position);

        currentGridPosition = field != null
            ? field.LocalToGrid(local)
            : new Vector3Int(
                Mathf.RoundToInt(local.x / cellSize),
                Mathf.RoundToInt(local.y / cellSize),
                Mathf.RoundToInt(local.z / cellSize));

        UpdateWorldPosition();
    }

    private bool CanMoveTo(Vector3Int gridPos)
    {
        if (activeFigure == null || gridShape == null) return false;

        if (field == null) return true; // fallback на старую логику

        return field.CanPlaceShape(gridPos, GetRotatedCells());
    }
}
