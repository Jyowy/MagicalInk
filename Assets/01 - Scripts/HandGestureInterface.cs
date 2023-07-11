using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

using Sirenix.OdinInspector;
using UnityEngine.XR.Hands;

[System.Serializable]
public struct HandInput
{
    public bool isTracked;
    public bool indexPinch;
    public bool middlePinch;
    public bool tripplePinch;

    public Vector3 position;
    public Quaternion rotation;
}

public class HandGestureInterface : MonoBehaviour
{
    [SerializeField]
    private float indexPinchThreshold = 0.03f;
    [SerializeField]
    private float middlePinchThreshold = 0.05f;
    [SerializeField]
    private float tripplePinchThreshold = 0.05f;

    [ShowInInspector, ReadOnly]
    public HandInput leftHand = new HandInput();
    [ShowInInspector, ReadOnly]
    public HandInput rightHand = new HandInput();

    private XRHandSubsystem subsystem = null;

    private List<XRHandSubsystem> subsystems = new List<XRHandSubsystem>();

    private void Update()
    {
        CheckSubsystem();

        UpdateGestureData();
    }

    private void CheckSubsystem()
    {
        if (subsystem != null
            && subsystem.running)
        {
            return;
        }

        SubsystemManager.GetSubsystems(subsystems);
        foreach (var subsystem in subsystems)
        {
            if (subsystem != null
                && subsystem.running)
            {
                this.subsystem = subsystem;
                break;
            }
        }
    }

    public void UpdateGestureData()
    {
        if (subsystem == null
            || !subsystem.running)
        {
            return;
        }

        GetHandData(subsystem.leftHand, ref leftHand);
        GetHandData(subsystem.rightHand, ref rightHand);
    }

    private void GetHandData(XRHand hand, ref HandInput handInput)
    {
        // TODO

        handInput.isTracked = hand.isTracked;
        if (!hand.isTracked)
        {
            return;
        }

        try
        {
            bool ok = true;

            ok &= hand.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out Pose thumbTip);
            ok &= hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out Pose indexTip);
            ok &= hand.GetJoint(XRHandJointID.MiddleDistal).TryGetPose(out Pose middleDistal);
            ok &= hand.GetJoint(XRHandJointID.Palm).TryGetPose(out Pose palm);

            if (!ok)
            {
                return;
            }

            // Input
            float thumbIndexDistance = Vector3.Distance(thumbTip.position, indexTip.position);
            float thumbMiddleDistance = Vector3.Distance(thumbTip.position, middleDistal.position);
            float indexMiddleDistance = Vector3.Distance(indexTip.position, middleDistal.position);

            handInput.indexPinch = thumbIndexDistance < indexPinchThreshold;
            handInput.middlePinch = thumbMiddleDistance < middlePinchThreshold;
            handInput.tripplePinch = handInput.indexPinch && handInput.middlePinch && indexMiddleDistance < tripplePinchThreshold;

            handInput.position = palm.position;
            handInput.rotation = palm.rotation;

            //Debug.Log($"Hand {hand.handedness}: index {indexTip.position} - thumb {thumbTip.position} = {thumbIndexDistance} ({handInput.pinch})");
        }
        catch (System.Exception e)
        {
            Debug.Log($"Exception catched: {e.Message}");
            subsystem = null;
        }
    }

    public HandInput GetHandInput(Handedness hand)
    {
        return hand == Handedness.Left ? leftHand : rightHand;
    }
}
