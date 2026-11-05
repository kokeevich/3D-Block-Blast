#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
#if UNITY_ANDROID
using UnityEditor.Android;
#endif

/// <summary>Use Unity's icon APIs, including reimport, instead of relying on settings-file merge state.</summary>
public sealed class ApplyGameIcons : IPreprocessBuildWithReport
{
    public int callbackOrder => -100;
    [MenuItem("Tools/Block Blast/Apply New App Icon")]
    public static void Apply()
    {
        var legacy=Load("BlockBlastIconV2.png");
        var round=Load("BlockBlastRound.png");
        var background=Load("BlockBlastAdaptiveBackground.png");
        var foreground=Load("BlockBlastAdaptiveForeground.png");
        PlayerSettings.SetIcons(NamedBuildTarget.Unknown,new[] { legacy },IconKind.Any);
#if UNITY_ANDROID
        Assign(AndroidPlatformIconKind.Legacy,new[] { legacy });
        Assign(AndroidPlatformIconKind.Round,new[] { round });
        Assign(AndroidPlatformIconKind.Adaptive,new[] { background,foreground });
        AssetDatabase.SaveAssets();
        Debug.Log("New Block Blast icon applied: Default, Legacy, Round and Adaptive. Reopen Player Settings to refresh previews.");
#else
        AssetDatabase.SaveAssets();
        Debug.LogWarning("Default icon updated. Select Android in Build Settings, Switch Platform, then run Tools/Block Blast/Apply New App Icon again to assign all Android slots.");
#endif
    }
    private static Texture2D Load(string name)
    {
        string path="Assets/AppIcon/"+name;
        if (!System.IO.File.Exists(path))
            throw new InvalidOperationException("Missing new icon asset: "+path+". Update the complete Assets/AppIcon folder from GitHub, including .meta files.");
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture==null) throw new InvalidOperationException("Cannot import icon: "+path);
        return texture;
    }
#if UNITY_ANDROID
    private static void Assign(PlatformIconKind kind,Texture2D[] textures)
    {
        var slots=PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android,kind);
        if (slots.Length==0) throw new BuildFailedException("No Android icon slots for "+kind+". Check Android Build Support.");
        foreach (var slot in slots) slot.SetTextures(textures);
        PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android,kind,slots);
    }
#endif
    public void OnPreprocessBuild(BuildReport report)
    { if (report.summary.platform==BuildTarget.Android) Apply(); }
}
#endif
