using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

using Sirenix.OdinInspector;

[RequireComponent(typeof(Collider))]
public class HandObjectManager : MonoBehaviour
{
    [SerializeField]
    private Handedness hand = Handedness.Right;
    [SerializeField]
    private Transform grabbingPoint = null;
    [SerializeField]
    private HandGestureInterface handGestureInterface = null;

    [ShowInInspector, ReadOnly]
    private List<GrabbableObject> hoveringList = new List<GrabbableObject>();

    [ShowInInspector, ReadOnly]
    private GrabbableObject currentlyHoveringObject = null;
    [ShowInInspector, ReadOnly]
    private GrabbableObject currentlyGrabbingObject = null;

    private bool hoveringListDirty = false;
    private bool positionChanged = false;

    private HandInput handInput = new HandInput();

    private Vector3 prevPosition = Vector3.zero;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<GrabbableObject>(out var grabbable))
        {
            hoveringList.Add(grabbable);
            SetHoveringListDirty();

            grabbable.OnInteractionChanged?.AddListener(OnGrabbableInteractionChanged);
            grabbable.OnInteractionChanged?.AddListener(OnGrabbableAvailabilityChanged);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<GrabbableObject>(out var grabbable))
        {
            if (grabbable == currentlyHoveringObject)
            {
                StopHoveringCurrentObject();
            }    

            hoveringList.Remove(grabbable);
            SetHoveringListDirty();

            grabbable.OnInteractionChanged?.RemoveListener(OnGrabbableInteractionChanged);
            grabbable.OnInteractionChanged?.RemoveListener(OnGrabbableAvailabilityChanged);
        }
    }

    private void OnGrabbableInteractionChanged(bool interactive)
    {
        SetHoveringListDirty();
    }

    private void OnGrabbableAvailabilityChanged(bool available)
    {
        SetHoveringListDirty();
    }

    private void SetHoveringListDirty() => hoveringListDirty = true;

    private void LateUpdate()
    {
        Vector3 grabbingPointPos = grabbingPoint.position;

        if (currentlyGrabbingObject == null
            && hoveringList.Count > 0)
        {
            if (Vector3.Distance(grabbingPointPos, prevPosition) >= 0.1f)
            {
                positionChanged = true;
            }

            if (hoveringListDirty
                || positionChanged)
            {
                CalculateHoveringObject(grabbingPointPos);
                hoveringListDirty = false;
                positionChanged = false;

                prevPosition = grabbingPointPos;
            }
        }

        CheckInput();

        if (currentlyGrabbingObject != null)
        {
            if (currentlyGrabbingObject.IsStabilized)
            {
                StabilizeGrabbingObject(grabbingPointPos);
            }
            else
            {
                currentlyGrabbingObject.UpdateGrabbingPoint(grabbingPointPos, handInput.rotation, hand);
            }
        }
    }

    private void CalculateHoveringObject(Vector2 grabbingPointPos)
    {
        if (hoveringList.Count == 0)
        {
            return;
        }

        hoveringList.RemoveAll(grabbable => grabbable == null);

        float bestFit = -1f;
        GrabbableObject bestGrabbable = null;
        foreach (var grabbable in hoveringList)
        {
            if (!grabbable.IsAvailabe
                || !grabbable.IsInteractive)
            {
                continue;
            }

            float fit = grabbable.GetDistanceTo(grabbingPointPos);
            if (fit > bestFit)
            {
                bestFit = fit;
                bestGrabbable = grabbable;
            }
        }

        if (bestGrabbable != currentlyHoveringObject)
        {
            StartHoveringObject(bestGrabbable);
        }
    }

    private void StopHoveringCurrentObject()
    {
        if (currentlyHoveringObject != null)
        {
            currentlyHoveringObject.StopHovering();
            currentlyHoveringObject = null;
        }
    }

    private void StartHoveringObject(GrabbableObject grabbable)
    {
        StopHoveringCurrentObject();
        if (grabbable != null)
        {
            currentlyHoveringObject = grabbable;
            currentlyHoveringObject.StartHovering();
        }
    }

    private void CheckInput()
    {
        handInput = handGestureInterface.GetHandInput(hand);

        if (currentlyGrabbingObject != null)
        {
            GrabbableObject grabbable = currentlyGrabbingObject;
            if (grabbable is Paperball)
            {
                if (!handInput.palmGrab)
                {
                    StopGrabbing();
                }
            }
            else if (!handInput.indexPinch)
            {
                StopGrabbing();
            }
        }
        else if (currentlyHoveringObject != null)
        {
            GrabbableObject grabbable = currentlyHoveringObject;
            if (grabbable is Quill)
            {
                if (handInput.tripplePinch)
                {
                    StartGrabbing(grabbable);
                }
            }
            else if (grabbable is Papyrus papyrus
                && handInput.palmGrab
                && !handInput.prevMiddleToPalm)
            {
                papyrus.DestroyPapyrus();
            }
            else if (grabbable is Paperball
                && handInput.palmGrab)
            {
                StartGrabbing(grabbable);
            }
            else if (handInput.indexPinch
                && !handInput.prevIndexPinch)
            {
                StartGrabbing(grabbable);
            }
        }
    }

    private void StartGrabbing(GrabbableObject grabbable)
    {
        StopHoveringCurrentObject();

        if (grabbable != null)
        {
            lastMoves.Clear();
            lastPos = transform.position;

            currentlyGrabbingObject = grabbable;
            currentlyGrabbingObject.StartGrabbing(grabbingPoint.position);

            currentlyGrabbingObject.OnGrabStopped?.AddListener(StopGrabbing);
        }
    }

    private void StopGrabbing()
    {
        if (currentlyGrabbingObject != null)
        {
            currentlyGrabbingObject.OnGrabStopped?.RemoveListener(StopGrabbing);

            currentlyGrabbingObject.StopGrabbing();
            currentlyGrabbingObject = null;
            SetHoveringListDirty();
        }
    }

    [ShowInInspector, ReadOnly]
    private readonly List<Vector3> lastMoves = new List<Vector3>();
    [ShowInInspector]
    private int maxLastMoves = 10;
    [ShowInInspector, ReadOnly]
    private Vector3 lastPos = Vector3.zero;

    private void StabilizeGrabbingObject(Vector3 grabbingPointPos)
    {
        Vector3 stabilizedPoint;
        Vector3 currentMove = grabbingPointPos - lastPos;
        lastPos = grabbingPointPos;

        Vector3 avgMove = currentMove;
        foreach (var move in lastMoves)
        {
            avgMove += move;
        }
        avgMove /= lastMoves.Count + 1;

        lastMoves.Add(currentMove);
        while (lastMoves.Count > maxLastMoves)
        {
            lastMoves.RemoveAt(0);
        }

        stabilizedPoint = lastPos + avgMove;
        currentlyGrabbingObject.UpdateGrabbingPoint(stabilizedPoint, handInput.rotation, hand);
    }
}
