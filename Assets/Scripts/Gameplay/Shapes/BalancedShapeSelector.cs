using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BalancedShapeSelector : MonoBehaviour
{
    [SerializeField] private ActiveFigureController activeFigureController;
    [SerializeField] private Field3D field;
    public ShapeCategory[] categories;
    public Transform[] spawnButtons;

    private readonly List<ShapeInstanceData> catalog = new List<ShapeInstanceData>();
    private ShapeInstanceData[] assignedShapes;
    private bool[] used;
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
        if (categories == null || categories.Length == 0)
            categories = new[] { new ShapeCategory { name = "Small" },
                new ShapeCategory { name = "Medium" }, new ShapeCategory { name = "Large" } };
        foreach (var category in categories)
        {
            category.prefabs = Resources.LoadAll<GameObject>("UI/Prefabs/" + category.name);
            foreach (var prefab in category.prefabs)
            {
                var shape = prefab.GetComponent<FigureGridShape>();
                if (shape == null) continue;
                var cells = Array.ConvertAll(shape.GetNormalizedCells(), c => new BoardModel.Cell(c.x, c.y, c.z));
                if (cells.Length == 0) continue;
                catalog.Add(new ShapeInstanceData { prefab = prefab, category = category.name,
                    offer = new ShapeOffer(catalog.Count, cells, 0), baseCells = cells });
            }
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
        return new ShapeInstanceData { prefab = source.prefab, category = source.category,
            baseCells = source.baseCells, offer = offer };
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
        if (count == 0 || catalog.Count == 0)
        { Debug.LogError("No spawn slots or valid shape prefabs configured."); return; }
        var offers = new List<ShapeOffer>();
        for (int i = 0; i < count; i++)
        {
            // Weight the actual prefab, then keep that exact prefab and rotation.
            var candidates = catalog.FindAll(d => d.baseCells.Length <= freeCellCount);
            if (candidates.Count == 0) candidates.AddRange(catalog);
            float total = 0;
            foreach (var c in candidates) total += Weight(c.category);
            float choice = UnityEngine.Random.value * total;
            var selected = candidates[candidates.Count - 1];
            foreach (var c in candidates)
            { choice -= Weight(c.category); if (choice <= 0) { selected = c; break; } }
            var offer = new ShapeOffer(selected.offer.CatalogIndex, selected.baseCells,
                UnityEngine.Random.Range(0, ShapeRules.Orientations.Count));
            if (field != null && offer.TryFit(field.Model, out var fitted, out _)) offer = fitted;
            offers.Add(offer);
        }
        if (field != null)
            OfferRules.EnsurePlayable(field.Model, offers, catalog.ConvertAll(d => d.offer));
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

    public static Color OfferColor(int index) => index % 3 == 0 ? new Color(0.22f,0.87f,0.78f) :
        index % 3 == 1 ? new Color(0.65f,0.56f,1f) : new Color(1f,0.71f,0.34f);

    private float Weight(string category)
    {
        foreach (var c in categories)
            if (c.name == category) return Mathf.Max(0.01f, c.baseWeight) / Mathf.Max(1, c.prefabs.Length);
        return 1;
    }

    public bool UseShape(int index)
    {
        if (!IsShapeAvailable(index)) return false;
        used[index] = true;
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
        var instance = Instantiate(data.prefab, spawnPosition, field.transform.rotation * data.GetRotation());
        activeFigureController.SetField(field);
        activeFigureController.SetActiveFigure(instance, new Vector3Int(anchor.x, anchor.y, anchor.z), data);
        return instance;
    }

    void RefreshPreview(int i)
    {
        OnOfferChanged?.Invoke();
        if (!ShowLegacyPreviews) return;
        var data = assignedShapes[i];
        if (data?.prefab == null || spawnButtons[i] == null) return;

        foreach (Transform child in spawnButtons[i])
            Destroy(child.gameObject);

        GameObject preview = Instantiate(data.prefab, spawnButtons[i]);
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
