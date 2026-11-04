Shader "BlockBlast/LayerGuide"
{
    Properties { _Color ("Color", Color) = (0.08,0.58,0.42,0.35) }
    SubShader
    {
        Tags { "Queue"="Transparent+50" "RenderType"="Transparent" }
        Pass
        {
            ZTest Always
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            float4 vert(float4 position:POSITION):SV_POSITION { return UnityObjectToClipPos(position); }
            fixed4 frag():SV_Target { return _Color; }
            ENDCG
        }
    }
}
