using UnityEngine;

/// <summary>A quiet interface click; gameplay sounds/music remain separate.</summary>
public class UIAudio : MonoBehaviour
{
    private static UIAudio instance;
    private AudioSource source;
    private AudioClip clip;
    public static void Click()
    {
        if (!GamePreferences.SoundEnabled || GamePreferences.Volume<=0) return;
        if (instance==null)
        {
            var root=new GameObject("UIAudio");
            DontDestroyOnLoad(root); instance=root.AddComponent<UIAudio>();
        }
        instance.source.PlayOneShot(instance.clip);
    }
    private void Awake()
    {
        source=gameObject.AddComponent<AudioSource>(); source.playOnAwake=false;
        source.spatialBlend=0; source.ignoreListenerPause=true;
        const int rate=22050; var data=new float[1323];
        for (int i=0;i<data.Length;i++)
        {
            float t=(float)i/rate,fade=1-(float)i/data.Length;
            data[i]=Mathf.Sin(2*Mathf.PI*720*t)*fade*fade*Mathf.Min(1,t*700)*0.10f;
        }
        clip=AudioClip.Create("Soft UI Click",data.Length,1,rate,false); clip.SetData(data,0);
    }
    private void OnDestroy()
    { if (clip!=null) Destroy(clip); if (instance==this) instance=null; }
}
