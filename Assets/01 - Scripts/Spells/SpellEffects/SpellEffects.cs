using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

using Sirenix.OdinInspector;
using KVRL.KVRLENGINE.Utilities;
using UnityEngine.Playables;

public abstract class SpellEffects : MonoBehaviour
{
    public enum State
    {
        None,
        Start,
        Spell,
        End
    }

    [TitleGroup("Spell Settings", order: 100)]
    [BoxGroup("Spell Settings/Animations")]
    [SerializeField]
    private PlayableDirector director = null;
    [BoxGroup("Spell Settings/Animations")]
    [SerializeField]
    private PlayableAsset failedAnimation = null;
    [BoxGroup("Spell Settings/Animations")]
    [SerializeField]
    private PlayableAsset startAnimation = null;
    [BoxGroup("Spell Settings/Animations")]
    [SerializeField]
    private PlayableAsset loopAnimation = null;
    [BoxGroup("Spell Settings/Animations")]
    [SerializeField]
    private PlayableAsset endAnimation = null;

    [SerializeField]
    private bool hasDuration = true;
    [BoxGroup("Spell Settings/Duration", VisibleIf = "hasDuration")]
    [SerializeField]
    protected float spellDuration = 60.0f;
    [BoxGroup("Spell Settings/Duration")]
    [SerializeField]
    protected float fairCastDurationFactor = 0.5f;
    [BoxGroup("Spell Settings/Duration")]
    [SerializeField]
    protected float goodCastDurationFactor = 0.75f;

    [TitleGroup("Spell Settings")]
    [SerializeField]
    protected FollowObject followObject = null;

    [FoldoutGroup("Spell Settings/Events")]
    public UnityEvent OnBadCast = new UnityEvent();
    [FoldoutGroup("Spell Settings/Events")]
    public UnityEvent OnFairCast = new UnityEvent();
    [FoldoutGroup("Spell Settings/Events")]
    public UnityEvent OnGoodCast = new UnityEvent();
    [FoldoutGroup("Spell Settings/Events")]
    public UnityEvent OnGreatCast = new UnityEvent();
    [FoldoutGroup("Spell Settings/Events")]
    public UnityEvent OnCastFinished = new UnityEvent();
    [FoldoutGroup("Spell Settings/Events")]
    public UnityEvent OnSpellEnded = new UnityEvent();

    public void Cast(Transform origin, SpellCastResult castResult)
    {
        if (hasDuration)
        {
            float duration = spellDuration;
            if (castResult == SpellCastResult.Good)
            {
                duration *= goodCastDurationFactor;
            }
            else if (castResult == SpellCastResult.Fair)
            {
                duration *= fairCastDurationFactor;
            }
            else if (castResult == SpellCastResult.Bad)
            {
                duration = 0f;
            }

            if (duration > 0f)
            {
                TimedActions.Start(this, "Spell", duration, StopCast);
            }
        }

        CenterSpellToOrigin(origin);

        switch (castResult)
        {
            case SpellCastResult.Bad:
                BadCast();
                break;
            case SpellCastResult.Fair:
                FairCast();
                break;
            case SpellCastResult.Good:
                GoodCast();
                break;
            case SpellCastResult.Great:
                GreatCast();
                break;
        }

        if (castResult != SpellCastResult.Bad)
        {
            PlayStartAnimation();
        }
    }

    [BoxGroup("Spell Settings/Runtime Debug")]
    [Button]
    public virtual void StopCast()
    {
        PlayEndAnimation();

        OnCastFinished?.Invoke();
    }

    protected virtual void CenterSpellToOrigin(Transform origin)
    {
        followObject.parent = origin;
        followObject.automatic = true;
    }

    protected virtual void BadCast()
    {
        PlayFailedAnimation();

        OnBadCast?.Invoke();
    }

    protected virtual void FairCast()
    {
        OnFairCast.Invoke();
    }

    protected virtual void GoodCast()
    {
        OnGoodCast.Invoke();
    }

    protected virtual void GreatCast()
    {
        OnGreatCast?.Invoke();
    }

    private void PlayFailedAnimation()
    {
        director.playableAsset = failedAnimation;
        director.extrapolationMode = DirectorWrapMode.None;

        director.stopped += OnSpellFinished;
        director.Play();
    }

    private void PlayStartAnimation()
    {
        director.playableAsset = startAnimation;
        director.extrapolationMode = DirectorWrapMode.None;

        director.stopped += StartAnimationFinished;
        director.Play();
    }

    private void StartAnimationFinished(PlayableDirector _)
    {
        director.stopped -= StartAnimationFinished;

        PlayLoopAnimation();
    }

    private void PlayLoopAnimation()
    {
        director.playableAsset = loopAnimation;
        director.extrapolationMode = DirectorWrapMode.Loop;

        director.Play();
    }

    private void PlayEndAnimation()
    {
        director.playableAsset = endAnimation;
        director.extrapolationMode = DirectorWrapMode.None;

        director.stopped += OnSpellFinished;
        director.Play();

        Debug.Log($"Play end animation");
    }

    private void OnSpellFinished(PlayableDirector _)
    {
        Debug.Log($"Spell Finished");
        OnSpellEnded?.Invoke();

        Destroy(gameObject);
    }
}