Shader "Custom/FogOfWar/VolumeRaymarch"
{
    Properties
    {
        _ResultVolume("Volume Texture", 3D) = "" {}
        _Steps("Raymarch Steps", Range(16,256)) = 96
        _VisibleColor("Visible Color", Color) = (1,1,1,1)
        _OccludedColor("Occluded Color", Color) = (0,0,0,0.1)
        _EdgeSoftness("Edge Softness", Float) = 0.01
        _BoxMin("Box Min", Vector) = (0, 0, 0)
        _BoxMax("Box Max", Vector) = (10, 0, 10)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 rayOriginOS : TEXCOORD0;
                float3 rayDirOS : TEXCOORD1;
            };

            TEXTURE3D(_ResultVolume);
            SAMPLER(sampler_ResultVolume);
            float4 _VisibleColor;
            float4 _OccludedColor;
            float3 _BoxMin;
            float3 _BoxMax;
            int _Steps;
            float _EdgeSoftness;

            Varyings vert(Attributes v)
            {
                Varyings o;

                float3 worldPos = TransformObjectToWorld(v.positionOS.xyz);
                float3 camPos = _WorldSpaceCameraPos;

                // ray origin in object space is camera position transformed to object space
                float3 rayOriginOS = mul(unity_WorldToObject, float4(camPos, 1)).xyz;

                // ray direction in world space from camera to vertex position
                float3 rayDirWS = normalize(worldPos - camPos);

                // ray direction in object space
                float3 rayDirOS = mul((float3x3)unity_WorldToObject, rayDirWS);

                o.rayOriginOS = rayOriginOS;
                o.rayDirOS = rayDirOS;

                o.positionCS = TransformObjectToHClip(v.positionOS);

                return o;
            }

            // Ray-box intersection in object space
            bool RayBoxIntersection(float3 ro, float3 rd, float3 boxMin, float3 boxMax, out float tMin, out float tMax)
            {
                float3 invDir = 1.0 / rd;
                float3 t0 = (boxMin - ro) * invDir;
                float3 t1 = (boxMax - ro) * invDir;
                float3 tmin3 = min(t0, t1);
                float3 tmax3 = max(t0, t1);
                tMin = max(max(tmin3.x, tmin3.y), tmin3.z);
                tMax = min(min(tmax3.x, tmax3.y), tmax3.z);
                return tMax > max(tMin, 0.0);
            }
            

            float4 frag(Varyings i) : SV_Target
            {

                float3 rayOrigin = i.rayOriginOS;
                float3 rayDir = normalize(i.rayDirOS);

                float tMin, tMax;
                if (!RayBoxIntersection(rayOrigin, rayDir, float3(-30, -30, -30), float3(30, 30, 30), tMin, tMax))
                    discard;

                // Hardcoded color just to confirm raymarching is working
                return float4(1, 0, 0, 1); // bright red

  
            }


            ENDHLSL
        }
    }
    FallBack "Unlit/Transparent"
}
