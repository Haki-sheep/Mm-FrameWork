Shader "Hidden/MieMieFrameWork/DynamicAtlasPadding"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always Blend Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment Fragment
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 AtlasLayout;

            float4 Fragment(v2f_img Input) : SV_Target
            {
                float2 SourceSize = abs(_MainTex_TexelSize.zw);
                float2 Uv = (Input.uv * AtlasLayout.xy - AtlasLayout.z) / SourceSize;
                float2 HalfTexel = 0.5 / SourceSize;
                Uv = clamp(Uv, HalfTexel, 1.0 - HalfTexel);
                return tex2D(_MainTex, Uv);
            }
            ENDCG
        }
    }
    Fallback Off
}
