using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class OverlayEffectRenderFeature : ScriptableRendererFeature
{
    // [수정 1] 불투명, 투명, 그리고 '둘 다(All)'를 지원하기 위한 커스텀 Enum 정의
    public enum TargetQueueType
    {
        Opaque,      // 불투명만
        Transparent, // 투명만
        All          // 둘 다 포함
    }

    [System.Serializable]
    public class OverlaySettings
    {
        public Material overlayMaterial;
        public LayerMask layerMask;
        public RenderPassEvent renderEvent = RenderPassEvent.AfterRenderingTransparents;
        
        [Tooltip("오버레이를 적용할 스텐실 ID입니다. (캐릭터 머테리얼 설정과 같아야 함)")]
        [Range(0, 255)]
        public int stencilReference = 2;

        // [수정 2] 기존 RenderQueueType 대신 새로 만든 커스텀 Enum 사용 (기본값 All)
        public TargetQueueType renderQueueType = TargetQueueType.All; 
    }

    class OverlayPass : ScriptableRenderPass
    {
        private Material overlayMaterial;
        private FilteringSettings filteringSettings;
        private List<ShaderTagId> shaderTagIdList;
        private ProfilingSampler profilingSampler;
        private int stencilRef;

        public OverlayPass(OverlaySettings settings)
        {
            this.overlayMaterial = settings.overlayMaterial;
            this.renderPassEvent = settings.renderEvent;
            this.stencilRef = settings.stencilReference;
            
            this.profilingSampler = new ProfilingSampler("Overlay Effect Stencil Pass");

            shaderTagIdList = new List<ShaderTagId>
            {
                new ShaderTagId("SRPDefaultUnlit"),
                new ShaderTagId("UniversalForward"),
                new ShaderTagId("UniversalForwardOnly"),
                new ShaderTagId("LightweightForward")
            };
            
            // [수정 3] 선택한 옵션에 따라 렌더링 범위(Range)를 결정하는 로직
            RenderQueueRange queueRange = RenderQueueRange.all; // 기본값: 전체

            switch (settings.renderQueueType)
            {
                case TargetQueueType.Opaque:
                    queueRange = RenderQueueRange.opaque;
                    break;
                case TargetQueueType.Transparent:
                    queueRange = RenderQueueRange.transparent;
                    break;
                case TargetQueueType.All:
                    queueRange = RenderQueueRange.all; // 0 ~ 5000 (모든 큐 포함)
                    break;
            }

            // 결정된 큐 범위와 레이어 마스크로 필터링 설정 생성
            filteringSettings = new FilteringSettings(queueRange, settings.layerMask);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (overlayMaterial == null) return;

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, profilingSampler))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                SortingCriteria sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;
                DrawingSettings drawingSettings = CreateDrawingSettings(shaderTagIdList, ref renderingData, sortingCriteria);
                
                // 스텐실 값 주입
                overlayMaterial.SetInt("_StencilRef", stencilRef);

                // 공통 설정
                drawingSettings.overrideMaterial = overlayMaterial;
                drawingSettings.enableDynamicBatching = true;
                drawingSettings.enableInstancing = true;

                // ====================================================
                // [1단계] 몸통 그리기 (Pass 0)
                // ====================================================
                drawingSettings.overrideMaterialPassIndex = 0; 
                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);

                // ====================================================
                // [2단계] 아웃라인 그리기 (Pass 1)
                // ====================================================
                 drawingSettings.overrideMaterialPassIndex = 1; 
                 context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);

            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    public OverlaySettings settings = new OverlaySettings();
    private OverlayPass overlayPass;

    public override void Create()
    {
        if (settings.overlayMaterial == null) return;
        overlayPass = new OverlayPass(settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.overlayMaterial != null && overlayPass != null)
        {
            renderer.EnqueuePass(overlayPass);
        }
    }
}