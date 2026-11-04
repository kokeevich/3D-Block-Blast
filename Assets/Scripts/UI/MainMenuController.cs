using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    private ScreenWidgets ui;
    private SettingsView settings;
    private RectTransform title,kicker,hero,record,subtitle,playRect,tutorialRect,settingsRect,footer;
    private Text titleText,recordText;
    private Button play,tutorial,options;
    private bool loading;
    private int width,height;
    private void Start()
    {
        Time.timeScale=1; AudioListener.pause=false; GamePreferences.ApplyAudio();
        if (Camera.main!=null) { Camera.main.clearFlags=CameraClearFlags.SolidColor; Camera.main.backgroundColor=GameTheme.Background; }
        ui=new ScreenWidgets("MainMenuHUD",100);
        kicker=ui.Label("Kicker",ui.Root,"ОБЪЁМНАЯ ГОЛОВОЛОМКА",new Vector2(.08f,.94f),new Vector2(.92f,.98f),26,GameTheme.Accent).rectTransform;
        titleText=ui.Label("Title",ui.Root,"BLOCK\nBLAST 3D",new Vector2(.08f,.79f),new Vector2(.92f,.94f),82,GameTheme.Ink,true); title=titleText.rectTransform;
        hero=ui.Panel("Hero",ui.Root,new Vector2(.18f,.48f),new Vector2(.82f,.78f),new Color(.84f,.89f,.85f));
        var art=ScreenWidgets.RectTransform("HeroShape",hero,new Vector2(.08f,.04f),new Vector2(.92f,.96f));
        art.gameObject.AddComponent<ShapeThumbnailGraphic>().SetShape(BalanceRules.Catalog[9].Cells,BalancedShapeSelector.OfferColor(0));
        record=ui.Panel("Record",ui.Root,new Vector2(.18f,.39f),new Vector2(.82f,.46f),GameTheme.Panel);
        ui.Label("RecordCaption",record,"ВАШ РЕКОРД",new Vector2(0,.55f),Vector2.one,26,GameTheme.Muted);
        recordText=ui.Label("BestScore",record,GamePreferences.BestScore.ToString(),Vector2.zero,new Vector2(1,.65f),48,GameTheme.Accent,true);
        subtitle=ui.Label("Subtitle",ui.Root,"Собирайте линии.\nОсвобождайте объём.",new Vector2(.08f,.30f),new Vector2(.92f,.37f),36,GameTheme.Muted).rectTransform;
        play=ui.Button("PlayGame",ui.Root,"Играть",new Vector2(.08f,.18f),new Vector2(.92f,.27f),PlayGame,true); playRect=play.GetComponent<RectTransform>();
        tutorial=ui.Button("LearnGame",ui.Root,"Как играть",new Vector2(.08f,.08f),new Vector2(.49f,.15f),PlayTutorial); tutorialRect=tutorial.GetComponent<RectTransform>();
        options=ui.Button("MenuSettings",ui.Root,"Настройки",new Vector2(.51f,.08f),new Vector2(.92f,.15f),() => settings.Show()); settingsRect=options.GetComponent<RectTransform>();
        footer=ui.Label("Footer",ui.Root,"8 × 8 × 8  ·  Три направления",new Vector2(.08f,.02f),new Vector2(.92f,.06f),24,GameTheme.Muted).rectTransform;
        settings=new SettingsView(ui,() => { }); Layout();
    }
    public void PlayGame() { Launch(false); }
    public void PlayTutorial() { Launch(true); }
    public void ToggleSound() { GamePreferences.SetSound(!GamePreferences.SoundEnabled); }
    private void Launch(bool learn)
    {
        if (loading) return;
        loading=true; GamePreferences.Save(); GameLaunch.RequestTutorial(learn);
        play.interactable=tutorial.interactable=options.interactable=false;
        play.GetComponentInChildren<Text>().text="Загрузка…";
        SceneManager.LoadSceneAsync("Game");
    }
    private void Update()
    {
        if (ui==null) return;
        ui.UpdateSafeArea(); settings.Refresh();
        if (width!=Screen.width || height!=Screen.height) Layout();
        recordText.text=GamePreferences.BestScore.ToString();
        if (Input.GetKeyDown(KeyCode.Escape) && !loading)
        { if (settings.Visible) settings.Hide(); else Application.Quit(); }
    }
    private static void Place(RectTransform rect,float x0,float y0,float x1,float y1)
    { rect.anchorMin=new Vector2(x0,y0); rect.anchorMax=new Vector2(x1,y1); }
    private void Layout()
    {
        width=Screen.width; height=Screen.height; bool wide=width>height;
        float relativeHeight=1080f*Screen.safeArea.height/Mathf.Max(1,width);
        titleText.fontSize=Mathf.Clamp(Mathf.FloorToInt(relativeHeight*(wide ? .29f : .15f)/2.35f),48,wide ? 72 : 82);
        if (wide)
        {
            Place(title,.05f,.62f,.48f,.91f); Place(kicker,.05f,.92f,.48f,.98f); Place(hero,.09f,.08f,.45f,.59f);
            Place(record,.58f,.74f,.94f,.92f); Place(subtitle,.55f,.56f,.96f,.72f); Place(playRect,.56f,.37f,.96f,.52f);
            Place(tutorialRect,.56f,.19f,.75f,.32f); Place(settingsRect,.77f,.19f,.96f,.32f); Place(footer,.56f,.05f,.96f,.13f);
        }
        else
        {
            Place(title,.08f,.79f,.92f,.94f); Place(kicker,.08f,.94f,.92f,.98f); Place(hero,.18f,.48f,.82f,.78f);
            Place(record,.18f,.39f,.82f,.46f); Place(subtitle,.08f,.30f,.92f,.37f); Place(playRect,.08f,.18f,.92f,.27f);
            Place(tutorialRect,.08f,.08f,.49f,.15f); Place(settingsRect,.51f,.08f,.92f,.15f); Place(footer,.08f,.02f,.92f,.06f);
        }
    }
    private void OnApplicationPause(bool paused) { if (paused) GamePreferences.Save(); }
    private void OnApplicationFocus(bool focused) { if (!focused && !Application.isEditor) GamePreferences.Save(); }
    private void OnApplicationQuit() { GamePreferences.Save(); }
    private void OnDestroy() { if (ui!=null) { GamePreferences.Save(); ui.Dispose(); } }
}
