using UnityEngine;

/// <summary>Small reusable sound bank; gameplay audio follows the listener's pause and volume.</summary>
public sealed class GameFeedback : MonoBehaviour
{
    public enum Cue { Place, Clear, Combo, GameOver, Invalid }
    private static GameFeedback instance;
    private AudioSource source;
    private AudioClip[] clips;
    private float lastInvalid=-10;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ConfigureMobileFrameRate()
    { if (Application.isMobilePlatform) Application.targetFrameRate=60; }
    public static void Prepare()
    {
        if (instance!=null) return;
        var root=new GameObject("GameFeedback"); DontDestroyOnLoad(root);
        instance=root.AddComponent<GameFeedback>();
    }
    private void Awake()
    {
        instance=this;
        source=gameObject.AddComponent<AudioSource>(); source.playOnAwake=false; source.spatialBlend=0;
        source.ignoreListenerPause=false;
        clips=new[] {
            Tone("Block placed",.10f,new[] { 370f,494f }),
            Tone("Line clear",.28f,new[] { 523f,659f,784f }),
            Tone("Combo",.44f,new[] { 523f,659f,784f,1046f }),
            Tone("Game over",.45f,new[] { 392f,330f,262f }),
            Tone("Cannot place",.10f,new[] { 196f,175f }) };
    }
    public static void Play(Cue cue)
    {
        if (AudioListener.pause || Time.timeScale==0 || !Application.isFocused) return;
        Prepare();
        if (cue==Cue.Invalid)
        { if (Time.unscaledTime-instance.lastInvalid<.2f) return; instance.lastInvalid=Time.unscaledTime; }
        if (GamePreferences.SoundEnabled && GamePreferences.Volume>0) instance.source.PlayOneShot(instance.clips[(int)cue]);
        if (cue==Cue.Place) GameHaptics.Pulse(12);
        else if (cue==Cue.Clear) GameHaptics.Pulse(24);
        else if (cue==Cue.Combo) GameHaptics.Pulse(36);
    }
    private static AudioClip Tone(string name,float duration,float[] notes)
    {
        const int rate=22050;
        var samples=new float[Mathf.CeilToInt(rate*duration)];
        float noteDuration=duration/notes.Length;
        for (int i=0;i<samples.Length;i++)
        {
            float t=(float)i/rate;
            int note=Mathf.Min(notes.Length-1,(int)(t/noteDuration));
            float local=t-note*noteDuration;
            float envelope=Mathf.Min(1,local/.006f)*Mathf.Pow(Mathf.Max(0,1-local/noteDuration),1.5f);
            float phase=2*Mathf.PI*notes[note]*local;
            samples[i]=(.16f*Mathf.Sin(phase)+.035f*Mathf.Sin(phase*2))*envelope;
        }
        var clip=AudioClip.Create(name,samples.Length,1,rate,false); clip.SetData(samples,0); return clip;
    }
    private void OnDestroy()
    { if (clips!=null) foreach (var clip in clips) if (clip!=null) Destroy(clip); if (instance==this) instance=null; }
}

public static class GameHaptics
{
    private static bool unavailable;
    public static void Pulse(long milliseconds)
    {
        if (unavailable || !GamePreferences.HapticsEnabled || AudioListener.pause || !Application.isFocused) return;
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity=unity.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var vibrator=activity.Call<AndroidJavaObject>("getSystemService","vibrator"))
            {
                if (vibrator==null || !vibrator.Call<bool>("hasVibrator")) { unavailable=true; return; }
                using (var version=new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    if (version.GetStatic<int>("SDK_INT")>=26)
                    {
                        using (var effectClass=new AndroidJavaClass("android.os.VibrationEffect"))
                        using (var effect=effectClass.CallStatic<AndroidJavaObject>("createOneShot",milliseconds,-1))
                            vibrator.Call("vibrate",effect);
                    }
                    else vibrator.Call("vibrate",milliseconds);
                }
            }
        }
        catch (AndroidJavaException) { unavailable=true; } // Unsupported hardware/services are silent.
#endif
    }
}
