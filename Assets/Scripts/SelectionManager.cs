using System.Collections.Generic;

public class SelectionManager
{
    public ISelectableObject SingleSelectedObject { get; private set; }
    public HashSet<ISelectableObject> MultiSelectedObjects { get; private set; } = new HashSet<ISelectableObject>();

    public void SingleSelect(ISelectableObject selectableObject)
    {
        if (SingleSelectedObject != null && !SingleSelectedObject.Equals(null))
        {
            SingleSelectedObject.OnSingleUnselected();
        }

        SingleSelectedObject = selectableObject;
        SingleSelectedObject.OnSingleSelected();
    }

    public void MultiSelect(ISelectableObject selectableObject)
    {
        if (MultiSelectedObjects.Contains(selectableObject))
        {
            MultiSelectedObjects.Remove(selectableObject);
            selectableObject.OnMultiUnselected();
        }
        else
        {
            MultiSelectedObjects.Add(selectableObject);
            selectableObject.OnMultiSelected();
        }
    }

    public void Reset()
    {
        if (SingleSelectedObject != null && !SingleSelectedObject.Equals(null))
        {
            SingleSelectedObject.OnSingleUnselected();
        }

        SingleSelectedObject = null;

        foreach (ISelectableObject selectedObject in MultiSelectedObjects)
        {
            if (!selectedObject.Equals(null))
            {
                selectedObject.OnMultiUnselected();
            }
        }

        MultiSelectedObjects.Clear();
    }
}
