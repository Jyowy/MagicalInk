using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

using Sirenix.OdinInspector;

public class Quill : GrabbableObject
{
    [TitleGroup("Quill Settings", order: 90)]
    [SerializeField]
    private Collider grabbableArea = null;

    [FoldoutGroup("Quill Settings/Events", order: 1)]
    public UnityEvent<Papyrus> OnStartDrawing = new UnityEvent<Papyrus>();
    [FoldoutGroup("Quill Settings/Events")]
    public UnityEvent<Papyrus> OnStopDrawing = new UnityEvent<Papyrus>();

    public bool BeingHeld => State == ObjectState.Grabbing;

    public override float GetDistanceTo(Vector3 point)
    {
        return grabbableArea != null
            ? Vector3.Distance(grabbableArea.ClosestPoint(point), point)
            : 0f;
    }

    public void StartDrawing(Papyrus papyrus)
    {
        OnStartDrawing?.Invoke(papyrus);
    }

    public void StopDrawing(Papyrus papyrus)
    {
        OnStopDrawing?.Invoke(papyrus);
    }
}
