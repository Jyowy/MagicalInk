using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

using Sirenix.OdinInspector;

public class PapyrusPile : MonoBehaviour
{
    [SerializeField]
    private Papyrus papyrusTemplate = null;
    [SerializeField]
    private Texture2D template = null;
    [SerializeField]
    private Transform papyrusRoot = null;
    [SerializeField]
    private Transform pileTop = null;
    [SerializeField]
    private Transform inactivePapyrusRoot = null;
    [SerializeField]
    private Paperball paperball = null;
    [SerializeField]
    private int maxSimultaneousPapyrus = 10;
    [SerializeField]
    private int maxInactivePapyrus = 20;
    [SerializeField]
    private float generationCD = 1f;

    [SerializeField]
    private PlayableDirector newPapyrusDirector = null;

    public UnityEvent<Papyrus> OnNewPapyrusTaken = new UnityEvent<Papyrus>();
    public UnityEvent<Papyrus> OnPapyrusGenerated = new UnityEvent<Papyrus>();

    [ShowInInspector, ReadOnly]
    private Papyrus topPapyrus = null;
    [ShowInInspector, ReadOnly]
    private readonly List<Papyrus> activePapyrus = new List<Papyrus>();
    [ShowInInspector, ReadOnly]
    private readonly List<GameObject> inactivePapyrus = new List<GameObject>();

    private Texture2D currentHelper = null;
    private Texture2D transparentTexture = null;
    private bool recentlyGenerated = false;

    private void Awake()
    {
        transparentTexture = new Texture2D(1, 1);
        transparentTexture.SetPixel(0, 0, Color.clear);
        transparentTexture.Apply();

        TryToGenerateNewPapyrus();
    }

    [Button]
    public void TryToGenerateNewPapyrus()
    {
        if (recentlyGenerated)
        {
            return;
        }

        if (activePapyrus.Count < maxSimultaneousPapyrus)
        {
            recentlyGenerated = true;
            TimedActions.Start(this, generationCD, GeneratedCDFinished);

            topPapyrus = Instantiate(papyrusTemplate, pileTop);
            if (template != null)
            {
                topPapyrus.ApplyTemplate(template);
            }
            topPapyrus.OnGrabStarted?.AddListener(TopPapyrusGrabbed);
            topPapyrus.OnConsumed?.AddListener(PapyrusConsumed);
            topPapyrus.OnDestroyed?.AddListener(PapyrusDestroyed);
            newPapyrusDirector.time = 0.0;
            newPapyrusDirector.Play();

            OnPapyrusGenerated?.Invoke(topPapyrus);
        }
    }

    private void GeneratedCDFinished()
    {
        recentlyGenerated = false;
        if (topPapyrus == null)
        {
            TryToGenerateNewPapyrus();
        }
    }

    private void TopPapyrusGrabbed()
    {
        if (topPapyrus != null)
        {
            activePapyrus.Add(topPapyrus);
            topPapyrus.transform.SetParent(papyrusRoot);
            topPapyrus.OnGrabStarted?.RemoveListener(TopPapyrusGrabbed);
            topPapyrus.SetHelper(currentHelper);

            OnNewPapyrusTaken?.Invoke(topPapyrus);

            topPapyrus = null;
        }

        TryToGenerateNewPapyrus();
    }

    private void PapyrusConsumed(Papyrus papyrus)
    {
        SetPapyrusInactive(papyrus);

        if (topPapyrus == null)
        {
            TryToGenerateNewPapyrus();
        }
    }

    private void SetPapyrusInactive(Papyrus papyrus)
    {
        activePapyrus.Remove(papyrus);
        inactivePapyrus.Add(papyrus.gameObject);

        while (inactivePapyrus.Count > maxInactivePapyrus)
        {
            GameObject papyrusToDestroy = inactivePapyrus[0];
            inactivePapyrus.RemoveAt(0);
            Destroy(papyrusToDestroy);
        }
    }

    private void PapyrusDestroyed(Papyrus papyrus)
    {
        if (activePapyrus.Contains(papyrus))
        {
            SetPapyrusInactive(papyrus);
        }

        ChangePapyrusForPaperball(papyrus);
    }

    private void ChangePapyrusForPaperball(Papyrus papyrus)
    {
        bool isBurned = papyrus.isBurned;

        Transform position = papyrus.transform;
        var ball = Instantiate(paperball, position.position, Quaternion.identity, inactivePapyrusRoot);
        inactivePapyrus.Add(ball.gameObject);
        if (isBurned)
        {
            ball.Ignite(0f);
        }

        Destroy(papyrus.gameObject);

        if (topPapyrus == papyrus
            || topPapyrus == null)
        {
            TryToGenerateNewPapyrus();
        }

        inactivePapyrus.RemoveAll(go => go == null);
    }

    public void SetHelper(SpellData spell)
    {
        Texture2D pattern = transparentTexture;
        if (spell != null)
        {
            pattern = spell.pattern;
        }

        currentHelper = pattern;
        foreach (var papyrus in activePapyrus)
        {
            papyrus.SetHelper(pattern);
        }
    }
}
