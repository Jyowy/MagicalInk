using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

using Sirenix.OdinInspector;

public class QuillTip : MonoBehaviour
{
    [SerializeField]
    private Quill quill = null;
    [SerializeField]
    private Color32 inkColor = new Color(0.01f, 0.02f, 0.1f);
    [SerializeField]
    private Transform tipPoint = null;
    [SerializeField]
    private float minTipDistance = 0.01f;
    [SerializeField]
    private float tipSize = 0.01f;
    [SerializeField]
    private float minDropSize = 0.1f;
    [SerializeField]
    private float maxDropSize = 1.5f;
    [SerializeField]
    private float inkTankMaxDistance = 1f;

    [ShowInInspector]
    private float inkAmount = 1f;

    [ShowInInspector, ReadOnly]
    private bool IsDrawing = false;

    [ShowInInspector, ReadOnly]
    private Papyrus papyrus = null;
    [ShowInInspector, ReadOnly]
    private bool connected = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out papyrus)
            && !papyrus.isBurned)
        {
            quill.StartDrawing(papyrus);
            prevTipPos = transform.position;

            IsDrawing = true;
            connected = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (papyrus != null && papyrus.gameObject == other.gameObject)
        {
            quill.StopDrawing(papyrus);

            papyrus = null;
            IsDrawing = false;
            connected = false;
        }
    }

    public void ChangeInkColor(Color inkColor)
    {
        this.inkColor = inkColor;
    }

    public void RefillInk()
    {
        inkAmount = 1f;
    }

    private const byte minAlpha = (byte)(byte.MaxValue * 0.05f);
    private Vector3 prevTipPos = Vector3.zero;
    private float tipDistance = 0f;
    private float timeWithoutMoving = 0f;

    private void Update()
    {
        if (papyrus != null
            && quill.BeingHeld)
        {
            Vector3 tipPos = tipPoint.position;
            tipDistance = Vector3.Distance(prevTipPos, tipPos);
            prevTipPos = tipPos;

            if (tipDistance <= minTipDistance)
            {
                timeWithoutMoving += Time.deltaTime;
                Debug.Log($"Time withoud moving: {timeWithoutMoving}");
            }
            else
            {
                timeWithoutMoving = 0f;
            }

            float paintSize = CheckSize();

            if (inkAmount > 0f)
            {
                papyrus.Paint(tipPos, paintSize, inkColor, connected);
                ConsumeInk();

                connected = true;
            }
        }
    }

    private float CheckSize()
    {
        float size = tipSize;

        if (inkAmount < 0.25f)
        {
            float f = (0.25f - inkAmount) / 0.25f;
            size *= Mathf.Lerp(1f, minDropSize, f);
        }

        if (timeWithoutMoving > 0f)
        {
            float f = Mathf.Clamp01(timeWithoutMoving);
            size *= Mathf.Lerp(1f, maxDropSize, f);
        }

        return size;
    }

    private void ConsumeInk()
    {
        float consumedInk = tipDistance / inkTankMaxDistance;
        inkAmount = Mathf.Clamp01(inkAmount - consumedInk);
    }
}
