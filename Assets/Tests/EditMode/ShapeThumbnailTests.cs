#if UNITY_EDITOR
using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class ShapeThumbnailTests
{
    [Test]
    public void SingleCubeHasACompleteCenteredIsometricSilhouette()
    {
        CheckMesh(new[] { new BoardModel.Cell(0,0,0) },mesh =>
        {
            // A complete projected cube has width sqrt(3), height 2. Drawing
            // the rear faces omitted its lowest vertex and shifted it upward.
            Assert.AreEqual(0,mesh.bounds.center.x,0.01f);
            Assert.AreEqual(0,mesh.bounds.center.y,0.01f);
            Assert.AreEqual(0.866f,mesh.bounds.size.x/mesh.bounds.size.y,0.001f);
            Assert.Greater(mesh.bounds.size.y,80);
        });
    }

    [Test]
    public void TallAndWideOffersFitInsideTheCardWithPadding()
    {
        var shape=new[] { new BoardModel.Cell(0,0,0),new BoardModel.Cell(1,0,0),
            new BoardModel.Cell(2,0,0),new BoardModel.Cell(0,1,0),new BoardModel.Cell(0,2,0) };
        for (int rotation=0;rotation<24;rotation++)
            CheckMesh(ShapeRules.Rotate(shape,rotation),mesh =>
            {
                Assert.LessOrEqual(mesh.bounds.max.x,120-10);
                Assert.GreaterOrEqual(mesh.bounds.min.x,-120+10);
                Assert.LessOrEqual(mesh.bounds.max.y,80-10);
                Assert.GreaterOrEqual(mesh.bounds.min.y,-80+10);
                Assert.AreEqual(0,mesh.bounds.center.x,0.01f);
                Assert.AreEqual(0,mesh.bounds.center.y,0.01f);
            });
    }
    private static void CheckMesh(BoardModel.Cell[] cells,Action<Mesh> assert)
    {
        var type=Type.GetType("ShapeThumbnailGraphic, Assembly-CSharp",true);
        var go=new GameObject("ThumbnailTest",typeof(RectTransform),typeof(CanvasRenderer));
        var mesh=new Mesh();
        try
        {
            go.GetComponent<RectTransform>().sizeDelta=new Vector2(240,160);
            var graphic=(Graphic)go.AddComponent(type);
            type.GetMethod("SetShape").Invoke(graphic,new object[] { cells,Color.white });
            using (var vertices=new VertexHelper())
            {
                type.GetMethod("OnPopulateMesh",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(graphic,new object[] { vertices });
                vertices.FillMesh(mesh);
            }
            mesh.RecalculateBounds();
            Assert.IsFalse(graphic.raycastTarget,"Decorative art must not intercept card clicks.");
            assert(mesh);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(mesh);
            UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
#endif
