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

            sampler2D _CameraDepthTexture;
            
            fixed4 frag(v2f i, out float depth : SV_Depth) : SV_Target
            {
                float3 rayOrigin = i.objectVertex;
                float3 rayDirection = mul(unity_WorldToObject, float4(normalize(i.vectorToSurface), 1));

                float4 color = float4(0, 0, 0, 0);
                float3 samplePosition = rayOrigin;

                bool depthSet = false;
                float finalDepth = 1.0; // Fallback to far plane if never set

                for (int j = 0; j < MAX_STEP_COUNT; j++)
                {
                    if (max(abs(samplePosition.x), max(abs(samplePosition.y), abs(samplePosition.z))) < 0.5f + EPSILON)
                    {
                        float4 sampledColor = tex3D(_MainTex, samplePosition + float3(0.5f, 0.5f, 0.5f));
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