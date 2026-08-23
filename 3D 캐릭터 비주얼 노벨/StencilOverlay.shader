Shader "Custom/StencilOverlay"
{
    Properties
    {
        // C# 스크립트에서 이 값들을 실시간으로 쏴줍니다.
        _Color ("Color", Color) = (1,1,1,1)
        _StencilRef ("Stencil ID", Int) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Overlay" "RenderPipeline"="UniversalPipeline"
        }


        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha // 투명도 섞기 (오버레이)
        
        Pass
        {
            Name "StencilOverlay"
            
            Stencil
            {
                Ref [_StencilRef]
                Comp Equal
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

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
            CBUFFER_END

            // 풀스크린용 버텍스 쉐이더
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                output.positionCS = float4(input.positionOS.x, input.positionOS.y, 0.0, 1.0);
                output.uv = input.uv;

                // 일부 플랫폼(DirectX 등)에서 UV 뒤집힘 방지
                #if UNITY_UV_STARTS_AT_TOP
                if (_ProjectionParams.x < 0)
                    output.uv.y = 1 - output.uv.y;
                #endif

                return output;
            }

            // 픽셀 쉐이더: 단순히 정해진 색상만 출력
            half4 frag(Varyings input) : SV_Target
            {
                return _Color;
            }
            ENDHLSL
        }
    }
}