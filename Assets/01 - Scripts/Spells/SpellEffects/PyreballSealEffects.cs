using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Sirenix.OdinInspector;

public class PyreballSealEffects : SpellEffects
{
    [TitleGroup("Pyreball Seal Settings", order: 90)]
    [SerializeField]
    private GameObject pyreball = null;
    [TitleGroup("Pyreball Seal Settings")]
    [SerializeField]
    private Transform pyreballContainer = null;

    [BoxGroup("Pyreball Seal Settings/Ball Size")]
    [SerializeField]
    private float maxBallSize = 1f;
    [BoxGroup("Pyreball Seal Settings/Ball Size")]
    [SerializeField]
    private float badBallSizeFactor = 0.25f;
    [BoxGroup("Pyreball Seal Settings/Ball Size")]
    [SerializeField]
    private float fairBallSizeFactor = 0.5f;
    [BoxGroup("Pyreball Seal Settings/Ball Size")]
    [SerializeField]
    private float goodBallSizeFactor = 0.75f;

    [BoxGroup("Pyreball Seal Settings/Fire Intensity")]
    [SerializeField]
    private float maxFireIntensity = 1f;
    [BoxGroup("Pyreball Seal Settings/Fire Intensity")]
    [SerializeField]
    private float badFireIntensityFactor = 0.25f;
    [BoxGroup("Pyreball Seal Settings/Fire Intensity")]
    [SerializeField]
    private float fairFireIntensityFactor = 0.5f;
    [BoxGroup("Pyreball Seal Settings/Fire Intensity")]
    [SerializeField]
    private float goodFireIntensityFactor = 0.75f;

    [BoxGroup("Pyreball Seal Settings/Runtime Debug")]
    [ShowInInspector, ReadOnly]
    private float ballSize = 0f;
    [BoxGroup("Pyreball Seal Settings/Runtime Debug")]
    [ShowInInspector, ReadOnly]
    private float fireIntensity = 0f;
    [BoxGroup("Pyreball Seal Settings/Runtime Debug")]

    protected override void BadCast()
    {
        PrepareSpell(maxBallSize * badBallSizeFactor, maxFireIntensity * badFireIntensityFactor); 

        base.BadCast();
    }

    protected override void FairCast()
    {
        PrepareSpell(maxBallSize * fairBallSizeFactor, maxFireIntensity * fairFireIntensityFactor);

        base.FairCast();
    }

    protected override void GoodCast()
    {
        PrepareSpell(maxBallSize * goodBallSizeFactor, maxFireIntensity * goodFireIntensityFactor);

        base.GoodCast();
    }

    protected override void GreatCast()
    {
        PrepareSpell(maxBallSize, maxFireIntensity);

        base.GreatCast();
    }

    [Button]
    public void PrepareSpell(float ballSize, float fireIntensity)
    {
        this.ballSize = ballSize;
        this.fireIntensity = fireIntensity;

        pyreballContainer.localScale = Vector3.one * ballSize;
    }

    public override void StopCast()
    {
        base.StopCast();
    }
}
