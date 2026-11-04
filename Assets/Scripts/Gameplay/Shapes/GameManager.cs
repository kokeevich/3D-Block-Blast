using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private BalancedShapeSelector shapeSelector;
    [SerializeField] private ActiveFigureController activeFigureController;
    [SerializeField] private Field3D field;
    [SerializeField] private ShapePlacer shapePlacer;

    [Header("Score")]
    [SerializeField] private int cellsPerBlock=1;

    private FieldRotator fieldRotator;
    public RunStatistics Statistics { get; } = new RunStatistics();
    private int score;
    private int bestScore;
    private bool isGameOver;

    const string BEST_SCORE_KEY = "blockblast.best";

    void Start()
    {
        bestScore = PlayerPrefs.GetInt(BEST_SCORE_KEY, 0);

        if (field == null) field = FindObjectOfType<Field3D>();
        if (field != null) fieldRotator=field.GetComponent<FieldRotator>();
        if (activeFigureController == null) activeFigureController = FindObjectOfType<ActiveFigureController>();
        if (shapeSelector == null) shapeSelector = FindObjectOfType<BalancedShapeSelector>();
        if (shapePlacer == null) shapePlacer = FindObjectOfType<ShapePlacer>();

        // Свяжем компоненты
        if (shapeSelector != null)
        {
            if (field != null) shapeSelector.SetField(field);
            if (activeFigureController != null) shapeSelector.SetActiveFigureController(activeFigureController);
            shapeSelector.OnNoValidPlacement += HandleGameOver;
        }

        if (activeFigureController != null && field != null)
        {
            activeFigureController.SetField(field);
        }

        if (shapePlacer != null)
        {
            if (field != null) shapePlacer.SetField(field);
            if (activeFigureController != null) shapePlacer.SetFigureController(activeFigureController);
            shapePlacer.OnShapePlaced += HandleShapePlaced;
        }

        if (field!=null && field.GetComponent<BoardVisibility>()==null)
            field.gameObject.AddComponent<BoardVisibility>();
        var controls = GetComponent<MobilePlacementControls>();
        if (controls == null) controls = gameObject.AddComponent<MobilePlacementControls>();
        controls.Initialize(activeFigureController, shapePlacer, this);
    }

    public void HandleShapePlaced(int cellsCount, BoardModel.ClearResult clearResult)
    {
        // Placement + lines + simultaneous-line combo + rare full-plane bonus.
        int gained = clearResult.Score(cellsCount, cellsPerBlock);
        score += gained;
        Statistics.RecordPlacement(clearResult,Field3D.Size*Field3D.Size*Field3D.Size-field.CountFreeCells());

        // Сообщим селектору, что фигура использована
        if (lastSpawnedButtonIndex >= 0 && shapeSelector != null)
        {
            shapeSelector.UseShape(lastSpawnedButtonIndex);
        }
        lastSpawnedButtonIndex = -1;

        // Активная фигура уже была LockActiveFigure в ShapePlacer, она больше не нужна
        if (activeFigureController != null)
            activeFigureController.ClearActiveFigure();
    }

    private int lastSpawnedButtonIndex = -1;

    /// <summary>
    /// Вызывается из ShapeSpawnButton. После этого — текущая кнопка "заблокирована" пока UseShape не сработает.
    /// </summary>
    public void NotifyShapeSpawned(int buttonIndex)
    {
        lastSpawnedButtonIndex = buttonIndex;
    }

    void HandleGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        if (score > bestScore)
        {
            bestScore = score;
            PlayerPrefs.SetInt(BEST_SCORE_KEY, bestScore);
        }
    }

    public bool TrySelectOffer(int index)
    {
        if (!CanSelectShape || shapeSelector == null || activeFigureController == null ||
            !shapeSelector.CanPlaceShapeAnywhere(index)) return false;
        if (activeFigureController.HasActiveFigure)
        {
            if (lastSpawnedButtonIndex == index) return true;
            activeFigureController.ClearActiveFigure();
            lastSpawnedButtonIndex = -1;
        }
        var instance = shapeSelector.SpawnRealShape(index, field.transform.position);
        if (instance == null) return false;
        lastSpawnedButtonIndex = index;
        return true;
    }

    public void CancelSelection()
    {
        if (!CanSelectShape) return;
        if (activeFigureController != null) activeFigureController.ClearActiveFigure();
        lastSpawnedButtonIndex = -1;
    }

    private void OnDestroy()
    {
        if (shapeSelector != null) shapeSelector.OnNoValidPlacement -= HandleGameOver;
        if (shapePlacer != null) shapePlacer.OnShapePlaced -= HandleShapePlaced;
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public int Score => score;
    public int BestScore => bestScore;
    public bool IsGameOver => isGameOver;
    public bool CanSelectShape => !isGameOver && (shapePlacer == null || !shapePlacer.IsResolving) &&
        (fieldRotator==null || !fieldRotator.IsRotating);
}
