using UnityEngine;
using UnityEngine.UI;

/// <summary>Runtime controls keep the existing Game scene usable without Inspector wiring.</summary>
public class MobilePlacementControls : MonoBehaviour
{
    private ActiveFigureController figure;
    private ShapePlacer placer;
    private GameManager game;
    private GameObject canvasObject;
    private RectTransform panel;
    private Text hint, depthLabel;
    private Slider depth;
    private Button rotateHorizontal, rotateVertical, cancel, place;
    private Font font;
    private RectTransform[] offerSlots;

    public void Initialize(ActiveFigureController controller, ShapePlacer shapePlacer, GameManager manager)
    {
        figure = controller; placer = shapePlacer; game = manager;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        canvasObject = new GameObject("PlacementControls", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0;
        var selector = FindObjectOfType<BalancedShapeSelector>();
        if (selector != null && selector.spawnButtons != null)
            offerSlots = System.Array.ConvertAll(selector.spawnButtons, slot => slot != null ? slot.parent as RectTransform : null);
        panel = Rect("Panel", canvasObject.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 280), new Vector2(0, 0));
        panel.pivot = new Vector2(0.5f, 0);
        var background = panel.gameObject.AddComponent<Image>();
        background.color = new Color(0.04f, 0.07f, 0.12f, 0.94f);
        hint = Label("Hint", panel, "", new Vector2(0.02f, 0.7f), new Vector2(0.98f, 1));
        hint.fontSize = 27;
        depthLabel = Label("Depth", panel, "", new Vector2(0.02f, 0.4f), new Vector2(0.25f, 0.7f));
        var sliderRect = Rect("DepthSlider", panel, new Vector2(0.28f, 0.43f), new Vector2(0.96f, 0.66f));
        var sliderBackground = sliderRect.gameObject.AddComponent<Image>();
        sliderBackground.color = new Color(0.2f, 0.25f, 0.32f);
        depth = sliderRect.gameObject.AddComponent<Slider>();
        depth.minValue = 0; depth.maxValue = 7; depth.wholeNumbers = true;
        var handleArea = Rect("HandleArea", sliderRect, Vector2.zero, Vector2.one);
        handleArea.offsetMin = new Vector2(25, 0); handleArea.offsetMax = new Vector2(-25, 0);
        var handle = Rect("Handle", handleArea, Vector2.zero, new Vector2(0, 1), new Vector2(50, 0));
        var handleImage = handle.gameObject.AddComponent<Image>();
        handleImage.color = new Color(0.4f, 0.85f, 1);
        depth.handleRect = handle; depth.targetGraphic = handleImage;
        depth.onValueChanged.AddListener(value => { if (CanInteract) figure.SetDepth(Mathf.RoundToInt(value)); });
        rotateHorizontal = MakeButton("Поворот ↔", 0, () => { if (CanInteract) figure.Rotate(figure.Frame.VerticalAxis); });
        rotateVertical = MakeButton("Поворот ↕", 1, () => { if (CanInteract) figure.Rotate(figure.Frame.HorizontalAxis); });
        cancel = MakeButton("Отмена", 2, () => { if (CanInteract) game.CancelSelection(); });
        place = MakeButton("Поставить", 3, () => { if (CanInteract) placer.TryPlaceActive(); });
    }

    private bool CanInteract => figure != null && figure.HasActiveFigure && placer != null && !placer.IsResolving && !game.IsGameOver;
    public bool Contains(Vector2 position) => panel != null && RectTransformUtility.RectangleContainsScreenPoint(panel, position);

    private void Update()
    {
        if (panel == null) return;
        var safe = Screen.safeArea;
        var canvas = canvasObject.GetComponent<Canvas>();
        panel.anchorMin = new Vector2(safe.xMin / Mathf.Max(1, Screen.width), 0);
        panel.anchorMax = new Vector2(safe.xMax / Mathf.Max(1, Screen.width), 0);
        panel.anchoredPosition = new Vector2(0, safe.yMin / canvas.scaleFactor);
        // Existing offer slots were at the bottom of Game's canvas. Keep them
        // above the new controls without requiring edits to the serialized scene.
        if (offerSlots != null)
            foreach (var slot in offerSlots)
            {
                if (slot == null) continue;
                var slotCanvas = slot.GetComponentInParent<Canvas>();
                float bottom = safe.yMin / (slotCanvas != null ? slotCanvas.scaleFactor : canvas.scaleFactor);
                var position = slot.anchoredPosition;
                position.y = Mathf.Max(position.y, 310 + bottom + slot.rect.height * slot.pivot.y);
                slot.anchoredPosition = position;
            }
        bool active = CanInteract;
        rotateHorizontal.interactable = rotateVertical.interactable = cancel.interactable = depth.interactable = active;
        place.interactable = active && figure.CanPlaceAtCurrent();
        if (figure != null && figure.HasActiveFigure)
        {
            depth.SetValueWithoutNotify(figure.Depth);
            depthLabel.text = "Слой " + (figure.Depth + 1) + " / 8";
            hint.text = figure.CanPlaceAtCurrent() ? "Двигайте фигуру • выберите глубину • поставьте" : "Красный: занято или за границей. Сдвиньте или поверните";
        }
        else
        {
            depthLabel.text = "Глубина";
            hint.text = game.IsGameOver ? "Нет доступных ходов" : placer.IsResolving ? "Размещение…" : "Вращайте куб пальцем • масштаб двумя • выберите фигуру";
        }
    }

    private Button MakeButton(string title, int index, UnityEngine.Events.UnityAction action)
    {
        float x = 0.02f + index * 0.245f;
        var rect = Rect(title, panel, new Vector2(x, 0.05f), new Vector2(x + 0.225f, 0.34f));
        var image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.15f, 0.37f, 0.52f);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        Label("Label", rect, title, Vector2.zero, Vector2.one);
        return button;
    }
    private Text Label(string name, Transform parent, string value, Vector2 min, Vector2 max)
    {
        var rect = Rect(name, parent, min, max);
        var text = rect.gameObject.AddComponent<Text>();
        text.font = font; text.fontSize = 29; text.alignment = TextAnchor.MiddleCenter;
        text.text = value; text.color = Color.white; text.raycastTarget = false;
        text.resizeTextForBestFit = true; text.resizeTextMinSize = 18; text.resizeTextMaxSize = 29;
        return text;
    }
    private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max,
        Vector2 size = default, Vector2 position = default)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchorMin = min; rect.anchorMax = max;
        rect.sizeDelta = size; rect.anchoredPosition = position;
        return rect;
    }
    private void OnDestroy() { if (canvasObject != null) Destroy(canvasObject); }
}
