using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class BalancedShapeSelector : MonoBehaviour
{
    [SerializeField] private ActiveFigureController activeFigureController;
    [SerializeField] private Field3D field;

    [Header("Настройки категорий")]
    public ShapeCategory[] categories;

    [Header("Ссылки")]
    public Transform[] spawnButtons; // Должно быть ровно 3 (или >=3)

    private Dictionary<string, GameObject[]> loadedPrefabs;
    private ShapeInstanceData[] assignedShapes;
    private bool[] used; // какие фигуры уже размещены

    public event Action OnShapesAllUsed;
    public event Action OnNewSetGenerated;
    public event Action OnNoValidPlacement; // game over

    public int ButtonCount => spawnButtons?.Length ?? 0;
    public bool IsShapeAvailable(int idx) => assignedShapes != null && idx >= 0 && idx < assignedShapes.Length && !used[idx];

    /// <summary>
    /// Можно ли вообще разместить фигуру (с учётом поворота) на текущем поле.
    /// </summary>
    public bool CanPlaceShapeAnywhere(int idx)
    {
        if (!IsShapeAvailable(idx)) return false;
        if (field == null) return true;
        var data = assignedShapes[idx];
        if (data?.prefab == null) return false;
        var shape = data.prefab.GetComponent<FigureGridShape>();
        if (shape == null || shape.GetNormalizedCells().Length == 0) return false;
        Vector3Int[] rotated = shape.GetRotatedCells(Quaternion.Euler(data.eulerAngles));
        return field.CanPlaceAnywhere(rotated);
    }

    /// <summary>
    /// Можно ли разместить хотя бы одну из текущих неиспользованных фигур.
    /// </summary>
    public bool AnyRemainingShapeCanBePlaced()
    {
        if (assignedShapes == null) return false;
        for (int i = 0; i < assignedShapes.Length; i++)
        {
            if (used != null && i < used.Length && used[i]) continue;
            if (CanPlaceShapeAnywhere(i)) return true;
        }
        return false;
    }

    public ShapeInstanceData GetShape(int idx)
    {
        if (assignedShapes == null) return null;
        if (idx < 0 || idx >= assignedShapes.Length) return null;
        return assignedShapes[idx];
    }

    void Awake()
    {
        LoadPrefabs();
        ValidateCategories();
    }

    void Start()
    {
        // Сгенерируем первую тройку, если Field уже инициализирован
        if (field == null && activeFigureController != null)
        {
            var t = activeFigureController.FieldTransform;
            if (t != null) field = t.GetComponent<Field3D>();
        }
        if (field != null)
            GenerateThreeShapes(field.CountFreeCells());
        else
            GenerateThreeShapes(Field3D.Size * Field3D.Size * Field3D.Size);
    }

    public void SetField(Field3D f) { field = f; }
    public void SetActiveFigureController(ActiveFigureController c) { activeFigureController = c; }

    void LoadPrefabs()
    {
        loadedPrefabs = new Dictionary<string, GameObject[]>();
        string basePath = "UI/Prefabs/";
        string[] sizes = { "Small", "Medium", "Large" };

        foreach (var size in sizes)
        {
            var prefabs = Resources.LoadAll<GameObject>($"{basePath}{size}");
            loadedPrefabs[size] = prefabs;
            Debug.Log($"Загружено {prefabs.Length} префабов для {size}");
        }
    }

    void ValidateCategories()
    {
        foreach (var cat in categories)
        {
            if (loadedPrefabs.TryGetValue(cat.name, out var prefabs))
            {
                cat.prefabs = prefabs;
                if (prefabs.Length == 0)
                    Debug.LogWarning($"Категория {cat.name} найдена, но префабов нет!");
            }
            else
            {
                Debug.LogError($"Категория {cat.name} не найдена в Resources!");
            }
        }
    }

    public void GenerateThreeShapes(int freeCellCount)
    {
        int buttonCount = Mathf.Min(3, spawnButtons?.Length ?? 0);
        if (buttonCount == 0)
        {
            Debug.LogError("spawnButtons не назначен в инспекторе!");
            return;
        }

        assignedShapes = new ShapeInstanceData[buttonCount];
        used = new bool[buttonCount];

        var availableOptions = new List<(string name, float weight, GameObject[] prefabs)>();

        foreach (var cat in categories)
        {
            if (cat.prefabs == null || cat.prefabs.Length == 0) continue;
            if (freeCellCount < cat.minCellsNeeded) continue;

            float adjustedWeight = cat.baseWeight;
            if (cat.name == "Large" && freeCellCount < 10)
                adjustedWeight *= 0.3f;
            else if (cat.name == "Small" && freeCellCount > 15)
                adjustedWeight *= 0.7f;

            if (adjustedWeight > 0)
            {
                availableOptions.Add((cat.name, adjustedWeight, cat.prefabs));
            }
        }

        if (availableOptions.Count == 0)
        {
            Debug.LogWarning("Нет доступных категорий при freeCellCount = " + freeCellCount);
            foreach (var cat in categories)
            {
                if (cat.prefabs != null && cat.prefabs.Length > 0)
                {
                    availableOptions.Add((cat.name, 1f, cat.prefabs));
                }
            }
        }

        if (availableOptions.Count == 0)
        {
            Debug.LogError("Полностью пусто: ни одной фигуры ни в одной категории!");
            return;
        }

        float[] angles = { 0f, 90f, 180f, 270f };

        for (int i = 0; i < buttonCount; i++)
        {
            // Сначала пытаемся подобрать фигуру, которую можно разместить
            string pickedName = PickCategoryThatFits(availableOptions, freeCellCount, maxTries: 12) ?? PickWeightedCategory(availableOptions);
            var prefabs = GetPrefabsForCategory(pickedName);
            GameObject randomPrefab = prefabs[UnityEngine.Random.Range(0, prefabs.Length)];

            Vector3 rotation = new Vector3(
                angles[UnityEngine.Random.Range(0, angles.Length)],
                angles[UnityEngine.Random.Range(0, angles.Length)],
                angles[UnityEngine.Random.Range(0, angles.Length)]);

            assignedShapes[i] = new ShapeInstanceData
            {
                prefab = randomPrefab,
                eulerAngles = rotation,
                category = pickedName
            };

            Debug.Log($"[ASSIGN] Кнопка {i}: {randomPrefab.name} (категория {pickedName})");

            if (spawnButtons[i] != null)
            {
                RefreshPreview(i);
            }
            else
            {
                Debug.LogWarning($"spawnButtons[{i}] не назначен!");
            }
        }

        Debug.Log($"Сгенерировано {buttonCount} фигур с поворотами");
        OnNewSetGenerated?.Invoke();
    }

    /// <summary>
    /// Ищет категорию, в которой есть фигура, подходящая под свободные клетки.
    /// </summary>
    string PickCategoryThatFits(
        List<(string name, float weight, GameObject[] prefabs)> options,
        int freeCellCount,
        int maxTries)
    {
        if (field == null) return null;

        // Соберём взвешенный список и попробуем несколько раз
        for (int t = 0; t < maxTries; t++)
        {
            string name = PickWeightedCategory(options);
            var prefabs = GetPrefabsForCategory(name);
            if (prefabs == null || prefabs.Length == 0) continue;

            // Проверим, есть ли в этой категории фигура, которую можно разместить
            // Простая эвристика: длина массива cells <= freeCellCount
            for (int k = 0; k < 6; k++)
            {
                var p = prefabs[UnityEngine.Random.Range(0, prefabs.Length)];
                var shape = p.GetComponent<FigureGridShape>();
                if (shape == null) continue;
                Vector3Int[] normalizedCells = shape.GetNormalizedCells();
                if (normalizedCells.Length == 0) continue;
                if (normalizedCells.Length > freeCellCount) continue;
                if (field.CanPlaceAnywhere(normalizedCells)) return name;
            }
        }
        return null;
    }

    void RefreshPreview(int i)
    {
        var data = assignedShapes[i];
        if (data?.prefab == null || spawnButtons[i] == null) return;

        foreach (Transform child in spawnButtons[i])
            Destroy(child.gameObject);

        GameObject preview = Instantiate(data.prefab, spawnButtons[i]);
        preview.transform.localPosition = Vector3.zero;
        preview.transform.localRotation = Quaternion.Euler(data.eulerAngles);

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

    string PickWeightedCategory(List<(string name, float weight, GameObject[] prefabs)> options)
    {
        float totalWeight = 0f;
        foreach (var opt in options)
            totalWeight += opt.weight;

        if (totalWeight <= 0) return options[0].name;

        float rand = UnityEngine.Random.Range(0f, totalWeight);
        foreach (var opt in options)
        {
            if (rand < opt.weight)
                return opt.name;
            rand -= opt.weight;
        }
        return options[0].name;
    }

    GameObject[] GetPrefabsForCategory(string name)
    {
        foreach (var cat in categories)
            if (cat.name == name)
                return cat.prefabs;
        return new GameObject[0];
    }

    void DisableComponentsForPreview(GameObject obj)
    {
        var collider = obj.GetComponent<Collider>();
        if (collider) collider.enabled = false;

        var rb = obj.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;
    }

    /// <summary>
    /// Помечает фигуру как использованную и, если все использованы, генерирует новые.
    /// Возвращает true, если был сгенерирован новый набор.
    /// </summary>
    public bool UseShape(int buttonIndex)
    {
        if (used == null || buttonIndex < 0 || buttonIndex >= used.Length) return false;
        used[buttonIndex] = true;

        // Спрятать превью
        if (spawnButtons != null && buttonIndex < spawnButtons.Length && spawnButtons[buttonIndex] != null)
        {
            foreach (Transform child in spawnButtons[buttonIndex])
                Destroy(child.gameObject);
        }

        // Проверим, все ли использованы
        bool allUsed = true;
        for (int i = 0; i < used.Length; i++)
        {
            if (!used[i]) { allUsed = false; break; }
        }

        if (allUsed)
        {
            OnShapesAllUsed?.Invoke();
            // Проверим Game Over: есть ли место хотя бы для одной из новых фигур
            int free = field != null ? field.CountFreeCells() : 0;
            GenerateThreeShapes(free);

            if (field != null && assignedShapes != null)
            {
                if (!AnyRemainingShapeCanBePlaced())
                {
                    Debug.Log("[BalancedShapeSelector] Game Over — нет места ни для одной новой фигуры.");
                    OnNoValidPlacement?.Invoke();
                }
            }
            return true;
        }
        else
        {
            // Не все использованы — проверим Game Over для оставшихся
            if (field != null && !AnyRemainingShapeCanBePlaced())
            {
                Debug.Log("[BalancedShapeSelector] Game Over — нет места для оставшихся фигур.");
                OnNoValidPlacement?.Invoke();
            }
        }
        return false;
    }

    public GameObject SpawnRealShape(int buttonIndex, Vector3 spawnPosition)
    {
        Debug.Log($"[SPAWN] Запрос на спавн: buttonIndex = {buttonIndex}");

        if (assignedShapes == null)
        {
            Debug.LogWarning("assignedShapes == null. Генерируем...");
            int free = field != null ? field.CountFreeCells() : 20;
            GenerateThreeShapes(free);
        }

        if (buttonIndex < 0 || buttonIndex >= assignedShapes.Length)
        {
            Debug.LogError($"Неверный индекс: {buttonIndex} (доступно: {assignedShapes?.Length})");
            return null;
        }

        var shapeData = assignedShapes[buttonIndex];
        if (shapeData?.prefab == null)
        {
            Debug.LogError($"[SPAWN] Фигура для кнопки {buttonIndex} — NULL!");
            return null;
        }

        if (used != null && buttonIndex < used.Length && used[buttonIndex])
        {
            Debug.LogWarning($"[SPAWN] Фигура {buttonIndex} уже использована.");
            return null;
        }

        Quaternion shapeRotation = Quaternion.Euler(shapeData.eulerAngles);
        Quaternion worldRotation = field != null
            ? field.transform.rotation * shapeRotation
            : shapeRotation;

        GameObject instance = Instantiate(
            shapeData.prefab,
            spawnPosition,
            worldRotation);

        if (activeFigureController != null)
        {
            // Стартовая позиция — если есть место, поставим по центру, иначе попробуем найти
            Vector3Int startGridPos = Vector3Int.zero;
            var shape = instance.GetComponent<FigureGridShape>();
            if (shape != null && shape.GetNormalizedCells().Length > 0 && field != null)
            {
                // Получаем клетки с учётом поворота
                Vector3Int[] rotatedCells = shape.GetRotatedCells(shapeRotation);
                if (!field.CanPlaceShape(startGridPos, rotatedCells))
                {
                    if (!field.TryFindPlacement(rotatedCells, out startGridPos))
                    {
                        // Совсем нет места — разместим как есть
                        Debug.LogWarning("[SPAWN] Нет валидной стартовой позиции для фигуры.");
                    }
                }
            }
            activeFigureController.SetActiveFigure(instance, startGridPos);
        }

        StartCoroutine(PlaySpawnEffect(instance));
        return instance;
    }

    IEnumerator PlaySpawnEffect(GameObject obj)
    {
        if (obj == null) yield break;

        Vector3 originalScale = obj.transform.localScale;
        obj.transform.localScale = Vector3.zero;

        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (obj == null) yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = Mathf.SmoothStep(0f, 1f, t);
            obj.transform.localScale = originalScale * t;
            yield return null;
        }

        if (obj != null)
            obj.transform.localScale = originalScale;
    }
}
