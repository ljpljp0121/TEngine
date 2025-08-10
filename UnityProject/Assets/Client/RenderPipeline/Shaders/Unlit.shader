Shader"CustomRP/Unlit"
{
    Properties
    {
        _BaseColor("Color",Color) = (1,1,1,1)
    }

    SubShader
    {
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #include "UnlitPass.hlsl"
           
            ENDHLSL
        }
    }
}