#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// SessionState survives Enter/Exit Play Mode domain reloads. Restore after the
// scene's OnDestroy saves, so tests never replace the player's real preferences.
public static class PreferenceTestScope
{
    private static readonly string[] IntKeys={ "blockblast.rotation.free","blockblast.sound","blockblast.tutorial.completed","blockblast.best" };
    public static void Save(string scope)
    {
        SessionState.SetBool(scope+"run.present",PlayerPrefs.HasKey("blockblast.run.v1"));
        SessionState.SetString(scope+"run",PlayerPrefs.GetString("blockblast.run.v1",""));
        PlayerPrefs.DeleteKey("blockblast.run.v1");
        foreach (string key in IntKeys)
        { SessionState.SetBool(scope+key+".present",PlayerPrefs.HasKey(key)); SessionState.SetInt(scope+key,PlayerPrefs.GetInt(key)); }
        SessionState.SetBool(scope+"volume.present",PlayerPrefs.HasKey("blockblast.volume"));
        SessionState.SetFloat(scope+"volume",PlayerPrefs.GetFloat("blockblast.volume"));
    }
    public static void Restore(string scope)
    {
        if (SessionState.GetBool(scope+"run.present",false)) PlayerPrefs.SetString("blockblast.run.v1",SessionState.GetString(scope+"run",""));
        else PlayerPrefs.DeleteKey("blockblast.run.v1");
        foreach (string key in IntKeys)
        { if (SessionState.GetBool(scope+key+".present",false)) PlayerPrefs.SetInt(key,SessionState.GetInt(scope+key,0)); else PlayerPrefs.DeleteKey(key); }
        if (SessionState.GetBool(scope+"volume.present",false)) PlayerPrefs.SetFloat("blockblast.volume",SessionState.GetFloat(scope+"volume",0));
        else PlayerPrefs.DeleteKey("blockblast.volume");
        var launch=System.Type.GetType("GameLaunch, Assembly-CSharp");
        if (launch!=null) launch.GetMethod("RequestTutorial").Invoke(null,new object[] { false });
        PlayerPrefs.Save(); Time.timeScale=1; AudioListener.pause=false;
        AudioListener.volume=PlayerPrefs.GetInt("blockblast.sound",1)!=0 ? PlayerPrefs.GetFloat("blockblast.volume",.8f) : 0;
    }
}
#endif
