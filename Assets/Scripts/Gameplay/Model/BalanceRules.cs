using System;
using System.Collections.Generic;
using Cell=BoardModel.Cell;

/// <summary>Deterministic difficulty stages, with the same catalog used by game and simulations.</summary>
public static class BalanceRules
{
    public sealed class Piece
    {
        public string Name { get; }
        private readonly Cell[] cells;
        public int Count => cells.Length;
        public Cell[] Cells => (Cell[])cells.Clone();
        internal Piece(string name,Cell[] cells) { Name=name; this.cells=cells; }
    }
    private static readonly Piece[] catalog=BuildCatalog();
    public static IReadOnlyList<Piece> Catalog => Array.AsReadOnly(catalog);
    public static int Stage(int placements) => placements<9 ? 0 : placements<27 ? 1 : 2;
    public static string StageName(int placements) => Stage(placements)==0 ? "Разминка" : Stage(placements)==1 ? "Темп" : "Вызов";
    public static bool Eligible(int size,int placements) => Stage(placements)==0 ? size>=3 && size<=8 :
        Stage(placements)==1 ? size>=5 && size<=12 : size>=7 && size<=16;
    public static float Weight(int size,int placements)
    {
        if (!Eligible(size,placements)) return 0;
        return Stage(placements)==0 ? (size<=4 ? 4 : size<=6 ? 3 : 1) :
            Stage(placements)==1 ? (size<=6 ? 1 : size<=8 ? 3 : 4) : (size<=8 ? 1 : size<=12 ? 3 : 2);
    }
    public static List<ShapeOffer> GenerateOffers(BoardModel board,int placements,Random random)
    {
        if (board==null) throw new ArgumentNullException(nameof(board));
        if (random==null) throw new ArgumentNullException(nameof(random));
        var pool=new List<int>();
        var rescue=new List<ShapeOffer>();
        for (int i=0;i<catalog.Length;i++)
            if (Eligible(catalog[i].Count,placements))
            { pool.Add(i); rescue.Add(new ShapeOffer(i,catalog[i].Cells,0)); }
        var offers=new List<ShapeOffer>();
        for (int slot=0;slot<3;slot++)
        {
            double total=0;
            foreach (int index in pool) total+=Weight(catalog[index].Count,placements);
            double pick=random.NextDouble()*total;
            int chosen=pool[pool.Count-1];
            foreach (int index in pool)
            { pick-=Weight(catalog[index].Count,placements); if (pick<=0) { chosen=index; break; } }
            pool.Remove(chosen); // distinct shapes in one set
            var offer=new ShapeOffer(chosen,catalog[chosen].Cells,random.Next(24));
            if (offer.TryFit(board,out var fitted,out _)) offer=fitted;
            offers.Add(offer);
        }
        // The fallback has the same difficulty constraints as the draw. Tiny
        // retired pieces cannot keep reviving a late game with isolated holes.
        OfferRules.EnsurePlayable(board,offers,rescue);
        return offers;
    }
    private static Piece[] BuildCatalog()
    {
        return new[]
        {
            new Piece("Тройка",Box(3,1,1)),
            new Piece("Малый угол",Cells(0,0,0, 1,0,0, 0,1,0)),
            new Piece("Квадрат",Box(2,2,1)),
            new Piece("Объёмный угол",Cells(0,0,0, 1,0,0, 0,1,0, 0,0,1)),
            new Piece("Пятёрка",Box(5,1,1)),
            new Piece("Большой угол",Cells(0,0,0, 1,0,0, 2,0,0, 0,1,0, 0,2,0)),
            new Piece("Пластина",Box(3,2,1)),
            new Piece("Ступень",Cells(0,0,0, 1,0,0, 2,0,0, 1,1,0, 2,1,0, 2,2,0)),
            new Piece("Тройной угол",Cells(0,0,0, 1,0,0, 2,0,0, 0,1,0, 0,2,0, 0,0,1, 0,0,2)),
            new Piece("Куб",Box(2,2,2)),
            new Piece("Длинная пластина",Box(4,2,1)),
            new Piece("Большой квадрат",Box(3,3,1)),
            new Piece("Куб с хвостом",Join(Box(2,2,2),Cells(2,0,0, 2,1,0))),
            new Piece("Мост",Join(Box(3,2,1),Cells(0,0,1, 2,0,1, 0,1,1, 2,1,1))),
            new Piece("Брусок",Box(3,2,2)),
            new Piece("Высокая ступень",Join(Box(3,2,2),Cells(1,2,0, 2,2,0, 1,2,1, 2,2,1))),
            new Piece("Двойная пластина",Box(4,2,2)),
            new Piece("Седло",Join(Box(3,1,3),Cells(0,1,0, 0,1,1, 0,1,2, 2,1,0, 2,1,1, 2,1,2)))
        };
    }
    private static Cell[] Box(int x,int y,int z)
    {
        var result=new List<Cell>();
        for (int a=0;a<x;a++) for (int b=0;b<y;b++) for (int c=0;c<z;c++) result.Add(new Cell(a,b,c));
        return result.ToArray();
    }
    private static Cell[] Cells(params int[] coordinates)
    {
        var result=new Cell[coordinates.Length/3];
        for (int i=0;i<result.Length;i++) result[i]=new Cell(coordinates[i*3],coordinates[i*3+1],coordinates[i*3+2]);
        return result;
    }
    private static Cell[] Join(Cell[] a,Cell[] b)
    { var result=new List<Cell>(a); result.AddRange(b); return result.ToArray(); }
}
