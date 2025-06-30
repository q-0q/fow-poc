Shader "Custom/Debug/FogVolumeSliceFlexible"
{
    Properties
    {
        _ResultVolume("Fog Volume (3D Texture)", 3D) = "" {}
        _SliceIndex("Slice Index", Float) = 0
        _VolumeDepth("Volume Depth", Float) = 256
        _SliceAxis("Slice Axis (0=X,1=Y,2=Z)", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE3D(_ResultVolume);
            SAMPLER(sampler_ResultVolume);

            float _SliceIndex;
            float _VolumeDepth;
            float _SliceAxis; // 0 = X, 1 = Y, 2 = Z

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
                float sliceNorm = clamp(_SliceIndex / (_VolumeDepth - 1.0), 0.0, 1.0);

                float3 uvw;

                // Choose slice axis
                if (_SliceAxis == 0)
                {
                    // Slice along X (YZ plane)
                    uvw = float3(sliceNorm, i.uv.xy);
                }
                else if (_SliceAxis == 1)
                {
                    // Slice along Y (XZ plane)
                    uvw = float3(i.uv.x, sliceNorm, i.uv.y);
                }
                else
                {
                    // Default: Slice along Z (XY plane)
                    uvw = float3(i.uv.xy, sliceNorm);
                }

                float v = SAMPLE_TEXTURE3D(_ResultVolume, sampler_ResultVolume, uvw).r;
                return float4(v, v, v, 1);
            }
            ENDHLSL
        }
    }
    FallBack "Diffuse"
}
