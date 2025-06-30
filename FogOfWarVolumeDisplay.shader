Shader "Custom/URP/FogOfWarVolumetric"
{
    Properties
    {
        _FogVolumeTex("Fog Volume", 3D) = "" {}
        _VisibleColor("Visible Color", Color) = (1,1,1,1)
        _FogColor("Fog Color", Color) = (0,0,0,1)
        _WorldMin("World Min (XYZ)", Vector) = (0,0,0,0)
        _WorldMax("World Max (XYZ)", Vector) = (10,5,10,0)
        _StepSize("Raymarch Step Size", Float) = 0.1
        _Density("Fog Density", Float) = 1.5
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE3D(_FogVolumeTex);
            SAMPLER(sampler_FogVolumeTex);

            float4 _VisibleColor;
            float4 _FogColor;
            float3 _WorldMin;
            float3 _WorldMax;
            float _StepSize;
            float _Density;

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 worldPos = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(worldPos);
                o.worldPos = worldPos;

                // View direction from camera
                float3 viewDir = normalize(worldPos - _WorldSpaceCameraPos);
                o.viewDir = viewDir;

                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                float3 rayOrigin = i.worldPos;
                float3 rayDir = normalize(i.viewDir);

                // Intersect ray with box volume (world space AABB)
                float3 tMin = (_WorldMin - rayOrigin) / rayDir;
                float3 tMax = (_WorldMax - rayOrigin) / rayDir;
                float3 t1 = min(tMin, tMax);
                float3 t2 = max(tMin, tMax);

                float tNear = max(max(t1.x, t1.y), t1.z);
                float tFar = min(min(t2.x, t2.y), t2.z);

                if (tFar < 0 || tNear > tFar)
                    discard;

                float t = max(tNear, 0); // Start after camera
                float4 finalColor = float4(0, 0, 0, 0);

                const int maxSteps = 128;
                int steps = 0;

                while (t < tFar && steps < maxSteps && finalColor.a < 1.0)
                {
                    float3 samplePos = rayOrigin + t * rayDir;

                    float3 uvw = (samplePos - _WorldMin) / (_WorldMax - _WorldMin);
                    if (any(uvw < 0) || any(uvw > 1)) break;

                    float visibility = SAMPLE_TEXTURE3D(_FogVolumeTex, sampler_FogVolumeTex, uvw).r;

                    float fogAmount = (1.0 - visibility) * _Density * _StepSize;
                    float3 fogRGB = lerp(_FogColor.rgb, _VisibleColor.rgb, visibility);

                    // Alpha compositing (front-to-back)
                    float alpha = fogAmount * (1.0 - finalColor.a);
                    finalColor.rgb += fogRGB * alpha;
                    finalColor.a += alpha;

                    t += _StepSize;
                    steps++;
                }

                return finalColor;
            }
            ENDHLSL
        }
    }
}
