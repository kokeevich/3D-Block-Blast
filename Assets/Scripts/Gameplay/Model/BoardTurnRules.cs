using System;

/// <summary>A released swipe selects exactly one world-axis quarter turn.</summary>
public static class BoardTurnRules
{
    public static bool TryGetTurn(float x,float y,float threshold,out int axis,out int direction)
    {
        axis=0; direction=0;
        if (Math.Max(Math.Abs(x),Math.Abs(y))<threshold) return false;
        if (Math.Abs(x)>=Math.Abs(y)) { axis=1; direction=x>0 ? -1 : 1; }
        else { axis=0; direction=y>0 ? 1 : -1; }
        return true;
    }
    public static int Apply(int orientation,int axis,int direction)
    {
        if (direction!=1 && direction!=-1) throw new ArgumentOutOfRangeException(nameof(direction));
        for (int i=0;i<(direction>0 ? 1 : 3);i++) orientation=ShapeRules.RotateOrientation(orientation,axis);
        return orientation;
    }
}
