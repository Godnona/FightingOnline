using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class NoPostProcessRenderFeature : ScriptableRendererFeature
{
    class NoPostProcessPass : ScriptableRenderPass
    {
        private FilteringSettings filteringSettings;
        private ShaderTagId shaderTagId = new ShaderTagId("UniversalForward");

        public NoPostProcessPass(LayerMask layerMask)
        {
            filteringSettings = new FilteringSettings(RenderQueueRange.all, layerMask);
            renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        }

        [System.Obsolete]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            DrawingSettings drawingSettings = CreateDrawingSettings(shaderTagId, ref renderingData, SortingCriteria.CommonTransparent);

            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
        }

        private class PassData
        {
            public RendererListHandle rendererListHandle;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("NoPostProcessPass", out var passData))
            {
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Write);

                SortingSettings sortingSettings = new SortingSettings(cameraData.camera) { criteria = SortingCriteria.CommonTransparent };
                DrawingSettings drawingSettings = new DrawingSettings(shaderTagId, sortingSettings)
                {
                    perObjectData = renderingData.perObjectData
                };
                RendererListParams rendererListParams = new RendererListParams(renderingData.cullResults, drawingSettings, filteringSettings);

                passData.rendererListHandle = renderGraph.CreateRendererList(rendererListParams);
                builder.UseRendererList(passData.rendererListHandle);
                
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    context.cmd.DrawRendererList(data.rendererListHandle);
                });
            }
        }
    }

    public LayerMask noPostProcessLayer;
    private NoPostProcessPass pass;

    public override void Create()
    {
        pass = new NoPostProcessPass(noPostProcessLayer);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(pass);
    }
}