using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>The game HUD owns explicit, raycastable offer cards and placement actions.</summary>
public class MobilePlacementControls : MonoBehaviour
{
    private static readonly Color Background = new Color(0.68f,0.76f,0.73f);
    private static readonly Color PanelColor = new Color(0.80f,0.86f,0.82f);
    private static readonly Color Muted = new Color(0.25f,0.35f,0.34f);
    private static readonly Color Accent = new Color(0.04f,0.36f,0.28f);
    private static readonly Color Ink = new Color(0.12f,0.21f,0.22f);
    private static readonly Color SoftButton = new Color(0.71f,0.80f,0.76f);
    private ActiveFigureController figure;
    private ShapePlacer placer;
    private GameManager game;
    private BalancedShapeSelector selector;
    private Field3D field;
    private BoardVisibility visibility;
    private Button xrayToggle;
    private Text xrayLabel;
    private GameObject canvasObject;
    private Canvas canvas;
    private RectTransform safeRoot, boardArea, tray, hud;
    private GameObject placementActions, browseHelp, gameOver;
    private Text scoreText, recordText, hint, modeText, depthText, setText, gainText, finalScore;
    private readonly Button[] cards = new Button[3];
    private readonly Image[] cardBackgrounds = new Image[3];
    private readonly Outline[] cardBorders = new Outline[3];
    private readonly ShapeThumbnailGraphic[] thumbnails = new ShapeThumbnailGraphic[3];
    private readonly Text[] cardLabels = new Text[3];
    private readonly ShapeInstanceData[] drawnOffers = new ShapeInstanceData[3];
    private readonly int[] drawnRotations = { -1,-1,-1 };
    private readonly bool[] offerFits = new bool[3];
    private readonly Button[] layers = new Button[8];
    private Button rotateHorizontal, rotateVertical, cancel, place;
    private Text finalStatistics,brandText;
    private Font font;
    private Sprite rounded;
    private Texture2D roundedTexture;
    private int lastScore, screenWidth, screenHeight;
    private float displayedScore, gainTime;

    public void Initialize(ActiveFigureController controller, ShapePlacer shapePlacer, GameManager manager)
    {
        figure = controller; placer = shapePlacer; game = manager;
        selector = FindObjectOfType<BalancedShapeSelector>();
        field = FindObjectOfType<Field3D>();
        visibility=field!=null ? field.GetComponent<BoardVisibility>() : null;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        MakeRoundedSprite();
        if (figure != null) figure.SetPreviewColors(new Color(0.08f,0.68f,0.43f),new Color(0.94f,0.31f,0.23f));
        canvasObject = new GameObject("GameHUD",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080,1920);
        scaler.matchWidthOrHeight = 0;
        safeRoot = Rect("SafeArea",canvasObject.transform,Vector2.zero,Vector2.one);
        boardArea = Rect("BoardGestureArea",safeRoot,new Vector2(0.02f,0.40f),new Vector2(0.98f,0.84f));
        BuildHeader(); BuildTray(); BuildGameOver();
        ConfigureView();
    }

    private void BuildHeader()
    {
        hud = Rect("Header",safeRoot,new Vector2(0,1),Vector2.one,new Vector2(-88,260),new Vector2(0,-24));
        hud.pivot = new Vector2(0.5f,1);
        brandText=Label("Brand",hud,"BLOCK BLAST",new Vector2(0,0.77f),new Vector2(0.70f,1),42,Muted,TextAnchor.MiddleLeft,true);
        Label("ScoreCaption",hud,"СЧЁТ",new Vector2(0,0.55f),new Vector2(0.4f,0.74f),30,Muted,TextAnchor.MiddleLeft);
        scoreText = Label("Score",hud,"0",new Vector2(0,0.08f),new Vector2(0.6f,0.58f),104,Ink,TextAnchor.MiddleLeft,true);
        var record = Box("RecordPill",hud,new Vector2(0.70f,0.56f),new Vector2(1,0.99f),PanelColor);
        Label("Caption",record,"РЕКОРД",new Vector2(0,0.55f),Vector2.one,28,Muted);
        recordText = Label("Record",record,"0",new Vector2(0,0.04f),new Vector2(1,0.62f),38,Accent,TextAnchor.MiddleCenter,true);
        xrayToggle=Button("XRayToggle",hud,"Рентген: выкл",new Vector2(0.70f,0.02f),new Vector2(1,0.50f),
            SoftButton,Ink,() => { if (visibility!=null && visibility.isActiveAndEnabled && !game.IsGameOver) visibility.ToggleXRay(); });
        xrayLabel=xrayToggle.GetComponentInChildren<Text>();
        gainText = Label("ScoreGain",hud,"",new Vector2(0.3f,0.14f),new Vector2(0.65f,0.6f),34,Accent,TextAnchor.MiddleLeft,true);
        modeText = Label("Mode",safeRoot,"Выберите фигуру",new Vector2(0.1f,0.815f),new Vector2(0.9f,0.85f),32,Accent);
        hint = Label("Hint",safeRoot,"Свайпните, чтобы повернуть куб на 90°",new Vector2(0.06f,0.775f),new Vector2(0.94f,0.813f),36,Muted);
    }

    private void BuildTray()
    {
        tray = Box("OfferTray",safeRoot,Vector2.zero,new Vector2(1,0),PanelColor,new Vector2(-48,700),new Vector2(0,20));
        tray.pivot = new Vector2(0.5f,0);
        Label("TrayCaption",tray,"ВАШИ ФИГУРЫ",new Vector2(0.03f,0.91f),new Vector2(0.48f,0.99f),30,Muted,TextAnchor.MiddleLeft,true);
        setText = Label("SetCount",tray,"3 / 3",new Vector2(0.48f,0.91f),new Vector2(0.97f,0.99f),26,Muted,TextAnchor.MiddleRight);
        for (int i = 0; i < 3; i++)
        {
            int index = i;
            float left = 0.025f + i*0.325f;
            cards[i] = Button("OfferCard_"+i,tray,"",new Vector2(left,0.55f),new Vector2(left+0.30f,0.89f),
                SoftButton,Ink,() => game.TrySelectOffer(index));
            cardBackgrounds[i] = cards[i].GetComponent<Image>();
            cardBorders[i] = cards[i].gameObject.AddComponent<Outline>();
            cardBorders[i].effectDistance = new Vector2(2,-2);
            cardBorders[i].effectColor = Color.clear;
            var art = Rect("ShapeArt",cards[i].transform,new Vector2(0.08f,0.21f),new Vector2(0.92f,0.96f));
            thumbnails[i] = art.gameObject.AddComponent<ShapeThumbnailGraphic>();
            thumbnails[i].raycastTarget = false;
            cardLabels[i] = Label("OfferLabel",cards[i].transform,"",new Vector2(0,0.02f),new Vector2(1,0.22f),34,Muted);
        }
        browseHelp = Rect("BrowseHelp",tray,new Vector2(0.04f,0.03f),new Vector2(0.96f,0.46f)).gameObject;
        Label("Instruction",browseHelp.transform,"Выберите фигуру и найдите ей место",new Vector2(0,0.55f),Vector2.one,40,Ink);
        Label("Rule",browseHelp.transform,"Соберите 8 блоков в линию, чтобы её очистить",new Vector2(0,0.24f),new Vector2(1,0.6f),34,Muted);
        Label("Gestures",browseHelp.transform,"Свайп — поворот на 90°   ·   Два пальца — масштаб",Vector2.zero,new Vector2(1,0.3f),30,Muted);
        placementActions = Rect("PlacementActions",tray,Vector2.zero,new Vector2(1,0.53f)).gameObject;
        var actions = placementActions.transform;
        Label("DepthCaption",actions,"ГЛУБИНА",new Vector2(0.03f,0.82f),new Vector2(0.5f,1),28,Muted,TextAnchor.MiddleLeft);
        depthText = Label("DepthValue",actions,"1 / 8",new Vector2(0.65f,0.82f),new Vector2(0.97f,1),32,Accent,TextAnchor.MiddleRight);
        for (int i = 0; i < 8; i++)
        {
            int layer = i;
            float x = 0.027f+i*0.119f;
            layers[i] = Button("Layer_"+i,actions,(i+1).ToString(),new Vector2(x,0.48f),new Vector2(x+0.108f,0.79f),
                SoftButton,Muted,() => { if (CanInteract) figure.SetDepth(layer); });
        }
        rotateHorizontal = Button("RotateSide",actions,"↻ Вбок",new Vector2(0.027f,0.07f),new Vector2(0.185f,0.40f),
            SoftButton,Ink,() => { if (CanInteract) figure.Rotate(figure.Frame.VerticalAxis); });
        rotateVertical = Button("RotateUp",actions,"↻ Вверх",new Vector2(0.20f,0.07f),new Vector2(0.358f,0.40f),
            SoftButton,Ink,() => { if (CanInteract) figure.Rotate(figure.Frame.HorizontalAxis); });
        cancel = Button("Cancel",actions,"Отмена",new Vector2(0.38f,0.07f),new Vector2(0.57f,0.40f),
            SoftButton,Ink,() => { if (CanInteract) game.CancelSelection(); });
        place = Button("ConfirmPlacement",actions,"Поставить",new Vector2(0.59f,0.07f),new Vector2(0.973f,0.40f),
            Accent,Color.white,() => { if (CanInteract) placer.TryPlaceActive(); });
        placementActions.SetActive(false);
    }

    private void BuildGameOver()
    {
        gameOver = Box("GameOverOverlay",safeRoot,Vector2.zero,Vector2.one,new Color(0.66f,0.75f,0.70f,0.96f)).gameObject;
        gameOver.GetComponent<Image>().raycastTarget = true;
        var card = Box("GameOverCard",gameOver.transform,new Vector2(0.08f,0.23f),new Vector2(0.92f,0.77f),PanelColor);
        Label("Title",card,"Хорошая партия!",new Vector2(0.04f,0.83f),new Vector2(0.96f,0.96f),48,Ink,TextAnchor.MiddleCenter,true);
        Label("Caption",card,"Больше нет места для оставшихся фигур",new Vector2(0.05f,0.72f),new Vector2(0.95f,0.83f),36,Muted);
        finalScore = Label("FinalScore",card,"0",new Vector2(0,0.49f),new Vector2(1,0.71f),80,Accent,TextAnchor.MiddleCenter,true);
        finalStatistics=Label("FinalStatistics",card,"",new Vector2(0.06f,0.22f),new Vector2(0.94f,0.49f),34,Ink);
        Button("Restart",card,"Ещё раз",new Vector2(0.1f,0.06f),new Vector2(0.9f,0.18f),Accent,Color.white,game.RestartGame);
        gameOver.SetActive(false);
    }

    private bool CanInteract => figure != null && figure.HasActiveFigure && placer != null && !placer.IsResolving && !game.IsGameOver;
    public bool Contains(Vector2 position) => tray != null && RectTransformUtility.RectangleContainsScreenPoint(tray,position);
    public bool IsBoardArea(Vector2 position) => boardArea != null && !game.IsGameOver && !Contains(position) &&
        RectTransformUtility.RectangleContainsScreenPoint(boardArea,position);

    private void Update()
    {
        if (safeRoot == null) return;
        Rect safe = Screen.safeArea;
        safeRoot.anchorMin = new Vector2(safe.xMin/Mathf.Max(1,Screen.width),safe.yMin/Mathf.Max(1,Screen.height));
        safeRoot.anchorMax = new Vector2(safe.xMax/Mathf.Max(1,Screen.width),safe.yMax/Mathf.Max(1,Screen.height));
        if (screenWidth != Screen.width || screenHeight != Screen.height) ConfigureView();
        bool xray=visibility!=null && visibility.XRayEnabled;
        xrayToggle.interactable=visibility!=null && visibility.isActiveAndEnabled && !game.IsGameOver;
        xrayToggle.GetComponent<Image>().color=xray ? Accent : SoftButton;
        xrayLabel.text=xray ? "Рентген: вкл" : "Рентген: выкл";
        xrayLabel.color=xray ? Color.white : Ink;
        bool active = CanInteract;
        placementActions.SetActive(figure.HasActiveFigure);
        browseHelp.SetActive(!figure.HasActiveFigure);
        rotateHorizontal.interactable = rotateVertical.interactable = cancel.interactable = active;
        place.interactable = active && figure.CanPlaceAtCurrent();
        for (int i = 0; i < layers.Length; i++)
        {
            layers[i].interactable = active;
            layers[i].GetComponent<Image>().color = figure.HasActiveFigure && figure.Depth == i ? Accent : SoftButton;
            layers[i].GetComponentInChildren<Text>().color = figure.HasActiveFigure && figure.Depth == i ? Color.white : Muted;
        }
        depthText.text = (figure.Depth+1)+" / 8";
        modeText.text = game.IsGameOver ? "Спасибо за игру!" : placer.IsResolving ? "Отличный ход!" :
            !figure.HasActiveFigure ? "Выберите фигуру" : figure.CanPlaceAtCurrent() ? "Отличное место!" : "Попробуем другое место";
        modeText.color = figure.HasActiveFigure && !figure.CanPlaceAtCurrent() ? new Color(0.78f,0.26f,0.20f) : Accent;
        hint.text = placer.IsResolving ? "" : !figure.HasActiveFigure ? "Свайпните, чтобы повернуть куб на 90°" :
            figure.CanPlaceAtCurrent() ? "Двигайте фигуру • меняйте глубину" : "Сдвиньте фигуру или смените глубину";
        if (xray && !placer.IsResolving && !game.IsGameOver)
            hint.text=!figure.HasActiveFigure ? "Рентген: видны внутренние блоки" : figure.CanPlaceAtCurrent() ?
                "Выделен слой "+(figure.Depth+1)+" — остальные прозрачны" : hint.text;
        RefreshOffers();
        displayedScore = Mathf.MoveTowards(displayedScore,game.Score,Mathf.Max(30,Mathf.Abs(game.Score-displayedScore)*8)*Time.unscaledDeltaTime);
        scoreText.text = Mathf.RoundToInt(displayedScore).ToString();
        recordText.text = Mathf.Max(game.BestScore,game.Score).ToString();
        if (game.Score > lastScore)
        { gainText.text = "+"+(game.Score-lastScore); lastScore = game.Score; gainTime = 1; }
        gainTime = Mathf.Max(0,gainTime-Time.unscaledDeltaTime);
        gainText.color = new Color(Accent.r,Accent.g,Accent.b,gainTime);
        gameOver.SetActive(game.IsGameOver);
        if (game.IsGameOver)
        {
            finalScore.text=game.Score.ToString();
            finalStatistics.text=$"Ходы: {game.Statistics.Moves}\nОчищено линий: {game.Statistics.Lines}\nМакс. заполненность: {game.Statistics.PeakPercent:0}%";
        }
    }

    private void RefreshOffers()
    {
        if (selector == null) return;
        int remaining = 0;
        for (int i = 0; i < cards.Length; i++)
        {
            var offer = selector.GetShape(i);
            if (offer != null) remaining++;
            bool selected = offer != null && figure.ActiveOffer == offer;
            int rotation = offer != null ? offer.offer.Orientation : -1;
            bool redraw = offer != drawnOffers[i] || rotation != drawnRotations[i] || game.Score != lastScore;
            if (redraw) offerFits[i] = offer != null && selector.CanPlaceShapeAnywhere(i);
            cards[i].interactable = offer != null && offerFits[i] && game.CanSelectShape;
            cardBorders[i].effectColor = selected ? Accent : Color.clear;
            cardBackgrounds[i].color = offer == null ? new Color(0.75f,0.81f,0.77f) :
                Color.Lerp(new Color(0.84f,0.89f,0.85f),offer.color,selected ? 0.25f : 0.10f);
            cardLabels[i].text = offer == null ? "Готово" : selected ? "Выбрано" : !offerFits[i] ? "Нет места" : offer.baseCells.Length+" блок.";
            cardLabels[i].color = selected ? Accent : Muted;
            if (redraw)
            {
                drawnOffers[i] = offer; drawnRotations[i] = rotation;
                thumbnails[i].SetShape(offer?.offer.Cells,offer == null ? Color.clear : offerFits[i] ? offer.color : new Color(Muted.r,Muted.g,Muted.b,0.4f));
            }
        }
        setText.text = remaining+" / 3 · "+BalanceRules.StageName(selector.PlacedShapes);
    }

    private void ConfigureView()
    {
        screenWidth = Screen.width; screenHeight = Screen.height;
        var view = Camera.main;
        if (view == null || field == null) return;
        view.clearFlags = CameraClearFlags.SolidColor; view.backgroundColor = Background;
        view.orthographic = false; view.fieldOfView = 38; view.nearClipPlane = 0.05f;
        bool landscape = Screen.width > Screen.height;
        if (landscape)
        {
            tray.anchorMin = new Vector2(0.58f,0.03f); tray.anchorMax = new Vector2(0.98f,0.97f);
            tray.pivot = new Vector2(0.5f,0.5f); tray.sizeDelta = Vector2.zero; tray.anchoredPosition = Vector2.zero;
            hud.anchorMin = new Vector2(0,1); hud.anchorMax = new Vector2(0.55f,1);
            hud.sizeDelta = new Vector2(-64,176);
            boardArea.anchorMin = new Vector2(0.02f,0.06f); boardArea.anchorMax = new Vector2(0.56f,0.70f);
            modeText.rectTransform.anchorMin = new Vector2(0.03f,0.72f); modeText.rectTransform.anchorMax = new Vector2(0.53f,0.77f);
            hint.rectTransform.anchorMin = new Vector2(0.03f,0.66f); hint.rectTransform.anchorMax = new Vector2(0.53f,0.72f);
        }
        else
        {
            tray.anchorMin = Vector2.zero; tray.anchorMax = new Vector2(1,0);
            tray.pivot = new Vector2(0.5f,0); tray.sizeDelta = new Vector2(-48,700); tray.anchoredPosition = new Vector2(0,20);
            hud.anchorMin = new Vector2(0,1); hud.anchorMax = Vector2.one; hud.sizeDelta = new Vector2(-88,260);
            boardArea.anchorMin = new Vector2(0.02f,0.40f); boardArea.anchorMax = new Vector2(0.98f,0.84f);
            modeText.rectTransform.anchorMin = new Vector2(0.10f,0.815f); modeText.rectTransform.anchorMax = new Vector2(0.90f,0.85f);
            hint.rectTransform.anchorMin = new Vector2(0.06f,0.775f); hint.rectTransform.anchorMax = new Vector2(0.94f,0.813f);
        }
        brandText.fontSize=landscape ? 34 : 42;
        scoreText.fontSize=landscape ? 72 : 104;
        recordText.fontSize=landscape ? 34 : 38;
        Quaternion rotation = Quaternion.Euler(24,36,0);
        float half = Field3D.Size*field.CellSize*0.5f;
        float radius = Vector3.Scale(Vector3.one*half,field.transform.lossyScale).magnitude;
        float tan = Mathf.Tan(view.fieldOfView*Mathf.Deg2Rad*0.5f);
        float angle = Mathf.Atan(tan*Mathf.Min(landscape ? 0.52f : 0.44f,view.aspect*(landscape ? 0.46f : 0.90f)));
        float distance = radius/Mathf.Sin(angle);
        view.transform.rotation = rotation;
        float centerX = landscape ? 0.28f : 0.5f, centerY = landscape ? 0.40f : 0.62f;
        view.transform.position = field.transform.position-rotation*Vector3.forward*distance-
            rotation*Vector3.up*(2*distance*tan*(centerY-0.5f))-
            rotation*Vector3.right*(2*distance*tan*view.aspect*(centerX-0.5f));
        RenderSettings.ambientLight = new Color(0.62f,0.69f,0.65f);
        var rotator = field.GetComponent<FieldRotator>();
        if (rotator != null) rotator.RefreshZoomLimits();
    }

    private Button Button(string name,Transform parent,string title,Vector2 min,Vector2 max,Color background,Color foreground,UnityEngine.Events.UnityAction action)
    {
        var rect = Box(name,parent,min,max,background);
        var image = rect.GetComponent<Image>(); image.raycastTarget = true;
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        var colors = button.colors;
        colors.highlightedColor = new Color(0.97f,1f,0.98f); colors.pressedColor = new Color(0.83f,0.94f,0.88f);
        colors.selectedColor = Color.white; colors.disabledColor = new Color(0.65f,0.70f,0.78f,0.7f);
        button.colors = colors; button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(action);
        if (!string.IsNullOrEmpty(title)) Label("Label",rect,title,new Vector2(0.03f,0.02f),new Vector2(0.97f,0.98f),34,foreground,TextAnchor.MiddleCenter,true);
        return button;
    }
    private Text Label(string name,Transform parent,string value,Vector2 min,Vector2 max,int size,Color tint,TextAnchor alignment=TextAnchor.MiddleCenter,bool bold=false)
    {
        var rect = Rect(name,parent,min,max);
        var text = rect.gameObject.AddComponent<Text>();
        text.font = font; text.fontSize = size; text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        text.alignment = alignment; text.text = value; text.color = tint; text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }
    private RectTransform Box(string name,Transform parent,Vector2 min,Vector2 max,Color tint,Vector2 size=default,Vector2 position=default)
    {
        var rect = Rect(name,parent,min,max,size,position);
        var image = rect.gameObject.AddComponent<Image>(); image.sprite = rounded; image.type = Image.Type.Sliced;
        image.color = tint; image.raycastTarget = false;
        return rect;
    }
    private static RectTransform Rect(string name,Transform parent,Vector2 min,Vector2 max,Vector2 size=default,Vector2 position=default)
    {
        var rect = new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent,false); rect.anchorMin=min; rect.anchorMax=max; rect.sizeDelta=size; rect.anchoredPosition=position;
        return rect;
    }
    private void MakeRoundedSprite()
    {
        const int size=128; const float radius=24;
        roundedTexture = new Texture2D(size,size,TextureFormat.RGBA32,false) { name="HUD Rounded Panel",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp };
        var pixels = new Color[size*size];
        for (int y=0;y<size;y++) for (int x=0;x<size;x++)
        {
            float dx=Mathf.Max(Mathf.Abs(x+0.5f-size*0.5f)-(size*0.5f-radius),0);
            float dy=Mathf.Max(Mathf.Abs(y+0.5f-size*0.5f)-(size*0.5f-radius),0);
            pixels[y*size+x]=new Color(1,1,1,Mathf.Clamp01(radius-Mathf.Sqrt(dx*dx+dy*dy)));
        }
        roundedTexture.SetPixels(pixels); roundedTexture.Apply();
        rounded = Sprite.Create(roundedTexture,new Rect(0,0,size,size),new Vector2(0.5f,0.5f),100,0,SpriteMeshType.FullRect,new Vector4(32,32,32,32));
    }
    private void OnDestroy()
    {
        if (canvasObject != null) Destroy(canvasObject);
        if (rounded != null) Destroy(rounded);
        if (roundedTexture != null) Destroy(roundedTexture);
    }
}
