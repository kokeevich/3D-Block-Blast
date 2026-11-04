#if UNITY_EDITOR
using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneCleanupTests
{
    [Test]
    public void AuthoredGameSceneHasNoLegacyPresentationAndKeepsGameplayConnections()
    {
        const string path="Assets/Scenes/Game.unity";
        if (SceneManager.GetSceneByPath(path).isLoaded)
            Assert.Ignore("Open another scene before this asset test; do not replace or close the user's open Game scene.");
        var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try
        {
            var roots=scene.GetRootGameObjects();
            string[] legacy={ "Canvas","Background","GameInputBinder","GridManager","SwipeZoneFigure","SpawnPoint" };
            foreach (var name in legacy) Assert.IsFalse(roots.Any(go => go.name==name),name);
            Assert.IsEmpty(roots.SelectMany(go => go.GetComponentsInChildren<Canvas>(true)).ToArray());
            Assert.IsEmpty(roots.SelectMany(go => go.GetComponentsInChildren<Renderer>(true)).ToArray());
            foreach (var go in roots)
            {
                Assert.IsFalse(PrefabUtility.IsPartOfPrefabInstance(go));
                Assert.AreEqual(0,GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go));
            }
            var field=roots.Single(go => go.name=="GameFieldRoot");
            Assert.AreEqual(new Vector3(2,2.5f,-1.75f),field.transform.position);
            Assert.AreEqual(Vector3.one*0.5f,field.transform.lossyScale);
            Assert.IsNull(field.transform.parent);
            var fieldType=Type.GetType("Field3D, Assembly-CSharp",true);
            var fieldComponent=field.GetComponent(fieldType);
            Assert.IsNotNull(fieldComponent);
            var manager=roots.Single(go => go.name=="GameManager").GetComponent(Type.GetType("GameManager, Assembly-CSharp",true));
            var selector=roots.Single(go => go.name=="BalancedShapeSelector").GetComponent(Type.GetType("BalancedShapeSelector, Assembly-CSharp",true));
            var placer=roots.Single(go => go.name=="ShapePlacer").GetComponent(Type.GetType("ShapePlacer, Assembly-CSharp",true));
            Assert.AreEqual(3,selector.GetType().GetProperty("ButtonCount").GetValue(selector));
            foreach (var component in new[] { manager,selector,placer })
                Assert.AreEqual(fieldComponent,new SerializedObject(component).FindProperty("field").objectReferenceValue);
            Assert.AreEqual(placer,new SerializedObject(manager).FindProperty("shapePlacer").objectReferenceValue);
            Assert.AreEqual(field.transform,new SerializedObject(placer).FindProperty("blocksParent").objectReferenceValue);
            Assert.IsNotNull(new SerializedObject(placer).FindProperty("planeClearer").objectReferenceValue);
            var view=roots.Single(go => go.name=="Main Camera").GetComponent<Camera>();
            Assert.AreEqual(CameraClearFlags.SolidColor,view.clearFlags);
        }
        finally { EditorSceneManager.CloseScene(scene,true); }
    }
    [Test]
    public void MainMenuUsesTheCurrentControllerAndBothSceneRoutesAreBuilt()
    {
        const string path="Assets/Scenes/MainMenu.unity";
        if (SceneManager.GetSceneByPath(path).isLoaded) Assert.Ignore("Open a different scene before this asset-only test.");
        var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try
        {
            var roots=scene.GetRootGameObjects();
            Assert.IsEmpty(roots.SelectMany(go => go.GetComponentsInChildren<Canvas>(true)).ToArray());
            var controller=roots.Single(go => go.name=="MainMenuController");
            Assert.IsNotNull(controller.GetComponent(Type.GetType("MainMenuController, Assembly-CSharp",true)));
            var view=roots.Single(go => go.name=="Main Camera").GetComponent<Camera>();
            Assert.AreEqual(CameraClearFlags.SolidColor,view.clearFlags);
            foreach (var name in new[] { "MainMenu","Game" })
                Assert.IsTrue(EditorBuildSettings.scenes.Any(s => s.enabled && s.path=="Assets/Scenes/"+name+".unity"));
        }
        finally { EditorSceneManager.CloseScene(scene,true); }
    }
}
#endif
