#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class OfferInteractionTests
{
    [UnityTest]
    public IEnumerator VisibleOfferReceivesClickAndCreatesAnActiveFigure()
    {
        yield return new EnterPlayMode();
        yield return SceneManager.LoadSceneAsync("Game");
        yield return null;
        yield return null; // first offer set is generated after Start binding
        Canvas.ForceUpdateCanvases();

        var controllerType = Type.GetType("ActiveFigureController, Assembly-CSharp", true);
        var controller = UnityEngine.Object.FindObjectOfType(controllerType);
        Assert.IsNotNull(controller);
        Assert.IsFalse((bool)controllerType.GetProperty("HasActiveFigure").GetValue(controller));
        var button = GameObject.Find("OfferCard_0").GetComponent<Button>();
        ClickVisibleButton(button);
        yield return null;
        Assert.IsTrue((bool)controllerType.GetProperty("HasActiveFigure").GetValue(controller),
            "Clicking the visible offer must create an active figure.");
        Assert.IsTrue((bool)controllerType.GetMethod("CanPlaceAtCurrent").Invoke(controller, null));

        var managerType = Type.GetType("GameManager, Assembly-CSharp", true);
        var manager = UnityEngine.Object.FindObjectOfType(managerType);
        managerType.GetMethod("CancelSelection").Invoke(manager, null);
        yield return null;
        Assert.IsFalse((bool)controllerType.GetProperty("HasActiveFigure").GetValue(controller));
        Assert.IsTrue(button.interactable, "Cancellation must leave the offer selectable.");
        // Switching offers must not consume the previous offer.
        for (int index=0; index<3; index++)
        {
            ClickVisibleButton(GameObject.Find("OfferCard_"+index).GetComponent<Button>());
            yield return null;
            Assert.IsTrue((bool)controllerType.GetProperty("HasActiveFigure").GetValue(controller));
            Assert.IsTrue((bool)controllerType.GetMethod("CanPlaceAtCurrent").Invoke(controller,null));
        }
        int blocks = ((Array)controllerType.GetMethod("GetRotatedCells").Invoke(controller,null)).Length;
        ClickVisibleButton(GameObject.Find("ConfirmPlacement").GetComponent<Button>());
        var placerType = Type.GetType("ShapePlacer, Assembly-CSharp",true);
        var placer = UnityEngine.Object.FindObjectOfType(placerType);
        float deadline = Time.realtimeSinceStartup+5;
        while ((bool)placerType.GetProperty("IsResolving").GetValue(placer) && Time.realtimeSinceStartup<deadline) yield return null;
        Assert.IsFalse((bool)placerType.GetProperty("IsResolving").GetValue(placer));
        Assert.IsFalse((bool)controllerType.GetProperty("HasActiveFigure").GetValue(controller));
        Assert.GreaterOrEqual((int)managerType.GetProperty("Score").GetValue(manager),blocks);
        var selectorType = Type.GetType("BalancedShapeSelector, Assembly-CSharp",true);
        var selector = UnityEngine.Object.FindObjectOfType(selectorType);
        Assert.IsNull(selectorType.GetMethod("GetShape").Invoke(selector,new object[] { 2 }));
        Assert.IsNotNull(selectorType.GetMethod("GetShape").Invoke(selector,new object[] { 0 }));
        yield return new ExitPlayMode();
    }
    [UnityTest]
    public IEnumerator QuarterTurnSnapsToNinetyDegreesAndBlocksSelectionDuringAnimation()
    {
        yield return new EnterPlayMode();
        yield return SceneManager.LoadSceneAsync("Game");
        yield return null; yield return null;
        var type=Type.GetType("FieldRotator, Assembly-CSharp",true);
        var rotator=(Component)UnityEngine.Object.FindObjectOfType(type);
        var managerType=Type.GetType("GameManager, Assembly-CSharp",true);
        var manager=UnityEngine.Object.FindObjectOfType(managerType);
        Quaternion start=rotator.transform.rotation;
        Vector2 point=Camera.main.WorldToScreenPoint(rotator.transform.position);
        type.GetMethod("Begin",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(rotator,new object[] { point });
        type.GetMethod("End",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(rotator,new object[] { point+Vector2.right*100 });
        Assert.IsTrue((bool)type.GetProperty("IsRotating").GetValue(rotator));
        Assert.IsFalse((bool)managerType.GetMethod("TrySelectOffer").Invoke(manager,new object[] { 0 }));
        float deadline=Time.realtimeSinceStartup+2;
        while ((bool)type.GetProperty("IsRotating").GetValue(rotator) && Time.realtimeSinceStartup<deadline) yield return null;
        Assert.IsFalse((bool)type.GetProperty("IsRotating").GetValue(rotator));
        Assert.AreEqual(90,Quaternion.Angle(start,rotator.transform.rotation),0.01f);
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator BoardWithNoFittingChallengeOfferShowsGameOver()
    {
        yield return new EnterPlayMode();
        yield return SceneManager.LoadSceneAsync("Game");
        yield return null; yield return null;
        var fieldType=Type.GetType("Field3D, Assembly-CSharp",true);
        var field=UnityEngine.Object.FindObjectOfType(fieldType);
        var board=(BoardModel)fieldType.GetProperty("Model").GetValue(field);
        for (int x=0;x<8;x++) for (int y=0;y<8;y++) for (int z=0;z<8;z++) board.SetOccupied(x,y,z,true);
        for (int x=0;x<8;x+=2) for (int y=0;y<8;y+=2) for (int z=0;z<8;z+=2) board.ClearCell(x,y,z);
        var selectorType=Type.GetType("BalancedShapeSelector, Assembly-CSharp",true);
        var selector=UnityEngine.Object.FindObjectOfType(selectorType);
        selectorType.GetMethod("GenerateThreeShapes").Invoke(selector,new object[] { board.CountFreeCells() });
        yield return null;
        var managerType=Type.GetType("GameManager, Assembly-CSharp",true);
        var manager=UnityEngine.Object.FindObjectOfType(managerType);
        Assert.IsTrue((bool)managerType.GetProperty("IsGameOver").GetValue(manager));
        Assert.IsNotNull(GameObject.Find("GameOverOverlay"));
        yield return new ExitPlayMode();
    }

    private static void ClickVisibleButton(Button button)
    {
        Canvas.ForceUpdateCanvases();
        Assert.IsNotNull(button);
        Assert.IsTrue(button.interactable);
        var rect=button.GetComponent<RectTransform>();
        var canvas=button.GetComponentInParent<Canvas>();
        var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        var screenPoint=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center));
        var data=new PointerEventData(EventSystem.current) { position=screenPoint,button=PointerEventData.InputButton.Left };
        var hits=new List<RaycastResult>(); EventSystem.current.RaycastAll(data,hits);
        Assert.IsNotEmpty(hits,"The visible card must be raycastable.");
        Assert.AreEqual(button,hits[0].gameObject.GetComponentInParent<Button>(),
            "A gesture region or decorative graphic swallowed the click.");
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,data,ExecuteEvents.pointerClickHandler);
    }
}
#endif
