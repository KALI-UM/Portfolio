Shader "Custom/UnlitOverlay"
{
  Properties
    {
        [MainColor] _OverlayColor ("Overlay Color", Color) = (1,1,1,1)
        [MainTexture] _OverlayMainTex ("Overlay Texture", 2D) = "white" {}
        
        //어차피 스탠실을 써서 넉넉잡아 넘쳐도 됨
        _OutlineWidth ("Outline Width", Range(0, 0.5)) = 0.03
        
        _StencilRef ("Stencil Reference", Int) = 225
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comparison", Int) = 3 // 3 = Equal
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        // Pass 1: 몸통 
        Pass
        {
            Name "OverlayBody"
            Tags { "LightMode" = "UniversalForward" }

            //이미 몸통을 그린 곳은 스탠실 값을 0으로 만들어서 아웃라인 그릴때 덧그리지 않도록 함
            Stencil
            {
                Ref [_StencilRef]
                Comp [_StencilComp]
                Pass Zero
            }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual // 몸통은 보통 깊이 테스트를 합니다
            Cull Back
            Offset -1, -1 // Z-Fighting 방지

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_OverlayMainTex);
            SAMPLER(sampler_OverlayMainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _OverlayMainTex_ST;
                float _OutlineWidth;
            CBUFFER_END

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _OverlayColor)
            UNITY_INSTANCING_BUFFER_END(Props)

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _OverlayMainTex);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float4 overlayColor = UNITY_ACCESS_INSTANCED_PROP(Props, _OverlayColor);
                half4 texColor = SAMPLE_TEXTURE2D(_OverlayMainTex, sampler_OverlayMainTex, input.uv);
                return texColor * overlayColor;
            }
            ENDHLSL
        }

        //  Pass 2: 외곽선 (추가된 부분)
        Pass
        {
            Name "OverlayOutline"
            Tags { "LightMode" = "UniversalForward" }

            //몸통에서 남은 부분을 그림
            Stencil
            {
                Ref [_StencilRef]
                Comp [_StencilComp]
                Pass Zero
            }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            
            ZTest Always 
            
            // 뒤집힌 헐(Inverted Hull) 방식
            Cull Front 
            
            Offset -5, -5

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL; // 확장을 위해 노멀 필요
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_OverlayMainTex);
            SAMPLER(sampler_OverlayMainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _OverlayMainTex_ST;
                float _OutlineWidth;
            CBUFFER_END

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _OverlayColor)
            UNITY_INSTANCING_BUFFER_END(Props)

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // 노멀 방향으로 버텍스를 밀어냄
                float3 extrudedPos = input.positionOS.xyz + (input.normalOS * _OutlineWidth);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(extrudedPos);
                output.positionCS = vertexInput.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _OverlayMainTex);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float4 overlayColor = UNITY_ACCESS_INSTANCED_PROP(Props, _OverlayColor);
                half4 texColor = SAMPLE_TEXTURE2D(_OverlayMainTex, sampler_OverlayMainTex, input.uv);
                return texColor * overlayColor;
            }
            ENDHLSL
        }
    }
}