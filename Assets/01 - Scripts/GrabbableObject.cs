using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

using Sirenix.OdinInspector;
using UnityEngine.XR.Hands;

[RequireComponent(typeof(Collider))]
public abstract class GrabbableObject : MonoBehaviour
{
    public enum ObjectState
    {
        Idle,
        Hovering,
        Grabbing
    }

    [SerializeField]
    private UnityEvent OnHoverStarted = new UnityEvent();
    [SerializeField]
    private UnityEvent OnHoverStopped = new UnityEvent();
    [SerializeField]
    private UnityEvent OnGrabStarted = new UnityEvent();
    [SerializeField]
    private UnityEvent OnGrabStopped = new UnityEvent();

    [SerializeField]
    protected Vector3 leftHandPositionOffset = Vector3.zero;
    [SerializeField]
    protected Vector3 rightHandPositionOffset = Vector3.zero;
    [SerializeField]
    protected Quaternion leftHandRotationOffset = Quaternion.identity;
    [SerializeField]
    protected Quaternion rightHandRotationOffset = Quaternion.identity;

    private bool available = true;

    [ShowInInspector, ReadOnly]
    public bool IsAvailabe => available;
    [ShowInInspector, ReadOnly]
    public ObjectState State { get; private set; } = ObjectState.Idle;

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

    [Button]
    public void StartGrabbing()
    {
        if (State == ObjectState.Grabbing)
        {
            Debug.LogWarning($"Object '{name}' was already being grabbed.");
            return;
        }

        State = ObjectState.Grabbing;
        OnStartGrabbing();
        OnGrabStarted?.Invoke();
    }

    protected virtual void OnStartGrabbing() { }

    [Button]
    public void StopGrabbing()
    {
        if (State != ObjectState.Grabbing)
        {
            Debug.LogWarning($"Object '{name}' was not being grabbed.");
            return;
        }

        State = ObjectState.Idle;
        OnStopGrabbing();
        OnGrabStopped?.Invoke();
    }

    protected virtual void OnStopGrabbing() { }

    /// <summary>
    /// Returns a score, from 0 to 1, on how close the given point is to a grabbing point.
    /// I. e.: a paper may have its grabbing points on the edges, while a quill may have it in the mass center.
    /// </summary>
    /// <returns></returns>
    public abstract float GetFitRate(Vector3 point);

    public virtual void UpdateGrabbingPoint(Vector3 point, Quaternion rotation, Handedness hand)
    {
        Vector3 positionOffset = hand == Handedness.Right ? rightHandPositionOffset : leftHandPositionOffset;
        transform.position = point
            + transform.forward * positionOffset.z
            + transform.right * positionOffset.x
            + transform.up * positionOffset.y;
        Quaternion rotationOffset = hand == Handedness.Right ? rightHandRotationOffset : leftHandRotationOffset;
        transform.rotation = rotation * rotationOffset;
    }
}
