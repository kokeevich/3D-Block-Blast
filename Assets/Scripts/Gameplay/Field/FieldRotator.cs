using UnityEngine;

/// <summary>One gesture owner for orbit, camera-relative placement and pinch zoom.</summary>
public class FieldRotator : MonoBehaviour
{
    public RectTransform swipeZone;
    [SerializeField] private float orbitDegreesPerScreen = 180f;
    private ActiveFigureController figure;
    private ShapePlacer placer;
    private MobilePlacementControls controls;
    private GameManager game;
    private Camera view;
    private RectTransform figureZone;
    private ShapeSpawnButton[] offerButtons;
    private bool dragging, suppressUntilReleased, pinching;
    private int fingerId;
    private Vector2 lastPosition;
    private float lastPinch, initialDistance, initialOrtho;
    private Plane dragPlane;
    private Vector3 dragStart;
    private Vector3Int anchorStart;
    private bool draggingFigure;

    private void Start()
    {
        figure = FindObjectOfType<ActiveFigureController>();
        placer = FindObjectOfType<ShapePlacer>();
        controls = FindObjectOfType<MobilePlacementControls>();
        game = FindObjectOfType<GameManager>();
        var zone = FindObjectOfType<SwipeZoneFigure>();
        figureZone = zone != null ? zone.GetComponent<RectTransform>() : null;
        offerButtons = FindObjectsOfType<ShapeSpawnButton>();
        view = Camera.main;
        if (view != null)
        {
            initialDistance = Vector3.Distance(view.transform.position, transform.position);
            initialOrtho = view.orthographicSize;
        }
    }

    private bool Blocked => (placer != null && placer.IsResolving) || (game != null && game.IsGameOver);
    private bool InZone(Vector2 position)
    {
        if (controls != null && controls.Contains(position)) return false;
        if (offerButtons != null)
            foreach (var button in offerButtons)
                if (button != null && Contains(button.GetComponent<RectTransform>(), position)) return false;
        return Contains(swipeZone, position) || Contains(figureZone, position);
    }
    private static bool Contains(RectTransform rect, Vector2 position)
    {
        if (rect == null) return false;
        var canvas = rect.GetComponentInParent<Canvas>();
        return RectTransformUtility.RectangleContainsScreenPoint(rect, position,
            canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null);
    }

    private void Update()
    {
        if (controls == null) controls = FindObjectOfType<MobilePlacementControls>();
        if (view == null || Blocked) { dragging = false; pinching = false; return; }
        if (Input.touchCount >= 2)
        {
            dragging = false;
            suppressUntilReleased = true;
            var a = Input.GetTouch(0); var b = Input.GetTouch(1);
            if (!InZone(a.position) || !InZone(b.position)) { pinching = false; return; }
            float distance = Vector2.Distance(a.position, b.position);
            if (pinching) Zoom((distance - lastPinch) / Mathf.Max(1, Screen.height));
            lastPinch = distance; pinching = true;
            return;
        }
        pinching = false;
        if (suppressUntilReleased)
        {
            if (Input.touchCount == 0 && !Input.GetMouseButton(0)) suppressUntilReleased = false;
            return;
        }
        if (Input.touchCount == 1)
        {
            var touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began) { fingerId = touch.fingerId; Begin(touch.position); }
            else if (touch.fingerId == fingerId && touch.phase == TouchPhase.Moved) Drag(touch.position);
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) dragging = false;
            return;
        }
        Vector2 mouse = Input.mousePosition;
        if (Input.GetMouseButtonDown(0)) Begin(mouse);
        else if (Input.GetMouseButton(0)) Drag(mouse);
        if (Input.GetMouseButtonUp(0)) dragging = false;
        if (InZone(mouse) && Mathf.Abs(Input.mouseScrollDelta.y) > 0)
            Zoom(Input.mouseScrollDelta.y * 0.04f);
    }

    private void Begin(Vector2 position)
    {
        dragging = InZone(position);
        if (!dragging) return;
        lastPosition = position;
        draggingFigure = figure != null && figure.HasActiveFigure;
        if (!draggingFigure) return;
        anchorStart = figure.CurrentGridPosition;
        var frame = figure.Frame;
        Vector3 axis = frame.DepthAxis == 0 ? Vector3.right : frame.DepthAxis == 1 ? Vector3.up : Vector3.forward;
        var field = figure.FieldTransform.GetComponent<Field3D>();
        dragPlane = new Plane(transform.TransformDirection(axis), field.GridToWorld(anchorStart));
        var ray = view.ScreenPointToRay(position);
        if (!dragPlane.Raycast(ray, out float enter)) { dragging = false; return; }
        dragStart = ray.GetPoint(enter);
    }

    private void Drag(Vector2 position)
    {
        if (!dragging) return;
        // Selection/cancellation during a pointer gesture cannot change its meaning.
        if (draggingFigure != (figure != null && figure.HasActiveFigure)) { dragging = false; return; }
        Vector2 delta = position - lastPosition;
        lastPosition = position;
        if (!draggingFigure)
        {
            float scale = orbitDegreesPerScreen / Mathf.Max(1, Screen.height);
            transform.rotation = Quaternion.AngleAxis(-delta.x * scale, view.transform.up) *
                Quaternion.AngleAxis(delta.y * scale, view.transform.right) * transform.rotation;
            return;
        }
        var ray = view.ScreenPointToRay(position);
        if (!dragPlane.Raycast(ray, out float enter)) return;
        Vector3 localDelta = transform.InverseTransformVector(ray.GetPoint(enter) - dragStart) / figure.CellSize;
        var offset = Vector3Int.RoundToInt(localDelta);
        if (figure.Frame.DepthAxis == 0) offset.x = 0;
        else if (figure.Frame.DepthAxis == 1) offset.y = 0;
        else offset.z = 0;
        figure.SetGridPosition(anchorStart + offset);
    }

    private void Zoom(float amount)
    {
        if (view.orthographic)
            view.orthographicSize = Mathf.Clamp(view.orthographicSize * Mathf.Exp(-amount * 3), initialOrtho * 0.55f, initialOrtho * 1.8f);
        else
        {
            Vector3 offset = view.transform.position - transform.position;
            float distance = Mathf.Clamp(offset.magnitude * Mathf.Exp(-amount * 3), initialDistance * 0.55f, initialDistance * 1.8f);
            view.transform.position = transform.position + offset.normalized * distance;
        }
    }
}
