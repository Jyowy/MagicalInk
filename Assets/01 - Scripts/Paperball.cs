using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Sirenix.OdinInspector;
using UnityEngine.XR.Hands;

public class Paperball : GrabbableObject, IFlammable
{
    [SerializeField]
    private Renderer ballRenderer = null;
    [SerializeField]
    private Color burnedColor = Color.black;
    [SerializeField]
    private Rigidbody ballRigidbody = null;
    [SerializeField]
    private float throwStrength = 1f;

    [ShowInInspector, ReadOnly]
    private bool burned = false;

    private Vector3 lastPos = Vector3.zero;
    private Vector3 lastSpeedAcc = Vector3.zero;
    private int lastCount = 0;
    private int lastMaxCount = 10;
    private readonly List<Vector3> lastSpeeds = new List<Vector3>();

    protected override void OnStartGrabbing()
    {
        ballRigidbody.isKinematic = true;

        base.OnStartGrabbing();
    }

    protected override void OnStopGrabbing()
    {
        Vector3 lastSpeed = Vector3.zero;
        if (lastSpeeds.Count > 0)
        {
            foreach (var speed in lastSpeeds)
            {
                lastSpeed += speed;
            }
            lastSpeed *= throwStrength / lastSpeeds.Count;
        }

        ballRigidbody.isKinematic = false;
        ballRigidbody.velocity = lastSpeed;
        Debug.Log($"Velocity set to {lastSpeed}");

        base.OnStopGrabbing();
    }

    public override void UpdateGrabbingPoint(Vector3 point, Quaternion rotation, Handedness hand)
    {
        base.UpdateGrabbingPoint(point, rotation, hand);

        Vector3 newPos = transform.position;
        Vector3 currentSpeed = (newPos - lastPos) / Time.deltaTime;
        lastPos = newPos;

        lastSpeeds.Add(currentSpeed);
        if (lastSpeeds.Count > lastMaxCount)
        {
            lastSpeeds.RemoveAt(0);
        }
    }

    [Button]
    public void Ignite(float fireIntensity)
    {
        if (burned)
        {
            return;
        }

        burned = true;
        ballRenderer.material.SetColor("_BaseColor", burnedColor);
    }
}
