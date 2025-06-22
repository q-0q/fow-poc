Shader "Custom/FogOfWarRaymarch"
{
    Properties
    {
        _OcclusionTexture ("Occlusion Texture", 2D) = "white" {}
        _PlayerPosRadius ("Player Position and Radius", Vector) = (0,0,5,0)
        _WorldMin ("World Min (X,Z)", Vector) = (0,0,0,0)
        _WorldMax ("World Max (X,Z)", Vector) = (10,10,0,0)
        _FogColor ("Fog Color", Color) = (0,0,0,1)
        _VisibleColor ("Visible Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Transparent" }
        Pass
        {
            ZWrite Off
            ZTest Always
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_OcclusionTexture);
            SAMPLER(sampler_OcclusionTexture);

            float4 _PlayerPosRadius; // x,y = player pos (world X,Z), z = radius
            float4 _WorldMin; // x,z = min world bounds
            float4 _WorldMax; // x,z = max world bounds
            float4 _FogColor;
            float4 _VisibleColor;

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

            // Map world X,Z pos to UV in occlusion texture
            float2 WorldToUV(float2 worldPosXZ)
            {
                float2 uv;
                uv.x = saturate((worldPosXZ.x - _WorldMin.x) / (_WorldMax.x - _WorldMin.x));
                uv.y = saturate((worldPosXZ.y - _WorldMin.y) / (_WorldMax.y - _WorldMin.y));
                return uv;
            }

            bool IsOccluded(float2 playerUV, float2 pixelUV)
            {
                const int steps = 128;
                float2 dir = normalize(pixelUV - playerUV);
                float dist = distance(pixelUV, playerUV);

                [loop]
                for(int i = 0; i < steps; i++)
                {
                    float t = i / (float)steps;
                    float2 sampleUV = playerUV + dir * dist * t;
                    float occ = SAMPLE_TEXTURE2D(_OcclusionTexture, sampler_OcclusionTexture, sampleUV).r;
                    if (occ < 0.5) // black = obstacle
                        return true;
                }
                return false;
            }

            float4 frag(Varyings i) : SV_Target
            {
                // Reconstruct world pos (assuming ortho camera aligned XY screen space matches XZ world space)
                // Here we assume screen uv corresponds to world space XZ in range _WorldMin->_WorldMax
                float2 pixelWorldXZ = lerp(_WorldMin.xy, _WorldMax.xy, i.uv);

                float2 playerPosXZ = _PlayerPosRadius.xy;
                float radius = _PlayerPosRadius.z;

                float dist = distance(playerPosXZ, pixelWorldXZ);

                if (dist > radius)
                    return _FogColor; // fully fogged outside vision radius

                float2 playerUV = WorldToUV(playerPosXZ);
                float2 pixelUV = WorldToUV(pixelWorldXZ);

                if (IsOccluded(playerUV, pixelUV))
                    return _FogColor; // occluded by obstacle

                return _VisibleColor; // visible
            }
            ENDHLSL
        }
    }
}
