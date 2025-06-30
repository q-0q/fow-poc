Shader "Unlit/Black"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Color (0,0,0,1)
            ZWrite On
            ZTest LEqual
        }
    }
}
