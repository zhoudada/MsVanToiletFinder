using Microsoft.MixedReality.Toolkit.Core.Utilities;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
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

    private Task showAsyncTask;

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

        //KeyboardUI.SetActive(false);
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
        if (showAsyncTask != null && !showAsyncTask.IsCompleted)
        {
            return;
        }

        showAsyncTask = ShowAsync(position, defaultText);
    }

    public void Hide()
    {
        foreach (Transform child in KeyboardUI.transform)
        {
            child.gameObject.SetActive(false);
        }
    }

    private async Task ShowAsync(Vector3 position, string defaultText)
    {
        CurrentText = defaultText;
        transform.position = position + offset;
        int countToYield = 0;
        foreach (Transform child in KeyboardUI.transform)
        {
            if (countToYield >= 3)
            {
                countToYield = 0;
                await Task.Yield();
            }

            child.gameObject.SetActive(true);
            countToYield++;
        }
        KeyboardUI.SetActive(true);
        isVisible = true;
    }

    public string OnComplete()
    {
        string text = currentText;
        Debug.Log($"Keyboard complete: {currentText}.");
        CurrentText = "";
        Hide();
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
