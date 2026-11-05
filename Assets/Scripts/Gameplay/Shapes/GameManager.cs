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
    private bool ownsPause,initialized,practice,discardSave,restoring;
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
        GameFeedback.Prepare();
        GameAds.Get();
        Time.timeScale=1; AudioListener.pause=false; GamePreferences.ApplyAudio();
        bestScore=GamePreferences.BestScore;
        practice=GameLaunch.ConsumeTutorialRequest();
        if (practice || !GamePreferences.TutorialCompleted) Tutorial.Start();

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
        SaveSession();
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
        session.EndGame();
        if (!practice) { RunSaveStore.Clear(); if (!Tutorial.Active) GameAds.Get().RecordLoss(); }
        ReleasePause(); PersistProgress();
        if (!restoring) GameFeedback.Play(GameFeedback.Cue.GameOver);
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
        SaveSession();
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
    public void RestartGame()
    {
        if (Transitioning) return;
        discardSave=true; if (!practice) RunSaveStore.Clear();
        GameLaunch.RequestTutorial(practice); LoadScene("Game");
    }
    public void ReturnToMenu() { LoadScene("MainMenu"); }
    private void LoadScene(string name)
    {
        if (Transitioning) return;
        PersistProgress();
        if (!session.TryBeginTransition()) return;
        ReleasePause();
        Time.timeScale=1;
        if (IsGameOver && !practice && !Tutorial.Active) GameAds.Get().AfterLoss(() => SceneManager.LoadSceneAsync(name));
        else SceneManager.LoadSceneAsync(name);
    }

    public void SaveSession()
    {
        if (!initialized || practice || discardSave || restoring || IsGameOver || Transitioning ||
            field==null || shapeSelector==null || !shapeSelector.HasOffers) return;
        var pending=shapePlacer!=null ? shapePlacer.PendingClear : null;
        var snapshot=new RunSnapshot { score=score+(pending!=null ? shapePlacer.PendingScore(cellsPerBlock) : 0),
            moves=Statistics.Moves+(pending!=null ? 1 : 0), lines=Statistics.Lines+(pending!=null ? pending.LinesCleared : 0),
            peak=Statistics.PeakOccupied, consume=pending!=null ? lastSpawnedButtonIndex : -1,
            tutorial=(int)Tutorial.Current };
        shapeSelector.CaptureOffers(snapshot);
        var removed=pending!=null ? new System.Collections.Generic.HashSet<BoardModel.Cell>(pending.Cells) : null;
        var blocks=new System.Collections.Generic.List<RunSnapshot.Block>();
        for (int x=0;x<Field3D.Size;x++) for (int y=0;y<Field3D.Size;y++) for (int z=0;z<Field3D.Size;z++)
        {
            if (!field.IsOccupied(x,y,z) || (removed!=null && removed.Contains(new BoardModel.Cell(x,y,z)))) continue;
            Color32 c=field.CellColor(x,y,z);
            blocks.Add(new RunSnapshot.Block { x=x,y=y,z=z,rgba=(c.r<<24)|(c.g<<16)|(c.b<<8)|c.a });
        }
        snapshot.blocks=blocks.ToArray();
        snapshot.peak=Mathf.Max(snapshot.peak,blocks.Count+(pending!=null ? pending.Cells.Count : 0));
        if (pending!=null && Tutorial.Current==TutorialProgress.Step.PlaceShape) snapshot.tutorial=(int)TutorialProgress.Step.Complete;
        if (pending==null && activeFigureController!=null && activeFigureController.HasActiveFigure)
        {
            snapshot.selected=lastSpawnedButtonIndex;
            var anchor=activeFigureController.CurrentGridPosition;
            snapshot.anchorX=anchor.x; snapshot.anchorY=anchor.y; snapshot.anchorZ=anchor.z;
        }
        Quaternion q=field.transform.rotation; snapshot.rotation=new[] { q.x,q.y,q.z,q.w };
        var camera=Camera.main;
        if (camera==null) return;
        snapshot.distance=Vector3.Distance(camera.transform.position,field.transform.position); snapshot.ortho=camera.orthographicSize;
        GamePreferences.RecordBest(snapshot.score);
        RunSaveStore.Save(snapshot);
    }

    // Called by the selector after every Start has bound its references and set up the camera.
    public bool TryRestoreSession()
    {
        if (practice || discardSave) return false;
        var snapshot=RunSaveStore.Load();
        if (snapshot==null) return false;
        restoring=true;
        try
        {
            score=snapshot.score; bestScore=Mathf.Max(bestScore,score); GamePreferences.RecordBest(bestScore);
            Statistics.Restore(snapshot.moves,snapshot.lines,snapshot.peak);
            Tutorial.Restore(snapshot.tutorial);
            if (Tutorial.Current==TutorialProgress.Step.Complete) GamePreferences.CompleteTutorial();
            Quaternion q=new Quaternion(snapshot.rotation[0],snapshot.rotation[1],snapshot.rotation[2],snapshot.rotation[3]);
            if (fieldRotator!=null) fieldRotator.RestoreRotation(q); else field.transform.rotation=q;
            var camera=Camera.main;
            if (camera!=null)
            {
                Vector3 offset=camera.transform.position-field.transform.position;
                camera.transform.position=field.transform.position+offset.normalized*snapshot.distance;
                camera.orthographicSize=snapshot.ortho;
            }
            foreach (var block in snapshot.blocks) shapePlacer.RestoreBlock(block);
            shapeSelector.RestoreOffers(snapshot);
            if (!IsGameOver && snapshot.selected>=0 && TrySelectOffer(snapshot.selected))
                activeFigureController.RestoreGridPosition(new Vector3Int(snapshot.anchorX,snapshot.anchorY,snapshot.anchorZ));
        }
        finally { restoring=false; }
        GetComponent<MobilePlacementControls>()?.SyncRestoredScore();
        SaveSession(); return true;
    }

    public int Score => score;
    public int BestScore => bestScore;
    public bool IsGameOver => session.GameOver;
    public bool CanSelectShape => !InputBlocked && Tutorial.CanSelect && (shapePlacer == null || !shapePlacer.IsResolving) &&
        (fieldRotator==null || !fieldRotator.IsRotating);
}
