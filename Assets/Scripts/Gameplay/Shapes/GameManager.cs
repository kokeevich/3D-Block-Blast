using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private BalancedShapeSelector shapeSelector;
    [SerializeField] private ActiveFigureController activeFigureController;
    [SerializeField] private Field3D field;
    [SerializeField] private ShapePlacer shapePlacer;

    [Header("Spawn Buttons")]
    [SerializeField] private ShapeSpawnButton[] spawnButtons; // length 3

    [Header("Score")]
    [SerializeField] private Text scoreText;
    [SerializeField] private Text bestScoreText;
    [SerializeField] private int cellsPerBlock = 1;

    [Header("Game Over")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Text gameOverScoreText;
    [SerializeField] private Button restartButton;

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

        var controls = GetComponent<MobilePlacementControls>();
        if (controls == null) controls = gameObject.AddComponent<MobilePlacementControls>();
        controls.Initialize(activeFigureController, shapePlacer, this);

        // Привязка кнопок спавна
        AutoFindSpawnButtons();
        BindSpawnButtons();

        // UI
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (restartButton != null) restartButton.onClick.AddListener(RestartGame);
        UpdateScoreUI();
    }

    void AutoFindSpawnButtons()
    {
        // Если кнопки уже привязаны — оставляем
        if (spawnButtons != null && spawnButtons.Length >= 3) return;

        // Поищем на сцене все ShapeSpawnButton и отсортируем по buttonIndex
        var all = FindObjectsOfType<ShapeSpawnButton>();
        if (all == null || all.Length == 0) return;

        System.Array.Sort(all, (a, b) => a.buttonIndex.CompareTo(b.buttonIndex));
        spawnButtons = all;
    }

    void BindSpawnButtons()
    {
        if (spawnButtons == null) return;
        for (int i = 0; i < spawnButtons.Length; i++)
        {
            int idx = i;
            if (spawnButtons[i] == null) continue;
            spawnButtons[i].shapeSelector = shapeSelector;
            spawnButtons[i].buttonIndex = idx;
        }
    }

    public void HandleShapePlaced(int cellsCount, BoardModel.ClearResult clearResult)
    {
        // Placement + lines + simultaneous-line combo + rare full-plane bonus.
        int gained = clearResult.Score(cellsCount, cellsPerBlock);
        score += gained;
        Statistics.RecordPlacement(clearResult,Field3D.Size*Field3D.Size*Field3D.Size-field.CountFreeCells());
        UpdateScoreUI();

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
        UpdateScoreUI();

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            if (gameOverScoreText != null)
                gameOverScoreText.text = $"Счёт: {score}\nРекорд: {bestScore}";
        }
    }

    void UpdateScoreUI()
    {
        if (scoreText != null) scoreText.text = score.ToString();
        if (bestScoreText != null) bestScoreText.text = bestScore.ToString();
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
