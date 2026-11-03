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
        var card = GameObject.Find("OfferCard_0");
        var button = card != null ? card.GetComponent<Button>() : GameObject.Find("ButtonArea1").GetComponentInParent<Button>();
        Assert.IsNotNull(button);
        Assert.IsTrue(button.interactable);
        var rect = button.GetComponent<RectTransform>();
        var canvas = button.GetComponentInParent<Canvas>();
        var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        var screenPoint = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
        var data = new PointerEventData(EventSystem.current) { position = screenPoint, button = PointerEventData.InputButton.Left };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(data, hits);
        Assert.IsNotEmpty(hits, "The visible card must be raycastable.");
        Assert.AreEqual(button, hits[0].gameObject.GetComponentInParent<Button>(),
            "A transparent gesture region or decorative graphic swallowed the click.");
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, data, ExecuteEvents.pointerClickHandler);
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
        yield return new ExitPlayMode();
    }
}
#endif
