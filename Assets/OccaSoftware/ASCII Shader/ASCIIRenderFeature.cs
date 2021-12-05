using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

public class ASCIIRenderFeature : ScriptableRendererFeature
{
    class CustomRenderPass : ScriptableRenderPass
    {

        private RenderTargetIdentifier source;

        private Material asciiMaterial;
        private RenderTargetHandle asciiRenderTarget;

        //For Downscaling
        private Material rescaleMaterial;
        RenderTargetHandle currentSource;
        RenderTargetHandle currentTarget;

        public void SetSource(RenderTargetIdentifier identifier)
        {
            source = identifier;
        }

        public void SetASCIIMaterial(Material material)
        {
            asciiMaterial = material;
        }

        public void SetRescaleMaterial(Material material)
        {
            rescaleMaterial = material;
        }


        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in an performance manner.
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            RenderTextureDescriptor rtDescriptor = cameraTextureDescriptor;
            cmd.GetTemporaryRT(asciiRenderTarget.id, rtDescriptor);
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("ASCIIPass");

            //Ping-Pong down
            RenderTextureDescriptor rtDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            
            // Set up corrected height
            int targetResX = 64;
            int targetResY = 32;
            //float targetAspect = targetResX / (float)targetResY;
            //float currentAspect = rtDescriptor.width / (float)rtDescriptor.height;
            //float ratioMultiplier = targetAspect / currentAspect;
            //rtDescriptor.height = (int)(rtDescriptor.height * ratioMultiplier); 

            List<RenderTargetHandle> renderTargetHandles = new List<RenderTargetHandle>();

            cmd.GetTemporaryRT(currentSource.id, rtDescriptor);
            int downscaleCount = 0;
            for (int i = 0; i < 10; i++)
            {
                rtDescriptor.width /= 2;
                rtDescriptor.height /= 2;
                if (rtDescriptor.width < targetResX || rtDescriptor.height < targetResY)
                {
                    downscaleCount = i;
                    break;
                }
                    

                cmd.GetTemporaryRT(currentTarget.id, rtDescriptor);
                renderTargetHandles.Add(currentTarget);
                Blit(cmd, currentSource.Identifier(), currentTarget.Identifier(), rescaleMaterial);
                currentSource = currentTarget;
            }

            for(int i = 0; i < downscaleCount; i++)
            {
                currentTarget = renderTargetHandles[i];
                Blit(cmd, currentSource.Identifier(), currentTarget.Identifier(), rescaleMaterial);
                currentSource = currentTarget;
            }


            Blit(cmd, source, asciiRenderTarget.Identifier(), asciiMaterial);
            Blit(cmd, asciiRenderTarget.Identifier(), source);


            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        /// Cleanup any allocated resources that were created during the execution of this render pass.
        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(asciiRenderTarget.id);
        }
    }

    [System.Serializable]
    public class Settings
    {
        public Material asciiMaterial;
        public Material rescaleMaterial;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
    }

    CustomRenderPass m_ScriptablePass;
    public Settings settings = new Settings();

    public override void Create()
    {
        m_ScriptablePass = new CustomRenderPass();

        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = settings.renderPassEvent;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        m_ScriptablePass.SetSource(renderer.cameraColorTarget);
        m_ScriptablePass.SetASCIIMaterial(settings.asciiMaterial);
        m_ScriptablePass.SetRescaleMaterial(settings.rescaleMaterial);
        renderer.EnqueuePass(m_ScriptablePass);
    }
}


