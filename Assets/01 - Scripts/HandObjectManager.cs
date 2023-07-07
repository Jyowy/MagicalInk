using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

using Sirenix.OdinInspector;

[RequireComponent(typeof(Collider))]
public class HandObjectManager : MonoBehaviour
{
    [SerializeField]
    private bool rightHand = true;
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
        //Debug.Log($"OnTriggerEnter {other.name}");

        if (other.TryGetComponent<GrabbableObject>(out var grabbable))
        {
            hoveringList.Add(grabbable);
            hoveringListDirty = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        //Debug.Log($"OnTriggerExit {other.name}");

        if (other.TryGetComponent<GrabbableObject>(out var grabbable))
        {
            if (grabbable == currentlyHoveringObject)
            {
                StopHoveringCurrentObject();
            }    

            hoveringList.Remove(grabbable);
            hoveringListDirty = true;
        }
    }

    private void LateUpdate()
    {
        Vector3 grabbingPointPos = grabbingPoint.position;

        if (currentlyGrabbingObject == null)
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
            currentlyGrabbingObject.UpdateGrabbingPoint(grabbingPointPos, handInput.rotation, rightHand ? Handedness.Right : Handedness.Left);
        }
    }

    private void CalculateHoveringObject(Vector2 grabbingPointPos)
    {
        if (hoveringList.Count == 0)
        {
            return;
        }

        float bestFit = -1f;
        GrabbableObject bestGrabbable = null;
        foreach (var grabbable in hoveringList)
        {
            if (!grabbable.IsAvailabe)
            {
                continue;
            }

            float fit = grabbable.GetFitRate(grabbingPointPos);
            if (fit > bestFit)
            {
                bestFit = fit;
                bestGrabbable = grabbable;
            }
        }

        if (bestGrabbable != currentlyHoveringObject)
        {
            StopHoveringCurrentObject();
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
        if (grabbable != null)
        {
            currentlyHoveringObject = grabbable;
            currentlyHoveringObject.StartHovering();
        }
    }

    private void CheckInput()
    {
        handInput = handGestureInterface.GetHandInput(rightHand ? Handedness.Right : Handedness.Left);

        if (handInput.tripplePinch
            && currentlyHoveringObject != null)
        {
            GrabbableObject grabbable = currentlyHoveringObject;
            StopHoveringCurrentObject();
            StartGrabbing(grabbable);
        }
        else if (!handInput.indexPinch
            && currentlyGrabbingObject != null)
        {
            StopGrabbing();
        }
    }

    private void StartGrabbing(GrabbableObject grabbable)
    {
        if (grabbable != null)
        {
            currentlyGrabbingObject = grabbable;
            currentlyGrabbingObject.StartGrabbing();
        }
    }

    private void StopGrabbing()
    {
        if (currentlyGrabbingObject != null)
        {
            currentlyGrabbingObject.StopGrabbing();
            currentlyGrabbingObject = null;
        }
    }
}
