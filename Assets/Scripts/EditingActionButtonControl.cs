using UnityEngine;

public class EditingActionButtonControl : MonoBehaviour
{
    [SerializeField]
    private GameObject clearAnchorButton;

    [SerializeField]
    private GameObject resetSelectionButton;

    [SerializeField]
    private GameObject updateNeighboursButton;

    public void UpdateButtons(EditingAction currentAction)
    {
        if (currentAction == EditingAction.PlaceAnchor)
        {
            clearAnchorButton.SetActive(true);
            resetSelectionButton.SetActive(false);
            updateNeighboursButton.SetActive(false);
        }
        else
        {
            clearAnchorButton.SetActive(false);
            resetSelectionButton.SetActive(true);
            updateNeighboursButton.SetActive(true);
        }
    }
}
