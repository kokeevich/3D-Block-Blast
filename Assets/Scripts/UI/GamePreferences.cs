using UnityEngine;

public static class GamePreferences
{
    public const string SoundKey="blockblast.sound";
    public const string VolumeKey="blockblast.volume";
    public const string TutorialKey="blockblast.tutorial.completed";
    public const string BestKey="blockblast.best";
    public static bool SoundEnabled => PlayerPrefs.GetInt(SoundKey,1)!=0;
    public static float Volume => Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey,0.8f));
    public static bool TutorialCompleted => PlayerPrefs.GetInt(TutorialKey,0)!=0;
    public static int BestScore => Mathf.Max(0,PlayerPrefs.GetInt(BestKey,0));
    public static void ApplyAudio() { AudioListener.volume=SoundEnabled ? Volume : 0; }
    public static void SetSound(bool enabled)
    { PlayerPrefs.SetInt(SoundKey,enabled ? 1 : 0); ApplyAudio(); Save(); }
    public static void SetVolume(float value)
    { PlayerPrefs.SetFloat(VolumeKey,Mathf.Clamp01(value)); ApplyAudio(); }
    public static void CompleteTutorial() { PlayerPrefs.SetInt(TutorialKey,1); Save(); }
    public static void RecordBest(int value)
    { if (value>BestScore) PlayerPrefs.SetInt(BestKey,value); }
    public static void Save() { PlayerPrefs.Save(); }
}

public static class GameLaunch
{
    private static bool tutorial;
    public static void RequestTutorial(bool value) { tutorial=value; }
    public static bool ConsumeTutorialRequest() { bool value=tutorial; tutorial=false; return value; }
}
