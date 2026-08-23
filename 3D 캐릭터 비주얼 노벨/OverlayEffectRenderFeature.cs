using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class OverlayEffectRenderFeature : ScriptableRendererFeature
{
    // 불투명, 투명, 그리고 '둘 다(All)'를 지원하기 위한 커스텀 Enum 정의
    public enum TargetQueueType
    {
        Opaque, // 불투명만
        Transparent, // 투명만
        All // 둘 다 포함
    }

    [System.Serializable]
    public class StencilMapping
    {
        [Range(0, 255)] public int stencilId; // 타겟 스텐실 번호
        public Color outlineColor; // 덮어씌울 색상
        [HideInInspector]public Material material;
    }

    [System.Serializable]
    public class OverlaySettings
    {
        public LayerMask layerMask;

        public RenderPassEvent renderEvent = RenderPassEvent.AfterRenderingTransparents;

        // 기존 RenderQueueType 대신 새로 만든 커스텀 Enum 사용 (기본값 All)
        public TargetQueueType renderQueueType = TargetQueueType.All;

        [Tooltip("아웃라인 전용")] 
        public Shader stencilOutlineShader;
        public List<StencilMapping> stencilColorMappings;
    }
    
    [System.Serializable]
    public class NoOverlaySettings
    {
        public LayerMask layerMask;
        public Material eraserMaterial;
    }

    class NoOverlayPass : ScriptableRenderPass
    {
        private Material eraserMaterial;
        private FilteringSettings filteringSettings;
        private List<ShaderTagId> shaderTagIdList;
        private ProfilingSampler profilingSampler;

        public NoOverlayPass(NoOverlaySettings settings)
        {
            this.eraserMaterial = settings.eraserMaterial;
            this.profilingSampler = new ProfilingSampler("Stencil Eraser Pass");
            RenderQueueRange queueRange = RenderQueueRange.all;
            this.filteringSettings = new FilteringSettings(queueRange, settings.layerMask);
            this.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            
            shaderTagIdList = new List<ShaderTagId>
            {
                new ShaderTagId("SRPDefaultUnlit"),
                new ShaderTagId("UniversalForward"),
                new ShaderTagId("UniversalForwardOnly"),
                new ShaderTagId("LightweightForward")
            };
        }
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (eraserMaterial == null) return;

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, profilingSampler))
            {
                SortingCriteria sortingCriteria = SortingCriteria.CommonTransparent;
                DrawingSettings drawingSettings =
                    CreateDrawingSettings(shaderTagIdList, ref renderingData, sortingCriteria);
                drawingSettings.overrideMaterial = eraserMaterial;
                drawingSettings.overrideMaterialPassIndex = 0;
                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }
    }
    
    class OverlayPass : ScriptableRenderPass
    {
        private FilteringSettings filteringSettings;
        private List<ShaderTagId> shaderTagIdList;
        private ProfilingSampler profilingSampler;

        private List<StencilMapping> stencilColorMappings;

        public OverlayPass(OverlaySettings settings)
        {

            this.renderPassEvent = settings.renderEvent;
            this.profilingSampler = new ProfilingSampler("Overlay Effect Stencil Pass");
            shaderTagIdList = new List<ShaderTagId>
            {
                new ShaderTagId("SRPDefaultUnlit"),
                new ShaderTagId("UniversalForward"),
                new ShaderTagId("UniversalForwardOnly"),
                new ShaderTagId("LightweightForward")
            };

            RenderQueueRange queueRange = RenderQueueRange.all;

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
            stencilColorMappings = settings.stencilColorMappings;
            foreach (var mapping in stencilColorMappings)
            {
                if(mapping.material==null)
                    mapping.material=new Material(settings.stencilOutlineShader);    
                    
                mapping.material.SetFloat("_StencilRef", mapping.stencilId);
                mapping.material.SetColor("_Color", mapping.outlineColor);
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (stencilColorMappings == null||stencilColorMappings.Count==0) return;

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, profilingSampler))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                SortingCriteria sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;
                DrawingSettings drawingSettings =
                    CreateDrawingSettings(shaderTagIdList, ref renderingData, sortingCriteria);


                drawingSettings.enableDynamicBatching = true;
                drawingSettings.enableInstancing = true;

                // // [stencil로 다시 그리는 overlay] 몸통 그리기
                foreach (var mapping in stencilColorMappings)
                {
                    if(mapping.material==null)
                        continue;
                    //2026-01-26 KHJ : 각 스탠실 번호마다 머테리얼 배정, 색 지정 
                    mapping.material.SetColor("_Color", mapping.outlineColor);
                    cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, mapping.material);
                }

                // // [override material을 통한 overlay] 몸통 그리던 버전
                // drawingSettings.overrideMaterialPassIndex = 0; 
                // context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);

                
                // foreach (var setting in stencilColorMappings)
                // {
                //     MaterialPropertyBlock props = new MaterialPropertyBlock();
                //     cmd.SetGlobalFloat("_StencilRef", setting.stencilId);
                //     props.SetColor("_Color", setting.outlineColor);
                //     
                //     outlineMaterial.SetColor("_Color", setting.outlineColor);
                //     outlineMaterial.SetFloat("_Stencil", setting.stencilId);
                //     cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, outlineMaterial,0,0,props);
                // }
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            // Color: 화면 색상 버퍼 / Depth: 스텐실 버퍼
            ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            //현재 렌더러(카메라) 정보를 가져옴
            var renderer = renderingData.cameraData.renderer;
            ConfigureTarget(renderer.cameraColorTargetHandle, renderer.cameraDepthTargetHandle);
            //화면을 지우지 말고 그 위에 덧그려라
            ConfigureClear(ClearFlag.None, Color.black);
        }
    }

    public OverlaySettings overlaySettings = new OverlaySettings();
    public NoOverlaySettings noOverlaySettings = new NoOverlaySettings();
    private OverlayPass overlayPass;
    private NoOverlayPass noOverlayPass;

    public override void Create()
    {
        //noOverlaySettings.layerMask = ~overlaySettings.layerMask;
        
        if (noOverlaySettings.eraserMaterial != null)
            noOverlayPass = new NoOverlayPass(noOverlaySettings);
        
        if (overlaySettings.stencilColorMappings != null && overlaySettings.stencilColorMappings.Count != 0)
            overlayPass = new OverlayPass(overlaySettings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (noOverlayPass != null)
            renderer.EnqueuePass(noOverlayPass);


        if (overlayPass != null)
            renderer.EnqueuePass(overlayPass);
    }


    public void SetOutlineColor(int index, Color color)
    {
        if (index >= 0 && index < overlaySettings.stencilColorMappings.Count)
        {
            overlaySettings.stencilColorMappings[index].outlineColor = color;
        }
    }
}