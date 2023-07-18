using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Collection", fileName = "SpellCollection")]
public class SpellCollection : ScriptableObject
{
    public List<SpellData> spells = new List<SpellData>();
}
