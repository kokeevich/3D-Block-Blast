using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Lightweight isometric offer art. It never participates in UI hit tests.</summary>
public class ShapeThumbnailGraphic : MaskableGraphic
{
    private BoardModel.Cell[] cells = new BoardModel.Cell[0];
    public void SetShape(BoardModel.Cell[] value, Color tint)
    {
        cells = value == null ? new BoardModel.Cell[0] : (BoardModel.Cell[])value.Clone();
        Array.Sort(cells, (a, b) =>
        {
            int order = (a.x + a.z).CompareTo(b.x + b.z);
            return order != 0 ? order : a.y.CompareTo(b.y);
        });
        color = tint;
        raycastTarget = false;
        SetVerticesDirty();
    }
    private static Vector2 Project(float x, float y, float z) => new Vector2((x-z)*0.866f, y-(x+z)*0.5f);
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if (cells.Length == 0) return;
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = -min;
        foreach (var c in cells)
            for (int x = 0; x <= 1; x++)
            for (int y = 0; y <= 1; y++)
            for (int z = 0; z <= 1; z++)
            { var p = Project(c.x+x, c.y+y, c.z+z); min = Vector2.Min(min,p); max = Vector2.Max(max,p); }
        var rect = GetPixelAdjustedRect();
        float scale = Mathf.Min((rect.width-16)/Mathf.Max(1,max.x-min.x), (rect.height-16)/Mathf.Max(1,max.y-min.y));
        Vector2 center = (min+max)*0.5f;
        foreach (var c in cells)
        {
            Vector2 p = (Project(c.x,c.y,c.z)-center)*scale + rect.center;
            Vector2 x = Project(1,0,0)*scale, y = Project(0,1,0)*scale, z = Project(0,0,1)*scale;
            Face(mesh, p, p+z, p+z+y, p+y, Shade(color,0.66f));
            Face(mesh, p, p+y, p+x+y, p+x, Shade(color,0.88f));
            Face(mesh, p+y, p+z+y, p+x+z+y, p+x+y, Shade(color,1.12f));
        }
    }
    private static Color Shade(Color c, float factor) => new Color(c.r*factor,c.g*factor,c.b*factor,c.a);
    private static void Face(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color tint)
    {
        AddQuad(mesh,a,b,c,d,new Color(0.03f,0.06f,0.12f,tint.a));
        Vector2 center = (a+b+c+d)*0.25f;
        AddQuad(mesh,Vector2.Lerp(a,center,0.065f),Vector2.Lerp(b,center,0.065f),
            Vector2.Lerp(c,center,0.065f),Vector2.Lerp(d,center,0.065f),tint);
    }
    private static void AddQuad(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color tint)
    {
        int start = mesh.currentVertCount;
        mesh.AddVert(a,tint,Vector2.zero); mesh.AddVert(b,tint,Vector2.zero);
        mesh.AddVert(c,tint,Vector2.zero); mesh.AddVert(d,tint,Vector2.zero);
        mesh.AddTriangle(start,start+1,start+2); mesh.AddTriangle(start,start+2,start+3);
    }
}
