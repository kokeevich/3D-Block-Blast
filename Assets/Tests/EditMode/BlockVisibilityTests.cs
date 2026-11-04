using NUnit.Framework;
using Cell=BoardModel.Cell;
using Direction=PlacementFrame.Direction;
using Style=BlockVisibilityRules.Style;

public class BlockVisibilityTests
{
    [TestCase(0,1)] [TestCase(0,-1)] [TestCase(1,1)] [TestCase(1,-1)] [TestCase(2,1)] [TestCase(2,-1)]
    public void OnlyLayersBetweenCameraAndSelectionFade(int axis,int sign)
    {
        var normal=axis==0 ? new Direction(sign,0,0) : axis==1 ? new Direction(0,sign,0) : new Direction(0,0,sign);
        var frame=new PlacementFrame(normal,new Direction(1,0,0),new Direction(0,1,0));
        for (int layer=0;layer<8;layer++)
        for (int cell=0;cell<8;cell++)
        {
            var anchor=PlacementFrame.WithCoordinate(new Cell(3,3,3),axis,layer);
            var position=PlacementFrame.WithCoordinate(new Cell(2,2,2),axis,cell);
            var expected=(cell-layer)*sign>0 ? Style.FrontFade : Style.Solid;
            Assert.AreEqual(expected,BlockVisibilityRules.Evaluate(position,frame,anchor,false));
        }
    }
    [TestCase(0,1)] [TestCase(0,-1)] [TestCase(1,1)] [TestCase(1,-1)] [TestCase(2,1)] [TestCase(2,-1)]
    public void XRayKeepsTheSelectedLayerDistinct(int axis,int sign)
    {
        var normal=axis==0 ? new Direction(sign,0,0) : axis==1 ? new Direction(0,sign,0) : new Direction(0,0,sign);
        var frame=new PlacementFrame(normal,new Direction(1,0,0),new Direction(0,1,0));
        for (int layer=0;layer<8;layer++)
        for (int cell=0;cell<8;cell++)
        {
            var anchor=PlacementFrame.WithCoordinate(new Cell(3,3,3),axis,layer);
            var position=PlacementFrame.WithCoordinate(new Cell(2,2,2),axis,cell);
            Assert.AreEqual(cell==layer ? Style.Focus : Style.XRay,BlockVisibilityRules.Evaluate(position,frame,anchor,true));
        }
    }
    [Test] public void CancelAndSwitchOffReturnToSolidAndBrowsingXRayDoesNotSelectALayer()
    {
        var position=new Cell(3,3,3);
        Assert.AreEqual(Style.XRay,BlockVisibilityRules.Evaluate(position,null,position,true));
        Assert.AreEqual(Style.Solid,BlockVisibilityRules.Evaluate(position,null,position,false));
        Assert.Less(BlockVisibilityRules.FaceAlpha(Style.FrontFade),BlockVisibilityRules.FaceAlpha(Style.Focus));
        Assert.Less(BlockVisibilityRules.FaceAlpha(Style.XRay),BlockVisibilityRules.FaceAlpha(Style.Focus));
        foreach (Style style in System.Enum.GetValues(typeof(Style)))
            Assert.GreaterOrEqual(BlockVisibilityRules.EdgeAlpha(style),BlockVisibilityRules.FaceAlpha(style));
    }
}
