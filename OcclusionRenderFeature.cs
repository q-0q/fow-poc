using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class OcclusionRenderFeature : ScriptableRendererFeature
{
    class OcclusionRenderPass : ScriptableRenderPass
    {
        private Material overrideMaterial;
        private FilteringSettings filteringSettings;
        private RenderTargetIdentifier target;
        private string profilerTag;

        public OcclusionRenderPass(Material material, LayerMask layerMask, string tag, RenderPassEvent passEvent)
        {
            overrideMaterial = material;
            filteringSettings = new FilteringSettings(RenderQueueRange.opaque, layerMask);
            profilerTag = tag;
            renderPassEvent = passEvent;
        }

        public void SetTarget(RenderTargetIdentifier target)
        {
            this.target = target;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(profilerTag);

            var drawingSettings = CreateDrawingSettings(
                new ShaderTagId("UniversalForward"),
                ref renderingData,
                SortingCriteria.CommonOpaque
            );
            drawingSettings.overrideMaterial = overrideMaterial;

            cmd.SetRenderTarget(target);
            cmd.ClearRenderTarget(true, true, Color.white); // clear to white if you want a white background

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    [System.Serializable]
    public class OcclusionSettings
    {
        public Material overrideMaterial;
        public LayerMask occluderLayer;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    public OcclusionSettings settings = new OcclusionSettings();

    OcclusionRenderPass occlusionPass;
    RenderTargetHandle tempRT;

    public override void Create()
    {
        occlusionPass = new OcclusionRenderPass(
            settings.overrideMaterial,
            settings.occluderLayer,
            "OcclusionRenderPass",
            settings.renderPassEvent
        );

        tempRT.Init("_OcclusionTexture");
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.camera.name != "OcclusionCamera")
            return;

        occlusionPass.SetTarget(tempRT.Identifier());
        renderer.EnqueuePass(occlusionPass);
    }
}
