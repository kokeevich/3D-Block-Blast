using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Shared menu/modal surfaces, typography, safe areas and touch controls.</summary>
public sealed class ScreenWidgets : IDisposable
{
    public GameObject Object { get; }
    public RectTransform Root { get; }
    private readonly Font font;
    private readonly Sprite rounded;
    private readonly Texture2D texture;
    public ScreenWidgets(string name,int order)
    {
        font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        const int size=128; const float radius=24;
        texture=new Texture2D(size,size,TextureFormat.RGBA32,false) { filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp };
        var pixels=new Color[size*size];
        for (int y=0;y<size;y++) for (int x=0;x<size;x++)
        {
            float dx=Mathf.Max(Mathf.Abs(x+0.5f-size*0.5f)-(size*0.5f-radius),0);
            float dy=Mathf.Max(Mathf.Abs(y+0.5f-size*0.5f)-(size*0.5f-radius),0);
            pixels[y*size+x]=new Color(1,1,1,Mathf.Clamp01(radius-Mathf.Sqrt(dx*dx+dy*dy)));
        }
        texture.SetPixels(pixels); texture.Apply();
        rounded=Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(0.5f,0.5f),100,0,SpriteMeshType.FullRect,new Vector4(32,32,32,32));
        Object=new GameObject(name,typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        var canvas=Object.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=order;
        var scaler=Object.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1080,1920); scaler.matchWidthOrHeight=0;
        Root=RectTransform("SafeArea",Object.transform,Vector2.zero,Vector2.one); UpdateSafeArea();
    }
    public void UpdateSafeArea()
    {
        var safe=Screen.safeArea;
        Root.anchorMin=new Vector2(safe.xMin/Mathf.Max(1,Screen.width),safe.yMin/Mathf.Max(1,Screen.height));
        Root.anchorMax=new Vector2(safe.xMax/Mathf.Max(1,Screen.width),safe.yMax/Mathf.Max(1,Screen.height));
    }
    public static RectTransform RectTransform(string name,Transform parent,Vector2 min,Vector2 max)
    {
        var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent,false); rect.anchorMin=min; rect.anchorMax=max; rect.sizeDelta=Vector2.zero; return rect;
    }
    public RectTransform Panel(string name,Transform parent,Vector2 min,Vector2 max,Color tint,bool blockInput=false)
    {
        var rect=RectTransform(name,parent,min,max); var image=rect.gameObject.AddComponent<Image>();
        image.sprite=rounded; image.type=Image.Type.Sliced; image.color=tint; image.raycastTarget=blockInput; return rect;
    }
    public Text Label(string name,Transform parent,string value,Vector2 min,Vector2 max,int size,Color color,bool bold=false)
    {
        var rect=RectTransform(name,parent,min,max); var text=rect.gameObject.AddComponent<Text>();
        text.font=font; text.fontSize=size; text.color=color; text.text=value; text.fontStyle=bold ? FontStyle.Bold : FontStyle.Normal;
        text.alignment=TextAnchor.MiddleCenter; text.raycastTarget=false; return text;
    }
    public Button Button(string name,Transform parent,string title,Vector2 min,Vector2 max,UnityEngine.Events.UnityAction action,bool primary=false)
    {
        var rect=Panel(name,parent,min,max,primary ? GameTheme.Accent : GameTheme.SoftButton,true);
        var button=rect.gameObject.AddComponent<Button>(); button.targetGraphic=rect.GetComponent<Image>();
        button.navigation=new Navigation { mode=Navigation.Mode.None };
        button.onClick.AddListener(() => { action(); UIAudio.Click(); });
        Label("Label",rect,title,new Vector2(0.04f,0.02f),new Vector2(0.96f,0.98f),36,primary ? Color.white : GameTheme.Ink,true);
        return button;
    }
    public RectTransform Modal(string name,out RectTransform card)
    {
        var overlay=Panel(name,Root,Vector2.zero,Vector2.one,new Color(GameTheme.Background.r,GameTheme.Background.g,GameTheme.Background.b,0.97f),true);
        card=Panel("Card",overlay,new Vector2(0.08f,0.12f),new Vector2(0.92f,0.88f),GameTheme.Panel);
        return overlay;
    }
    public static void FitModal(RectTransform card)
    {
        bool wide=Screen.width>Screen.height;
        card.anchorMin=wide ? new Vector2(0.18f,0.035f) : new Vector2(0.08f,0.12f);
        card.anchorMax=wide ? new Vector2(0.82f,0.965f) : new Vector2(0.92f,0.88f);
    }
    public void Dispose()
    {
        if (Object!=null) UnityEngine.Object.Destroy(Object);
        if (rounded!=null) UnityEngine.Object.Destroy(rounded);
        if (texture!=null) UnityEngine.Object.Destroy(texture);
    }
}
