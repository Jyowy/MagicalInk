using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

using UnityEngine.UI;

public class DebugInput : MonoBehaviour
{
    [SerializeField]
    private InputActionAsset inputAsset = null;
    [SerializeField]
    private Papyrus papyrus = null;

    [SerializeField]
    private TMP_Text paintMethodText = null;
    [SerializeField]
    private TMP_Text connectedText = null;
    [SerializeField]
    private TMP_Text resolutionText = null;

    private void Awake()
    {
        var map = inputAsset.FindActionMap("Debug");
        if (map != null)
        {
            var action = map.FindAction("ChangeToPoint");
            if (action != null)
            {
                action.performed += ChangeToPoint;
            }

            action = map.FindAction("ChangeToOptimizedPoint");
            if (action != null)
            {
                action.performed += ChangeToOptimizedPoint;
            }

            action = map.FindAction("ChangeToBrush");
            if (action != null)
            {
                action.performed += ChangeToBrush;
            }

            action = map.FindAction("ToggleConnected");
            if (action != null)
            {
                action.performed += ToggleConnected;
            }

            action = map.FindAction("Clear");
            if (action != null)
            {
                action.performed += Clear;
            }

            action = map.FindAction("SetResolution1");
            if (action != null)
            {
                action.performed += SetResolution1;
            }

            action = map.FindAction("SetResolution2");
            if (action != null)
            {
                action.performed += SetResolution2;
            }

            action = map.FindAction("SetResolution3");
            if (action != null)
            {
                action.performed += SetResolution3;
            }

            action = map.FindAction("SetResolution4");
            if (action != null)
            {
                action.performed += SetResolution4;
            }

            action = map.FindAction("SetResolution5");
            if (action != null)
            {
                action.performed += SetResolution5;
            }
        }
    }

    private void ChangeToPoint(InputAction.CallbackContext _)
    {
        papyrus.ChangePaintMethod(Papyrus.PaintMethod.Point);

        paintMethodText.text = "Point";
    }

    private void ChangeToOptimizedPoint(InputAction.CallbackContext _)
    {
        papyrus.ChangePaintMethod(Papyrus.PaintMethod.OptimizedPoint);

        paintMethodText.text = "Optimized Point";
    }

    private void ChangeToBrush(InputAction.CallbackContext _)
    {
        papyrus.ChangePaintMethod(Papyrus.PaintMethod.Brush);

        paintMethodText.text = "Brush";
    }

    private void ToggleConnected(InputAction.CallbackContext _)
    {
        bool connected = papyrus.ToggleConnected();

        connectedText.text = connected ? "Yes" : "No";
    }

    private void Clear(InputAction.CallbackContext _)
    {
        papyrus.ClearTexture();
    }

    private void SetResolution1(InputAction.CallbackContext _)
    {
        papyrus.SetupTexture(500);

        resolutionText.text = "500";
    }

    private void SetResolution2(InputAction.CallbackContext _)
    {
        papyrus.SetupTexture(750);

        resolutionText.text = "750";
    }

    private void SetResolution3(InputAction.CallbackContext _)
    {
        papyrus.SetupTexture(1000);

        resolutionText.text = "1000";
    }

    private void SetResolution4(InputAction.CallbackContext _)
    {
        papyrus.SetupTexture(1250);

        resolutionText.text = "1250";
    }

    private void SetResolution5(InputAction.CallbackContext _)
    {
        papyrus.SetupTexture(1500);

        resolutionText.text = "1500";
    }
}
