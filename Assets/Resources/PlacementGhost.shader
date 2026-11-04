Shader "BlockBlast/PlacementGhost"
{
    Properties { _Color ("Color", Color) = (0.08,0.68,0.43,0.55) }
    SubShader
    {
        Tags { "Queue"="Transparent+100" "RenderType"="Transparent" }
        Pass
        {
            ZTest Always
            ZWrite Off
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; };
            struct v2f { float4 position:SV_POSITION; float3 local:TEXCOORD0; float3 normal:TEXCOORD1; };
            v2f vert(appdata v)
            {
                v2f o; o.position=UnityObjectToClipPos(v.vertex);
                o.local=v.vertex.xyz; o.normal=UnityObjectToWorldNormal(v.normal); return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float3 d=abs(abs(i.local)-0.5);
                float edgeDistance=min(max(d.x,d.y),min(max(d.y,d.z),max(d.x,d.z)));
                float edge=1-smoothstep(0.012,0.012+max(fwidth(edgeDistance)*1.5,0.002),edgeDistance);
                float light=0.72+0.28*saturate(dot(normalize(i.normal),normalize(float3(0.4,0.8,-0.5))));
                return fixed4(_Color.rgb*light,lerp(_Color.a*0.35,min(1,_Color.a+0.30),edge));
            }
            ENDCG
        }
    }
}
