using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pyreball : MonoBehaviour
{
    [ShowInInspector, ReadOnly]
    private float fireIntensity = 1f;
    [ShowInInspector, ReadOnly]
    private readonly List<IFlammable> nearFlammables = new List<IFlammable>();

    [ReadOnly]
    public bool active = false;

    public void Setup(float fireIntensity)
    {
        this.fireIntensity = fireIntensity;
    }

    public void Activate() => active = true;
    public void Deactivate() => active = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent<IFlammable>(out var flammable))
        {
            nearFlammables.Add(flammable);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.TryGetComponent<IFlammable>(out var flammable)
            && nearFlammables.Contains(flammable))
        {
            nearFlammables.Remove(flammable);
        }
    }

    public void Update()
    {
        IgniteNearFlammables();
    }

    public void IgniteNearFlammables()
    {
        if (active)
        {
            foreach (var flammable in nearFlammables)
            {
                flammable.Ignite(fireIntensity);
            }
        }
    }
}
