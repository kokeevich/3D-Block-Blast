using System.Collections.Generic;
using UnityEngine;

/// <summary>Quiet outer frame, a floor reference and only the currently selected layer.</summary>
public class VoxelField : MonoBehaviour
{
    [SerializeField] private bool visible = true;
    private Field3D field;
    private ActiveFigureController figure;
    private Mesh frameMesh, floorMesh, layerMesh;
    private MeshRenderer frameRenderer, floorRenderer, layerRenderer;
    private readonly List<Material> materials = new List<Material>();
    private int layerKey = int.MinValue;
    private float Half => Field3D.Size*field.CellSize*0.5f;

    private void Start()
    {
        field = GetComponent<Field3D>();
        figure = FindObjectOfType<ActiveFigureController>();
        HideLegacyFieldModel();
        frameMesh = CreateMesh("Board Frame",new Color(0.42f,0.56f,0.73f,0.72f),out frameRenderer);
        floorMesh = CreateMesh("Floor Guide",new Color(0.28f,0.39f,0.55f,0.22f),out floorRenderer);
        layerMesh = CreateMesh("Active Layer",new Color(0.32f,0.91f,0.78f,0.5f),out layerRenderer);
        var vertices = new List<Vector3>();
        for (int axis=0;axis<3;axis++)
        for (int a=-1;a<=1;a+=2)
        for (int b=-1;b<=1;b+=2)
            Line(vertices,Point(axis,-Half,a*Half,b*Half),Point(axis,Half,a*Half,b*Half));
        Upload(frameMesh,vertices);
        vertices.Clear();
        for (int i=0;i<=Field3D.Size;i++)
        {
            float p=-Half+i*field.CellSize;
            Line(vertices,new Vector3(-Half,-Half,p),new Vector3(Half,-Half,p));
            Line(vertices,new Vector3(p,-Half,-Half),new Vector3(p,-Half,Half));
        }
        Upload(floorMesh,vertices);
        SetVisibility(visible);
    }
    private void HideLegacyFieldModel()
    {
        if (transform.parent == null) return;
        foreach (var renderer in transform.parent.GetComponentsInChildren<Renderer>(true))
            if (!renderer.transform.IsChildOf(transform)) renderer.enabled=false;
    }
    private void LateUpdate()
    {
        if (layerRenderer == null) return;
        bool active=visible && figure != null && figure.HasActiveFigure && figure.Frame != null;
        layerRenderer.enabled=active;
        if (!active) { layerKey=int.MinValue; return; }
        int axis=figure.Frame.DepthAxis;
        Vector3Int position=figure.CurrentGridPosition;
        int coordinate=axis==0 ? position.x : axis==1 ? position.y : position.z;
        int key=axis*100+coordinate+(figure.Frame.NearSign>0 ? 1000 : 0);
        if (key==layerKey) return;
        layerKey=key;
        float layer=-Half+(coordinate+0.5f+figure.Frame.NearSign*0.5f)*field.CellSize;
        var vertices=new List<Vector3>();
        for (int i=0;i<=Field3D.Size;i++)
        {
            float p=-Half+i*field.CellSize;
            Line(vertices,Point(axis,layer,-Half,p),Point(axis,layer,Half,p));
            Line(vertices,Point(axis,layer,p,-Half),Point(axis,layer,p,Half));
        }
        Upload(layerMesh,vertices);
    }
    private Mesh CreateMesh(string name,Color tint,out MeshRenderer renderer)
    {
        var child=new GameObject(name); child.transform.SetParent(transform,false);
        var mesh=new Mesh { name=name };
        child.AddComponent<MeshFilter>().sharedMesh=mesh;
        renderer=child.AddComponent<MeshRenderer>();
        var material=new Material(Shader.Find("Sprites/Default")) { name=name+" Material",color=tint };
        materials.Add(material); renderer.sharedMaterial=material;
        renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows=false;
        return mesh;
    }
    private static Vector3 Point(int axis,float depth,float a,float b) => axis==0 ? new Vector3(depth,a,b) :
        axis==1 ? new Vector3(a,depth,b) : new Vector3(a,b,depth);
    private static void Line(List<Vector3> vertices,Vector3 a,Vector3 b) { vertices.Add(a); vertices.Add(b); }
    private static void Upload(Mesh mesh,List<Vector3> vertices)
    {
        mesh.Clear(); mesh.SetVertices(vertices);
        var indices=new int[vertices.Count]; for (int i=0;i<indices.Length;i++) indices[i]=i;
        mesh.SetIndices(indices,MeshTopology.Lines,0); mesh.RecalculateBounds();
    }
    public void SetVisibility(bool value)
    {
        visible=value;
        if (frameRenderer != null) frameRenderer.enabled=value;
        if (floorRenderer != null) floorRenderer.enabled=value;
        if (layerRenderer != null) layerRenderer.enabled=false;
    }
    private void OnDestroy()
    {
        if (frameMesh != null) Destroy(frameMesh);
        if (floorMesh != null) Destroy(floorMesh);
        if (layerMesh != null) Destroy(layerMesh);
        foreach (var material in materials) if (material != null) Destroy(material);
    }
}
