using UnityEngine;

/// <summary>Modal pause/settings/help/confirmation panels, outside the board input surface.</summary>
public class GameSessionUI : MonoBehaviour
{
    private GameManager game;
    private ScreenWidgets ui;
    private SettingsView settings;
    private GameObject pause,intro,help,confirm;
    private RectTransform pauseCard,introCard,helpCard,confirmCard;
    private UnityEngine.UI.Text confirmationTitle,confirmationText;
    private UnityEngine.UI.Button confirmButton,skipButton;
    private UnityEngine.UI.Text helpBody,introBody;
    private bool restart;
    public void Initialize(GameManager manager)
    {
        game=manager; ui=new ScreenWidgets("SessionOverlays",200);
        pause=ui.Modal("PauseOverlay",out pauseCard).gameObject;
        ui.Label("Title",pauseCard,"Пауза",new Vector2(.08f,.84f),new Vector2(.92f,.97f),48,GameTheme.Ink,true);
        ui.Label("Subtitle",pauseCard,"Ваш ход подождёт",new Vector2(.08f,.75f),new Vector2(.92f,.84f),30,GameTheme.Muted);
        ui.Button("Resume",pauseCard,"Продолжить",new Vector2(.1f,.61f),new Vector2(.9f,.73f),game.ResumeGame,true);
        ui.Button("PauseSettings",pauseCard,"Настройки",new Vector2(.1f,.47f),new Vector2(.9f,.59f),() => settings.Show());
        ui.Button("PauseHelp",pauseCard,"Как играть",new Vector2(.1f,.33f),new Vector2(.9f,.45f),() => help.SetActive(true));
        ui.Button("PauseRestart",pauseCard,"Начать заново",new Vector2(.1f,.19f),new Vector2(.9f,.31f),() => ShowConfirmation(true));
        ui.Button("PauseMenu",pauseCard,"В главное меню",new Vector2(.1f,.05f),new Vector2(.9f,.17f),() => ShowConfirmation(false));
        intro=ui.Modal("TutorialIntro",out introCard).gameObject;
        ui.Label("Title",introCard,"Освоимся в объёме",new Vector2(.06f,.81f),new Vector2(.94f,.96f),46,GameTheme.Ink,true);
        introBody=ui.Label("Body",introCard,"Поставьте фигуры внутри куба.\nЛинии из 8 блоков исчезают.\n\nПройдём пять коротких шагов\nпрямо в первой партии.",new Vector2(.08f,.35f),new Vector2(.92f,.79f),36,GameTheme.Muted);
        ui.Button("BeginTutorial",introCard,"Попробовать",new Vector2(.1f,.19f),new Vector2(.9f,.32f),game.BeginTutorial,true);
        ui.Button("SkipTutorialIntro",introCard,"Пропустить",new Vector2(.1f,.04f),new Vector2(.9f,.17f),() => game.SkipTutorial());
        help=ui.Modal("HelpOverlay",out helpCard).gameObject;
        ui.Label("Title",helpCard,"Как играть",new Vector2(.08f,.84f),new Vector2(.92f,.97f),46,GameTheme.Ink,true);
        helpBody=ui.Label("Body",helpCard,"Свайп без фигуры — поворот куба.\nДва пальца — масштаб.\n\nВыберите фигуру, сдвиньте её\nи задайте глубину кнопками 1–8.\nЗелёное место — можно поставить.\n\nСобирайте линии по трём направлениям.\nРентген помогает видеть внутренние клетки.",new Vector2(.07f,.28f),new Vector2(.93f,.82f),32,GameTheme.Muted);
        skipButton=ui.Button("SkipTutorialPause",helpCard,"Пропустить обучение",new Vector2(.1f,.17f),new Vector2(.9f,.28f),() => { game.SkipTutorial(); help.SetActive(false); game.ResumeGame(); });
        ui.Button("HelpBack",helpCard,"Назад",new Vector2(.1f,.035f),new Vector2(.9f,.15f),() => help.SetActive(false),true);
        confirm=ui.Modal("ConfirmAbandon",out confirmCard).gameObject;
        confirmationTitle=ui.Label("Title",confirmCard,"",new Vector2(.08f,.72f),new Vector2(.92f,.93f),46,GameTheme.Ink,true);
        confirmationText=ui.Label("Body",confirmCard,"",new Vector2(.08f,.37f),new Vector2(.92f,.69f),34,GameTheme.Muted);
        confirmButton=ui.Button("Confirm",confirmCard,"",new Vector2(.1f,.20f),new Vector2(.9f,.34f),() => { if (restart) game.RestartGame(); else game.ReturnToMenu(); },true);
        ui.Button("Stay",confirmCard,"Остаться",new Vector2(.1f,.04f),new Vector2(.9f,.18f),() => confirm.SetActive(false));
        settings=new SettingsView(ui,() => { });
        pause.SetActive(false); intro.SetActive(false); help.SetActive(false); confirm.SetActive(false);
    }
    private void ShowConfirmation(bool again)
    {
        restart=again; confirmationTitle.text=again ? "Начать заново?" : "Вернуться в меню?";
        confirmationText.text=again ? "Текущая партия завершится.\nВаш рекорд сохранится." : "Партия сохранится.\nВы сможете продолжить её позже.";
        confirmButton.GetComponentInChildren<UnityEngine.UI.Text>().text=again ? "Новая партия" : "В меню";
        confirm.SetActive(true); confirm.transform.SetAsLastSibling();
    }
    public void HandleBack()
    {
        if (game.IsGameOver) { game.ReturnToMenu(); return; }
        if (!game.IsPaused) { game.PauseGame(); return; }
        if (settings.Visible) settings.Hide();
        else if (confirm.activeSelf) confirm.SetActive(false);
        else if (help.activeSelf) help.SetActive(false);
        else game.ResumeGame();
    }
    private void Update()
    {
        if (ui==null) return;
        ui.UpdateSafeArea(); ScreenWidgets.FitModal(pauseCard); ScreenWidgets.FitModal(introCard);
        ScreenWidgets.FitModal(helpCard); ScreenWidgets.FitModal(confirmCard); settings.Refresh();
        pause.SetActive(game.IsPaused);
        intro.SetActive(game.Tutorial.IsIntro && !game.IsPaused && !game.IsGameOver && !game.Transitioning);
        skipButton.gameObject.SetActive(game.Tutorial.Active);
        bool wide=Screen.width>Screen.height;
        helpBody.fontSize=wide ? 22 : 32;
        introBody.fontSize=wide ? 28 : 36;
        if (!game.IsPaused)
        { help.SetActive(false); confirm.SetActive(false); if (settings.Visible) settings.Hide(); }
    }
    private void OnDestroy() { if (ui!=null) ui.Dispose(); }
}
