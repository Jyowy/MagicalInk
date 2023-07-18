using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InkPot : MonoBehaviour
{
    [SerializeField]
    private Renderer potRenderer = null;
    [SerializeField, OnValueChanged("SetupMaterial")]
    private Color inkColor = Color.black;
    [SerializeField, OnValueChanged("SetupMaterial")]
    private Color potColor = Color.grey;

    private void Awake()
    {
        SetupMaterial();
    }

    private void SetupMaterial()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        potRenderer.material.SetColor("_InkColor", inkColor);
        potRenderer.material.SetColor("_PotColor", potColor);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent(out QuillTip quillTip))
        {
            quillTip.ChangeInkColor(inkColor);
            quillTip.RefillInk();
        }
    }
}
