using UnityEngine;
using UnityEngine.UI;

public sealed class SettingsView
{
    public GameObject Object { get; }
    private readonly RectTransform card;
    private readonly Button sound;
    private readonly Slider volume;
    private readonly Text volumeText;
    public bool Visible => Object.activeSelf;
    public SettingsView(ScreenWidgets ui,System.Action closed)
    {
        var overlay=ui.Modal("SettingsOverlay",out card); Object=overlay.gameObject;
        ui.Label("Title",card,"Настройки",new Vector2(0.08f,0.83f),new Vector2(0.92f,0.96f),48,GameTheme.Ink,true);
        ui.Label("Subtitle",card,"Удобный звук для вашей игры",new Vector2(0.08f,0.73f),new Vector2(0.92f,0.83f),30,GameTheme.Muted);
        sound=ui.Button("SoundToggle",card,"",new Vector2(0.10f,0.58f),new Vector2(0.90f,0.71f),() => GamePreferences.SetSound(!GamePreferences.SoundEnabled));
        volumeText=ui.Label("VolumeValue",card,"",new Vector2(0.1f,0.43f),new Vector2(0.9f,0.54f),32,GameTheme.Muted);
        var rect=ui.Panel("VolumeSlider",card,new Vector2(0.14f,0.30f),new Vector2(0.86f,0.39f),GameTheme.SoftButton,true);
        volume=rect.gameObject.AddComponent<Slider>(); volume.minValue=0; volume.maxValue=100; volume.wholeNumbers=true;
        var area=ScreenWidgets.RectTransform("HandleArea",rect,Vector2.zero,Vector2.one);
        area.offsetMin=new Vector2(40,0); area.offsetMax=new Vector2(-40,0);
        var handle=ui.Panel("Handle",area,Vector2.zero,new Vector2(0,1),GameTheme.Accent,true);
        handle.sizeDelta=new Vector2(80,0); volume.handleRect=handle; volume.targetGraphic=handle.GetComponent<Image>();
        volume.onValueChanged.AddListener(value => GamePreferences.SetVolume(value/100));
        ui.Button("SettingsBack",card,"Назад",new Vector2(0.10f,0.07f),new Vector2(0.90f,0.21f),() => { Hide(); closed(); },true);
        Object.SetActive(false);
    }
    public void Show() { Object.SetActive(true); Object.transform.SetAsLastSibling(); Refresh(); }
    public void Hide() { Object.SetActive(false); GamePreferences.Save(); }
    public void Refresh()
    {
        if (!Visible) return;
        ScreenWidgets.FitModal(card);
        sound.GetComponentInChildren<Text>().text=GamePreferences.SoundEnabled ? "Звук: включён" : "Звук: выключен";
        volume.SetValueWithoutNotify(GamePreferences.Volume*100);
        volumeText.text="Громкость: "+Mathf.RoundToInt(GamePreferences.Volume*100)+"%";
    }
}
