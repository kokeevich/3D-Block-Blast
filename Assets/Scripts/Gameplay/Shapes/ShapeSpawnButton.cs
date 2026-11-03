using UnityEngine;

public class ShapeSpawnButton : MonoBehaviour
{
    [Tooltip("Индекс кнопки: 0, 1 или 2")]
    public int buttonIndex = 0;

    [Tooltip("Точка в мире, где появится фигура (может быть null, тогда используется offset от камеры)")]
    public Transform spawnPoint;

    [Tooltip("Если spawnPoint не задан, фигура появится на этом расстоянии от камеры")]
    public float spawnDistance = 3f;

    public BalancedShapeSelector shapeSelector;
    public GameManager gameManager;

    /// <summary>
    /// Этот метод вызывается из OnClick() в UI-кнопке.
    /// </summary>
    public void SpawnSelectedShape()
    {
        if (shapeSelector == null)
        {
            shapeSelector = FindObjectOfType<BalancedShapeSelector>();
            if (shapeSelector == null)
            {
                Debug.LogError("BalancedShapeSelector не найден на сцене");
                return;
            }
        }

        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();

        if (gameManager != null && !gameManager.CanSelectShape) return;

        if (!shapeSelector.IsShapeAvailable(buttonIndex))
        {
            Debug.Log("ShapeSpawnButton: фигура уже использована или недоступна.");
            return;
        }

        if (!shapeSelector.CanPlaceShapeAnywhere(buttonIndex))
        {
            Debug.Log("ShapeSpawnButton: для этой фигуры нет места на поле.");
            return;
        }

        // Проверим, нет ли уже активной фигуры
        var activeCtrl = FindObjectOfType<ActiveFigureController>();
        if (activeCtrl != null && activeCtrl.HasActiveFigure)
        {
            Debug.Log("ShapeSpawnButton: уже есть активная фигура. Сначала разместите её.");
            return;
        }

        // Позиция спавна
        Vector3 spawnPosition;
        if (spawnPoint != null)
        {
            spawnPosition = spawnPoint.position;
        }
        else
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                spawnPosition = cam.transform.position + cam.transform.forward * spawnDistance;
                spawnPosition.y = 1f;
            }
            else
            {
                spawnPosition = Vector3.up;
            }
        }

        var instance = shapeSelector.SpawnRealShape(buttonIndex, spawnPosition);
        if (instance != null && gameManager != null) gameManager.NotifyShapeSpawned(buttonIndex);
    }
}
