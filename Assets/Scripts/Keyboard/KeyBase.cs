using Microsoft.MixedReality.Toolkit.SDK.UX.Interactable;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class KeyBase : MonoBehaviour
{
    [SerializeField]
    private KeyboardManager keyboardManager;

    private Interactable interactable;

    protected KeyboardManager KeyboardManager { get { return keyboardManager; } }

    public abstract void UpdateText(ref string currentText);

    private void Start()
    {
        interactable = GetComponent<Interactable>();
        interactable.OnClick.AddListener(OnKeyPressed);
    }

    public void OnKeyPressed()
    {
        KeyboardManager.UpdateText(this);
    }

    private void OnDestroy()
    {
        if (interactable != null)
        {
            interactable.OnClick.RemoveListener(OnKeyPressed);
        }
    }
}
