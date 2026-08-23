Shader "Custom/StencilEraser"
{
    Properties
    {
        [IntRange] _StencilID ("Stencil ID to Write (Default 0)", Range(0, 255)) = 0
        
        _MainTex ("Base Map (UI/Texture)", 2D) = "white" {}
         //알파 값이 이 수치보다 낮으면 무시함 (기본 0.5: 반투명 이하는 무시)
        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
    }
    SubShader
    {
        // TransparentCutout으로 설정하여 알파 테스트가 있음을 명시
        Tags { "RenderType"="TransparentCutout" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        
        Pass
        {
            Name "StencilEraser"
            
            ColorMask 0 
            ZWrite Off
            ZTest LEqual 

            Stencil
            {
                Ref [_StencilID]
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
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            // 텍스처와 샘플러 선언
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float _Cutoff;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                //텍스처 색상을 읽어옴
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                
                //최종 알파 계산 (텍스처 알파 * 버텍스 컬러 알파)
                //UI 이미지 컴포넌트의 투명도 조절 등도 반영됨
                half alpha = texColor.a * input.color.a;

                // 알파 컷오프 (Alpha Clipping)
                // 알파 값이 _Cutoff(0.5)보다 작으면 무시
                // 픽셀이 버려지면 Stencil도 안 밀리고 보존
                clip(alpha - _Cutoff);

                return half4(0,0,0,0);
            }
            ENDHLSL
        }
    }
}