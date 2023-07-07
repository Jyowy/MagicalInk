using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Sirenix.OdinInspector;
using DG.Tweening;
using UnityEngine.XR.Hands;

public class Quill : GrabbableObject
{
    [SerializeField, TitleGroup("Quill")]
    private float length = 1f;
    [SerializeField, TitleGroup("Quill")]
    private float weight = 1f;
    [SerializeField, TitleGroup("Quill")]
    private float thickness = 1f;

    [SerializeField]
    private Collider grabbableArea = null;
    [SerializeField]
    private Transform quillParent = null;

    [ShowInInspector, ReadOnly]
    private Vector3 grabbingPoint = Vector3.zero;

    public override float GetFitRate(Vector3 point)
    {
        return grabbableArea != null
            ? Vector3.Distance(grabbableArea.ClosestPoint(point), point)
            : 0f;
    }

    //public override void UpdateGrabbingPoint(Vector3 point, Quaternion rotation, Handedness hand)
    //{
    //    Vector3 positionOffset = hand == Handedness.Right ? rightHandPositionOffset : leftHandPositionOffset;
    //    quillParent.position = point
    //        + quillParent.forward * positionOffset.z
    //        + quillParent.right * positionOffset.x
    //        + quillParent.up * positionOffset.y;
    //    Quaternion rotationOffset = hand == Handedness.Right ? rightHandRotationOffset : leftHandRotationOffset;
    //    quillParent.rotation = rotation * rotationOffset;
    //}
}
