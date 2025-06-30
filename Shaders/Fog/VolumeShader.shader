Shader "Unlit/VolumeShader"
{
    Properties
    {
        _MainTex ("Texture", 3D) = "white" {}
        _Alpha ("Alpha", float) = 0.02
        _StepSize ("Step Size", float) = 0.01
        _TopColor("Top Color", Color) = (1,1,1,1)
        _BottomColor("Bottom Color", Color) = (0,0,0,0.1)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        Blend One OneMinusSrcAlpha
        ZTest Less
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            // Maximum number of raymarching samples
            #define MAX_STEP_COUNT 64

            // Allowed floating point inaccuracy
            #define EPSILON 0.00001f

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 objectVertex : TEXCOORD0;
                float3 vectorToSurface : TEXCOORD1;
            };

            sampler3D _MainTex;
            float4 _MainTex_ST;
            float _Alpha;
            float _StepSize;
            float4 _TopColor;
            float4 _BottomColor;

            float4x4 _VolumeToWorldMatrix;
            float3 _WorldMin;
            float3 _WorldMax;


            v2f vert (appdata v)
            {
                v2f o;

                // Vertex in object space. This is the starting point for the raymarching.
                o.objectVertex = v.vertex;

                // Calculate vector from camera to vertex in world space
                float3 worldVertex = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.vectorToSurface = worldVertex - _WorldSpaceCameraPos;

                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            float4 BlendUnder(float4 color, float4 newColor)
            {
                color.rgb += (1.0 - color.a) * newColor.a * newColor.rgb;
                color.a += (1.0 - color.a) * newColor.a;
                return color;
            }

            float hash(float n) { return frac(sin(n) * 43758.5453); }
            
            float noise3D(float3 x) {
    float3 p = floor(x);
    float3 f = frac(x);
    f = f * f * (3.0 - 2.0 * f); // smoothstep

    float n = p.x + p.y * 57.0 + p.z * 113.0;

    float n000 = hash(n + 0.0);
    float n100 = hash(n + 1.0);
    float n010 = hash(n + 57.0);
    float n110 = hash(n + 58.0);
    float n001 = hash(n + 113.0);
    float n101 = hash(n + 114.0);
    float n011 = hash(n + 170.0);
    float n111 = hash(n + 171.0);

    float nx00 = lerp(n000, n100, f.x);
    float nx01 = lerp(n001, n101, f.x);
    float nx10 = lerp(n010, n110, f.x);
    float nx11 = lerp(n011, n111, f.x);

    float nxy0 = lerp(nx00, nx10, f.y);
    float nxy1 = lerp(nx01, nx11, f.y);

    return lerp(nxy0, nxy1, f.z);
}

            sampler2D _CameraDepthTexture;
            
            fixed4 frag(v2f i, out float depth : SV_Depth) : SV_Target
            {
                float3 rayOrigin = i.objectVertex;
                float3 rayDirection = mul((float3x3)unity_WorldToObject, normalize(i.vectorToSurface));

                float4 color = float4(0, 0, 0, 0);
                float3 samplePosition = rayOrigin;

                bool depthSet = false;
                float finalDepth = 1.0; // Fallback to far plane if never set

                for (int j = 0; j < MAX_STEP_COUNT; j++)
                {
                    if (max(abs(samplePosition.x), max(abs(samplePosition.y), abs(samplePosition.z))) < 0.5f + EPSILON)
                    {

                        float3 volumeCoord = samplePosition + float3(0.5, 0.5, 0.5);

                        // Now use volume-to-world matrix
                        float3 worldPos = mul(_VolumeToWorldMatrix, float4(volumeCoord, 1.0)).xyz;

                        // Map to [0,1] for sampling the texture
                        float3 uvw = (worldPos - _WorldMin) / (_WorldMax - _WorldMin);
                        
                        
                        // Base frequency for noise
                        float frequency = 10.5;

                        // Compute three distinct noise coords by mixing uvw and some axis-specific constants:
                        float3 coordX = uvw * frequency + float3(15.3, 42.7, 78.1) + _Time.y * 0.7;
                        float3 coordY = uvw * frequency + float3(88.2, 17.9, 33.4) + _Time.y * 0.7;
                        float3 coordZ = uvw * frequency + float3(9.4, 65.1, 21.7) + _Time.y * 0.7;

                        // Sample noise at those coords:
                        float offsetX = (noise3D(coordX) - 0.5) * 0.05;
                        float offsetY = (noise3D(coordY) - 0.5) * 0.05;
                        float offsetZ = (noise3D(coordZ) - 0.5) * 0.05;

                        // Add per-axis distortion:
                        float3 distortedUVW = uvw + float3(offsetX, offsetY, offsetZ);

                        
                        // Broader noise for wavy distortion
                        float3 noiseCoord = uvw * 1.5 + _Time.y * 0.2;

                        float3 noise = float3(
                            frac(sin(dot(noiseCoord.yz, float2(12.9898, 78.233))) * 43758.5453),
                            frac(sin(dot(noiseCoord.xz, float2(39.3468, 11.135))) * 12345.6789),
                            frac(sin(dot(noiseCoord.xy, float2(93.9898, 67.345))) * 98765.4321)
                        );

                        noise = noise * 2.0 - 1.0; // Map to [-1, 1]

                        // Gentle distortion strength
                        distortedUVW += noise * 0.015;


                        float4 sampledColor = tex3D(_MainTex, distortedUVW);

                        sampledColor = lerp(_TopColor, float4(0, 0, 0, 0), sampledColor.r);
                        if (sampledColor.a > 0.001)
                        {
                            sampledColor = lerp(sampledColor, _BottomColor, samplePosition.g);
                        }
                        sampledColor.a *= _Alpha;

                        // Only write depth the first time we hit visible voxel content
                        if (!depthSet && sampledColor.a > 0.001)
                        {
                            float4 clipPos = UnityObjectToClipPos(samplePosition);
                            finalDepth = clipPos.z / clipPos.w;
                            depthSet = true;
                        }

                        color = BlendUnder(color, sampledColor);
                        if (color.a > 0.99)
                            break;

                        samplePosition += rayDirection * _StepSize;
                    }
                }

                depth = finalDepth;
                return color;
            }
            


            ENDCG
        }
    }
}