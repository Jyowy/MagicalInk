using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Sirenix.OdinInspector;

public class SpellCaster : MonoBehaviour
{
    [ShowInInspector, ReadOnly]
    private List<SpellEffects> activeEffects = new List<SpellEffects>();

    private Dictionary<Papyrus, SpellEffects> papyrusSpellRecord = new Dictionary<Papyrus, SpellEffects>();

    public void Cast(SpellData spell, Papyrus papyrus, SpellCastResult result)
    {
        Transform spellOrigin = papyrus.GetSpellAnchorPoint();
        SpellEffects spellToCast = Instantiate(spell.spellEffects, spellOrigin.position, Quaternion.identity);

        activeEffects.Add(spellToCast);
        spellToCast.Cast(spellOrigin, result);

        papyrus.Consume();
        papyrus.OnDestroyed?.AddListener(OnPapyrusDestroyed);
        papyrus.OnBurned?.AddListener(OnPapyrusDestroyed);
        papyrusSpellRecord.Add(papyrus, spellToCast);
    }

    private void OnPapyrusDestroyed(Papyrus papyrus)
    {
        papyrus.OnDestroyed?.RemoveListener(OnPapyrusDestroyed);
        papyrus.OnBurned?.RemoveListener(OnPapyrusDestroyed);

        if (papyrusSpellRecord.ContainsKey(papyrus))
        {
            var spell = papyrusSpellRecord[papyrus];
            spell.StopCast();
        }
        else
        {
            foreach (var key in papyrusSpellRecord.Keys)
            {
                if (key == null)
                {
                    var spell = papyrusSpellRecord[key];
                    spell.StopCast();
                }
            }
        }
    }

    [SerializeField]
    private SpellData debugSpellToCast = null;
    [SerializeField]
    private Papyrus debugPapyrus = null;
    [SerializeField]
    private SpellCastResult debugResult = SpellCastResult.Great;

    [Button]
    public void DebugCastSpell()
    {
        Cast(debugSpellToCast, debugPapyrus, debugResult);
    }

    [Button]
    public void StopAllActiveEffects()
    {
        foreach (var spellEffect in activeEffects)
        {
            if (spellEffect != null)
            {
                spellEffect.StopCast();
            }
        }

        activeEffects.Clear();
    }
}
