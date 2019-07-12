using UnityEngine;
using UnityEngine.UI;

public class AnchorTagButton: MonoBehaviour
{
    [SerializeField]
    private TextMesh tagText;

    private AnchorTagManager anchorTagManager;

    public void Initialize(AnchorTagManager anchorTagManager, string name)
    {
        this.anchorTagManager = anchorTagManager;
        tagText.text = name;
    }

    public void OnClick()
    {
        anchorTagManager.OnTagChosen(tagText.text);
    }
}
