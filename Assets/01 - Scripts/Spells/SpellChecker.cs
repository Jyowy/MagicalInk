using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Sirenix.OdinInspector;
using UnityEngine.Events;

public enum SpellCastResult
{
    Failed,
    Bad,
    Fair,
    Good,
    Great
}

public class SpellChecker : MonoBehaviour
{
    [SerializeField]
    private SpellCollection spellCollection = null;
    [SerializeField]
    private SpellCaster spellCaster = null;

    [SerializeField]
    private float minBlueRatio = 0.9f;
    [SerializeField]
    private float maxRedRatio = 0.01f;
    [SerializeField]
    private float minGreenRatio = 0.4f;
    [SerializeField]
    private float goodGreenRatio = 0.6f;
    [SerializeField]
    private float idealMaxGreenRatio = 0.75f;

    private Coroutine checkCoroutine = null;

    public void CheckPapyrusSpell(Papyrus papyrus)
    {
        if (checkCoroutine != null)
        {
            StopCoroutine(checkCoroutine);
            checkCoroutine = null;
        }

        checkCoroutine = StartCoroutine(AsyncCheckAllSpells(papyrus));
    }

    public SpellCastResult CheckSpell(Papyrus papyrus, SpellData spell)
    {
        return CheckSpell(papyrus.GetDrawingData(), spell.valueData);
    }

    private IEnumerator AsyncCheckAllSpells(Papyrus papyrus)
    {
        if (papyrus.IsConsumed)
        {
            yield break;
        }

        Color32[] drawingData = papyrus.GetDrawingData();

        List<SpellData> spells = spellCollection.spells;
        foreach (var spell in spells)
        {
            SpellCastResult result = CheckSpell(drawingData, spell.valueData);

            Debug.Log($"Checked against spell {spell.spellName}: {result}");

            if (result != SpellCastResult.Failed)
            {
                CastSpell(papyrus, spell, result);

                break;
            }

            yield return null;
        }
    }

    [Button]
    public SpellCastResult CheckSpell(Color32[] drawingData, SpellValueData valueData)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        byte minAlpha = (byte)(byte.MaxValue * 0.1f);

        int redsPainted = 0;
        int greensPainted = 0;
        int bluesPainted = 0;
        int blanksPainted = 0;

        Color32[] valueMap = valueData.valueMap.GetPixels32();

        for (int i = 0; i < drawingData.Length; ++i)
        {
            if (drawingData[i].a > minAlpha)
            {
                if (valueMap[i].r > 0)
                {
                    redsPainted++;
                }
                else if (valueMap[i].g > 0)
                {
                    greensPainted++;
                }
                else if (valueMap[i].b > 0)
                {
                    bluesPainted++;
                }
                else
                {
                    blanksPainted++;
                }
            }
        }

        bool canScore = true;
        bool badResult = false;
        bool goodScore = false;
        bool greatScore = false;

        // Necessary blues
        float blueRatio = bluesPainted / (float)valueData.blues;
        if (blueRatio < minBlueRatio)
        {
            canScore = false;
        }

        // Disqualifying reds
        float redRatio = redsPainted / (float)valueData.reds;
        if (redRatio > maxRedRatio)
        {
            canScore = false;
        }

        // Score greens (+) and blanks (-)
        float blankRatio = blanksPainted / (float)valueData.blanks;
        float greenRatio = greensPainted / (float)valueData.greens;
        float balancedRatio = greenRatio - blankRatio - redRatio * 100f;
        if (balancedRatio > goodGreenRatio)
        {
            goodScore = true;
            if (balancedRatio > idealMaxGreenRatio)
            {
                greatScore = true;
            }
        }
        else if (balancedRatio < minGreenRatio)
        {
            badResult = true;
        }

        //Debug.Log($"Ratios: red {redRatio}, green {greenRatio}, blue {blueRatio}, black {blankRatio}");
        //Debug.Log($"Balanced ratio: {balancedRatio}");

        SpellCastResult result = SpellCastResult.Failed;
        if (canScore)
        {
            if (greatScore)
            {
                result = SpellCastResult.Great;
            }
            else if (goodScore)
            {
                result = SpellCastResult.Good;
            }
            else if (badResult)
            {
                result = SpellCastResult.Bad;
            }
            else
            {
                result = SpellCastResult.Fair;
            }
        }

        stopwatch.Stop();

        //Debug.Log($"Result {result}");
        Debug.Log($"Time: {stopwatch.ElapsedMilliseconds}ms");

        return result;
    }

    public void CastSpell(Papyrus papyrus, SpellData spell, SpellCastResult result)
    {
        spellCaster.Cast(spell, papyrus, result);
    }
}
