Shader "BlockBlast/PlacementGhost"
{
    Properties { _Color ("Color", Color) = (0.4, 0.95, 0.4, 0.55) }
    SubShader
    {
        Tags { "Queue"="Transparent+100" "RenderType"="Transparent" }
        Pass
        {
            // Target cells remain readable even when occupied blocks overlap them.
            ZTest Always
            ZWrite Off
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            float4 vert(float4 position : POSITION) : SV_POSITION
            {
                return UnityObjectToClipPos(position);
            }
            fixed4 frag() : SV_Target { return _Color; }
            ENDCG
        }
    }
}
