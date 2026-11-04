using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BalancedShapeSelector : MonoBehaviour
{
    [SerializeField] private ActiveFigureController activeFigureController;
    [SerializeField] private Field3D field;
    public const int OfferCount=3;

    private readonly List<ShapeInstanceData> catalog = new List<ShapeInstanceData>();
    private ShapeInstanceData[] assignedShapes;
    private bool[] used;
    private readonly System.Random random = new System.Random();
    public int PlacedShapes { get; private set; }
    public event Action OnOfferChanged;
    public event Action OnShapesAllUsed;
    public event Action OnNewSetGenerated;
    public event Action OnNoValidPlacement;
    public int ButtonCount => OfferCount;
    public bool IsShapeAvailable(int i) => assignedShapes != null && used != null &&
        i >= 0 && i < assignedShapes.Length && !used[i] && assignedShapes[i] != null;

    public ShapeInstanceData GetShape(int i) => IsShapeAvailable(i) ? assignedShapes[i] : null;
    public void SetField(Field3D value) { field = value; }
    public void SetActiveFigureController(ActiveFigureController value) { activeFigureController = value; }

    private void Awake()
    {
        for (int i=0;i<BalanceRules.Catalog.Count;i++)
        {
            var piece=BalanceRules.Catalog[i];
            var cells=piece.Cells;
            catalog.Add(new ShapeInstanceData { shapeName=piece.Name,category="Challenge",
                offer=new ShapeOffer(i,cells,0),baseCells=cells });
        }
    }

    private IEnumerator Start()
    {
        // GameManager binds callbacks and references in Start before the first set.
        yield return null;
        if (field == null) field = FindObjectOfType<Field3D>();
        if (activeFigureController == null) activeFigureController = FindObjectOfType<ActiveFigureController>();
        GenerateThreeShapes(field != null ? field.CountFreeCells() : 512);
    }

    private ShapeInstanceData FromOffer(ShapeOffer offer)
    {
        var source = catalog[offer.CatalogIndex];
        return new ShapeInstanceData { shapeName=source.shapeName,category=source.category,
            baseCells=source.baseCells,offer=offer };
    }

    public bool CanPlaceShapeAnywhere(int i) => IsShapeAvailable(i) && field != null &&
        assignedShapes[i].offer.TryFit(field.Model, out _, out _);

    public bool AnyRemainingShapeCanBePlaced()
    {
        if (assignedShapes == null) return false;
        for (int i = 0; i < assignedShapes.Length; i++)
            if (CanPlaceShapeAnywhere(i)) return true;
        return false;
    }

    public void GenerateThreeShapes(int freeCellCount)
    {
        const int count=OfferCount;
        // Keep the legacy signature, but low free-cell counts no longer bias
        // the draw towards tiny pieces or reduce the current difficulty.
        if (field==null) { Debug.LogError("Cannot generate offers without the board."); return; }
        var offers=BalanceRules.GenerateOffers(field.Model,PlacedShapes,random);
        assignedShapes = offers.ConvertAll(FromOffer).ToArray();
        used = new bool[count];
        for (int i = 0; i < count; i++)
        {
            assignedShapes[i].color = OfferColor(i);
            assignedShapes[i].onRotationChanged = () => OnOfferChanged?.Invoke();
        }
        OnNewSetGenerated?.Invoke();
        OnOfferChanged?.Invoke();
        if (field != null && !AnyRemainingShapeCanBePlaced()) OnNoValidPlacement?.Invoke();
    }

    public static Color OfferColor(int index) => index % 3 == 0 ? new Color(0.12f,0.68f,0.57f) :
        index % 3 == 1 ? new Color(0.55f,0.43f,0.88f) : new Color(1f,0.61f,0.28f);

    public bool UseShape(int index)
    {
        if (!IsShapeAvailable(index)) return false;
        used[index] = true;
        PlacedShapes++;
        bool allUsed = Array.TrueForAll(used, value => value);
        if (allUsed)
        {
            OnShapesAllUsed?.Invoke();
            GenerateThreeShapes(field != null ? field.CountFreeCells() : 512);
            return true;
        }
        OnOfferChanged?.Invoke();
        if (field != null && !AnyRemainingShapeCanBePlaced()) OnNoValidPlacement?.Invoke();
        return false;
    }

    public GameObject SpawnRealShape(int index, Vector3 spawnPosition)
    {
        if (field == null || !CanPlaceShapeAnywhere(index) || activeFigureController == null ||
            activeFigureController.HasActiveFigure) return null;
        var placer = FindObjectOfType<ShapePlacer>();
        if (placer != null && placer.IsResolving) return null;
        var data = assignedShapes[index];
        var instance=CreateShapeObject(data);
        instance.transform.SetPositionAndRotation(spawnPosition,field.transform.rotation*data.GetRotation());
        instance.SetActive(true);
        activeFigureController.SetField(field);
        activeFigureController.SetActiveFigure(instance,data);
        return instance;
    }

    private static GameObject CreateShapeObject(ShapeInstanceData data)
    {
        var instance=new GameObject(data.shapeName ?? "Figure");
        instance.SetActive(false);
        // Assign geometry before Awake so the runtime mesh, ghost, rules and
        // card all derive from the very same cell array.
        var shape=instance.AddComponent<FigureGridShape>();
        shape.cells=Array.ConvertAll(data.baseCells,c => new Vector3Int(c.x,c.y,c.z));
        return instance;
    }

}
