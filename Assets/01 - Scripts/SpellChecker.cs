using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Sirenix.OdinInspector;

public class SpellChecker : MonoBehaviour
{
    [SerializeField]
    private Papyrus papyrus = null;

    [SerializeField]
    private Texture2D helper = null;
    [SerializeField]
    private Texture2D valueMap = null;

    [SerializeField]
    private float minBlueRatio = 0.9f;
    [SerializeField]
    private float maxRedRatio = 0.01f;
    [SerializeField]
    private float goodGreenRatio = 0.4f;
    [SerializeField]
    private float idealMaxGreenRatio = 0.75f;

    private int reds = 0;
    private int greens = 0;
    private int blues = 0;
    private int blacks = 0;

    //private Color32[] helperData = null;
    private Color32[] valueData = null;

    private void Start()
    {
        if (helper != null)
        {
            SetupHelper(helper, valueMap);
        }
    }

    [Button]
    public void SetupHelper(Texture2D newHelper, Texture2D valueMap)
    {
        helper = newHelper;
        this.valueMap = valueMap;

        valueData = valueMap.GetPixels32();
        reds = 0;
        greens = 0;
        blues = 0;
        foreach (var value in valueData)
        {
            reds += value.r;
            greens += value.g;
            blues += value.b;
        }

        reds /= byte.MaxValue;
        greens /= byte.MaxValue;
        blues /= byte.MaxValue;

        int total = valueData.Length;
        blacks = total - (reds + greens + blues);

        Debug.Log($"Total {total}, Reds {reds}, Greens {greens}, Blues {blues}, Blacks {blacks}");

        ShowHelper();
    }

    [Button]
    private void ShowHelper()
    {
        papyrus.SetHelper(helper);
    }

    [Button]
    private void RemoveHelper()
    {
        papyrus.RemoveHelper();
    }

    [Button]
    public void CheckSpell()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var drawingData = papyrus.GetDrawingData();

        byte minAlpha = (byte)(byte.MaxValue * 0.1f);

        int redsPainted = 0;
        int greensPainted = 0;
        int bluesPainted = 0;
        int blacksPainted = 0;

        for (int i = 0; i < drawingData.Length; ++i)
        {
            if (drawingData[i].a > minAlpha)
            {
                if (valueData[i].r > 0)
                {
                    redsPainted++;
                }
                else if (valueData[i].g > 0)
                {
                    greensPainted++;
                }
                else if (valueData[i].b > 0)
                {
                    bluesPainted++;
                }
                else
                {
                    blacksPainted++;
                }
            }
        }

        float score = 0f;
        bool canScore = true;
        bool goodScore = false;
        bool greatScore = false;

        // Necessary blues
        float blueRatio = bluesPainted / (float)blues;
        if (blueRatio < minBlueRatio)
        {
            Debug.Log($"Failed because too few blues");
            canScore = false;
        }

        // Disqualifying reds
        float redRatio = redsPainted / (float)reds;
        if (redRatio > maxRedRatio)
        {
            Debug.Log($"Failed because too many red");
            canScore = false;
        }

        // Score greens (+) and blacks (-)
        float blackRatio = blacksPainted / (float)blacks;
        float greenRatio = greensPainted / (float)greens;
        float balancedRatio = greenRatio - blackRatio;
        if (balancedRatio > goodGreenRatio)
        {
            goodScore = true;
            if (balancedRatio > idealMaxGreenRatio)
            {
                Debug.Log($"Ideal amount of green");
                greatScore = true;
            }
            else
            {
                Debug.Log($"Good amount of green");
            }
        }
        else
        {
            if (greenRatio < goodGreenRatio)
            {
                Debug.Log($"Barely good because too few greens.");
            }
            else
            {
                Debug.Log($"Barely good because too many blacks.");
            }
        }


        Debug.Log($"Ratios: red {redRatio}, green {greenRatio}, blue {blueRatio}, black {blackRatio}");
        Debug.Log($"Balanced ratio: {balancedRatio}");

        string result = "Failed";
        if (canScore)
        {
            if (greatScore)
            {
                result = "Great";
            }
            else if (goodScore)
            {
                result = "Good";
            }
            else
            {
                result = "Fair enough";
            }
        }    
        Debug.Log($"Result {result}");

        stopwatch.Stop();
        Debug.Log($"Time: {stopwatch.ElapsedMilliseconds}ms");
    }
}
