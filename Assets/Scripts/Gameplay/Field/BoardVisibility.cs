using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Style=BlockVisibilityRules.Style;

/// <summary>Shared translucent materials; original materials/properties are restored verbatim.</summary>
[RequireComponent(typeof(Field3D))]
public class BoardVisibility : MonoBehaviour
{
    private sealed class RendererState
    {
        public Renderer Renderer;
        public Material[] Original,Fade,XRay,Focus;
        public MaterialPropertyBlock Properties;
        public Color Color;
        public ShadowCastingMode Shadows;
    }
    private sealed class CellState
    {
        public readonly List<RendererState> Renderers=new List<RendererState>();
        public Style Style=Style.Solid;
    }
    private readonly Dictionary<Vector3Int,CellState> blocks=new Dictionary<Vector3Int,CellState>();
    private MaterialPropertyBlock display;
    private Field3D field;
    private ActiveFigureController figure;
    private Material fadeMaterial,xrayMaterial,focusMaterial;
    public bool XRayEnabled { get; private set; }
    public void SetXRay(bool value) { XRayEnabled=value; }
    public void ToggleXRay() { SetXRay(!XRayEnabled); }

    private void Awake()
    {
        display=new MaterialPropertyBlock();
        field=GetComponent<Field3D>();
        figure=FindObjectOfType<ActiveFigureController>();
        var shader=Resources.Load<Shader>("BoardTransparency");
        if (shader==null) { Debug.LogError("BoardTransparency shader is missing.",this); enabled=false; return; }
        fadeMaterial=Material(shader,"Front Layer Fade",CompareFunction.LessEqual,3000);
        xrayMaterial=Material(shader,"Board XRay",CompareFunction.Always,3000);
        focusMaterial=Material(shader,"Focused XRay Layer",CompareFunction.Always,3020);
    }
    private static Material Material(Shader shader,string name,CompareFunction depth,int queue)
    {
        var material=new Material(shader) { name=name,renderQueue=queue };
        material.SetInt("_ZTest",(int)depth);
        return material;
    }
    private void OnEnable()
    {
        if (field==null || fadeMaterial==null) return;
        field.OnCellChanged+=Register;
        for (int x=0;x<Field3D.Size;x++) for (int y=0;y<Field3D.Size;y++) for (int z=0;z<Field3D.Size;z++)
        {
            var cell=new Vector3Int(x,y,z); var block=field.GetCell(x,y,z);
            if (block!=null) Register(cell,block);
        }
    }
    private static Material[] Repeat(Material material,int count)
    { var result=new Material[count]; for (int i=0;i<count;i++) result[i]=material; return result; }
    private void Register(Vector3Int cell,Transform block)
    {
        if (blocks.TryGetValue(cell,out var old)) { Apply(old,Style.Solid); blocks.Remove(cell); }
        if (block==null) return;
        var state=new CellState();
        foreach (var renderer in block.GetComponentsInChildren<Renderer>(true))
        {
            var properties=new MaterialPropertyBlock(); renderer.GetPropertyBlock(properties);
            var originals=renderer.sharedMaterials;
            Color color=properties.GetColor("_Color");
            if (color.a<=0) color=properties.GetColor("_BaseColor");
            var source=renderer.sharedMaterial;
            if (color.a<=0) color=source!=null && source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") :
                source!=null && source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white;
            state.Renderers.Add(new RendererState { Renderer=renderer,Original=originals,Properties=properties,Color=color,
                Fade=Repeat(fadeMaterial,originals.Length),XRay=Repeat(xrayMaterial,originals.Length),
                Focus=Repeat(focusMaterial,originals.Length),Shadows=renderer.shadowCastingMode });
        }
        blocks[cell]=state;
    }
    public Style GetCellStyle(Vector3Int cell) => blocks.TryGetValue(cell,out var state) ? state.Style : Style.Solid;
    private void LateUpdate()
    {
        var frame=figure!=null && figure.HasActiveFigure ? figure.Frame : null;
        var position=frame!=null ? figure.CurrentGridPosition : Vector3Int.zero;
        var anchor=new BoardModel.Cell(position.x,position.y,position.z);
        foreach (var pair in blocks)
        {
            var c=pair.Key;
            var style=BlockVisibilityRules.Evaluate(new BoardModel.Cell(c.x,c.y,c.z),frame,anchor,XRayEnabled);
            Apply(pair.Value,style);
        }
    }
    private void Apply(CellState state,Style style)
    {
        if (state.Style==style) return;
        foreach (var item in state.Renderers)
        {
            if (item.Renderer==null) continue;
            if (style==Style.Solid)
            {
                item.Renderer.sharedMaterials=item.Original;
                item.Renderer.SetPropertyBlock(item.Properties);
                item.Renderer.shadowCastingMode=item.Shadows;
            }
            else
            {
                item.Renderer.sharedMaterials=style==Style.FrontFade ? item.Fade : style==Style.Focus ? item.Focus : item.XRay;
                display.Clear();
                var color=item.Color; color.a=BlockVisibilityRules.FaceAlpha(style);
                display.SetColor("_Color",color); display.SetFloat("_EdgeAlpha",BlockVisibilityRules.EdgeAlpha(style));
                item.Renderer.SetPropertyBlock(display);
                item.Renderer.shadowCastingMode=ShadowCastingMode.Off;
            }
        }
        state.Style=style;
    }
    private void OnDisable()
    {
        if (field!=null) field.OnCellChanged-=Register;
        foreach (var state in blocks.Values) Apply(state,Style.Solid);
        blocks.Clear();
    }
    private void OnDestroy()
    {
        if (fadeMaterial!=null) Destroy(fadeMaterial);
        if (xrayMaterial!=null) Destroy(xrayMaterial);
        if (focusMaterial!=null) Destroy(focusMaterial);
    }
}
