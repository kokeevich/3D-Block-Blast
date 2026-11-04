Shader "BlockBlast/BoardTransparency"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,0.05)
        _EdgeAlpha ("Edge opacity", Range(0,1)) = 0.3
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Depth test", Float) = 4
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            ZTest [_ZTest]
            ZWrite Off
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            float _EdgeAlpha;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; };
            struct v2f { float4 position:SV_POSITION; float3 local:TEXCOORD0; float3 normal:TEXCOORD1; };
            v2f vert(appdata v)
            {
                v2f o;
                o.position=UnityObjectToClipPos(v.vertex);
                o.local=v.vertex.xyz;
                o.normal=UnityObjectToWorldNormal(v.normal);
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                // Primitive cube faces are at +/-0.5 in mesh coordinates.
                float3 d=abs(abs(i.local)-0.5);
                float edgeDistance=min(max(d.x,d.y),min(max(d.y,d.z),max(d.x,d.z)));
                float edge=1-smoothstep(0.012,0.012+max(fwidth(edgeDistance)*1.5,0.002),edgeDistance);
                float light=0.68+0.32*saturate(dot(normalize(i.normal),normalize(float3(0.4,0.8,-0.5))));
                return fixed4(_Color.rgb*light,lerp(_Color.a,_EdgeAlpha,edge));
            }
            ENDCG
        }
    }
}
