using Microsoft.MixedReality.Toolkit.SDK.UX.Interactable;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class EditableText: MonoBehaviour
{
    [SerializeField]
    private Interactable editButton;

    [SerializeField]
    private Interactable completeButton;

    [SerializeField]
    private Interactable cancelButton;

    [SerializeField]
    private Text textField;

    public EditableTextCompletionEvent EditingCompletionEvent;

    public string Text { get { return textField.text; } }

    private bool isEditing;

    public void StartEditing()
    {
        SetEditingUI();
        KeyboardManager.Instance.Show(transform.position, Text);
        isEditing = true;
    }

    public void OnComplete()
    {
        textField.text = KeyboardManager.Instance.OnComplete();
        SetIdleUI();
        EditingCompletionEvent.Invoke(textField.text);
        isEditing = false;
    }

    public void UpdateText(string text)
    {
        textField.text = text;
    }

    public void OnCancel()
    {
        KeyboardManager.Instance.OnComplete();
        SetIdleUI();
        isEditing = false;
    }

    public void EnableEdit()
    {
        if (isEditing)
        {
            return;
        }

        editButton.gameObject.SetActive(true);
    }

    public void DisableEdit()
    {
        if (isEditing)
        {
            return;
        }

        editButton.gameObject.SetActive(false);
    }

    private void SetEditingUI()
    {
        editButton.gameObject.SetActive(false);
        completeButton.gameObject.SetActive(true);
        cancelButton.gameObject.SetActive(true);
    }

    private void SetIdleUI()
    {
        editButton.gameObject.SetActive(true);
        completeButton.gameObject.SetActive(false);
        cancelButton.gameObject.SetActive(false);
    }

    [Serializable]
    public class EditableTextCompletionEvent : UnityEvent<string>
    {
    }
}
