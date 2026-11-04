using Cell=BoardModel.Cell;

/// <summary>Presentation policy only: does not read or mutate board occupancy.</summary>
public static class BlockVisibilityRules
{
    public enum Style { Solid, FrontFade, XRay, Focus }
    public static Style Evaluate(Cell cell,PlacementFrame frame,Cell anchor,bool xray)
    {
        if (frame==null) return xray ? Style.XRay : Style.Solid;
        int delta=PlacementFrame.Coordinate(cell,frame.DepthAxis)-PlacementFrame.Coordinate(anchor,frame.DepthAxis);
        if (xray) return delta==0 ? Style.Focus : Style.XRay;
        return delta*frame.NearSign>0 ? Style.FrontFade : Style.Solid;
    }
    public static float FaceAlpha(Style style) => style==Style.FrontFade ? 0.035f : style==Style.XRay ? 0.055f : style==Style.Focus ? 0.20f : 1;
    public static float EdgeAlpha(Style style) => style==Style.FrontFade ? 0.16f : style==Style.XRay ? 0.30f : style==Style.Focus ? 0.80f : 1;
}
