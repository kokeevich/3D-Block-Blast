using UnityEngine;

/// <summary>Axis-aligned quarter turns, camera-relative placement and pinch zoom.</summary>
public class FieldRotator : MonoBehaviour
{
    [SerializeField] private float animationTime = 0.22f;
    [SerializeField] private float minSwipeScreenPercent = 0.03f;
    private Vector2 swipeStart;
    private int boardOrientation;
    private Quaternion rotationStart,rotationTarget;
    private float rotationTime;
    public bool IsRotating { get; private set; }
    private ActiveFigureController figure;
    private ShapePlacer placer;
    private MobilePlacementControls controls;
    private GameManager game;
    private Camera view;
    private bool dragging, suppressUntilReleased, pinching;
    private int fingerId;
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
        view = Camera.main;
        float best=-1;
        for (int i=0;i<ShapeRules.Orientations.Count;i++)
        {
            float dot=Mathf.Abs(Quaternion.Dot(transform.rotation,GridRotation(i)));
            if (dot>best) { best=dot; boardOrientation=i; }
        }
        transform.rotation=GridRotation(boardOrientation);
        if (view != null)
        {
            initialDistance = Vector3.Distance(view.transform.position, transform.position);
            initialOrtho = view.orthographicSize;
        }
    }

    private bool Blocked => (placer != null && placer.IsResolving) || (game != null && game.IsGameOver);
    private bool InZone(Vector2 position) => controls!=null && controls.IsBoardArea(position);

    private void Update()
    {
        if (IsRotating)
        {
            rotationTime+=Time.unscaledDeltaTime;
            float progress=Mathf.Clamp01(rotationTime/Mathf.Max(0.01f,animationTime));
            transform.rotation=Quaternion.Slerp(rotationStart,rotationTarget,Mathf.SmoothStep(0,1,progress));
            if (progress>=1) { transform.rotation=rotationTarget; IsRotating=false; }
        }
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
        if (IsRotating) { dragging=false; return; }
        if (Input.touchCount == 1)
        {
            var touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began) { fingerId = touch.fingerId; Begin(touch.position); }
            else if (touch.fingerId == fingerId && touch.phase == TouchPhase.Moved) Drag(touch.position);
            else if (touch.phase==TouchPhase.Ended && touch.fingerId==fingerId) End(touch.position);
            else if (touch.phase==TouchPhase.Canceled) dragging=false;
            return;
        }
        Vector2 mouse = Input.mousePosition;
        if (Input.GetMouseButtonDown(0)) Begin(mouse);
        else if (Input.GetMouseButton(0)) Drag(mouse);
        if (Input.GetMouseButtonUp(0)) End(mouse);
        if (InZone(mouse) && Mathf.Abs(Input.mouseScrollDelta.y) > 0)
            Zoom(Input.mouseScrollDelta.y * 0.04f);
    }

    private void Begin(Vector2 position)
    {
        dragging = InZone(position);
        if (!dragging) return;
        swipeStart=position;
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
        if (!draggingFigure) return; // one turn is chosen on release, never a free orbit
        var ray = view.ScreenPointToRay(position);
        if (!dragPlane.Raycast(ray, out float enter)) return;
        Vector3 localDelta = transform.InverseTransformVector(ray.GetPoint(enter) - dragStart) / figure.CellSize;
        var offset = Vector3Int.RoundToInt(localDelta);
        if (figure.Frame.DepthAxis == 0) offset.x = 0;
        else if (figure.Frame.DepthAxis == 1) offset.y = 0;
        else offset.z = 0;
        figure.SetGridPosition(anchorStart + offset);
    }

    private static Quaternion GridRotation(int index)
    {
        var o=ShapeRules.Orientations[index];
        return Quaternion.LookRotation(new Vector3(o.Forward.x,o.Forward.y,o.Forward.z),
            new Vector3(o.Up.x,o.Up.y,o.Up.z));
    }
    private void End(Vector2 position)
    {
        bool turn=dragging && !draggingFigure && !Blocked && !IsRotating &&
            figure!=null && !figure.HasActiveFigure;
        dragging=false;
        if (!turn) return;
        Vector2 delta=position-swipeStart;
        if (!BoardTurnRules.TryGetTurn(delta.x,delta.y,Mathf.Max(16,Screen.height*minSwipeScreenPercent),out int axis,out int direction)) return;
        rotationStart=transform.rotation;
        boardOrientation=BoardTurnRules.Apply(boardOrientation,axis,direction);
        rotationTarget=GridRotation(boardOrientation);
        rotationTime=0; IsRotating=true;
    }

    public void RefreshZoomLimits()
    {
        if (view == null) view = Camera.main;
        if (view == null) return;
        initialDistance = Vector3.Distance(view.transform.position,transform.position);
        initialOrtho = view.orthographicSize;
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
