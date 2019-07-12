using Microsoft.MixedReality.Toolkit.Core.Utilities;
using System.Collections.Generic;
using UnityEngine;

public class AnchorTagManager: MonoBehaviour
{
    [SerializeField]
    private EditableText editableText;

    [SerializeField]
    private GameObject tagButton;

    [SerializeField]
    private Vector3 offset = new Vector3(0, -0.2f, 0);

    [SerializeField]
    private Transform startPoint;

    [SerializeField]
    private int maxTagShown = 10;

    private bool isShown = false;

    private List<GameObject> tagButtonObjects = new List<GameObject>();

    public void ShowOrHide()
    {
        isShown = !isShown;

        if (isShown)
        {
            List<string> names = new List<string>();
            int count = 0;

            foreach (AnchorHandler handler in GameMaster.Instance.ExistingAnchors.Values)
            {
                if (!string.IsNullOrEmpty(handler.AnchorName))
                {
                    if (count > maxTagShown)
                    {
                        break;
                    }

                    names.Add(handler.AnchorName);
                    count++;
                }
            }

            names.Sort();
            Vector3 tagButtonPosition = startPoint.position;

            foreach (string name in names)
            {
                CreateTag(name, tagButtonPosition);
                tagButtonPosition += offset;
            }
        }
        else
        {
            DestroyButtons();
        }
    }

    private void CreateTag(string name, Vector3 position)
    {
        GameObject buttonObject = Instantiate(tagButton, position, Quaternion.identity);
        buttonObject.transform.forward = CameraCache.Main.transform.forward;
        AnchorTagButton button = buttonObject.GetComponent<AnchorTagButton>();
        button.Initialize(this, name);
        tagButtonObjects.Add(buttonObject);
    }

    public void OnTagChosen(string name)
    {
        editableText.UpdateText(name);
        DestroyButtons();
    }

    private void DestroyButtons()
    {
        foreach (GameObject buttonObject in tagButtonObjects)
        {
            Destroy(buttonObject);
        }
    }
}
