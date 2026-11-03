using System.Collections.Generic;
using NUnit.Framework;
using Cell = BoardModel.Cell;
using Direction = PlacementFrame.Direction;

public class PlacementFrameTests
{
    [TestCase(0, 1)] [TestCase(0, -1)] [TestCase(1, 1)]
    [TestCase(1, -1)] [TestCase(2, 1)] [TestCase(2, -1)]
    public void AllSixFacesHaveDistinctPlaneAxesAndEightDepthLayers(int axis, int sign)
    {
        var normal = Axis(axis, sign);
        var right = Axis((axis + 1) % 3, -1);
        var up = Axis((axis + 2) % 3, 1);
        var frame = new PlacementFrame(normal, right, up);
        Assert.AreEqual(axis, frame.DepthAxis);
        Assert.AreEqual(sign, frame.NearSign);
        Assert.AreEqual((axis + 1) % 3, frame.HorizontalAxis);
        Assert.AreEqual((axis + 2) % 3, frame.VerticalAxis);
        for (int depth = 0; depth < 8; depth++)
        {
            var at = frame.AtDepth(new Cell(3,3,3), depth);
            Assert.AreEqual(depth, frame.Depth(at));
            Assert.AreEqual(sign > 0 ? 7 - depth : depth, PlacementFrame.Coordinate(at, axis));
            var moved = frame.Move(at, 1, 1);
            Assert.AreEqual(depth, frame.Depth(moved));
            Assert.AreEqual(2, PlacementFrame.Coordinate(moved, frame.HorizontalAxis));
            Assert.AreEqual(4, PlacementFrame.Coordinate(moved, frame.VerticalAxis));
        }
    }

    [Test] public void ObliqueCameraChoosesNearestFaceAndScreenAlignedAxes()
    {
        var frame = new PlacementFrame(new Direction(0.3f,0.4f,-0.9f),
            new Direction(-0.95f,0.1f,-0.3f), new Direction(-0.1f,0.9f,0.4f));
        Assert.AreEqual(2, frame.DepthAxis);
        Assert.AreEqual(-1, frame.NearSign);
        Assert.AreEqual(0, frame.HorizontalAxis);
        Assert.AreEqual(-1, frame.HorizontalSign);
        Assert.AreEqual(1, frame.VerticalSign);
    }

    [Test] public void DepthAndMovementClampToBoard()
    {
        var frame = new PlacementFrame(new Direction(0,0,1), new Direction(1,0,0), new Direction(0,1,0));
        Assert.AreEqual(new Cell(7,0,7), frame.Move(new Cell(3,3,7), 99, -99));
        Assert.AreEqual(0, frame.Depth(frame.AtDepth(Cell.zero, -99)));
        Assert.AreEqual(7, frame.Depth(frame.AtDepth(Cell.zero, 99)));
    }

    [TestCase(0)] [TestCase(1)] [TestCase(2)]
    public void TwoPlaneRotationButtonsReachAll24Orientations(int depthAxis)
    {
        var seen = new HashSet<int> { 0 };
        var queue = new Queue<int>(); queue.Enqueue(0);
        while (queue.Count > 0)
        {
            int orientation = queue.Dequeue();
            for (int axis = 0; axis < 3; axis++)
            {
                if (axis == depthAxis) continue;
                int next = ShapeRules.RotateOrientation(orientation, axis);
                if (seen.Add(next)) queue.Enqueue(next);
            }
        }
        Assert.AreEqual(24, seen.Count);
    }
    private static Direction Axis(int axis, int sign) => axis == 0 ? new Direction(sign,0,0) :
        axis == 1 ? new Direction(0,sign,0) : new Direction(0,0,sign);
}
