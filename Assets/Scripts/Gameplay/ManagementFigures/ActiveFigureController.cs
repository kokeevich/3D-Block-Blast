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

    private GameObject activeFigure;
    private FigureGridShape gridShape;
    private ShapeInstanceData data;
    private Vector3Int currentGridPosition;
    private Transform ghost;
    private Material ghostMaterial;
    private readonly List<Transform> ghostCells = new List<Transform>();
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
        ghost = new GameObject("PlacementGhost").transform;
        ghost.SetParent(fieldTransform, false);
        Shader shader = Resources.Load<Shader>("PlacementGhost");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        ghostMaterial = new Material(shader) { name = "Placement Ghost" };
        foreach (var cell in data.offer.Cells)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(ghost, false);
            Destroy(cube.GetComponent<Collider>());
            cube.GetComponent<Renderer>().sharedMaterial = ghostMaterial;
            ghostCells.Add(cube.transform);
        }
        RefreshGhost();
    }

    private static PlacementFrame.Direction Direction(Vector3 v) => new PlacementFrame.Direction(v.x, v.y, v.z);
    private static BoardModel.Cell ToCell(Vector3Int c) => new BoardModel.Cell(c.x, c.y, c.z);
    private static Vector3Int ToVector(BoardModel.Cell c) => new Vector3Int(c.x, c.y, c.z);
    public Vector3Int[] GetRotatedCells() => data == null ? null : Array.ConvertAll(data.offer.Cells, ToVector);

    public bool CanPlaceAtCurrent() => HasActiveFigure && field != null &&
        field.CanPlaceShape(currentGridPosition, GetRotatedCells());

    public void SetGridPosition(Vector3Int position)
    {
        if (!HasActiveFigure) return;
        currentGridPosition = new Vector3Int(Mathf.Clamp(position.x, 0, 7), Mathf.Clamp(position.y, 0, 7), Mathf.Clamp(position.z, 0, 7));
        RefreshGhost();
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
        if (!HasActiveFigure) return;
        data.offer = new ShapeOffer(data.offer.CatalogIndex, data.baseCells,
            ShapeRules.RotateOrientation(data.offer.Orientation, localAxis));
        data.onRotationChanged?.Invoke();
        RefreshGhost();
    }

    private void LateUpdate() { if (HasActiveFigure) RefreshGhost(); }
    private void RefreshGhost()
    {
        if (ghost == null || data == null) return;
        var cells = GetRotatedCells();
        bool valid = CanPlaceAtCurrent();
        Color color = valid ? validColor : invalidColor;
        color.a = valid ? 0.55f : 0.65f;
        colorBlock.SetColor("_Color", color);
        colorBlock.SetColor("_BaseColor", color);
        for (int i = 0; i < cells.Length; i++)
        {
            ghostCells[i].localPosition = field.GridToLocal(currentGridPosition + cells[i]);
            ghostCells[i].localScale = Vector3.one * field.CellSize * 0.92f;
            ghostCells[i].GetComponent<Renderer>().SetPropertyBlock(colorBlock);
        }
    }

    private void ReleaseGhost()
    {
        if (ghost != null) { ghost.gameObject.SetActive(false); Destroy(ghost.gameObject); }
        if (ghostMaterial != null) Destroy(ghostMaterial);
        ghost = null; ghostMaterial = null; ghostCells.Clear(); Frame = null;
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
    private void OnDestroy() { ClearActiveFigure(); }
}
