using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using System.ComponentModel;

namespace OccaSoftware
{
    public class ASCIIRenderFeature : ScriptableRendererFeature
    {
        class CustomRenderPass : ScriptableRenderPass
        {

            private RenderTargetIdentifier source;

            private Material asciiMaterial;
            private RenderTargetHandle asciiRenderTarget;

            //For Rescaling
            private Material rescaleMaterial;
            List<RenderTargetHandle> renderTargetHandles;
            RenderTargetHandle finalUpscaleHandle;

            private int iterations = 5;
            ASCIIShaderData shaderData;

            public CustomRenderPass(int iterations)
            {
                this.iterations = iterations;
                InitializeRenderTextures(iterations);
            }

            public void SetShaderData(ASCIIShaderData shaderData)
            {
                this.shaderData = shaderData;
            }

            public void SetAsciiMaterialParameters()
            {
                if (asciiMaterial == null)
                    return;

                asciiMaterial.SetFloat(ShaderParams.numberOfCharacters, shaderData.numberOfCharacters);
                asciiMaterial.SetVector(ShaderParams.resolution, shaderData.resolution);
                asciiMaterial.SetFloat(ShaderParams.fontRatio, shaderData.fontRatio);
                asciiMaterial.SetColor(ShaderParams.fontColor, shaderData.fontColor);
                asciiMaterial.SetFloat(ShaderParams.fontColorStrength, shaderData.fontColorStrength);
                asciiMaterial.SetColor(ShaderParams.backingColor, shaderData.backingColor);
                asciiMaterial.SetFloat(ShaderParams.backingColorStrength, shaderData.backingColorStrength);
                asciiMaterial.SetTexture(ShaderParams.fontAsset, shaderData.fontAsset);
            }

            public void SetSource(RenderTargetIdentifier source)
            {
                this.source = source;
            }


            public void InitializeRenderTextures(int iterations)
            {
                asciiRenderTarget.Init("_ASCIITarget");
                if (iterations >= 1)
                {
                    finalUpscaleHandle.Init("_FinalUpscale");
                }
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

                CoreUtils.Destroy(asciiMaterial);
                CoreUtils.Destroy(rescaleMaterial);
                asciiMaterial = CoreUtils.CreateEngineMaterial("Shader Graphs/ASCII Shader");

                if (iterations >= 1)
                    rescaleMaterial = CoreUtils.CreateEngineMaterial("Shader Graphs/Image Filtering Shader");
            }

            // Here you can implement the rendering logic.
            // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
            // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
            // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                CommandBuffer cmd = CommandBufferPool.Get("ASCIIPass");

                if (asciiMaterial == null)
                    return;

                source = renderingData.cameraData.renderer.cameraColorTarget;
                RenderTextureDescriptor rtDescriptor = renderingData.cameraData.cameraTargetDescriptor;
                rtDescriptor.colorFormat = RenderTextureFormat.DefaultHDR;

                SetAsciiMaterialParameters();

                if (iterations >= 1)
                {
                    if (rescaleMaterial == null)
                        return;

                    renderTargetHandles = new List<RenderTargetHandle>();
                    RenderTextureDescriptor downscaleDescriptor = rtDescriptor;
                    rescaleMaterial.SetFloat("_Rescale_UVOffset", 1.0f);

                    for (int i = 0; i < iterations; i++)
                    {
                        downscaleDescriptor.width = (int)(downscaleDescriptor.width * 0.5f);
                        downscaleDescriptor.height = (int)(downscaleDescriptor.height * 0.5f);
                        if (downscaleDescriptor.width < 2 || downscaleDescriptor.height < 2)
                        {
                            iterations = i;
                            break;
                        }

                        RenderTargetHandle tempHandle = new RenderTargetHandle();
                        tempHandle.Init("_Sample" + i);
                        cmd.GetTemporaryRT(tempHandle.id, downscaleDescriptor);
                        renderTargetHandles.Add(tempHandle);

                        RenderTargetIdentifier downscaleSource = i == 0 ? source : renderTargetHandles[i - 1].Identifier();
                        Blit(cmd, downscaleSource, tempHandle.Identifier(), rescaleMaterial);
                    }

                    rescaleMaterial.SetFloat("_Rescale_UVOffset", 0.5f);
                    for (int i = 1; i < renderTargetHandles.Count; i++)
                    {
                        RenderTargetHandle currentSource = renderTargetHandles[renderTargetHandles.Count - i];
                        RenderTargetHandle currentTarget = renderTargetHandles[renderTargetHandles.Count - i - 1];
                        Blit(cmd, currentSource.Identifier(), currentTarget.Identifier(), rescaleMaterial);
                    }

                    cmd.GetTemporaryRT(finalUpscaleHandle.id, rtDescriptor);
                    Blit(cmd, renderTargetHandles[0].id, finalUpscaleHandle.id, rescaleMaterial);
                }

                RenderTargetIdentifier asciiMatInputTex = iterations >= 1 ? finalUpscaleHandle.id : source;
                Blit(cmd, asciiMatInputTex, asciiRenderTarget.Identifier(), asciiMaterial);
                Blit(cmd, asciiRenderTarget.Identifier(), source);

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                CommandBufferPool.Release(cmd);
            }

            /// Cleanup any allocated resources that were created during the execution of this render pass.
            public override void FrameCleanup(CommandBuffer cmd)
            {

                cmd.ReleaseTemporaryRT(asciiRenderTarget.id);

                if (finalUpscaleHandle != null)
                    cmd.ReleaseTemporaryRT(finalUpscaleHandle.id);

                if (renderTargetHandles != null)
                {
                    for (int i = 0; i < renderTargetHandles.Count; i++)
                    {
                        cmd.ReleaseTemporaryRT(renderTargetHandles[i].id);
                    }
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
            public float fontRatio = 3;

            [Header("Display Settings")]
            [Min(1)]
            public int columnCount = 128;

            public AspectRatio aspectRatioDesc = AspectRatio.OneToOne;
            [HideInInspector]
            public Vector2Int aspectRatio = new Vector2Int(1, 1);
            public bool flipAspect = false;
            [ColorUsage(false, true)]
            public Color fontColor = Color.white;
            [Range(0f, 1f)]
            public float fontColorStrength = 0.0f;
            [ColorUsage(false, true)]
            public Color backingColor = Color.black;
            [Range(0f, 1f)]
            public float backingColorStrength = 0.8f;

            [Header("Rescaling Settings")]
            [Range(0, 8)]
            public int iterations = 2;
        }

        CustomRenderPass m_ScriptablePass;
        public Settings settings = new Settings();
        ASCIIShaderData shaderData;

        [HideInInspector]
        public Vector2Int cachedScreenResolution;
        [HideInInspector]
        public int cachedColumnCount;

        Vector2Int screenSize;

        public override void Create()
        {
            m_ScriptablePass = new CustomRenderPass(settings.iterations);
            screenSize = new Vector2Int(Screen.width, Screen.height);
            cachedColumnCount = settings.columnCount;
            UpdateResolutionParam();

            // Configures where the render pass should be injected.
            m_ScriptablePass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        private void UpdateResolutionParam()
        {
            settings.aspectRatio = GetAspectRatio(settings.aspectRatioDesc, settings.aspectRatio, settings.flipAspect);
            cachedScreenResolution = new Vector2Int(settings.columnCount, 1);
            cachedScreenResolution.y = (int)(((float)Screen.height / Screen.width) * cachedScreenResolution.x * ((float)settings.aspectRatio.x / settings.aspectRatio.y));
        }

        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (screenSize.x != Screen.width || screenSize.y != Screen.height || cachedColumnCount != settings.columnCount)
            {
                cachedColumnCount = settings.columnCount;
                screenSize = new Vector2Int(Screen.width, Screen.height);
                UpdateResolutionParam();
            }


            shaderData = new ASCIIShaderData(
                settings.numberOfCharacters,
                cachedScreenResolution,
                settings.fontRatio,
                settings.fontColor,
                settings.fontColorStrength,
                settings.backingColor,
                settings.backingColorStrength,
                settings.fontAsset
                );

            m_ScriptablePass.SetShaderData(shaderData);
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

        public Vector2Int GetAspectRatio(AspectRatio aspectRatioDesc, Vector2Int customAspectRatio, bool flip)
        {
            Vector2Int temp = GetAspectRatio(aspectRatioDesc, customAspectRatio);
            if (flip)
                temp = new Vector2Int(temp.y, temp.x);

            return temp;
        }

        public Vector2Int GetAspectRatio(AspectRatio aspectRatioDesc, Vector2Int customAspectRatio)
        {
            switch (aspectRatioDesc)
            {
                case AspectRatio.SixteenToNine:
                    return new Vector2Int(16, 9);
                case AspectRatio.SixteenToTen:
                    return new Vector2Int(16, 10);
                case AspectRatio.ThreeToTwo:
                    return new Vector2Int(3, 2);
                case AspectRatio.FourToThree:
                    return new Vector2Int(4, 3);
                case AspectRatio.FiveToFour:
                    return new Vector2Int(5, 4);
                case AspectRatio.OneToOne:
                    return new Vector2Int(1, 1);
                default:
                    return new Vector2Int(1, 1);
            }

        }

        public enum AspectRatio
        {
            [InspectorName("16:9")] SixteenToNine,
            [InspectorName("16:10")] SixteenToTen,
            [InspectorName("3:2")] ThreeToTwo,
            [InspectorName("4:3")] FourToThree,
            [InspectorName("5:4")] FiveToFour,
            [InspectorName("1:1")] OneToOne
        }
    }



}
