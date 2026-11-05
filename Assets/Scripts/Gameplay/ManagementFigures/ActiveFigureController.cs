using System;
using System.Collections.Generic;
using UnityEngine;

public class ActiveFigureController : MonoBehaviour
{
    [SerializeField] private float cellSize = 0.5f;
    [SerializeField] private Transform fieldTransform;
    [SerializeField] private Field3D field;
    [SerializeField] private Color validColor = new Color(0.4f, 0.95f, 0.4f, 0.55f);
    [SerializeField] private Color invalidColor = new Color(0.95f, 0.4f, 0.4f, 0.65f);

    private GameManager game;
    private GameObject activeFigure;
    private void Awake() { game=FindObjectOfType<GameManager>(); }
    private bool InteractionBlocked => game!=null && (game.InputBlocked || !game.Tutorial.CanManipulate);
    private FigureGridShape gridShape;
    private ShapeInstanceData data;
    private Vector3Int currentGridPosition;
    private Transform ghost;
    private Material ghostMaterial;
    private readonly List<Transform> ghostCells = new List<Transform>();
    private readonly List<Renderer> ghostRenderers=new List<Renderer>();
    private ShapeOffer cachedOffer;
    private BoardModel.Cell[] modelCells;
    private Vector3Int[] visualCells;
    private int checkedRevision=-1;
    private Vector3Int checkedAnchor,drawnAnchor;
    private bool cachedValid,drawn;
    private ShapeOffer drawnOffer;
    private Color drawnColor;
    private MaterialPropertyBlock colorBlock;
    public PlacementFrame Frame { get; private set; }
    public Vector3Int CurrentGridPosition => currentGridPosition;
    public bool HasActiveFigure => activeFigure != null;
    public GameObject ActiveFigure => activeFigure;
    public FigureGridShape ActiveShape => gridShape;
    public float CellSize => field != null ? field.CellSize : cellSize;
    public Transform FieldTransform => fieldTransform;
    public Color ActiveColor => data != null ? data.color : Color.white;
    public ShapeInstanceData ActiveOffer => data;
    public int Depth => Frame == null ? 0 : Frame.Depth(ToCell(currentGridPosition));

    public void SetPreviewColors(Color valid, Color invalid)
    {
        validColor=valid; invalidColor=invalid;
        if (HasActiveFigure) RefreshGhost();
    }

    public void SetField(Field3D value)
    {
        field = value;
        fieldTransform = value != null ? value.transform : null;
    }

    public void SetActiveFigure(GameObject figure, ShapeInstanceData offerData)
    {
        ClearActiveFigure();
        if (colorBlock == null) colorBlock = new MaterialPropertyBlock();
        activeFigure = figure;
        gridShape = figure.GetComponent<FigureGridShape>();
        data = offerData;
        var camera = Camera.main;
        Vector3 toCamera = camera != null ? camera.transform.position - fieldTransform.position : -fieldTransform.forward;
        Vector3 right = camera != null ? camera.transform.right : fieldTransform.right;
        Vector3 up = camera != null ? camera.transform.up : fieldTransform.up;
        Frame = new PlacementFrame(Direction(fieldTransform.InverseTransformDirection(toCamera)),
            Direction(fieldTransform.InverseTransformDirection(right)), Direction(fieldTransform.InverseTransformDirection(up)));
        // A neutral, repeatable preview may be blocked. Choosing a free position
        // and orientation is the player's move, not the spawn system's move.
        currentGridPosition=ToVector(Frame.StartAnchor(data.offer.Cells));
        // Keep the source object for shape/color data; render exact target cells instead.
        foreach (var renderer in figure.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
        if (ghost==null)
        {
            ghost = new GameObject("PlacementGhost").transform;
            Shader shader = Resources.Load<Shader>("PlacementGhost");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            ghostMaterial = new Material(shader) { name = "Placement Ghost" };
        }
        ghost.SetParent(fieldTransform,false); ghost.gameObject.SetActive(true);
        EnsureGeometry();
        while (ghostCells.Count<visualCells.Length)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(ghost,false);
            var collider=cube.GetComponent<Collider>(); collider.enabled=false; Destroy(collider);
            var renderer=cube.GetComponent<Renderer>(); renderer.sharedMaterial=ghostMaterial;
            ghostCells.Add(cube.transform); ghostRenderers.Add(renderer);
        }
        for (int i=0;i<ghostCells.Count;i++) ghostCells[i].gameObject.SetActive(i<visualCells.Length);
        drawn=false;
        RefreshGhost();
    }

    private static PlacementFrame.Direction Direction(Vector3 v) => new PlacementFrame.Direction(v.x, v.y, v.z);
    private static BoardModel.Cell ToCell(Vector3Int c) => new BoardModel.Cell(c.x, c.y, c.z);
    private static Vector3Int ToVector(BoardModel.Cell c) => new Vector3Int(c.x, c.y, c.z);
    private void EnsureGeometry()
    {
        if (data==null || cachedOffer==data.offer) return;
        cachedOffer=data.offer; modelCells=cachedOffer.Cells;
        visualCells=Array.ConvertAll(modelCells,ToVector); checkedRevision=-1;
    }
    public Vector3Int[] GetRotatedCells()
    { if (data==null) return null; EnsureGeometry(); return (Vector3Int[])visualCells.Clone(); }

    public bool CanPlaceAtCurrent()
    {
        if (!HasActiveFigure || field==null) return false;
        EnsureGeometry();
        if (checkedRevision!=field.Revision || checkedAnchor!=currentGridPosition)
        {
            cachedValid=field.Model.CanPlaceShape(ToCell(currentGridPosition),modelCells);
            checkedRevision=field.Revision; checkedAnchor=currentGridPosition;
        }
        return cachedValid;
    }

    public void RestoreGridPosition(Vector3Int position)
    { currentGridPosition=position; RefreshGhost(); }

    public void SetGridPosition(Vector3Int position)
    {
        if (!HasActiveFigure || InteractionBlocked) return;
        var previous=currentGridPosition;
        currentGridPosition = new Vector3Int(Mathf.Clamp(position.x, 0, 7), Mathf.Clamp(position.y, 0, 7), Mathf.Clamp(position.z, 0, 7));
        RefreshGhost();
        if (currentGridPosition!=previous && Frame!=null && game!=null)
        {
            int a=Frame.DepthAxis;
            bool depth=a==0 ? previous.x!=currentGridPosition.x : a==1 ? previous.y!=currentGridPosition.y : previous.z!=currentGridPosition.z;
            game.NotifyTutorialAction(depth ? TutorialProgress.Action.ChangeDepth : TutorialProgress.Action.MoveShape);
        }
    }

    public void SetDepth(int depth)
    {
        if (Frame != null && HasActiveFigure) SetGridPosition(ToVector(Frame.AtDepth(ToCell(currentGridPosition), depth)));
    }

    public void Move(MoveAxis axis, int direction)
    {
        var delta = axis == MoveAxis.X ? Vector3Int.right : axis == MoveAxis.Y ? Vector3Int.up : Vector3Int.forward;
        SetGridPosition(currentGridPosition + delta * direction);
    }

    public void MoveOnScreen(int horizontal, int vertical)
    {
        if (Frame != null) SetGridPosition(ToVector(Frame.Move(ToCell(currentGridPosition), horizontal, vertical)));
    }

    public void Rotate(int localAxis)
    {
        if (!HasActiveFigure || InteractionBlocked) return;
        data.offer = new ShapeOffer(data.offer.CatalogIndex, data.baseCells,
            ShapeRules.RotateOrientation(data.offer.Orientation, localAxis));
        data.onRotationChanged?.Invoke();
        RefreshGhost();
    }

    private void LateUpdate() { if (HasActiveFigure) RefreshGhost(); }
    private void RefreshGhost()
    {
        if (ghost == null || data == null) return;
        EnsureGeometry();
        var cells = visualCells;
        bool valid = CanPlaceAtCurrent();
        Color color = valid ? validColor : invalidColor;
        color.a = valid ? 0.55f : 0.65f;
        if (drawn && drawnOffer==cachedOffer && drawnAnchor==currentGridPosition && drawnColor==color) return;
        drawn=true; drawnOffer=cachedOffer; drawnAnchor=currentGridPosition; drawnColor=color;
        colorBlock.SetColor("_Color", color);
        colorBlock.SetColor("_BaseColor", color);
        for (int i = 0; i < cells.Length; i++)
        {
            ghostCells[i].localPosition = field.GridToLocal(currentGridPosition + cells[i]);
            ghostCells[i].localScale = Vector3.one * field.CellSize * 0.92f;
            ghostRenderers[i].SetPropertyBlock(colorBlock);
        }
    }

    private void ReleaseGhost()
    {
        if (ghost != null) ghost.gameObject.SetActive(false);
        Frame=null; cachedOffer=null; drawn=false; checkedRevision=-1;
    }
    public void ClearActiveFigure()
    {
        if (activeFigure != null) Destroy(activeFigure);
        LockActiveFigure();
    }
    public void LockActiveFigure()
    {
        ReleaseGhost();
        activeFigure = null; gridShape = null; data = null;
        currentGridPosition = Vector3Int.zero;
    }
    public void SnapToNearestCell() { RefreshGhost(); }
    private void OnDestroy()
    {
        ClearActiveFigure();
        if (ghost!=null) Destroy(ghost.gameObject);
        if (ghostMaterial!=null) Destroy(ghostMaterial);
    }
}
