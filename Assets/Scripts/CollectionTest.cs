using Microsoft.MixedReality.Toolkit.SDK.UX.Iteractable;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectionTest : MonoBehaviour
{
    [SerializeField]
    private InteractableToggleCollection toggleCollection;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnSelection()
    {
        Debug.Log($"{toggleCollection.CurrentIndex} is selected.");
    }
}
