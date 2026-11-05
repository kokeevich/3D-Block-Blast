#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
#if UNITY_ANDROID
using System.Xml;
using UnityEditor.Android;
#endif

public static class AndroidDevelopmentBuild
{
    [MenuItem("Tools/Block Blast/Build Android Development APK")]
    public static void Build()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android,BuildTarget.Android))
            throw new BuildFailedException("Install Android Build Support, SDK/NDK and OpenJDK for this Unity version.");
        if (EditorUserBuildSettings.activeBuildTarget!=BuildTarget.Android)
            throw new BuildFailedException("Select Android and Switch Platform in Build Settings first (batch mode: -buildTarget Android).");
        string root=Directory.GetParent(Application.dataPath).FullName;
        string output=Path.Combine(root,"Builds/Android/3DBlockBlast-development.apk");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        bool bundle=EditorUserBuildSettings.buildAppBundle;
        bool export=EditorUserBuildSettings.exportAsGoogleAndroidProject;
        try
        {
            EditorUserBuildSettings.buildAppBundle=false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject=false;
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes=new[] { "Assets/Scenes/MainMenu.unity","Assets/Scenes/Game.unity" },
                locationPathName=output,target=BuildTarget.Android,options=BuildOptions.Development });
            if (report.summary.result!=BuildResult.Succeeded)
                throw new BuildFailedException("Android Development Build failed: "+report.summary.result);
            Debug.Log("Android Development APK: "+output+" ("+report.summary.totalSize+" bytes)");
        }
        finally
        {
            EditorUserBuildSettings.buildAppBundle=bundle;
            EditorUserBuildSettings.exportAsGoogleAndroidProject=export;
        }
    }
}

#if UNITY_ANDROID
/// <summary>Add the normal vibration permission without replacing Unity's activity manifest.</summary>
public sealed class HapticsManifest : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 0;
    public void OnPostGenerateGradleAndroidProject(string path)
    {
        const string android="http://schemas.android.com/apk/res/android";
        string file=Path.Combine(path,"src/main/AndroidManifest.xml");
        var doc=new XmlDocument(); doc.Load(file);
        var manifest=doc.DocumentElement;
        foreach (XmlNode child in manifest.ChildNodes)
            if (child.Name=="uses-permission" && child.Attributes?["name",android]?.Value=="android.permission.VIBRATE") return;
        var permission=doc.CreateElement("uses-permission");
        var name=doc.CreateAttribute("android","name",android); name.Value="android.permission.VIBRATE";
        permission.Attributes.Append(name); manifest.AppendChild(permission); doc.Save(file);
    }
}
#endif
#endif
