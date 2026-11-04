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
    [SetUp] public void SavePlayerPreferences() { PreferenceTestScope.Save("OfferTests."); }
    [UnityTearDown] public IEnumerator RestorePlayerPreferences()
    {
        if (Application.isPlaying) yield return new ExitPlayMode();
        PreferenceTestScope.Restore("OfferTests.");
    }

    [UnityTest]
    public IEnumerator VisibleOfferReceivesClickAndCreatesAnActiveFigure()
    {
        yield return new EnterPlayMode();
        yield return SceneManager.LoadSceneAsync("Game");
        yield return null;
        yield return null; // first offer set is generated after Start binding
        SkipGuideForTest();
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
        SkipGuideForTest();
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
        SkipGuideForTest();
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

    [UnityTest]
    public IEnumerator ReSelectingAnOfferDoesNotAutoRotateOrAvoidAnOccupiedStart()
    {
        yield return new EnterPlayMode();
        yield return SceneManager.LoadSceneAsync("Game");
        yield return null; yield return null;
        SkipGuideForTest();
        var managerType=Type.GetType("GameManager, Assembly-CSharp",true);
        var manager=UnityEngine.Object.FindObjectOfType(managerType);
        var controllerType=Type.GetType("ActiveFigureController, Assembly-CSharp",true);
        var controller=UnityEngine.Object.FindObjectOfType(controllerType);
        Assert.IsTrue((bool)managerType.GetMethod("TrySelectOffer").Invoke(manager,new object[] { 0 }));
        Vector3Int start=(Vector3Int)controllerType.GetProperty("CurrentGridPosition").GetValue(controller);
        var cells=(Vector3Int[])controllerType.GetMethod("GetRotatedCells").Invoke(controller,null);
        var data=controllerType.GetProperty("ActiveOffer").GetValue(controller);
        var offer=(ShapeOffer)data.GetType().GetField("offer").GetValue(data);
        int rotation=offer.Orientation;
        managerType.GetMethod("CancelSelection").Invoke(manager,null);
        var fieldType=Type.GetType("Field3D, Assembly-CSharp",true);
        var field=UnityEngine.Object.FindObjectOfType(fieldType);
        var board=(BoardModel)fieldType.GetProperty("Model").GetValue(field);
        var blocked=start+cells[0]; board.SetOccupied(blocked.x,blocked.y,blocked.z,true);
        Assert.IsTrue((bool)managerType.GetMethod("TrySelectOffer").Invoke(manager,new object[] { 0 }));
        yield return null;
        Assert.AreEqual(start,controllerType.GetProperty("CurrentGridPosition").GetValue(controller));
        data=controllerType.GetProperty("ActiveOffer").GetValue(controller);
        Assert.AreEqual(rotation,((ShapeOffer)data.GetType().GetField("offer").GetValue(data)).Orientation);
        Assert.IsFalse((bool)controllerType.GetMethod("CanPlaceAtCurrent").Invoke(controller,null));
        Assert.IsFalse(GameObject.Find("ConfirmPlacement").GetComponent<Button>().interactable);
        Assert.IsFalse((bool)managerType.GetProperty("IsGameOver").GetValue(manager));
        Assert.AreEqual(0,((RunStatistics)managerType.GetProperty("Statistics").GetValue(manager)).Moves);
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator XRayAndLayerFadeRestoreMaterialsColorsAndOccupancy()
    {
        yield return new EnterPlayMode();
        yield return SceneManager.LoadSceneAsync("Game");
        yield return null; yield return null;
        SkipGuideForTest();
        var managerType=Type.GetType("GameManager, Assembly-CSharp",true);
        var manager=UnityEngine.Object.FindObjectOfType(managerType);
        Assert.IsTrue((bool)managerType.GetMethod("TrySelectOffer").Invoke(manager,new object[] { 0 }));
        var controllerType=Type.GetType("ActiveFigureController, Assembly-CSharp",true);
        var controller=UnityEngine.Object.FindObjectOfType(controllerType);
        controllerType.GetMethod("SetDepth").Invoke(controller,new object[] { 3 });
        var frame=(PlacementFrame)controllerType.GetProperty("Frame").GetValue(controller);
        var anchor=(Vector3Int)controllerType.GetProperty("CurrentGridPosition").GetValue(controller);
        int selected=frame.DepthAxis==0 ? anchor.x : frame.DepthAxis==1 ? anchor.y : anchor.z;
        var fieldType=Type.GetType("Field3D, Assembly-CSharp",true);
        var field=(Component)UnityEngine.Object.FindObjectOfType(fieldType);
        var visibilityType=Type.GetType("BoardVisibility, Assembly-CSharp",true);
        var visibility=(Behaviour)field.GetComponent(visibilityType);
        var renderers=new Renderer[3]; var materials=new Material[3]; var coordinates=new Vector3Int[3];
        Color tint=new Color(0.2f,0.5f,0.7f,1);
        for (int i=0;i<3;i++)
        {
            int coordinate=selected+(1-i)*frame.NearSign;
            var c=PlacementFrame.WithCoordinate(new BoardModel.Cell(0,0,0),frame.DepthAxis,coordinate);
            coordinates[i]=new Vector3Int(c.x,c.y,c.z);
            var block=GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.transform.SetParent(field.transform,false);
            block.transform.localPosition=(Vector3)fieldType.GetMethod("GridToLocal").Invoke(field,new object[] { coordinates[i] });
            renderers[i]=block.GetComponent<Renderer>(); materials[i]=renderers[i].sharedMaterial;
            var properties=new MaterialPropertyBlock(); properties.SetColor("_Color",tint); renderers[i].SetPropertyBlock(properties);
            fieldType.GetMethod("SetCell").Invoke(field,new object[] { c.x,c.y,c.z,block.transform });
        }
        yield return null;
        var board=(BoardModel)fieldType.GetProperty("Model").GetValue(field);
        int free=board.CountFreeCells();
        Assert.AreEqual(BlockVisibilityRules.Style.FrontFade,visibilityType.GetMethod("GetCellStyle").Invoke(visibility,new object[] { coordinates[0] }));
        Assert.AreEqual(BlockVisibilityRules.Style.Solid,visibilityType.GetMethod("GetCellStyle").Invoke(visibility,new object[] { coordinates[1] }));
        Assert.AreEqual(BlockVisibilityRules.Style.Solid,visibilityType.GetMethod("GetCellStyle").Invoke(visibility,new object[] { coordinates[2] }));
        ClickVisibleButton(GameObject.Find("XRayToggle").GetComponent<Button>());
        yield return null;
        Assert.IsTrue((bool)visibilityType.GetProperty("XRayEnabled").GetValue(visibility));
        Assert.AreEqual(BlockVisibilityRules.Style.XRay,visibilityType.GetMethod("GetCellStyle").Invoke(visibility,new object[] { coordinates[0] }));
        Assert.AreEqual(BlockVisibilityRules.Style.Focus,visibilityType.GetMethod("GetCellStyle").Invoke(visibility,new object[] { coordinates[1] }));
        Assert.AreEqual(BlockVisibilityRules.Style.XRay,visibilityType.GetMethod("GetCellStyle").Invoke(visibility,new object[] { coordinates[2] }));
        Assert.AreEqual(free,board.CountFreeCells());
        ClickVisibleButton(GameObject.Find("XRayToggle").GetComponent<Button>());
        yield return null;
        visibility.enabled=false;
        for (int i=0;i<3;i++) Assert.AreEqual(materials[i],renderers[i].sharedMaterial);
        visibility.enabled=true;
        yield return null;
        Assert.AreEqual(BlockVisibilityRules.Style.FrontFade,visibilityType.GetMethod("GetCellStyle").Invoke(visibility,new object[] { coordinates[0] }));
        managerType.GetMethod("CancelSelection").Invoke(manager,null);
        yield return null;
        for (int i=0;i<3;i++)
        {
            Assert.AreEqual(materials[i],renderers[i].sharedMaterial);
            var properties=new MaterialPropertyBlock(); renderers[i].GetPropertyBlock(properties);
            Assert.AreEqual(tint,properties.GetColor("_Color"));
            Assert.AreEqual(UnityEngine.Rendering.ShadowCastingMode.On,renderers[i].shadowCastingMode);
        }
        Assert.AreEqual(free,board.CountFreeCells());
        fieldType.GetMethod("ClearCell").Invoke(field,new object[] { coordinates[0].x,coordinates[0].y,coordinates[0].z });
        Assert.AreEqual(BlockVisibilityRules.Style.Solid,visibilityType.GetMethod("GetCellStyle").Invoke(visibility,new object[] { coordinates[0] }));
        var planeType=fieldType.GetNestedType("Plane");
        var axisType=fieldType.GetNestedType("PlaneAxis");
        var axis=Enum.Parse(axisType,frame.DepthAxis==0 ? "YZ" : frame.DepthAxis==1 ? "XZ" : "XY");
        var plane=Activator.CreateInstance(planeType,new object[] { axis,selected });
        fieldType.GetMethod("ClearPlane").Invoke(field,new[] { plane });
        Assert.AreEqual(BlockVisibilityRules.Style.Solid,visibilityType.GetMethod("GetCellStyle").Invoke(visibility,new object[] { coordinates[1] }));
        Assert.AreEqual(free+2,board.CountFreeCells());
        yield return new ExitPlayMode();
    }

    private static void SkipGuideForTest()
    {
        var type=Type.GetType("GameManager, Assembly-CSharp",true);
        var game=UnityEngine.Object.FindObjectOfType(type);
        type.GetMethod("SkipTutorial").Invoke(game,new object[] { false });
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
