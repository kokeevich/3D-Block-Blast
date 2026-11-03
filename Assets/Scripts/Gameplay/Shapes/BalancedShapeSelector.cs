using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BalancedShapeSelector : MonoBehaviour
{
    [SerializeField] private ActiveFigureController activeFigureController;
    [SerializeField] private Field3D field;
    public Transform[] spawnButtons;

    private readonly List<ShapeInstanceData> catalog = new List<ShapeInstanceData>();
    private ShapeInstanceData[] assignedShapes;
    private bool[] used;
    private readonly System.Random random = new System.Random();
    public int PlacedShapes { get; private set; }
    public bool ShowLegacyPreviews { get; set; } = true;
    public event Action OnOfferChanged;
    public event Action OnShapesAllUsed;
    public event Action OnNewSetGenerated;
    public event Action OnNoValidPlacement;
    public int ButtonCount => assignedShapes?.Length ?? spawnButtons?.Length ?? 0;
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
        int count = Mathf.Min(3, spawnButtons?.Length ?? 0);
        if (count!=3 || catalog.Count==0)
        { Debug.LogError("Three offer slots and a valid gameplay catalog are required."); return; }
        // Keep the legacy signature, but low free-cell counts no longer bias
        // the draw towards tiny pieces or reduce the current difficulty.
        if (field==null) { Debug.LogError("Cannot generate offers without the board."); return; }
        var offers=BalanceRules.GenerateOffers(field.Model,PlacedShapes,random);
        assignedShapes = offers.ConvertAll(FromOffer).ToArray();
        used = new bool[count];
        for (int i = 0; i < count; i++)
        {
            int slot = i;
            assignedShapes[i].color = OfferColor(i);
            assignedShapes[i].onRotationChanged = () => RefreshPreview(slot);
            RefreshPreview(i);
        }
        OnNewSetGenerated?.Invoke();
        if (field != null && !AnyRemainingShapeCanBePlaced()) OnNoValidPlacement?.Invoke();
    }

    public static Color OfferColor(int index) => index % 3 == 0 ? new Color(0.12f,0.68f,0.57f) :
        index % 3 == 1 ? new Color(0.55f,0.43f,0.88f) : new Color(1f,0.61f,0.28f);

    public bool UseShape(int index)
    {
        if (!IsShapeAvailable(index)) return false;
        used[index] = true;
        PlacedShapes++;
        if (spawnButtons[index] != null)
            foreach (Transform child in spawnButtons[index]) Destroy(child.gameObject);
        bool allUsed = Array.TrueForAll(used, value => value);
        if (allUsed)
        {
            OnShapesAllUsed?.Invoke();
            GenerateThreeShapes(field != null ? field.CountFreeCells() : 512);
            return true;
        }
        if (field != null && !AnyRemainingShapeCanBePlaced()) OnNoValidPlacement?.Invoke();
        return false;
    }

    public GameObject SpawnRealShape(int index, Vector3 spawnPosition)
    {
        if (!IsShapeAvailable(index) || field == null || activeFigureController == null ||
            activeFigureController.HasActiveFigure) return null;
        var placer = FindObjectOfType<ShapePlacer>();
        if (placer != null && placer.IsResolving) return null;
        var data = assignedShapes[index];
        if (!data.offer.TryFit(field.Model, out var fitted, out var anchor)) return null;
        // If the board changed, display and spawn the same newly fitting orientation.
        data.offer = fitted;
        RefreshPreview(index);
        var instance=CreateShapeObject(data);
        instance.transform.SetPositionAndRotation(spawnPosition,field.transform.rotation*data.GetRotation());
        instance.SetActive(true);
        activeFigureController.SetField(field);
        activeFigureController.SetActiveFigure(instance, new Vector3Int(anchor.x, anchor.y, anchor.z), data);
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

    void RefreshPreview(int i)
    {
        OnOfferChanged?.Invoke();
        if (!ShowLegacyPreviews) return;
        var data = assignedShapes[i];
        if (data == null || spawnButtons[i] == null) return;

        foreach (Transform child in spawnButtons[i])
            Destroy(child.gameObject);

        GameObject preview=CreateShapeObject(data);
        preview.transform.SetParent(spawnButtons[i],false);
        preview.SetActive(true);
        preview.transform.localPosition = Vector3.zero;
        preview.transform.localRotation = data.GetRotation();

        float scale = 0.25f;
        switch (data.category)
        {
            case "Small":  scale = 0.30f; break;
            case "Medium": scale = 0.30f; break;
            case "Large":  scale = 0.25f; break;
            default:       scale = 0.20f; break;
        }
        preview.transform.localScale = Vector3.one * scale;

        DisableComponentsForPreview(preview);

        // Приглушим прозрачность, чтобы превью не выглядело как игровой объект
        SetPreviewAlpha(preview, 0.85f);
    }

    void SetPreviewAlpha(GameObject obj, float alpha)
    {
        var rends = obj.GetComponentsInChildren<MeshRenderer>(true);
        foreach (var r in rends)
        {
            if (r.material == null) continue;
            // Прозрачный материал — используем стандартный режим Fade
            var mat = r.material;
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f); // 0 Opaque, 1 Transparent
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = 3000;
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            if (mat.HasProperty("_BaseColor"))
            {
                var c = mat.GetColor("_BaseColor");
                c.a = alpha;
                mat.SetColor("_BaseColor", c);
            }
            if (mat.HasProperty("_Color"))
            {
                var c = mat.GetColor("_Color");
                c.a = alpha;
                mat.SetColor("_Color", c);
            }
        }
    }

    void DisableComponentsForPreview(GameObject obj)
    {
        var collider = obj.GetComponent<Collider>();
        if (collider) collider.enabled = false;

        var rb = obj.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;
    }

}
