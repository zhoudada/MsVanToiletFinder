using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.WSA;

public class AnchorHandler : MonoBehaviour
{
    [SerializeField]
    private Transform anchorTransform;

    [SerializeField]
    private TextMesh anchorIdText;

    [SerializeField]
    private Transform anchorVisual;

    [SerializeField]
    private GameObject editPanel;

    [SerializeField]
    private List<GameObject> coloredObjects;

    [SerializeField]
    private Material locatedColor;

    [SerializeField]
    private Material locationDeducedColor;

    [SerializeField]
    private Material locationLostColor;

    [SerializeField]
    private EditableText nameLabel;

    private const int offsetDeductionLimit = 10;

    public string AnchorId { get; private set; }
    public string AnchorName { get; private set; }
    public WorldAnchor Anchor { get; private set; }
    public GameObject AnchorGameObject { get { return anchorTransform.gameObject; } }
    public bool IsLocated { get { return Anchor.isLocated; } }
    public Transform AnchorVisualTransform { get { return anchorVisual; } }
    public AnchorTrackingState TrackingState { get; private set; } = AnchorTrackingState.Lost;

    public void Initialize(string id, string name)
    {
        AnchorId = id;
        AnchorName = name;
        Anchor = anchorTransform.gameObject.AddComponent<WorldAnchor>();
        anchorIdText.text = id;
        GameMaster.Instance.GameModeChangedEvent.AddListener(OnGameModeChanged);
        nameLabel.UpdateText(name);
        nameLabel.EditingCompletionEvent.AddListener(UpdateName);
    }

    private void OnGameModeChanged(GameMode currentMode)
    {
        if (currentMode == GameMode.Editing)
        {
            editPanel.SetActive(true);
        }
        else if (currentMode == GameMode.Release)
        {
            editPanel.SetActive(false);
        }
    }

    public void UpdateName(string name)
    {
        AnchorName = name;
        GameMaster.Instance.OnAnchorNameChanged(AnchorId, name);
    }

    public void OnDelete()
    {
        nameLabel.EditingCompletionEvent.RemoveListener(UpdateName);
        GameMaster.Instance.GameModeChangedEvent.RemoveListener(OnGameModeChanged);
        GameMaster.Instance.DeleteAnchor(AnchorId);
        Destroy(AnchorGameObject);
        Destroy(gameObject);
    }

    private void DeducePosition()
    {
        List<Tuple<string, SerializableVector3>> offsetList = GameMaster.Instance.GetOffsetList(AnchorId);
        if (offsetList == null)
        {
            TrackingState = AnchorTrackingState.Lost;
            return;
        }

        int count = Math.Min(offsetList.Count, offsetDeductionLimit);
        Vector3 average = Vector3.zero;
        int otherAnchorNumber = 0;
        for (int index = 0; index < count; index++)
        {
            string otherId = offsetList[index].Item1;
            AnchorHandler otherAnchorHandler = GameMaster.Instance.GetAnchorHandler(otherId);
            if (otherAnchorHandler.TrackingState != AnchorTrackingState.Lost)
            {
                Vector3 offset = offsetList[index].Item2.ToVector3();
                average += otherAnchorHandler.AnchorVisualTransform.position + offset;
                otherAnchorNumber++;
            }
        }
        if (otherAnchorNumber > 0)
        {
            average /= otherAnchorNumber;
            anchorVisual.transform.position = average;
            TrackingState = AnchorTrackingState.LocationDeduced;
        }
        else
        {
            TrackingState = AnchorTrackingState.Lost;
        }
    }

    private void UpdateColor()
    {
        Material color;
        if (TrackingState == AnchorTrackingState.Located)
        {
            color = locatedColor;
        }
        else if (TrackingState == AnchorTrackingState.LocationDeduced)
        {
            color = locationDeducedColor;
        }
        else
        {
            color = locationLostColor;
        }

        int count = coloredObjects.Count;
        for (int index = 0; index < count; index++)
        {
            coloredObjects[index].GetComponent<MeshRenderer>().material = color;
        }
    }

    private void Update()
    {
        if (Anchor.isLocated)
        {
            TrackingState = AnchorTrackingState.Located;
            anchorVisual.transform.position = Anchor.transform.position;
            anchorVisual.transform.rotation = Anchor.transform.rotation;
        }
        else
        {
            DeducePosition();
        }

        UpdateColor();
    }
}
