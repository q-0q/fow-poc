Shader "Custom/URP/FogOfWarComputeDisplay"
{
    Properties
    {
        _FogTex ("Fog Texture", 2D) = "white" {}
        _VisibleColor ("Visible Color", Color) = (1,1,1,1)
        _FogColor ("Fog Color", Color) = (0,0,0,1)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay" }
        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_FogTex);
            SAMPLER(sampler_FogTex);
            float4 _VisibleColor;
            float4 _FogColor;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                float visibility = SAMPLE_TEXTURE2D(_FogTex, sampler_FogTex, i.uv).r;
                return lerp(_FogColor, _VisibleColor, visibility);
            }
            ENDHLSL
        }
    }
}
