using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

using Sirenix.OdinInspector;

public class Spellbook : MonoBehaviour
{
    [SerializeField]
    private SpellCollection spellCollection = null;
    [SerializeField]
    private List<SpellData> knownSpells = new List<SpellData>();

    public List<SpellData> GetKnownSpells() => knownSpells;

    public UnityEvent<SpellData> OnSpellLearned = new UnityEvent<SpellData>();

    public int KnownSpellCount => knownSpells.Count;

    [Button]
    public SpellData GetKnownSpell(int index)
    {
        SpellData spell = null;

        if (index >= 0 && index < knownSpells.Count)
        {
            spell = knownSpells[index];
        }

        return spell;
    }

    [Button]
    public void LearnSpell(int collectionIndex)
    {
        var learnableSpells = spellCollection.spells;

        if (collectionIndex < 0 || collectionIndex  >= learnableSpells.Count)
        {
            Debug.LogError($"Can't learn spell with '{collectionIndex}' collection index: out of range!");
            return;
        }

        SpellData spellToLearn = learnableSpells[collectionIndex];
        if (knownSpells.Contains(spellToLearn))
        {
            Debug.LogError($"Can't learn spell with '{collectionIndex}' collection index: spell '{spellToLearn.spellName}' already learned.");
            return;
        }

        knownSpells.Add(spellToLearn);
        OnSpellLearned?.Invoke(spellToLearn);
    }
}
