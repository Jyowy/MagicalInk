using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

using Sirenix.OdinInspector;

public class SpellbookVisualizer : MonoBehaviour
{
    [SerializeField]
    private Spellbook spellbook = null;
    [SerializeField]
    private Renderer firstLeftPage = null;
    [SerializeField]
    private Renderer firstRightPage = null;
    [SerializeField]
    private Renderer secondLeftPage = null;
    [SerializeField]
    private Renderer secondRightPage = null;

    [SerializeField]
    private Transform leftPosition = null;
    [SerializeField]
    private Transform rightPosition = null;
    [SerializeField]
    private GrabbableObject leftHandle = null;
    [SerializeField]
    private GrabbableObject rightHandle = null;
    [SerializeField]
    private float pageTurnDuration = 2f;
    [SerializeField]
    private GameObject flippingPage = null;

    [BoxGroup("Animations")]
    [SerializeField]
    private PlayableDirector director = null;
    [BoxGroup("Animations")]
    [SerializeField]
    private PlayableAsset openToLeftAnimation = null;
    [BoxGroup("Animations")]
    [SerializeField]
    private PlayableAsset openToRightAnimation = null;
    [BoxGroup("Animations")]
    [SerializeField]
    private PlayableAsset closeToRightAnimation = null;
    [BoxGroup("Animations")]
    [SerializeField]
    private PlayableAsset closeToLeftAnimation = null;
    [BoxGroup("Animations")]
    [SerializeField]
    private PlayableAsset turnLeftAnimation = null;
    [BoxGroup("Animations")]
    [SerializeField]
    private PlayableAsset turnRightAnimation = null;

    public UnityEvent<SpellData> onPageChanged = null;

    [ShowInInspector, ReadOnly]
    private bool isBusy = false;
    [ShowInInspector, ReadOnly]
    private int currentPage = -1;
    [ShowInInspector, ReadOnly]
    private SpellData currentSpell = null;

    [ShowInInspector, ReadOnly]
    private int nextPage = -1;

    [ShowInInspector, ReadOnly]
    private bool checkLeftHandle = false;
    [ShowInInspector, ReadOnly]
    private bool checkRightHandle = false;


    private void Awake()
    {
        leftHandle.OnGrabStarted?.AddListener(LeftHandleGrabbed);
        leftHandle.OnGrabStopped?.AddListener(LeftHandleReleased);
        rightHandle.OnGrabStarted?.AddListener(RightHandleGrabbed);
        rightHandle.OnGrabStopped?.AddListener(RightHandleReleased);

        flippingPage.SetActive(false);
    }

    private void Start()
    {
        director.playableAsset = closeToRightAnimation;
        director.time = director.duration;
        director.Evaluate();

        UpdateCurrentPages();
    }

    private void OnDestroy()
    {
        if (rightHandle != null)
        {
            rightHandle.OnGrabStarted?.RemoveListener(RightHandleGrabbed);
            rightHandle.OnGrabStopped?.RemoveListener(RightHandleReleased);
        }

        if (leftHandle != null)
        {
            leftHandle.OnGrabStarted?.RemoveListener(LeftHandleGrabbed);
            leftHandle.OnGrabStopped?.RemoveListener(LeftHandleReleased);
        }
    }

    private void LeftHandleGrabbed()
    {
        if (isBusy)
        {
            return;
        }

        checkLeftHandle = true;
        checkRightHandle = false;
        PrevPage();
    }

    private void LeftHandleReleased()
    {
        float progress = GetLeftHandleProgress();
        if (progress >= 0.5f)
        {
            CompletePageTurn();
        }
        else
        {
            CancelPageTurn();
        }
    }

    private void RightHandleGrabbed()
    {
        if (isBusy)
        {
            return;
        }

        checkLeftHandle = false;
        checkRightHandle = true;
        NextPage();
    }

    private void RightHandleReleased()
    {
        float progress = GetRightHandleProgress();
        if (progress >= 0.5f)
        {
            CompletePageTurn();
        }
        else
        {
            CancelPageTurn();
        }
    }

    private void Update()
    {
        if (isBusy)
        {
            if (checkLeftHandle)
            {
                double duration = director.duration;
                float progress = GetLeftHandleProgress();
                director.time = duration * progress;
            }
            else if (checkRightHandle)
            {
                double duration = director.duration;
                float progress = GetRightHandleProgress();
                director.time = duration * progress;
            }
            director.Evaluate();
        }
    }

    private float GetLeftHandleProgress()
    {
        float progress = 0;

        if (checkLeftHandle)
        {
            Vector3 leftHandlePosition = leftHandle.GetGrabbingPoint().localPosition;
            Vector3 left = leftPosition.localPosition;
            Vector3 right = rightPosition.localPosition;

            float totalDistance = right.x - left.x;
            float currentDistance = leftHandlePosition.x - left.x;
            progress = Mathf.Clamp01(currentDistance / totalDistance);
        }

        return progress;
    }

    private float GetRightHandleProgress()
    {
        float progress = 0;

        if (checkRightHandle)
        {
            Vector3 rightHandlePosition = rightHandle.GetGrabbingPoint().localPosition;
            Vector3 left = leftPosition.localPosition;
            Vector3 right = rightPosition.localPosition;

            float totalDistance = left.x - right.x;
            float currentDistance = rightHandlePosition.x - right.x;
            progress = Mathf.Clamp01(currentDistance / totalDistance);

            Debug.Log($"Check Right handle progress: {currentDistance} / {totalDistance} => {progress}");
        }

        return progress;
    }

    [Button]
    public void CompletePageTurn()
    {
        Debug.Log($"CompletePageTurn ? {(isBusy ? "Yes" : "No")}");
        if (!isBusy)
        {
            return;
        }

        isBusy = false;

        completeStartTime = director.time;
        completeRemainingTime = director.duration - director.time;
        float progress = (float)(director.time / director.duration);
        float duration = pageTurnDuration * (1f - progress);

        rightHandle.transform.position = rightPosition.position;
        leftHandle.transform.position = leftPosition.position;

        //Debug.Log($"Start Complete Page Turn: start time {completeStartTime}, remaining time {completeRemainingTime}, progress {progress}, duration {duration}");
        TimedActions.Start(this, duration, OnPageTurnCompleted, TimedAction.UpdateMethod.UnityScaledTime, UpdateCompletePageTurn);
    }

    private double completeStartTime = 0.0;
    private double completeRemainingTime = 0.0;

    private void UpdateCompletePageTurn(float progress)
    {
        Debug.Log($"UpdateCompletePageTurn {progress}");
        director.time = completeStartTime + completeRemainingTime * progress;
        director.Evaluate();
    }

    private void OnPageTurnCompleted()
    {
        Debug.Log($"OnPageTurnCompeted");

        isBusy = false;
        currentPage = nextPage;
        currentSpell = spellbook.GetKnownSpell(currentPage);
        flippingPage.SetActive(false);

        UpdateCurrentPages();
        onPageChanged?.Invoke(currentSpell);
    }

    private void UpdateCurrentPages()
    {
        if (currentPage >= 0)
        {
            leftHandle.EnableInteraction();
        }
        else
        {
            leftHandle.DisableInteraction();
        }

        if (currentPage < spellbook.KnownSpellCount)
        {
            rightHandle.EnableInteraction();
        }
        else
        {
            rightHandle.DisableInteraction();
        }

        if (currentSpell != null)
        {
            Set4Pages(currentSpell.bookInfo, null, null, currentSpell.pattern);
        }
        else
        {
            Set4Pages(null, null, null, null);
        }
    }

    public void CancelPageTurn()
    {
        if (!isBusy)
        {
            return;
        }

        isBusy = false;

        revertStartTime = director.time;
        float progress = (float)(director.time / director.duration);
        float duration = pageTurnDuration * progress;

        rightHandle.transform.position = rightPosition.position;
        leftHandle.transform.position = leftPosition.position;

        TimedActions.Start(this, duration, OnCancelCompeted, TimedAction.UpdateMethod.UnityScaledTime, UpdateRevertPageTurn);
    }

    private void OnCancelCompeted()
    {
        flippingPage.SetActive(false);

        nextPage = currentPage;
        UpdateCurrentPages();
    }

    private double revertStartTime = 0.0;

    private void UpdateRevertPageTurn(float progress)
    {
        director.time = revertStartTime * (1f - progress);
        director.Evaluate();
    }

    [Button]
    public void NextPage()
    {
        if (currentPage >= spellbook.KnownSpellCount
            || isBusy)
        {
            return;
        }

        TimedActions.Stop(this);
        nextPage = currentPage + 1;
        if (currentPage < 0)
        {
            OpenToLeftAnimation();
        }
        else if (nextPage < spellbook.KnownSpellCount)
        {
            TurnToLeftAnimation();
        }
        else
        {
            CloseToLeftAnimation();
        }
    }

    private void OpenToLeftAnimation()
    {
        var firstSpell = spellbook.GetKnownSpell(nextPage);
        Set4Pages(firstSpell.bookInfo, null, null, firstSpell.pattern);

        PlayAnimation(openToLeftAnimation);
    }

    private void TurnToLeftAnimation()
    {
        var nextSpell = spellbook.GetKnownSpell(nextPage);
        Set4Pages(currentSpell.bookInfo, currentSpell.pattern, nextSpell.bookInfo, nextSpell.pattern);
        flippingPage.SetActive(true);

        PlayAnimation(turnLeftAnimation);
    }

    private void CloseToLeftAnimation()
    {
        PlayAnimation(closeToLeftAnimation);
    }

    [Button]
    public void PrevPage()
    {
        if (currentPage < 0
            || isBusy)
        {
            return;
        }

        TimedActions.Stop(this);
        nextPage = currentPage - 1;
        if (nextPage < 0)
        {
            CloseToRightAnimation();
        }
        else if (currentPage == spellbook.KnownSpellCount)
        {
            OpenToRightAnimation();
        }
        else
        {
            TurnToRightAnimation();
        }
    }

    private void OpenToRightAnimation()
    {
        var lastSpell = spellbook.GetKnownSpell(nextPage);
        Set4Pages(lastSpell.bookInfo, null, null, lastSpell.pattern);

        PlayAnimation(openToRightAnimation);
    }

    private void TurnToRightAnimation()
    {
        var prevSpell = spellbook.GetKnownSpell(nextPage);
        Set4Pages(prevSpell.bookInfo, prevSpell.pattern, currentSpell.bookInfo, currentSpell.pattern);
        flippingPage.SetActive(true);

        PlayAnimation(turnRightAnimation);
    }

    private void CloseToRightAnimation()
    {
        PlayAnimation(closeToRightAnimation);
    }

    private void PlayAnimation(PlayableAsset animation)
    {
        isBusy = true;

        director.playableAsset = animation;
        director.time = 0;
        director.Evaluate();

        Update();
    }

    private void Set4Pages(Texture2D page1, Texture2D page2, Texture2D page3, Texture2D page4)
    {
        firstLeftPage.material.SetTexture("_Content", page1);
        firstRightPage.material.SetTexture("_Content", page2);
        secondLeftPage.material.SetTexture("_Content", page3);
        secondRightPage.material.SetTexture("_Content", page4);
    }
}
