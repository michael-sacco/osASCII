using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using System;
using UnityEngine.Events;

public class EditShader : MonoBehaviour
{

    [SerializeField] ForwardRendererData rendererData;
    ASCIIRenderFeature renderFeature;
    ASCIIRenderFeature.ASCIIShaderData cachedShaderData;
    int columnCount;
    Action<ShaderData> updateShader;

    public enum Set
    {
        None,
        HighResBlurry,
        HighResSharp,
        HighResSharpColor,
        LowResBlurry,
        Inverse,
        Matrix,
        Pixelate,
        PixelateHighRes
    }

    public Set setStyle = Set.None;

    void Start()
    {
        renderFeature = rendererData.rendererFeatures.OfType<ASCIIRenderFeature>().FirstOrDefault();
        CacheShaderData();
        updateShader = UpdateShaderParams;
        updateShader(HighResSharpColor);
    }

    void OnValidate()
    {
        switch (setStyle)
        {
            case Set.HighResBlurry:
                UpdateShaderParams(HighResBlurry);
                break;
            case Set.HighResSharp:
                UpdateShaderParams(HighResSharp);
                break;
            case Set.HighResSharpColor:
                UpdateShaderParams(HighResSharpColor);
                break;
            case Set.Inverse:
                UpdateShaderParams(Inverse);
                break;
            case Set.LowResBlurry:
                UpdateShaderParams(LowResBlurry);
                break;
            case Set.Matrix:
                UpdateShaderParams(Matrix);
                break;
            case Set.Pixelate:
                UpdateShaderParams(Pixelate);
                break;
            case Set.PixelateHighRes:
                UpdateShaderParams(PixelateHighRes);
                break;
            default:
                break;
        }

        ResetStyle();
    }

    void ResetStyle()
    {
        setStyle = Set.None;
    }


    ShaderData HighResBlurry
    {
        get 
        { 
            return new ShaderData(128, ASCIIRenderFeature.AspectRatio.OneToOne, Color.white, 0.0f, Color.black, 0.8f, 4); 
        }
    }

    ShaderData HighResSharp
    {
        get
        {
            return new ShaderData(128, ASCIIRenderFeature.AspectRatio.OneToOne, Color.white, 1.0f, Color.black, 1.0f, 3);
        }
    }

    ShaderData HighResSharpColor
    {
        get
        {
            return new ShaderData(96, ASCIIRenderFeature.AspectRatio.OneToOne, Color.white, 0.0f, Color.black, 0.8f, 3);
        }
    }

    ShaderData LowResBlurry
    {
        get
        {
            return new ShaderData(48, ASCIIRenderFeature.AspectRatio.OneToOne, Color.white, 1.0f, Color.black, 0.6f, 5);
        }
    }

    ShaderData Inverse
    {
        get
        {
            return new ShaderData(72, ASCIIRenderFeature.AspectRatio.OneToOne, Color.black, 1.0f, Color.white, 1.0f, 3);
        }
    }

    ShaderData Matrix
    {
        get
        {
            return new ShaderData(64, ASCIIRenderFeature.AspectRatio.OneToOne, Color.green, 1.0f, Color.black, 1.0f, 3);
        }
    }

    ShaderData Pixelate
    {
        get
        {
            return new ShaderData(64, ASCIIRenderFeature.AspectRatio.OneToOne, Color.white, 0.0f, Color.black, 0.0f, 3);
        }
    }

    ShaderData PixelateHighRes
    {
        get
        {
            return new ShaderData(128, ASCIIRenderFeature.AspectRatio.OneToOne, Color.white, 0.0f, Color.black, 0.0f, 3);
        }
    }


    private void OnDisable()
    {
        ResetToInitialVals();
    }

    private void CacheShaderData()
    {
        cachedShaderData = new ASCIIRenderFeature.ASCIIShaderData(renderFeature.settings.numberOfCharacters, renderFeature.cachedScreenResolution, renderFeature.settings.fontRatio, renderFeature.settings.fontColor, renderFeature.settings.fontColorStrength, renderFeature.settings.backingColor, renderFeature.settings.backingColorStrength, renderFeature.settings.fontAsset);
        columnCount = renderFeature.settings.columnCount;
    }

    private void ResetToInitialVals()
    {
        if (renderFeature == null)
            return;


        renderFeature.settings.columnCount = columnCount;
        renderFeature.settings.numberOfCharacters = cachedShaderData.numberOfCharacters;
        renderFeature.cachedScreenResolution = new Vector2Int((int)cachedShaderData.resolution.x, (int)cachedShaderData.resolution.y);
        renderFeature.settings.fontRatio = cachedShaderData.fontRatio;
        renderFeature.settings.fontColor = cachedShaderData.fontColor;
        renderFeature.settings.fontColorStrength= cachedShaderData.fontColorStrength;
        renderFeature.settings.backingColor = cachedShaderData.backingColor;
        renderFeature.settings.backingColorStrength = cachedShaderData.backingColorStrength;
        renderFeature.settings.fontAsset = cachedShaderData.fontAsset;
    }


    IEnumerator WaitForThenSetDirty(int timing, Action<ShaderData> action, ShaderData data)
    {
        yield return new WaitForSeconds(timing);
        action(data);
        rendererData.SetDirty();
    }

    public void UpdateShaderParams(int columnCount, ASCIIRenderFeature.AspectRatio aspect, Color fontColor, float fontColorStrength, Color backingColor, float backingColorStrength, int iterations)
    {
        if (renderFeature == null)
            return;

        renderFeature.settings.columnCount = columnCount;
        renderFeature.settings.aspectRatioDesc = aspect;
        renderFeature.settings.fontColor = fontColor;
        renderFeature.settings.fontColorStrength = fontColorStrength;
        renderFeature.settings.backingColor = backingColor;
        renderFeature.settings.backingColorStrength = backingColorStrength;
        renderFeature.settings.iterations = iterations;
    }

    void UpdateShaderParams(ShaderData shaderData)
    {
        if (renderFeature == null)
            return;

        renderFeature.settings.columnCount = shaderData.columnCount.data;
        renderFeature.settings.aspectRatioDesc = shaderData.aspect.data;
        renderFeature.settings.fontColor = shaderData.fontColor.data;
        renderFeature.settings.fontColorStrength = shaderData.fontColorStrength.data;
        renderFeature.settings.backingColor = shaderData.backingColor.data;
        renderFeature.settings.backingColorStrength = shaderData.backingColorStrength.data;
        renderFeature.settings.iterations = shaderData.iterations.data;
    }



    // There's gotta be a better way to do this lmao
    class ShaderData
    {
        public IntData columnCount;
        public AspectData aspect;
        public ColorData fontColor;
        public FloatData fontColorStrength;
        public ColorData backingColor;
        public FloatData backingColorStrength;
        public IntData iterations;

        public ShaderData(int columnCount, ASCIIRenderFeature.AspectRatio aspect, Color fontColor, float fontColorStrength, Color backingColor, float backingColorStrength, int iterations)
        {
            this.columnCount = new IntData(columnCount);
            this.aspect = new AspectData(aspect);
            this.fontColor = new ColorData(fontColor);
            this.fontColorStrength = new FloatData(fontColorStrength);
            this.backingColor = new ColorData(backingColor);
            this.backingColorStrength = new FloatData(backingColorStrength);
            this.iterations = new IntData(iterations);
        }
    }


    class IntData
    {
        public int data;

        public IntData(int data)
        {
            this.data = data;
        }
    }

    class FloatData
    {
        public float data;

        public FloatData(float data)
        {
            this.data = data;
        }
    }

    class ColorData
    {
        public Color data;

        public ColorData(Color data)
        {
            this.data = data;
        }
    }

    class AspectData
    {
        public ASCIIRenderFeature.AspectRatio data;

        public AspectData(ASCIIRenderFeature.AspectRatio data)
        {
            this.data = data;
        }
    }
}
