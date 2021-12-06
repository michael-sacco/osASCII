using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{

    float deltaTime;
    float realtimePrevious;
    float trailingAvg;

    float delayDuration = 3f;
    float timeSinceActive = 0f;

    bool startCheckingFrameData = false;

    TMPro.TextMeshProUGUI text;

    private void Start()
    {
        text = GetComponent<TMPro.TextMeshProUGUI>();
        text.SetText("");
    }

    void Update()
    {
        if (!startCheckingFrameData)
        {
            DoWait();
        }
        else
        {
            DoCheckFrameData();
        }
    }


    void DoWait()
    {
        timeSinceActive += Time.deltaTime;
        if (timeSinceActive > delayDuration)
        {
            startCheckingFrameData = true;
            trailingAvg = Time.deltaTime / 0.001f;
            realtimePrevious = Time.realtimeSinceStartup;
        }

    }

    void DoCheckFrameData()
    {
        float delta = Time.realtimeSinceStartup - realtimePrevious;
        realtimePrevious = Time.realtimeSinceStartup;
        delta /= 0.001f;
        trailingAvg = Mathf.Lerp(trailingAvg, delta, 0.005f);

        string trailingAvgStr = string.Format("{0:N2}", System.Math.Round(trailingAvg, 2));
        string deltaStr = string.Format("{0:N2}", System.Math.Round(delta, 2));
        string trailingFpsStr = string.Format("{0:N0}", 1000.0f / trailingAvg);
        string textToPrint = "trailing avg (ms) " + trailingAvgStr + "\ncurrent frame (ms) " + deltaStr + "\nFPS " + trailingFpsStr;

        text.SetText(textToPrint);
    }
}