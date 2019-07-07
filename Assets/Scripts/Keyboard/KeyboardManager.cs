using Microsoft.MixedReality.Toolkit.Core.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class KeyboardManager : MonoBehaviour
{
    public static KeyboardManager Instance;

    [SerializeField]
    private Text previewText;

    [SerializeField]
    private GameObject KeyboardUI;

    [SerializeField]
    private Vector3 offset = new Vector3(0, -0.1f, 0);

    private string CurrentText
    {
        get { return currentText; }
        set
        {
            currentText = value;
            UpdatePreviewText();
        }
    }

    private string currentText;
    private bool isVisible;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            DestroyImmediate(gameObject);
        }

        KeyboardUI.SetActive(false);
        isVisible = false;
    }

    public void UpdateText(KeyBase key)
    {
        key.UpdateText(ref currentText);
        UpdatePreviewText();
    }

    private void UpdatePreviewText()
    {
        previewText.text = currentText;
    }

    public void Show(Vector3 position, string defaultText = "")
    {
        CurrentText = defaultText;
        KeyboardUI.SetActive(true);
        transform.position = position + offset;
        isVisible = true;
    }

    public string OnComplete()
    {
        string text = currentText;
        Debug.Log($"Keyboard complete: {currentText}.");
        CurrentText = "";
        KeyboardUI.SetActive(false);
        isVisible = false;

        return text;
    }

    private void Update()
    {
        if (!isVisible)
        {
            return;
        }

        transform.forward = CameraCache.Main.transform.forward;
    }
}
