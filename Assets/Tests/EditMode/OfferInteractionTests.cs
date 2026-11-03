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
