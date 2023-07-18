using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

using Sirenix.OdinInspector;
using UnityEngine.XR.Hands;

[RequireComponent(typeof(Collider))]
public class GrabbableObject : MonoBehaviour
{
    public enum ObjectState
    {
        Idle,
        Hovering,
        Grabbing
    }

    [TitleGroup("Grabbable Settings", order: 100)]
    [SerializeField]
    private bool relativeGrab = false;
    [TitleGroup("Grabbable Settings")]
    [SerializeField]
    private bool followPosition = true;
    [TitleGroup("Grabbable Settings")]
    [SerializeField]
    private bool followRotation = true;
    [TitleGroup("Grabbable Settings")]
    [SerializeField]
    private bool stabilizeDrag = false;

    [BoxGroup("Grabbable Settings/Hand Offsets", order: 0)]
    [SerializeField]
    private Transform customGrabPoint = null;
    [BoxGroup("Grabbable Settings/Hand Offsets"), ShowIf("followPosition")]
    [SerializeField]
    protected Vector3 leftHandPositionOffset = Vector3.zero;
    [BoxGroup("Grabbable Settings/Hand Offsets"), ShowIf("followPosition")]
    [SerializeField]
    protected Vector3 rightHandPositionOffset = Vector3.zero;
    [BoxGroup("Grabbable Settings/Hand Offsets"), ShowIf("followRotation")]
    [SerializeField]
    protected Quaternion leftHandRotationOffset = Quaternion.identity;
    [BoxGroup("Grabbable Settings/Hand Offsets"), ShowIf("followRotation")]
    [SerializeField]
    protected Quaternion rightHandRotationOffset = Quaternion.identity;

    [FoldoutGroup("Grabbable Settings/Events", order: 1)]
    public UnityEvent<bool> OnAvailabilityChanged = new UnityEvent<bool>();
    [FoldoutGroup("Grabbable Settings/Events")]
    public UnityEvent<bool> OnInteractionChanged = new UnityEvent<bool>();
    [FoldoutGroup("Grabbable Settings/Events")]
    public UnityEvent OnHoverStarted = new UnityEvent();
    [FoldoutGroup("Grabbable Settings/Events")]
    public UnityEvent OnHoverStopped = new UnityEvent();
    [FoldoutGroup("Grabbable Settings/Events")]
    public UnityEvent OnGrabStarted = new UnityEvent();
    [FoldoutGroup("Grabbable Settings/Events")]
    public UnityEvent OnGrabStopped = new UnityEvent();

    [BoxGroup("Grabbable Settings/Runtime Debug", order: 2)]
    [ShowInInspector, ReadOnly]
    public bool IsStabilized => stabilizeDrag;
    [ShowInInspector, ReadOnly]
    public bool IsAvailabe { get; protected set; } = true;
    [ShowInInspector, ReadOnly]
    public bool IsInteractive { get; protected set; } = true;
    [BoxGroup("Grabbable Settings/Runtime Debug")]
    [ShowInInspector, ReadOnly]
    public ObjectState State { get; private set; } = ObjectState.Idle;

    private Vector3 firstGrabPoint = Vector3.zero;
    private Vector3 positionOnStartGrab = Vector3.zero;

    public Transform GetGrabbingPoint() => customGrabPoint != null ? customGrabPoint.transform : transform;

    public void EnableInteraction()
    {
        if (IsInteractive)
        {
            return;
        }

        IsInteractive = true;
        OnInteractionChanged?.Invoke(IsInteractive);
    }

    public void DisableInteraction()
    {
        if (!IsInteractive)
        {
            return;
        }

        IsInteractive = false;

        if (State == ObjectState.Hovering)
        {
            StopHovering();
        }
        else if (State == ObjectState.Grabbing)
        {
            StopGrabbing();
        }

        OnInteractionChanged?.Invoke(IsInteractive);
    }

    [BoxGroup("Grabbable Settings/Runtime Debug")]
    [Button]
    public void StartHovering()
    {
        if (State == ObjectState.Hovering)
        {
            Debug.LogWarning($"Object '{name}' was already being hovered.");
            return;
        }

        State = ObjectState.Hovering;

        OnStartHovering();
        OnHoverStarted?.Invoke();
    }

    protected virtual void OnStartHovering() { }

    [BoxGroup("Grabbable Settings/Runtime Debug")]
    [Button]
    public void StopHovering()
    {
        if (State != ObjectState.Hovering)
        {
            Debug.LogWarning($"Object '{name}' was not being hovered.");
            return;
        }

        State = ObjectState.Idle;

        OnStopHovering();
        OnHoverStopped?.Invoke();
    }

    protected virtual void OnStopHovering() { }

    [BoxGroup("Grabbable Settings/Runtime Debug")]
    [Button]
    public void StartGrabbing(Vector3 grabPosition)
    {
        if (State == ObjectState.Grabbing)
        {
            Debug.LogWarning($"Object '{name}' was already being grabbed.");
            return;
        }

        State = ObjectState.Grabbing;
        if (relativeGrab)
        {
            firstGrabPoint = grabPosition;
            positionOnStartGrab = transform.position;
        }

        IsAvailabe = false;
        OnAvailabilityChanged?.Invoke(IsAvailabe);

        OnStartGrabbing();
        OnGrabStarted?.Invoke();
    }

    protected virtual void OnStartGrabbing() { }

    [BoxGroup("Grabbable Settings/Runtime Debug")]
    [Button]
    public void StopGrabbing()
    {
        if (State != ObjectState.Grabbing)
        {
            Debug.LogWarning($"Object '{name}' was not being grabbed.");
            return;
        }

        State = ObjectState.Idle;
        IsAvailabe = true;
        OnAvailabilityChanged?.Invoke(IsAvailabe);

        OnStopGrabbing();
        OnGrabStopped?.Invoke();
    }

    protected virtual void OnStopGrabbing() { }

    /// <summary>
    /// Returns the distance from the closest grab point of the object to the given point.
    /// Its main use is to prioritize the closest object when the user performs a grab while multiple grabbable objects are in reach.
    /// I. e.: a paper may have its grabbing points on the edges, while a quill may have it in its mass center.
    /// </summary>
    /// <returns></returns>
    public virtual float GetDistanceTo(Vector3 point) => Vector3.Distance(transform.position, point);

    public virtual void UpdateGrabbingPoint(Vector3 point, Quaternion rotation, Handedness hand)
    {
        if (followPosition)
        {
            Vector3 grabPoint = point;

            if (relativeGrab)
            {
                grabPoint = positionOnStartGrab + (point - firstGrabPoint);
            }

            Vector3 positionOffset = hand == Handedness.Right ? rightHandPositionOffset : leftHandPositionOffset;
            transform.position = grabPoint
                + transform.forward * positionOffset.z
                + transform.right * positionOffset.x
                + transform.up * positionOffset.y;
        }
        if (followRotation)
        {
            Quaternion rotationOffset = hand == Handedness.Right ? rightHandRotationOffset : leftHandRotationOffset;
            transform.rotation = rotation * rotationOffset;
        }
    }

    public virtual void UpdateGrabbingPoint(Vector3 point, Handedness hand)
    {
        Vector3 grabPoint = point;

        if (relativeGrab)
        {
            grabPoint = positionOnStartGrab + (point - firstGrabPoint);
        }

        Vector3 positionOffset = hand == Handedness.Right ? rightHandPositionOffset : leftHandPositionOffset;
        transform.position = grabPoint
            + transform.forward * positionOffset.z
            + transform.right * positionOffset.x
            + transform.up * positionOffset.y;
    }

    [Button]
    private void DebugMove(Vector3 newPos)
    {
        UpdateGrabbingPoint(newPos, Handedness.Right);
    }
}
