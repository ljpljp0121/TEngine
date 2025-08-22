Shader "TEngine/UI/CircleMask"
{
    Properties
    {
        _StencilRef ("Stencil Reference", Range(1, 255)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+1"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "CircleMask"
            ColorMask 0
            ZTest [unity_GUIZTestMode]
            ZWrite Off
            
            Stencil
            {
                Ref [_StencilRef]
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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


            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 center = float2(0.5, 0.5);
                float2 dist = input.uv - center;
                float distance = length(dist);
                
                // 完美圆形，半径0.5
                clip(0.5 - distance);
                
                return half4(0, 0, 0, 0);
            }
            ENDHLSL
        }
    }
}