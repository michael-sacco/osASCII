using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class EditShader : MonoBehaviour
{

    [SerializeField] ForwardRendererData rendererData;
    ASCIIRenderFeature renderFeature;
    // Start is called before the first frame update
    void Start()
    {
        renderFeature = rendererData.rendererFeatures.OfType<ASCIIRenderFeature>().FirstOrDefault();
        StartCoroutine(Update1());
        StartCoroutine(Update2());
        StartCoroutine(Update3());
    }

    IEnumerator Update1()
    {
        yield return new WaitForSeconds(5);

        renderFeature.settings.columnCount = 32;
        renderFeature.settings.backingColorStrength = 1.0f;
        renderFeature.settings.backingColor = Color.black;
        renderFeature.settings.fontColorStrength = 1.0f;
        renderFeature.settings.fontColor = Color.green;
        renderFeature.settings.iterations = 0;

        rendererData.SetDirty();
    }


    IEnumerator Update2()
    {
        yield return new WaitForSeconds(10);

        renderFeature.settings.columnCount = 64;
        renderFeature.settings.fontColor = Color.white;
        renderFeature.settings.fontColorStrength = 0.0f;
        renderFeature.settings.aspectRatioDesc = ASCIIRenderFeature.AspectRatio.SixteenToNine;
        renderFeature.settings.backingColorStrength = 0.0f;
        renderFeature.settings.iterations = 5;

        rendererData.SetDirty();
    }


    IEnumerator Update3()
    {
        yield return new WaitForSeconds(15);

        renderFeature.settings.backingColorStrength = 1.0f;
        renderFeature.settings.fontColorStrength = 1.0f;
        renderFeature.settings.iterations = 2;

        rendererData.SetDirty();
    }

}
