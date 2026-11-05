using System;
using System.Collections.Generic;

/// <summary>Versioned checkpoint; a pending confirmed move is resumed after its clears.</summary>
[Serializable]
public sealed class RunSnapshot
{
    [Serializable] public sealed class Block { public int x,y,z,rgba; }
    public int version=1,score,moves,lines,peak,placed;
    public Block[] blocks;
    public int[] catalog,orientation;
    public bool[] used;
    public int consume=-1,selected=-1,anchorX,anchorY,anchorZ,tutorial;
    public float[] rotation;
    public float distance,ortho;
    public bool IsValid()
    {
        if (version!=1 || blocks==null || blocks.Length>512 || score<0 || moves<0 || lines<0 ||
            peak<blocks.Length || peak>512 || placed<0 || moves!=placed+(consume>=0 ? 1 : 0) ||
            catalog==null || orientation==null || used==null || catalog.Length!=3 || orientation.Length!=3 || used.Length!=3 ||
            consume< -1 || consume>2 || selected< -1 || selected>2 || tutorial<0 || tutorial>7 ||
            rotation==null || rotation.Length!=4 || !Finite(distance) || distance<=0 || !Finite(ortho) || ortho<=0) return false;
        float norm=0;
        foreach (float v in rotation) { if (!Finite(v)) return false; norm+=v*v; }
        if (norm<.99f || norm>1.01f) return false;
        var cells=new HashSet<int>();
        foreach (var b in blocks)
            if (b==null || b.x<0 || b.x>=8 || b.y<0 || b.y>=8 || b.z<0 || b.z>=8 || !cells.Add((b.x*8+b.y)*8+b.z)) return false;
        bool available=false;
        for (int i=0;i<3;i++)
        {
            if (catalog[i]<0 || catalog[i]>=BalanceRules.Catalog.Count || orientation[i]<0 || orientation[i]>=ShapeRules.Orientations.Count) return false;
            available|=!used[i];
        }
        return available && (consume<0 || !used[consume]) && (selected<0 || (!used[selected] && consume<0 &&
            anchorX>=0 && anchorX<8 && anchorY>=0 && anchorY<8 && anchorZ>=0 && anchorZ<8));
    }
    private static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
}
