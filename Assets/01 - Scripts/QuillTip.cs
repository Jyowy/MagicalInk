using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Sirenix.OdinInspector;

public class QuillTip : MonoBehaviour
{
    [SerializeField]
    private Transform tipPoint = null;
    [SerializeField]
    private float tipSize = 0.01f;
    [SerializeField]
    private Color inkColor = new Color(0.01f, 0.02f, 0.1f);

    [ShowInInspector]
    private float inkAmount = 1f;

    [ShowInInspector, ReadOnly]
    private bool droppingInk = false;

    [ShowInInspector, ReadOnly]
    private Papyrus papyrus = null;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out papyrus))
        {
            droppingInk = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (papyrus != null && papyrus.gameObject == other.gameObject)
        {
            papyrus = null;
            droppingInk = false;
        }
    }

    private void Update()
    {
        if (papyrus != null)
        {
            //Debug.Log($"Painting!");
            papyrus.Paint(tipPoint.position, tipSize, inkColor);
        }
    }

    [Button]
    public Vector3 DebugGetTipPoint()
    {
        return tipPoint.position;
    }
}
