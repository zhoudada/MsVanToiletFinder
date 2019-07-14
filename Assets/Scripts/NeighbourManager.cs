using System.Collections.Generic;

public class NeighbourManager
{
    private SelectionManager selectionManager;

    private bool overridenNeighbourMasterSelected = false;
    private List<string> overridenNeighbours = new List<string>();

    public NeighbourManager(SelectionManager selectionManager)
    {
        this.selectionManager = selectionManager;
    }

    public void ShowNeighbour(AnchorHandler selectedAnchor)
    {
        if (selectionManager.SingleSelectedObject == selectedAnchor)
        {
            return;
        }

        Reset();
        selectionManager.SingleSelect(selectedAnchor);
        List<string> neighbourIds = GameMaster.Instance.GetNeighbourIds(selectedAnchor.AnchorId);

        foreach (string neighbourId in neighbourIds)
        {
            AnchorHandler neighbour = GameMaster.Instance.ExistingAnchors[neighbourId];
            selectionManager.MultiSelect(neighbour);
        }
    }

    public void OverrideNeighbourSelect(AnchorHandler selectedAnchor)
    {
        if (!overridenNeighbourMasterSelected)
        {
            // Select the main anchor to modify
            Reset();
            selectionManager.SingleSelect(selectedAnchor);
            overridenNeighbourMasterSelected = true;
        }
        else
        {
            // Select the overriden neighbours
            if (selectedAnchor == selectionManager.SingleSelectedObject)
            {
                return;
            }

            selectionManager.MultiSelect(selectedAnchor);
        }
    }

    public void UpdateNeighbours()
    {
        if (selectionManager.SingleSelectedObject == null)
        {
            return;
        }

        AnchorHandler master = selectionManager.SingleSelectedObject as AnchorHandler;
        List<string> previousNeighbourIds = GameMaster.Instance.GetNeighbourIds(master.AnchorId);
        foreach (string id in previousNeighbourIds)
        {
            GameMaster.Instance.GetNeighbourIds(id).Remove(master.AnchorId);
        }


        List<string> currentNeighbourIds = new List<string>();
        foreach (ISelectableObject selectedObject in selectionManager.MultiSelectedObjects)
        {
            string neighbourId = ((AnchorHandler)selectedObject).AnchorId;
            currentNeighbourIds.Add(neighbourId);
            GameMaster.Instance.GetNeighbourIds(neighbourId).Add(master.AnchorId);
        }
        GameMaster.Instance.OverrideNeighbourIds(master.AnchorId, currentNeighbourIds);
    }

    public void Reset()
    {
        selectionManager.Reset();
        overridenNeighbours.Clear();
        overridenNeighbourMasterSelected = false;
    }
}
