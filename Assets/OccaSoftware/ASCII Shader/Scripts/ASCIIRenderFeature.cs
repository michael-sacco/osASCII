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
        List<RenderTargetHandle> renderTargetHandles = new List<RenderTargetHandle>();
        RenderTargetHandle finalUpscaleHandle;

        private int iterations = 5;

        bool isAfterRendering = false;
        

        public void SetSource(RenderTargetIdentifier identifier)
        {
            source = identifier;
        }

        public void SetupASCIIMaterial(ASCIIShaderData shaderData)
        {
            asciiMaterial = CoreUtils.CreateEngineMaterial("Shader Graphs/ASCII Shader");
            asciiMaterial.SetFloat(ShaderParams.numberOfCharacters, shaderData.numberOfCharacters);
            asciiMaterial.SetVector(ShaderParams.resolution, shaderData.resolution);
            asciiMaterial.SetFloat(ShaderParams.fontRatio, shaderData.fontRatio);
            asciiMaterial.SetColor(ShaderParams.fontColor, shaderData.fontColor);
            asciiMaterial.SetFloat(ShaderParams.fontColorStrength, shaderData.fontColorStrength);
            asciiMaterial.SetColor(ShaderParams.backingColor, shaderData.backingColor);
            asciiMaterial.SetFloat(ShaderParams.backingColorStrength, shaderData.backingColorStrength);
            asciiMaterial.SetTexture(ShaderParams.fontAsset, shaderData.fontAsset);
        }


        public void InitializeRescaleMaterial()
        {
            rescaleMaterial = CoreUtils.CreateEngineMaterial("Shader Graphs/Image Filtering Shader");
        }

        public void SetIterations(int iterations)
        {
            this.iterations = iterations;
        }

        public void InitializeRenderTextures(int iterations)
        {
            asciiRenderTarget.Init("_ASCIITarget");
            if(iterations >= 1)
            {
                currentSource.Init("_CurrentSource");
                currentTarget.Init("_CurrentTarget");
                finalUpscaleHandle.Init("_FinalUpscale");
            }
        }

        public void SetRenderEventState(bool isAfterRendering)
        {
            this.isAfterRendering = isAfterRendering;
        }

        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in an performance manner.
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            RenderTextureDescriptor rtDescriptor = cameraTextureDescriptor;
            rtDescriptor.colorFormat = RenderTextureFormat.DefaultHDR;
            cmd.GetTemporaryRT(asciiRenderTarget.id, rtDescriptor);
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("ASCIIPass");

            RenderTextureDescriptor rtDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            rtDescriptor.colorFormat = RenderTextureFormat.DefaultHDR;
            
            if(renderTargetHandles.Count > 0)
                renderTargetHandles.Clear();

            RenderTargetIdentifier renderSource = isAfterRendering ? "_AfterPostProcessTexture" : source;
            RenderTargetIdentifier asciiSource = renderSource;

            if (iterations >= 1)
            {
                RenderTextureDescriptor downscaleDescriptor = rtDescriptor;
                rescaleMaterial.SetFloat("_Rescale_UVOffset", 1.0f);
                for (int i = 0; i < iterations; i++)
                {
                    downscaleDescriptor.width /= 2;
                    downscaleDescriptor.height /= 2;
                    if (downscaleDescriptor.width < 2 || downscaleDescriptor.height < 2)
                    {
                        iterations = i;
                        break;
                    }

                    RenderTargetHandle tempHandle = new RenderTargetHandle();
                    tempHandle.Init("_Sample" + i);
                    cmd.GetTemporaryRT(tempHandle.id, downscaleDescriptor);
                    renderTargetHandles.Add(tempHandle);

                    if (i == 0)
                    {
                        Blit(cmd, renderSource, tempHandle.Identifier(), rescaleMaterial);
                    }
                    else
                    {
                        Blit(cmd, currentSource.Identifier(), tempHandle.Identifier(), rescaleMaterial);
                    }

                    currentSource = tempHandle;
                }

                rescaleMaterial.SetFloat("_Rescale_UVOffset", 0.5f);
                for (int i = 1; i < renderTargetHandles.Count; i++)
                {
                    currentTarget = renderTargetHandles[renderTargetHandles.Count - i - 1];
                    Blit(cmd, currentSource.Identifier(), currentTarget.Identifier(), rescaleMaterial);
                    currentSource = currentTarget;
                }

                
                cmd.GetTemporaryRT(finalUpscaleHandle.id, rtDescriptor);
                Blit(cmd, currentSource.Identifier(), finalUpscaleHandle.Identifier(), rescaleMaterial);
                asciiSource = finalUpscaleHandle.id;
            }

            Blit(cmd, asciiSource, asciiRenderTarget.Identifier(), asciiMaterial);
            Blit(cmd, asciiRenderTarget.Identifier(), renderSource);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        /// Cleanup any allocated resources that were created during the execution of this render pass.
        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(asciiRenderTarget.id);
            cmd.ReleaseTemporaryRT(currentSource.id);
            cmd.ReleaseTemporaryRT(currentTarget.id);
            for(int i= 0; i < renderTargetHandles.Count; i++)
            {
                cmd.ReleaseTemporaryRT(renderTargetHandles[i].id);
            }
        }
    }

    [System.Serializable]
    public class Settings
    {
        [Header("Font Settings")]
        public Texture2D fontAsset;

        [Min(1)]
        public int numberOfCharacters = 10;
        [Min(1)]
        public Vector2Int resolution = new Vector2Int(64, 32);
        [Min(1)]
        public float fontRatio = 3;

        [Header("Display Settings")]
        [ColorUsage(false, true)]
        public Color fontColor = Color.white;
        [Range(0f, 1f)]
        public float fontColorStrength = 1f;
        [ColorUsage(false, true)]
        public Color backingColor = Color.black;
        [Range(0f, 1f)]
        public float backingColorStrength = 0.2f;

        [Header("Rescaling Settings")]
        [Range(0,8)]
        public int iterations = 4;


        [Header("Rendering Settings")]
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    CustomRenderPass m_ScriptablePass;
    public Settings settings = new Settings();
    ASCIIShaderData shaderData;

    public override void Create()
    {
        m_ScriptablePass = new CustomRenderPass();

        
        m_ScriptablePass.InitializeRenderTextures(settings.iterations);
        m_ScriptablePass.SetIterations(settings.iterations);

        // Configures where the render pass should be injected.
        if (settings.renderPassEvent == RenderPassEvent.AfterRenderingPostProcessing)
            settings.renderPassEvent = RenderPassEvent.AfterRendering;

        m_ScriptablePass.renderPassEvent = settings.renderPassEvent;

    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {

        shaderData = new ASCIIShaderData(
            settings.numberOfCharacters,
            settings.resolution,
            settings.fontRatio,
            settings.fontColor,
            settings.fontColorStrength,
            settings.backingColor,
            settings.backingColorStrength,
            settings.fontAsset
            );

        m_ScriptablePass.SetupASCIIMaterial(shaderData);
        m_ScriptablePass.InitializeRescaleMaterial();
        bool isAfterRendering = settings.renderPassEvent == RenderPassEvent.AfterRendering ? true : false;
        m_ScriptablePass.SetRenderEventState(isAfterRendering);
        m_ScriptablePass.SetSource(renderer.cameraColorTarget);
        renderer.EnqueuePass(m_ScriptablePass);
    }

    public class ASCIIShaderData
    {
        public int numberOfCharacters;
        public Vector4 resolution;
        public float fontRatio;
        public Color fontColor;
        public float fontColorStrength;
        public Color backingColor;
        public float backingColorStrength;
        public Texture2D fontAsset;

        public ASCIIShaderData(int numberOfCharacters, Vector2Int resolution, float fontRatio, Color fontColor, float fontColorStrength, Color backingColor, float backingColorStrength, Texture2D fontAsset)
        {
            this.numberOfCharacters = numberOfCharacters;
            this.resolution = new Vector4(resolution.x, resolution.y, 0, 0);
            this.fontRatio = fontRatio;
            this.fontColor = fontColor;
            this.fontColorStrength = fontColorStrength;
            this.backingColor = backingColor;
            this.backingColorStrength = backingColorStrength;
            this.fontAsset = fontAsset;
        }
    }

    private static class ShaderParams
    {
        public static int numberOfCharacters = Shader.PropertyToID("_ASCIICharacterCount");
        public static int resolution = Shader.PropertyToID("_ASCIIResolution");
        public static int inputTex = Shader.PropertyToID("_ASCIIInputTex");
        public static int fontRatio = Shader.PropertyToID("_ASCIIFontRatio");
        public static int fontColor = Shader.PropertyToID("_ASCIIFontColor");
        public static int fontColorStrength = Shader.PropertyToID("_ASCIIFontColorStrength");
        public static int backingColor = Shader.PropertyToID("_ASCIIBackingColor");
        public static int backingColorStrength = Shader.PropertyToID("_ASCIIBackingColorStrength");
        public static int fontAsset = Shader.PropertyToID("_ASCIIFontAsset");
    }
}


