using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Sirenix.OdinInspector;

public enum SpellName
{
    // Fire
    PyreballSeal, // To boil water, cook or burn ingredients
    FireExtinguisher, // Removes all fire
    PhantasmalFireball, // Visual, a strange fire that lights but doesn't burn or even heat
    SnugstoneSpell, // Warms anything around

    // Water
    RainbringerSeal, // For watering plants
    WaterBarrier, // Makes a spherical shield that blocks the rain

    WaterPen, // Creates a water pen that can be mixed with inks to draw over almost any surface

    // Earth
    BoulderStretchRope, // Tranforms stone into a flexible ribbon
    Integration, // Shapes the particles of powder to mimic the form of the original object
    WallBreakerSeal, // Useful to break through rock or to make powder from an object

    // Wind
    GraspingWind, // Creates a current of wind towards the object

    SylphShoesSeal, // For making an object fly

    // Light
    BirdOfLightBeacon, // Visual, a bird of light appears and flies arounde
    FloatGlowLamp, // Lights like a lamp, more visual than useful
    LightBeam, // Visual, a column of light flies from the center and straight

    DancingLight, // Visual, creates fireflies-like points of light that fly around

    // Mixed
    FloatingDrops, // Cools down the ambient temperature by flying water droplets around
    Rainflinger, // Repels or dries water from surrounding areas

    // Other
    FloatingExpansion, // Makes the object levitate and grow
    RepetitionSeal, // Makes a broken or rotten object return to its original form
    GatheringShadows, // Makes an object disappear into shadow
    SpellOfReduction, // Shrinks an object

    WindowwaySpell, // Opens a door/way to another place with the same spell

    MirrorSpell, // Mimics the shape of what's in front
}

public enum SpellNature
{
    None,

    Fire,
    Earth,
    Water,
    Wind,
    Light,

    Mixed
}

[System.Serializable]
public struct SpellValueData
{
    public Texture2D valueMap;

    public int reds;
    public int greens;
    public int blues;
    public int blanks;
}

[CreateAssetMenu(menuName = "Spells/SpellData", fileName = "SpellData")]
public class SpellData : ScriptableObject
{
    public string spellName = "";
    [TextArea]
    public string spellDescription = "";
    public SpellNature spellType = SpellNature.None;

    public SpellEffects spellEffects = null;

    public Texture2D bookInfo = null;
    public Texture2D pattern = null;

    [OnValueChanged("GenerateValueData")]
    public SpellValueData valueData = new SpellValueData();

    [Button]
    public void GenerateValueData()
    {
        int reds = 0;
        int greens = 0;
        int blues = 0;
        int blanks = 0;

        if (valueData.valueMap != null)
        {
            var values = valueData.valueMap.GetPixels32();
            foreach (var value in values)
            {
                reds += value.r;
                greens += value.g;
                blues += value.b;
            }

            reds /= byte.MaxValue;
            greens /= byte.MaxValue;
            blues /= byte.MaxValue;
            int total = values.Length;
            blanks = total - (reds + greens + blues);
        }

        valueData.reds = reds;
        valueData.greens = greens;
        valueData.blues = blues;
        valueData.blanks = blanks;
    }
}