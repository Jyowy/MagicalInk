using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;
using System;
using System.IO;

public static class CreateSpellMaps
{
    private readonly static int RedMinDistance = 55;
    private readonly static int GreenMaxDistance = 10;
    private readonly static float BlueThreshold = 0.4f;

    private readonly static int InfluenceSize = 55;

    [MenuItem("Assets/Create Value Map")]
    public static void CreateValueMap()
    {
        var texture = Selection.activeObject as Texture2D;
        if (texture == null)
        {
            return;
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        string textureName = texture.name;
        int width = texture.width;
        int height = texture.height;

        Texture2D valueMap = new Texture2D(width, height);
        string path = AssetDatabase.GetAssetPath(texture);
        string valueMapName = $"{path.Substring(0, path.Length - 4)}_ValueMap.png";

        Debug.Log($"Texture {textureName} ({width} x {height})");
        Debug.Log($"Texture path: {path}");
        Debug.Log($"Value Map Path: {valueMapName}");

        var textureData = texture.GetPixels32();
        var valueData = valueMap.GetPixels32();
        int count = textureData.Length;

        float[] values = new float[valueData.Length];

        byte maxByte = byte.MaxValue;
        byte minAlpha = (byte)(maxByte * 0.1f);

        float maxDistance = Mathf.Sqrt(width * width + height * height);
        float maxDistance2 = maxDistance * maxDistance;

        for (int i = 0; i < count; ++i)
        {
            if (textureData[i].a <= minAlpha)
            {
                values[i] = -maxDistance2;
            }
        }

        float farthestDistanceToEmptyPixel = 0f;

        for (int y = 0, index = 0; y < height; ++y)
        {
            for (int x = 0; x < width; ++x, ++index)
            {
                if (textureData[index].a > minAlpha)
                {
                    float distanceToClosestEmptyPixel = maxDistance2;

                    int ystart = Mathf.Max(y - InfluenceSize, 0);
                    int yend = Mathf.Min(y + InfluenceSize, height - 1);
                    int xstart = Mathf.Max(x - InfluenceSize, 0);
                    int xend = Mathf.Min(x + InfluenceSize, width - 1);

                    for (int y2 = ystart; y2 <= yend; ++y2)
                    {
                        int index2 = y2 * width + xstart;
                        int ydist = y2 - y;
                        int ydist2 = ydist * ydist;
                        for (int x2 = xstart; x2 <= xend; ++x2, ++index2)
                        {
                            if (textureData[index2].a <= minAlpha)
                            {
                                int xdist = x2 - x;
                                float dist = xdist * xdist + ydist2;
                                distanceToClosestEmptyPixel = Mathf.Min(distanceToClosestEmptyPixel, dist);
                                values[index2] = Mathf.Max(values[index2], -dist);
                            }
                        }
                    }

                    farthestDistanceToEmptyPixel = Mathf.Max(distanceToClosestEmptyPixel, farthestDistanceToEmptyPixel);
                    values[index] = distanceToClosestEmptyPixel;
                }
            }
        }

        float redThreshold = -(RedMinDistance * RedMinDistance);
        float greenMinThreshold = -(GreenMaxDistance * GreenMaxDistance);
        float blueThreshold = farthestDistanceToEmptyPixel * BlueThreshold * BlueThreshold;

        for (int i = 0; i < count; ++i)
        {
            Color32 color = new Color32(0, 0, 0, maxByte);
            float value = values[i];
            if (value < 0f)
            {
                if (value < redThreshold)
                {
                    color.r = maxByte;
                }
                else if (value > greenMinThreshold)
                {
                    color.g = maxByte;
                }
            }
            else if (value > 0f)
            {
                if (value > blueThreshold)
                {
                    color.b = maxByte;
                }
                else
                {
                    color.g = maxByte;
                }
            }

            valueData[i] = color;
        }

        long valuesCalculateTime = stopwatch.ElapsedMilliseconds;

        valueMap.SetPixels32(valueData);
        valueMap.Apply();

        SaveTexture(valueMap, valueMapName);

        long totalTime = stopwatch.ElapsedMilliseconds;

        stopwatch.Stop();

        Debug.Log($"Times (ms): {valuesCalculateTime}, total {totalTime}");
    }

    [MenuItem("Assets/Create Value Map", true)]
    public static bool CreateValueMapValidation()
    {
        return Selection.activeObject is Texture2D;
    }

    private static void SaveTexture(Texture2D texture, string textureName)
    {
        try
        {
            FileStream file = File.Open(textureName, FileMode.OpenOrCreate, FileAccess.Write);
            var data = texture.EncodeToPNG();
            file.Write(data);

            file.Close();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error while trying to save texture '{textureName}': {e.Message}");
        }
    }
}
