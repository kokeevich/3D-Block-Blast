using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    private ScreenWidgets ui;
    private SettingsView settings;
    private RectTransform title,kicker,hero,record,subtitle,playRect,settingsRect,bannerSpace,newGameRect;
    private RectTransform newGameCard;
    private GameObject newGameConfirm;
    private Text titleText,recordText;
    private Button play,options,newGame;
    private bool loading,hasSave;
    private int width,height;
    private void Start()
    {
        Time.timeScale=1; AudioListener.pause=false; GamePreferences.ApplyAudio();
        if (Camera.main!=null) { Camera.main.clearFlags=CameraClearFlags.SolidColor; Camera.main.backgroundColor=GameTheme.Background; }
        hasSave=RunSaveStore.Load()!=null;
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
        play.GetComponentInChildren<Text>().text=hasSave ? "Продолжить" : "Играть";
        newGame=ui.Button("NewGame",ui.Root,"Новая игра",new Vector2(.08f,.12f),new Vector2(.92f,.17f),() => newGameConfirm.SetActive(true));
        newGameRect=newGame.GetComponent<RectTransform>(); newGame.gameObject.SetActive(hasSave);
        options=ui.Button("MenuSettings",ui.Root,"",Vector2.one,Vector2.one,() => settings.Show());
        settingsRect=options.GetComponent<RectTransform>();
        var gear=ScreenWidgets.RectTransform("Gear",settingsRect,new Vector2(.22f,.22f),new Vector2(.78f,.78f));
        gear.gameObject.AddComponent<SettingsGearGraphic>().color=GameTheme.Ink;
        // Deliberately empty: reserved for the future bottom banner, not a fake ad.
        bannerSpace=ScreenWidgets.RectTransform("BannerSpace",ui.Root,Vector2.zero,new Vector2(1,.11f));
        settings=new SettingsView(ui,() => { },PlayTutorial);
        newGameConfirm=ui.Modal("ConfirmNewGame",out newGameCard).gameObject;
        ui.Label("Title",newGameCard,"Начать новую игру?",new Vector2(.08f,.72f),new Vector2(.92f,.93f),46,GameTheme.Ink,true);
        ui.Label("Body",newGameCard,"Сохранённая партия будет заменена.\nВаш рекорд останется.",new Vector2(.08f,.37f),new Vector2(.92f,.69f),34,GameTheme.Muted);
        ui.Button("ConfirmNewRun",newGameCard,"Новая игра",new Vector2(.1f,.20f),new Vector2(.9f,.34f),() => { RunSaveStore.Clear(); Launch(false); },true);
        ui.Button("KeepRun",newGameCard,"Отмена",new Vector2(.1f,.04f),new Vector2(.9f,.18f),() => newGameConfirm.SetActive(false));
        newGameConfirm.SetActive(false); Layout();
    }
    public void PlayGame() { Launch(false); }
    public void PlayTutorial() { Launch(true); }
    public void ToggleSound() { GamePreferences.SetSound(!GamePreferences.SoundEnabled); }
    private void Launch(bool learn)
    {
        if (loading) return;
        loading=true; GamePreferences.Save(); GameLaunch.RequestTutorial(learn);
        play.interactable=options.interactable=newGame.interactable=false;
        play.GetComponentInChildren<Text>().text="Загрузка…";
        SceneManager.LoadSceneAsync("Game");
    }
    private void Update()
    {
        if (ui==null) return;
        ui.UpdateSafeArea(); settings.Refresh(); ScreenWidgets.FitModal(newGameCard);
        if (width!=Screen.width || height!=Screen.height) Layout();
        recordText.text=GamePreferences.BestScore.ToString();
        if (Input.GetKeyDown(KeyCode.Escape) && !loading)
        { if (newGameConfirm.activeSelf) newGameConfirm.SetActive(false); else if (settings.Visible) settings.Hide(); else Application.Quit(); }
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
            Place(title,.05f,.62f,.48f,.91f); Place(kicker,.05f,.92f,.48f,.98f); Place(hero,.09f,.18f,.45f,.59f);
            Place(record,.56f,.74f,.84f,.92f); Place(subtitle,.55f,.56f,.96f,.72f); Place(playRect,.56f,.37f,.96f,.52f);
            Place(bannerSpace,0,0,1,.14f); Place(newGameRect,.56f,.20f,.96f,.32f);
        }
        else
        {
            Place(title,.08f,.79f,.92f,.94f); Place(kicker,.08f,.94f,.80f,.98f); Place(hero,.18f,.48f,.82f,.78f);
            Place(record,.18f,.39f,.82f,.46f); Place(subtitle,.08f,.30f,.92f,.37f); Place(playRect,.08f,.18f,.92f,.27f);
            Place(bannerSpace,0,0,1,.11f); Place(newGameRect,.08f,.12f,.92f,.17f);
        }
        // Square touch target anchored inside the safe area, in either orientation.
        settingsRect.anchorMin=settingsRect.anchorMax=Vector2.one;
        settingsRect.pivot=Vector2.one;
        float size=wide ? 96 : 112;
        settingsRect.sizeDelta=new Vector2(size,size);
        settingsRect.anchoredPosition=new Vector2(-24,-24);
    }
    private void OnApplicationPause(bool paused) { if (paused) GamePreferences.Save(); }
    private void OnApplicationFocus(bool focused) { if (!focused && !Application.isEditor) GamePreferences.Save(); }
    private void OnApplicationQuit() { GamePreferences.Save(); }
    private void OnDestroy() { if (ui!=null) { GamePreferences.Save(); ui.Dispose(); } }
}

/// <summary>Vector gear, independent of the device font's Unicode coverage.</summary>
public sealed class SettingsGearGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect=GetPixelAdjustedRect();
        Vector2 center=rect.center;
        float radius=Mathf.Min(rect.width,rect.height)*.5f;
        const int segments=64;
        for (int i=0;i<=segments;i++)
        {
            float angle=i*Mathf.PI*2/segments;
            Vector2 direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
            float outer=(i%8>=2 && i%8<=5) ? 1f : .77f;
            vh.AddVert(center+direction*radius*.36f,color,Vector2.zero);
            vh.AddVert(center+direction*radius*outer,color,Vector2.zero);
            if (i==0) continue;
            int a=(i-1)*2;
            vh.AddTriangle(a,a+1,a+3); vh.AddTriangle(a,a+3,a+2);
        }
    }
    protected override void Awake() { base.Awake(); raycastTarget=false; }
}
