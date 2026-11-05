#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class SessionInterfaceTests
{
    [SetUp] public void SavePreferences()
    {
        PreferenceTestScope.Save("SessionTests.");
        PlayerPrefs.SetInt("blockblast.sound",0);
        PlayerPrefs.SetInt("blockblast.rotation.free",0);
        R("GameLaunch").GetMethod("RequestTutorial").Invoke(null,new object[] { false });
    }
    [UnityTearDown] public IEnumerator RestorePreferences()
    {
        if (Application.isPlaying) yield return new ExitPlayMode();
        PreferenceTestScope.Restore("SessionTests.");
    }

    private static RunSnapshot SavedRun() => (RunSnapshot)R("RunSaveStore").GetMethod("Load").Invoke(null,null);

    [UnityTest]
    public IEnumerator ConfirmedLineClearSurvivesSceneExitDuringAnimationExactlyOnce()
    {
        yield return new EnterPlayMode(); PlayerPrefs.SetInt("blockblast.tutorial.completed",1);
        yield return Load("Game");
        var selector=Find("BalancedShapeSelector"); var placer=Find("ShapePlacer"); var game=Find("GameManager");
        Call(selector,"RestoreOffers",new RunSnapshot { catalog=new[] { 0,1,2 },orientation=new[] { 0,0,0 },used=new bool[3] });
        for (int x=3;x<8;x++) Call(placer,"RestoreBlock",new RunSnapshot.Block { x=x,rgba=unchecked((int)0x12ab45ff) });
        Call(placer,"RestoreBlock",new RunSnapshot.Block { x=7,y=7,z=7,rgba=unchecked((int)0x123456ff) });
        Assert.IsTrue((bool)Call(game,"TrySelectOffer",0));
        Call(Find("ActiveFigureController"),"RestoreGridPosition",Vector3Int.zero);
        Call(placer,"TryPlaceActive");
        Assert.IsTrue(Get<bool>(placer,"IsResolving"));
        var checkpoint=SavedRun(); Assert.IsNotNull(checkpoint);
        Assert.AreEqual(13,checkpoint.score); Assert.AreEqual(1,checkpoint.moves); Assert.AreEqual(1,checkpoint.lines);
        Assert.AreEqual(9,checkpoint.peak); Assert.AreEqual(1,checkpoint.blocks.Length); Assert.AreEqual(0,checkpoint.consume);
        Call(game,"PauseGame"); Call(game,"ReturnToMenu"); yield return WaitForScene("MainMenu"); yield return null;
        Assert.AreEqual("Продолжить",GameObject.Find("PlayGame").GetComponentInChildren<Text>().text);
        Click("PlayGame"); yield return WaitForScene("Game"); yield return null; yield return null;
        game=Find("GameManager"); Assert.AreEqual(13,Get<int>(game,"Score"));
        Assert.AreEqual(1,Get<RunStatistics>(game,"Statistics").Moves);
        Assert.AreEqual(511,Get<BoardModel>(Find("Field3D"),"Model").CountFreeCells());
        Assert.IsFalse((bool)Call(Find("BalancedShapeSelector"),"IsShapeAvailable",0));
        var restored=SavedRun(); Assert.AreEqual(-1,restored.consume);
        Assert.AreEqual(unchecked((int)0x123456ff),restored.blocks[0].rgba);
        ForceGameOver(); yield return null; Assert.IsNull(SavedRun());
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator LastOfferCheckpointGeneratesOneNewSetAndTutorialPreservesIt()
    {
        yield return new EnterPlayMode(); PlayerPrefs.SetInt("blockblast.tutorial.completed",1);
        yield return Load("Game");
        Call(Find("BalancedShapeSelector"),"RestoreOffers",new RunSnapshot {
            catalog=new[] { 0,1,2 },orientation=new[] { 0,0,0 },used=new[] { false,true,true } });
        var game=Find("GameManager"); Assert.IsTrue((bool)Call(game,"TrySelectOffer",0));
        Call(Find("ShapePlacer"),"TryPlaceActive"); Call(game,"ReturnToMenu");
        yield return WaitForScene("MainMenu"); yield return null;
        Click("PlayGame"); yield return WaitForScene("Game"); yield return null; yield return null;
        var snapshot=SavedRun(); Assert.IsNotNull(snapshot);
        Assert.AreEqual(1,snapshot.placed); Assert.AreEqual(1,snapshot.moves);
        CollectionAssert.AreEqual(new bool[3],snapshot.used);
        Assert.AreEqual(3,snapshot.score); Assert.AreEqual(3,snapshot.blocks.Length);
        Call(Find("GameManager"),"ReturnToMenu"); yield return WaitForScene("MainMenu"); yield return null;
        string saved=PlayerPrefs.GetString("blockblast.run.v1");
        Click("MenuSettings"); yield return null; Click("LearnGame"); yield return WaitForScene("Game"); yield return null; yield return null;
        Call(Find("GameManager"),"ReturnToMenu"); yield return WaitForScene("MainMenu"); yield return null;
        Assert.AreEqual(saved,PlayerPrefs.GetString("blockblast.run.v1"));
        Click("PlayGame"); yield return WaitForScene("Game"); yield return null; yield return null;
        Call(Find("GameManager"),"RestartGame");
        int old=SceneManager.GetActiveScene().handle;
        yield return WaitForReplacement(old,"Game"); yield return null; yield return null;
        Assert.AreEqual(0,SavedRun().score); Assert.AreEqual(0,SavedRun().blocks.Length);
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator MenuRequiresConfirmationToReplaceSavedRun()
    {
        yield return new EnterPlayMode(); PlayerPrefs.SetInt("blockblast.tutorial.completed",1);
        yield return Load("Game");
        var game=Find("GameManager"); Assert.IsTrue((bool)Call(game,"TrySelectOffer",0));
        var actor=Find("ActiveFigureController"); var anchor=Get<Vector3Int>(actor,"CurrentGridPosition");
        Call(game,"ReturnToMenu"); yield return WaitForScene("MainMenu"); yield return null;
        var original=SavedRun(); Assert.AreEqual(0,original.selected);
        Click("NewGame"); yield return null; Click("KeepRun");
        Assert.AreEqual(original.selected,SavedRun().selected);
        Click("PlayGame"); yield return WaitForScene("Game"); yield return null; yield return null;
        Assert.IsTrue(Get<bool>(Find("ActiveFigureController"),"HasActiveFigure"));
        Assert.AreEqual(anchor,Get<Vector3Int>(Find("ActiveFigureController"),"CurrentGridPosition"));
        Call(Find("GameManager"),"ReturnToMenu"); yield return WaitForScene("MainMenu"); yield return null;
        Click("NewGame"); yield return null; Click("ConfirmNewRun"); yield return WaitForScene("Game"); yield return null; yield return null;
        Assert.AreEqual(0,Get<int>(Find("GameManager"),"Score"));
        Assert.IsFalse(Get<bool>(Find("ActiveFigureController"),"HasActiveFigure"));
        Assert.AreEqual(-1,SavedRun().selected);
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator RotationSettingPersistsAndFreeOrbitSnapsBackToAxes()
    {
        yield return new EnterPlayMode();
        PlayerPrefs.SetInt("blockblast.tutorial.completed",1);
        yield return Load("MainMenu");
        Click("MenuSettings"); yield return null; Click("RotationToggle");
        Assert.AreEqual(1,PlayerPrefs.GetInt("blockblast.rotation.free"));
        Click("SettingsBack"); Click("PlayGame"); yield return WaitForScene("Game"); yield return null;
        var rotator=(Component)Find("FieldRotator");
        Vector2 point=Camera.main.WorldToScreenPoint(rotator.transform.position);
        Quaternion initial=rotator.transform.rotation;
        CallPrivate(rotator,"Begin",point); CallPrivate(rotator,"Drag",point+new Vector2(43,27));
        CallPrivate(rotator,"End",point+new Vector2(43,27));
        Quaternion free=rotator.transform.rotation;
        Assert.Greater(Quaternion.Angle(initial,free),1);
        Assert.IsFalse(Get<bool>(rotator,"IsRotating"));
        Click("PauseGame"); yield return null;
        CallPrivate(rotator,"Begin",point); CallPrivate(rotator,"Drag",point+Vector2.right*100);
        Assert.Less(Quaternion.Angle(free,rotator.transform.rotation),.01f);
        Click("PauseSettings"); yield return null; Click("RotationToggle");
        Assert.AreEqual(0,PlayerPrefs.GetInt("blockblast.rotation.free"));
        Click("SettingsBack"); Click("Resume"); yield return null; yield return null;
        Quaternion snapped=rotator.transform.rotation;
        foreach (var axis in new[] { Vector3.right,Vector3.up,Vector3.forward })
        {
            Vector3 v=snapped*axis;
            Assert.Greater(Mathf.Max(Mathf.Abs(v.x),Mathf.Abs(v.y),Mathf.Abs(v.z)),.999f);
        }
        CallPrivate(rotator,"Begin",point); CallPrivate(rotator,"End",point+Vector2.right*100);
        float deadline=Time.realtimeSinceStartup+5;
        while (Get<bool>(rotator,"IsRotating") && Time.realtimeSinceStartup<deadline) yield return null;
        Assert.AreEqual(90,Quaternion.Angle(snapped,rotator.transform.rotation),.01f);
        Assert.AreEqual(0,Get<int>(Find("GameManager"),"Score"));
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator MenuSettingsPauseAndReturnPreserveSoundAndSelection()
    {
        yield return new EnterPlayMode();
        PlayerPrefs.SetInt("blockblast.tutorial.completed",1);
        yield return Load("MainMenu");
        Assert.IsNotNull(GameObject.Find("MainMenuHUD"));
        Click("MenuSettings"); yield return null;
        GameObject.Find("VolumeSlider").GetComponent<Slider>().value=35;
        Assert.AreEqual(.35f,PlayerPrefs.GetFloat("blockblast.volume"),.001f);
        Assert.AreEqual(0,AudioListener.volume);
        Click("SettingsBack"); Click("PlayGame"); yield return WaitForScene("Game"); yield return null;
        var game=Find("GameManager"); var actor=Find("ActiveFigureController");
        Assert.IsTrue((bool)Call(game,"TrySelectOffer",0)); yield return null;
        var position=Get<Vector3Int>(actor,"CurrentGridPosition");
        Click("PauseGame"); yield return null;
        Assert.IsTrue(Get<bool>(game,"IsPaused")); Assert.AreEqual(0,Time.timeScale);
        Call(actor,"SetGridPosition",position+(position.x<7 ? Vector3Int.right : Vector3Int.left));
        Call(Find("ShapePlacer"),"TryPlaceActive");
        yield return new WaitForSecondsRealtime(.2f);
        Assert.AreEqual(position,Get<Vector3Int>(actor,"CurrentGridPosition"));
        Assert.AreEqual(0,Get<int>(game,"Score"));
        Click("Resume"); yield return null;
        Assert.AreEqual(1,Time.timeScale); Assert.IsFalse(AudioListener.pause); Assert.AreEqual(0,AudioListener.volume);
        Assert.IsTrue(Get<bool>(actor,"HasActiveFigure"));
        Click("PauseGame"); yield return null; Click("PauseSettings"); yield return null;
        GameObject.Find("VolumeSlider").GetComponent<Slider>().value=50;
        Click("SoundToggle"); yield return null;
        Assert.AreEqual(.5f,AudioListener.volume,.001f); Assert.IsTrue(AudioListener.pause);
        Click("SettingsBack"); Click("PauseMenu"); yield return null;
        Assert.IsNotNull(GameObject.Find("ConfirmAbandon"));
        Click("Stay"); yield return null; Assert.IsTrue(Get<bool>(game,"IsPaused"));
        Click("PauseMenu"); yield return null; Click("Confirm"); yield return WaitForScene("MainMenu"); yield return null;
        Assert.AreEqual(1,Time.timeScale); Assert.IsFalse(AudioListener.pause); Assert.AreEqual(.5f,AudioListener.volume,.001f);
        Assert.AreEqual(1,PlayerPrefs.GetInt("blockblast.sound"));
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator PausingDuringPlacementFreezesAnimationAndSavesCommittedRecord()
    {
        yield return new EnterPlayMode();
        PlayerPrefs.SetInt("blockblast.best",0); PlayerPrefs.SetInt("blockblast.tutorial.completed",1);
        yield return Load("Game");
        var game=Find("GameManager"); var actor=Find("ActiveFigureController"); var placer=Find("ShapePlacer");
        Assert.IsTrue((bool)Call(game,"TrySelectOffer",0));
        int count=((Array)Call(actor,"GetRotatedCells")).Length;
        Call(placer,"TryPlaceActive"); Call(game,"PauseGame"); yield return null;
        var field=(Component)Find("Field3D");
        var block=field.GetComponentsInChildren<MeshRenderer>().First(r => r.name.StartsWith("Block_"));
        Vector3 scale=block.transform.localScale;
        yield return new WaitForSecondsRealtime(.25f);
        Assert.IsTrue(Get<bool>(placer,"IsResolving")); Assert.AreEqual(scale,block.transform.localScale);
        Assert.GreaterOrEqual(PlayerPrefs.GetInt("blockblast.best"),count);
        Call(game,"ResumeGame"); yield return WaitForResolution(placer);
        Assert.AreEqual(count,Get<int>(game,"Score"));
        Assert.AreEqual(1,Get<RunStatistics>(game,"Statistics").Moves);
        ForceGameOver(); yield return null;
        Assert.IsTrue(Get<bool>(game,"IsGameOver"));
        int old=SceneManager.GetActiveScene().handle; Click("Restart"); yield return WaitForReplacement(old,"Game"); yield return null;
        game=Find("GameManager"); Assert.AreEqual(0,Get<int>(game,"Score")); Assert.AreEqual(1,Time.timeScale);
        Assert.GreaterOrEqual(Get<int>(game,"BestScore"),count);
        ForceGameOver(); yield return null; Click("GameOverMenu"); yield return WaitForScene("MainMenu");
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator InteractiveGuideRequiresActualActionsAndSavesCompletion()
    {
        yield return new EnterPlayMode();
        PlayerPrefs.SetInt("blockblast.tutorial.completed",0);
        yield return Load("Game");
        var game=Find("GameManager"); var guide=Get<TutorialProgress>(game,"Tutorial");
        Assert.IsTrue(guide.IsIntro); Assert.IsFalse((bool)Call(game,"TrySelectOffer",0));
        Click("BeginTutorial"); yield return null;
        var rotator=(Component)Find("FieldRotator"); Vector2 point=Camera.main.WorldToScreenPoint(rotator.transform.position);
        CallPrivate(rotator,"Begin",point); CallPrivate(rotator,"End",point+Vector2.right*100);
        float deadline=Time.realtimeSinceStartup+5;
        while (Get<bool>(rotator,"IsRotating") && Time.realtimeSinceStartup<deadline) yield return null;
        Assert.AreEqual(TutorialProgress.Step.SelectShape,guide.Current);
        Click("OfferCard_0"); yield return null; Assert.AreEqual(TutorialProgress.Step.MoveShape,guide.Current);
        var actor=Find("ActiveFigureController"); var frame=Get<PlacementFrame>(actor,"Frame");
        var position=Get<Vector3Int>(actor,"CurrentGridPosition"); int axis=frame.HorizontalAxis;
        int coordinate=axis==0 ? position.x : axis==1 ? position.y : position.z;
        var delta=axis==0 ? Vector3Int.right : axis==1 ? Vector3Int.up : Vector3Int.forward;
        Call(actor,"SetGridPosition",position+delta*(coordinate<7 ? 1 : -1));
        Assert.AreEqual(TutorialProgress.Step.ChangeDepth,guide.Current);
        Click("Layer_"+((Get<int>(actor,"Depth")+1)%8)); yield return null;
        Assert.AreEqual(TutorialProgress.Step.PlaceShape,guide.Current);
        var board=Get<BoardModel>(Find("Field3D"),"Model");
        var cells=Array.ConvertAll((Vector3Int[])Call(actor,"GetRotatedCells"),c => new BoardModel.Cell(c.x,c.y,c.z));
        Assert.IsTrue(board.TryFindPlacement(cells,out var anchor));
        Call(actor,"SetGridPosition",new Vector3Int(anchor.x,anchor.y,anchor.z)); yield return null;
        Click("ConfirmPlacement"); yield return WaitForResolution(Find("ShapePlacer"));
        Assert.AreEqual(TutorialProgress.Step.Complete,guide.Current);
        Assert.AreEqual(1,PlayerPrefs.GetInt("blockblast.tutorial.completed"));
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator MenuCanReplayCompletedTutorialAndSkipIt()
    {
        yield return new EnterPlayMode(); PlayerPrefs.SetInt("blockblast.tutorial.completed",1);
        yield return Load("MainMenu"); Click("MenuSettings"); yield return null; Click("LearnGame"); yield return WaitForScene("Game"); yield return null;
        var game=Find("GameManager"); Assert.IsTrue(Get<TutorialProgress>(game,"Tutorial").IsIntro);
        Click("SkipTutorialIntro"); yield return null;
        Assert.IsFalse(Get<TutorialProgress>(game,"Tutorial").Active); Assert.IsFalse(Get<bool>(game,"InputBlocked"));
        yield return new ExitPlayMode();
    }

    private static Type R(string name) => Type.GetType(name+", Assembly-CSharp",true);
    private static object Find(string name) => UnityEngine.Object.FindObjectOfType(R(name));
    private static T Get<T>(object target,string property) => (T)target.GetType().GetProperty(property).GetValue(target);
    private static object Call(object target,string method,params object[] args) => target.GetType().GetMethod(method).Invoke(target,args);
    private static void CallPrivate(object target,string method,params object[] args) => target.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,args);
    private static IEnumerator Load(string name)
    { yield return SceneManager.LoadSceneAsync(name); yield return null; yield return null; }
    private static IEnumerator WaitForScene(string name)
    {
        float deadline=Time.realtimeSinceStartup+15;
        while (SceneManager.GetActiveScene().name!=name && Time.realtimeSinceStartup<deadline) yield return null;
        Assert.AreEqual(name,SceneManager.GetActiveScene().name);
        yield return null;
    }
    private static IEnumerator WaitForReplacement(int old,string name)
    {
        float deadline=Time.realtimeSinceStartup+15;
        while (SceneManager.GetActiveScene().handle==old && Time.realtimeSinceStartup<deadline) yield return null;
        Assert.AreNotEqual(old,SceneManager.GetActiveScene().handle); Assert.AreEqual(name,SceneManager.GetActiveScene().name);
    }
    private static IEnumerator WaitForResolution(object placer)
    {
        float deadline=Time.realtimeSinceStartup+10;
        while (Get<bool>(placer,"IsResolving") && Time.realtimeSinceStartup<deadline) yield return null;
        Assert.IsFalse(Get<bool>(placer,"IsResolving")); yield return null;
    }
    private static void ForceGameOver()
    {
        var board=Get<BoardModel>(Find("Field3D"),"Model");
        for (int x=0;x<8;x++) for (int y=0;y<8;y++) for (int z=0;z<8;z++) board.SetOccupied(x,y,z,true);
        Call(Find("BalancedShapeSelector"),"GenerateThreeShapes",0);
    }
    private static void Click(string name)
    {
        Canvas.ForceUpdateCanvases(); var go=GameObject.Find(name); Assert.IsNotNull(go,name);
        var button=go.GetComponent<Button>(); Assert.IsTrue(button.interactable,name);
        var rect=button.GetComponent<RectTransform>();
        var data=new PointerEventData(EventSystem.current) { position=rect.TransformPoint(rect.rect.center),button=PointerEventData.InputButton.Left };
        var hits=new List<RaycastResult>(); EventSystem.current.RaycastAll(data,hits);
        Assert.IsNotEmpty(hits,name); Assert.AreEqual(button,hits[0].gameObject.GetComponentInParent<Button>(),name);
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,data,ExecuteEvents.pointerClickHandler);
    }
}
#endif
