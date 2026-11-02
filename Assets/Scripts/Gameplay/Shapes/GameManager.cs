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

    private int score;
    private int bestScore;
    private bool isGameOver;

    const string BEST_SCORE_KEY = "blockblast.best";

    void Start()
    {
        bestScore = PlayerPrefs.GetInt(BEST_SCORE_KEY, 0);

        if (field == null) field = FindObjectOfType<Field3D>();
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

        if (shapePlacer != null)
        {
            if (field != null) shapePlacer.SetField(field);
            if (activeFigureController != null) shapePlacer.SetFigureController(activeFigureController);
            shapePlacer.OnShapePlaced += HandleShapePlaced;
        }

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

    public void HandleShapePlaced(int cellsCount, int planesCleared, int comboBonus)
    {
        // Очки: 1 за блок + 10 за каждую очищенную плоскость + комбо-бонус
        int gained = cellsCount * cellsPerBlock + planesCleared * 10 + comboBonus;
        score += gained;
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

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public int Score => score;
    public int BestScore => bestScore;
    public bool IsGameOver => isGameOver;
}
