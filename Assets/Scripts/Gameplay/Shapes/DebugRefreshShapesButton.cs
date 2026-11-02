// DebugRefreshShapesButton.cs
using UnityEngine;

/// <summary>
/// ¬ременна€ кнопка дл€ обновлени€ фигур (только дл€ разработки!)
/// </summary>
public class DebugRefreshShapesButton : MonoBehaviour
{
    [Tooltip("—сылка на BalancedShapeSelector")]
    public BalancedShapeSelector shapeSelector;

    [Tooltip("¬ременно: количество свободных клеток (в реальном проекте Ч брать из GridSystem)")]
    public int freeCellCount = 20;

    // Ётот метод можно подключить к UI-кнопке через OnClick()
    public void RefreshShapes()
    {
        if (shapeSelector == null)
        {
            Debug.LogError("BalancedShapeSelector не назначен!");
            return;
        }

        shapeSelector.GenerateThreeShapes(freeCellCount);
        Debug.Log($"[DEBUG] ‘игуры обновлены. —вободных клеток: {freeCellCount}");
    }
}