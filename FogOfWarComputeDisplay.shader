Shader "Custom/URP/FogOfWarVolume"
{
    Properties
    {
        _FogTex ("Visibility Texture", 2D) = "white" {}
        _VisibleColor ("Visible Color", Color) = (1,1,1,1)
        _FogColor ("Fog Color", Color) = (0,0,0,1)
        _WorldMin ("World Min (XZ)", Vector) = (0,0,0,0)
        _WorldSize ("World Size (XZ)", Vector) = (10,0,10,0)
        _AlphaCutoff ("Alpha Cutoff", Float) = 0.01
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+1" }
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
            float4 _WorldMin;
            float4 _WorldSize;
            float _AlphaCutoff;

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 worldPos = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(worldPos);
                o.positionWS = worldPos;
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
    float2 uv = float2(
        (i.positionWS.x - _WorldMin.x) / _WorldSize.x,
        (i.positionWS.z - _WorldMin.z) / _WorldSize.z
    );
    uv = clamp(uv, 0, 1);

    float visibility = SAMPLE_TEXTURE2D(_FogTex, sampler_FogTex, uv).r;

    // if (visibility <= _AlphaCutoff)
    //     discard;

    float4 col = lerp(_FogColor, _VisibleColor, visibility);
    col.a = lerp(_FogColor.a, _VisibleColor.a, visibility);

    return col;
            }

            ENDHLSL
        }
    }
}
