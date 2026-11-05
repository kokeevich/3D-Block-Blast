using UnityEngine;
using UnityEngine.UI;

public sealed class SettingsView
{
    public GameObject Object { get; }
    private readonly RectTransform card;
    private readonly Button sound,rotation;
    private readonly Slider volume;
    private readonly Text volumeText;
    public bool Visible => Object.activeSelf;
    public SettingsView(ScreenWidgets ui,System.Action closed,System.Action learn=null)
    {
        var overlay=ui.Modal("SettingsOverlay",out card); Object=overlay.gameObject;
        ui.Label("Title",card,"Настройки",new Vector2(0.08f,0.83f),new Vector2(0.92f,0.96f),48,GameTheme.Ink,true);
        ui.Label("Subtitle",card,"Звук и управление",new Vector2(0.08f,0.75f),new Vector2(0.92f,0.83f),30,GameTheme.Muted);
        rotation=ui.Button("RotationToggle",card,"",new Vector2(0.10f,0.60f),new Vector2(0.90f,0.73f),
            () => { GamePreferences.SetFreeRotation(!GamePreferences.FreeRotation); Refresh(); });
        sound=ui.Button("SoundToggle",card,"",new Vector2(0.10f,0.44f),new Vector2(0.90f,0.57f),() => GamePreferences.SetSound(!GamePreferences.SoundEnabled));
        volumeText=ui.Label("VolumeValue",card,"",new Vector2(0.1f,0.33f),new Vector2(0.9f,0.43f),32,GameTheme.Muted);
        var rect=ui.Panel("VolumeSlider",card,new Vector2(0.14f,0.23f),new Vector2(0.86f,0.31f),GameTheme.SoftButton,true);
        volume=rect.gameObject.AddComponent<Slider>(); volume.minValue=0; volume.maxValue=100; volume.wholeNumbers=true;
        var area=ScreenWidgets.RectTransform("HandleArea",rect,Vector2.zero,Vector2.one);
        area.offsetMin=new Vector2(40,0); area.offsetMax=new Vector2(-40,0);
        var handle=ui.Panel("Handle",area,Vector2.zero,new Vector2(0,1),GameTheme.Accent,true);
        handle.sizeDelta=new Vector2(80,0); volume.handleRect=handle; volume.targetGraphic=handle.GetComponent<Image>();
        volume.onValueChanged.AddListener(value => GamePreferences.SetVolume(value/100));
        ui.Button("SettingsBack",card,"Назад",new Vector2(0.10f,0.07f),new Vector2(0.90f,0.21f),() => { Hide(); closed(); },true);
        if (learn!=null)
        {
            // Keep replayable help accessible without another main-menu button.
            var soundRect=sound.GetComponent<RectTransform>();
            soundRect.anchorMin=new Vector2(.10f,.48f); soundRect.anchorMax=new Vector2(.90f,.58f);
            rect.anchorMin=new Vector2(.14f,.33f); rect.anchorMax=new Vector2(.86f,.40f);
            volumeText.rectTransform.anchorMin=new Vector2(.1f,.40f);
            volumeText.rectTransform.anchorMax=new Vector2(.9f,.45f);
            ui.Button("LearnGame",card,"Как играть",new Vector2(.10f,.22f),new Vector2(.90f,.31f),() => { Hide(); learn(); });
        }
        Object.SetActive(false);
    }
    public void Show() { Object.SetActive(true); Object.transform.SetAsLastSibling(); Refresh(); }
    public void Hide() { Object.SetActive(false); GamePreferences.Save(); }
    public void Refresh()
    {
        if (!Visible) return;
        ScreenWidgets.FitModal(card);
        rotation.GetComponentInChildren<Text>().text=GamePreferences.FreeRotation ? "Вращение: свободное 360°" : "Вращение: по осям 90°";
        sound.GetComponentInChildren<Text>().text=GamePreferences.SoundEnabled ? "Звук: включён" : "Звук: выключен";
        volume.SetValueWithoutNotify(GamePreferences.Volume*100);
        volumeText.text="Громкость: "+Mathf.RoundToInt(GamePreferences.Volume*100)+"%";
    }
}
