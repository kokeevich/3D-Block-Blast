using UnityEngine;

[System.Serializable]
public class ShapeInstanceData
{
    public string shapeName;
    public string category;
    public Color color = Color.white;
    [System.NonSerialized] public ShapeOffer offer;
    [System.NonSerialized] public BoardModel.Cell[] baseCells;

    [System.NonSerialized] public System.Action onRotationChanged;

    public Quaternion GetRotation()
    {
        var o = ShapeRules.Orientations[offer.Orientation];
        return Quaternion.LookRotation(new Vector3(o.Forward.x, o.Forward.y, o.Forward.z),
            new Vector3(o.Up.x, o.Up.y, o.Up.z));
    }
}
