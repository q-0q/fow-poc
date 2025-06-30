Shader "Custom/FogOfWar/VolumeRaymarch"
{
    Properties
    {
        _ResultVolume("Volume Texture", 3D) = "" {}
        _Steps("Raymarch Steps", Range(16,256)) = 96
        _VisibleColor("Visible Color", Color) = (1,1,1,1)
        _OccludedColor("Occluded Color", Color) = (0,0,0,0.1)
        _Alpha("Alpha", Float) = 0.02
        _StepSize("Step Size", Float) = 0.01
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
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 objectVertex : TEXCOORD0;
                float3 rayDirOS : TEXCOORD1;
            };

            TEXTURE3D(_ResultVolume);
            SAMPLER(sampler_ResultVolume);

            float4 _VisibleColor;
            float4 _OccludedColor;
            int _Steps;
            float _Alpha;
            float _StepSize;

            Varyings vert(Attributes v)
            {
                Varyings o;

                o.objectVertex = v.positionOS.xyz;

                float3 worldVertex = mul(unity_ObjectToWorld, v.positionOS).xyz;
                float3 worldCam = _WorldSpaceCameraPos;

                // Ray direction from camera to vertex in world space
                float3 rayDirWS = normalize(worldVertex - worldCam);

                // Transform ray direction to object space (rotation + scale)
                o.rayDirOS = mul((float3x3)unity_WorldToObject, rayDirWS);

                o.positionCS = TransformObjectToHClip(v.positionOS);
                return o;
            }

            // Front-to-back alpha blending
            float4 BlendUnder(float4 color, float4 newColor)
            {
                color.rgb += (1.0 - color.a) * newColor.a * newColor.rgb;
                color.a += (1.0 - color.a) * newColor.a;
                return color;
            }

            bool IsInsideUnitCube(float3 p)
            {
                return max(abs(p.x), max(abs(p.y), abs(p.z))) <= 0.5;
            }

            float4 frag(Varyings i) : SV_Target
            {
                float3 rayOrigin = i.objectVertex;
                float3 rayDir = normalize(i.rayDirOS);

                float4 accumulated = float4(0, 0, 0, 0);
                float3 samplePos = rayOrigin;

                float time = _Time.y;
                float noiseScale = 5.0;       // Controls frequency of the noise
                float noiseStrength = 10.01;   // Controls how far the uvw is displaced

                [unroll]
                for (int step = 0; step < _Steps; step++)
                {
                    if (!IsInsideUnitCube(samplePos))
                        break;

                    float3 uvw = samplePos + float3(0.5, 0.5, 0.5);

                    float noiseVal = frac(sin(dot(uvw, float3(12.9898, 78.233, 37.719))) * 43758.5453);
                    
                    float vis = SAMPLE_TEXTURE3D(_ResultVolume, sampler_ResultVolume, uvw).r;

                    float4 col = lerp(_OccludedColor, _VisibleColor, vis);
                    col.a *= _Alpha * (0.5 + 0.5 * noiseVal); // wiggle alpha a bit

                    col = _VisibleColor;
                    col.a = (0.5 + 0.5 * noiseVal); // Full strength for debugging

                    accumulated = BlendUnder(accumulated, col);

                    // if (accumulated.a >= 0.95)
                    //     break;

                    samplePos += rayDir * _StepSize;
                }

                return accumulated;
            }


            ENDHLSL
        }
    }
    FallBack "Unlit/Transparent"
}
