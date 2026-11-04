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
    private readonly SessionFlow session=new SessionFlow();
    private GameSessionUI sessionUI;
    private float timeBeforePause=1;
    private bool ownsPause,initialized;
    public TutorialProgress Tutorial { get; } = new TutorialProgress();
    public int LastClearedLines { get; private set; }
    public float LastMoveAt { get; private set; } = -100;
    public float TutorialFinishedAt { get; private set; } = -100;
    public bool IsPaused => session.Paused;
    public bool Transitioning => session.Transitioning;
    public bool InputBlocked => !session.CanAcceptInput || Tutorial.IsIntro;
    public bool CanPlace => !InputBlocked && Tutorial.CanPlace;

    void Start()
    {
        initialized=true;
        Time.timeScale=1; AudioListener.pause=false; GamePreferences.ApplyAudio();
        bestScore=GamePreferences.BestScore;
        if (GameLaunch.ConsumeTutorialRequest() || !GamePreferences.TutorialCompleted) Tutorial.Start();

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
        sessionUI=GetComponent<GameSessionUI>();
        if (sessionUI==null) sessionUI=gameObject.AddComponent<GameSessionUI>();
        sessionUI.Initialize(this);
    }

    public void HandleShapePlaced(int cellsCount, BoardModel.ClearResult clearResult)
    {
        if (Transitioning) return;
        // Placement + lines + simultaneous-line combo + rare full-plane bonus.
        int gained = clearResult.Score(cellsCount, cellsPerBlock);
        score += gained;
        LastClearedLines=clearResult.LinesCleared; LastMoveAt=Time.unscaledTime;
        if (score>bestScore) { bestScore=score; GamePreferences.RecordBest(score); }
        NotifyTutorialAction(TutorialProgress.Action.PlaceShape);
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
        if (session.GameOver || Transitioning) return;
        session.EndGame(); ReleasePause(); PersistProgress();
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
        NotifyTutorialAction(TutorialProgress.Action.SelectShape);
        return true;
    }

    public void CancelSelection()
    {
        if (!CanSelectShape) return;
        if (activeFigureController != null) activeFigureController.ClearActiveFigure();
        lastSpawnedButtonIndex = -1;
    }

    public void BeginTutorial() { Tutorial.Begin(); }
    public void SkipTutorial(bool remember=true)
    {
        bool active=Tutorial.Active; Tutorial.Skip();
        if (remember && active) GamePreferences.CompleteTutorial();
    }
    public void NotifyTutorialAction(TutorialProgress.Action action)
    {
        if (InputBlocked) return;
        if (Tutorial.Signal(action))
        { TutorialFinishedAt=Time.unscaledTime; GamePreferences.CompleteTutorial(); }
    }
    public void PauseGame()
    {
        if (!session.TryPause()) return;
        timeBeforePause=Time.timeScale; ownsPause=true;
        Time.timeScale=0; AudioListener.pause=true; PersistProgress();
    }
    public void ResumeGame() { session.Resume(); ReleasePause(); }
    private void ReleasePause()
    {
        if (ownsPause) { Time.timeScale=timeBeforePause; ownsPause=false; }
        AudioListener.pause=false; GamePreferences.ApplyAudio();
    }
    private void PersistProgress()
    {
        int committed=score+(shapePlacer!=null ? shapePlacer.PendingScore(cellsPerBlock) : 0);
        bestScore=Mathf.Max(bestScore,committed); GamePreferences.RecordBest(bestScore); GamePreferences.Save();
    }
    private void Update()
    { if (Input.GetKeyDown(KeyCode.Escape) && !Transitioning && sessionUI!=null) sessionUI.HandleBack(); }
    private void OnApplicationPause(bool paused) { if (paused) { PauseGame(); PersistProgress(); } }
    private void OnApplicationFocus(bool focused)
    { if (!focused && !Application.isEditor) { PauseGame(); PersistProgress(); } }
    private void OnApplicationQuit() { PersistProgress(); }
    private void OnDestroy()
    {
        if (!initialized) return;
        if (shapeSelector != null) shapeSelector.OnNoValidPlacement -= HandleGameOver;
        if (shapePlacer != null) shapePlacer.OnShapePlaced -= HandleShapePlaced;
        PersistProgress(); ReleasePause();
    }
    public void RestartGame() { LoadScene("Game"); }
    public void ReturnToMenu() { LoadScene("MainMenu"); }
    private void LoadScene(string name)
    {
        if (!session.TryBeginTransition()) return;
        PersistProgress(); ReleasePause();
        Time.timeScale=1; SceneManager.LoadSceneAsync(name);
    }

    public int Score => score;
    public int BestScore => bestScore;
    public bool IsGameOver => session.GameOver;
    public bool CanSelectShape => !InputBlocked && Tutorial.CanSelect && (shapePlacer == null || !shapePlacer.IsResolving) &&
        (fieldRotator==null || !fieldRotator.IsRotating);
}
