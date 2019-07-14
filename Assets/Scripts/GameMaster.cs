using Microsoft.MixedReality.Toolkit.Core.Interfaces.InputSystem.Handlers;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.SpatialAwarenessSystem.Observers;
using Microsoft.MixedReality.Toolkit.Core.Services;
using Microsoft.MixedReality.Toolkit.Core.Utilities;
using Microsoft.MixedReality.Toolkit.SDK.Input.Handlers;
using Microsoft.MixedReality.Toolkit.SDK.UX.Iteractable;
using Microsoft.MixedReality.Toolkit.Services.InputSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.WSA;
using UnityEngine.XR.WSA.Persistence;

public class GameMaster : MonoBehaviour
{
    public static GameMaster Instance;

    [SerializeField]
    private InteractableToggleCollection gameModeSelection;

    [SerializeField]
    private InteractableToggleCollection editingActionSelection;

    [SerializeField]
    private GameObject anchorGameObjectPrefab;

    [SerializeField]
    private List<GameObject> controlPanel = new List<GameObject> { null, null, null };

    [SerializeField]
    private bool clearAnchorOnStart;

    [SerializeField]
    private LineRenderer pathRenderer;

    [SerializeField]
    private EditableText destinationName;

    [SerializeField]
    private EditingActionButtonControl editingActionButtonControl;

    public GameModeChangedEventType GameModeChangedEvent = new GameModeChangedEventType();

    public Dictionary<string, AnchorHandler> ExistingAnchors { get { return existingAnchors; } }

    private int currentModeIndex = 0;
    private Dictionary<string, AnchorHandler> existingAnchors = new Dictionary<string, AnchorHandler>();
    private const string graphInfoFileName = "anchorGraphInfo";
    private string graphInfoFilePath;
    private WorldAnchorStore anchorStore;
    private Transform cameraTransform;
    private GraphInfo graphInfo;
    private PathFinder pathFinder = new PathFinder();
    private TouchScreenKeyboard keyboard;
    private GazeProvider gazeProvider;
    private IMixedRealitySpatialAwarenessMeshObserver spatialMeshObserver;
    private Task updateNeighbourTask;
    private SelectionManager selectionManager = new SelectionManager();
    private int editingActionIndex;
    private NeighbourManager neighbourManager;

    private GameMode CurrentGameMode
    {
        get { return (GameMode)currentModeIndex; }
    }

    private EditingAction CurrentEditingAction
    {
        get { return (EditingAction)editingActionIndex; }
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            DestroyImmediate(gameObject);
        }

        cameraTransform = CameraCache.Main.transform;
        //Debug.Log($"Check persistent data path exists: {Application.persistentDataPath}.");
        //Directory.CreateDirectory(Application.persistentDataPath);
        graphInfoFilePath = Path.Combine(Application.persistentDataPath, graphInfoFileName);
        neighbourManager =  new NeighbourManager(selectionManager);
    }

    // Start is called before the first frame update
    void Start()
    {
        gameModeSelection.OnSelectionEvents.AddListener(OnGameModeChanged);
        editingActionSelection.OnSelectionEvents.AddListener(OnEditingActionChanged);
        LoadGraphInfo();
        WorldAnchorStore.GetAsync(OnAnchorStoreLoaded);
        gazeProvider = CameraCache.Main.GetComponent<GazeProvider>();
        spatialMeshObserver = MixedRealityToolkit.Instance.GetService<IMixedRealitySpatialAwarenessMeshObserver>();
    }

    void Update()
    {
    }

    public void OnPointerClicked()
    {
        if (CurrentGameMode == GameMode.Editing)
        {
            GameObject target = gazeProvider.GazeTarget;

            if (CurrentEditingAction == EditingAction.PlaceAnchor)
            {
                if (target != null && target.GetComponent<IMixedRealityFocusHandler>() != null)
                {
                    return;
                }

                if (target == null || Vector3.Distance(target.transform.position, cameraTransform.position) > 1)
                {
                    CreateAnchor();
                }
            }
            else
            {
                if (target == null)
                {
                    return;
                }

                AnchorHandler selectedAnchor = target.GetComponentInParent<AnchorHandler>();
                if (selectedAnchor == null)
                {
                    return;
                }

                if (CurrentEditingAction == EditingAction.ShowNeighbours)
                {
                    neighbourManager.ShowNeighbour(selectedAnchor);
                }
                else if (CurrentEditingAction == EditingAction.OverrideNeighbours)
                {
                    neighbourManager.OverrideNeighbourSelect(selectedAnchor);
                }
            }
        }
    }

    public void OnPointerDown()
    {
        GameObject target = gazeProvider.GazeTarget;
        //if (target == null)
        //{
        //    Debug.Log($"OnPointerDown: No target.");
        //}
        //else
        //{
        //    Debug.Log($"OnPointerDown: Target is {target.name}. IsFocusHandler: {target.GetComponent<IMixedRealityFocusHandler>()}");
        //}
        //Debug.Log($"OnPointerDown: Target is {gazeProvider.GazeTarget?.name}");
    }

    public void OnPointerUp()
    {
    }

    public List<Tuple<string, SerializableVector3>> GetOffsetList(string anchorId)
    {
        GraphNode node;
        if (graphInfo.Nodes.TryGetValue(anchorId, out node))
        {
            return node.OffsetList;
        }

        return null;
    }

    public List<string> GetNeighbourIds(string anchorId)
    {
        GraphNode node;
        if (graphInfo.Nodes.TryGetValue(anchorId, out node))
        {
            return node.NeighbourIds;
        }

        return null;
    }

    public void OverrideNeighbourIds(string masterAnchorId, List<string> neighbourIds)
    {
        graphInfo.Nodes[masterAnchorId].NeighbourIds = neighbourIds;
    }

    public void OverrideNeighbours()
    {
        neighbourManager.UpdateNeighbours();
    }

    public void ResetNeighbourSelection()
    {
        neighbourManager.Reset();
    }

    public AnchorHandler GetAnchorHandler(string anchorId)
    {
        return existingAnchors[anchorId];
    }

    public void OnAnchorNameChanged(string id, string anchorName)
    {
        GraphNode node;
        if (graphInfo.Nodes.TryGetValue(id, out node))
        {
            node.AnchorName = anchorName;
            return;
        }

        UpdateGraphInfoOnCreation(id);
    }

    private void OnAnchorStoreLoaded(WorldAnchorStore store)
    {
        anchorStore = store;

        if (clearAnchorOnStart)
        {
            anchorStore.Clear();
            if (graphInfo == null)
            {
                graphInfo = new GraphInfo();
            }
            else
            {
                graphInfo.Reset();
            }

            Debug.Log("All anchors are cleared.");

            return;
        }

        string[] anchorIds = anchorStore.GetAllIds();

        foreach (string id in anchorIds)
        {
            AnchorHandler anchorHandler = LoadAnchor(id);
            existingAnchors.Add(id, anchorHandler);
            anchorHandler.Anchor.OnTrackingChanged += OnAnchorTrackingChanged;
            Debug.Log($"Anchor {id} isLocated: {anchorHandler.Anchor.isLocated}");
        }

        Debug.Log($"Anchor Store loaded. Current Ids: {string.Join(", ", anchorIds)}.");

    }

    private void OnAnchorTrackingChanged(WorldAnchor worldAnchor, bool located)
    {
        AnchorHandler handler = worldAnchor.GetComponentInParent<AnchorHandler>();
        Debug.Log($"Anchor {handler.AnchorId} located: {located}.");
    }

    private void OnGameModeChanged()
    {
        int newModeIndex = gameModeSelection.CurrentIndex;
        if (newModeIndex == currentModeIndex)
        {
            return;
        }

        controlPanel[currentModeIndex].SetActive(false);
        controlPanel[newModeIndex].SetActive(true);
        currentModeIndex = newModeIndex;
        GameModeChangedEvent.Invoke(CurrentGameMode);
        selectionManager.Reset();
    }

    private void OnEditingActionChanged()
    {
        int newActionIndex = editingActionSelection.CurrentIndex;
        if (newActionIndex == editingActionIndex)
        {
            return;
        }

        editingActionIndex = newActionIndex;
        editingActionButtonControl.UpdateButtons(CurrentEditingAction);
        neighbourManager.Reset();
    }

    public void ClearAllAnchors()
    {
        if (anchorStore == null)
        {
            Debug.Log("Anchor store hasn't been loaded.");
            return;
        }

        string[] anchorIds = anchorStore.GetAllIds();

        foreach (string id in anchorIds)
        {
            existingAnchors[id].OnDelete();
        }

        if (graphInfo == null)
        {
            graphInfo = new GraphInfo();
        }
        else
        {
            graphInfo.Reset();
        }

        Debug.Log($"Anchor store is cleared. Anchor count: {anchorStore.anchorCount}.");
    }

    private WorldAnchor CreateAnchor()
    {
        if (anchorStore == null)
        {
            Debug.Log("Anchor store hasn't been loaded.");
            return null;
        }

        if (updateNeighbourTask != null && !updateNeighbourTask.IsCompleted)
        {
            Debug.Log("Anchor creation task is not finished.");
            return null;
        }

        string id = Guid.NewGuid().ToString();
        Vector3 position = cameraTransform.TransformPoint(1.1f * Vector3.forward);
        GameObject anchorGameObject = Instantiate(anchorGameObjectPrefab, position, cameraTransform.rotation);
        anchorGameObject.transform.up = Vector3.up;
        AnchorHandler anchorHandler = anchorGameObject.GetComponent<AnchorHandler>();
        anchorHandler.Initialize(id, "");
        WorldAnchor anchor = anchorHandler.Anchor;
        anchorStore.Save(id, anchor);
        existingAnchors.Add(id, anchorHandler);

        Debug.Log($"Anchor {id} is created. Anchor count: {anchorStore.anchorCount}.");

        UpdateGraphInfoOnCreation(id);

        return anchor;
    }

    private AnchorHandler LoadAnchor(string id)
    {
        if (anchorStore == null)
        {
            Debug.Log("Anchor store hasn't been loaded.");
            return null;
        }

        GameObject anchorGameObject = Instantiate<GameObject>(anchorGameObjectPrefab);
        AnchorHandler anchorHandler = anchorGameObject.GetComponent<AnchorHandler>();
        GraphNode node;
        string anchorName = "";
        if (graphInfo.Nodes.TryGetValue(id, out node))
        {
            anchorName = node.AnchorName;
        }
        else
        {
            Debug.LogWarning($"Unable to find id: {id} in local database.");
        }

        anchorHandler.Initialize(id, anchorName);
        anchorStore.Load(id, anchorHandler.AnchorGameObject);

        return anchorHandler;
    }

    public bool AllowDeleteAnchor()
    {
        return updateNeighbourTask == null || updateNeighbourTask.IsCompleted;
    }

    public void DeleteAnchor(string id)
    {
        if (anchorStore == null)
        {
            Debug.Log("Anchor store hasn't been loaded.");
            return;
        }

        anchorStore.Delete(id);
        existingAnchors.Remove(id);
        UpdateGraphInfoOnRemoval(id);

        Debug.Log($"Anchor {id} is deleted. Anchor count: {anchorStore.anchorCount}");
    }

    void OnDestroy()
    {
        SaveGraphInfo();
        gameModeSelection.OnSelectionEvents.RemoveListener(OnGameModeChanged);
        editingActionSelection.OnSelectionEvents.RemoveListener(OnEditingActionChanged);

        foreach (AnchorHandler handler in existingAnchors.Values)
        {
            handler.Anchor.OnTrackingChanged -= OnAnchorTrackingChanged;
        }
    }

    private void LoadGraphInfo()
    {
        if (!File.Exists(graphInfoFilePath))
        {
            Debug.LogWarning("Graph info file not exists.");
            return;
        }

        GraphInfo graphInfo;

        try
        {
            using (Stream stream = new FileStream(graphInfoFilePath, FileMode.Open, FileAccess.Read))
            {
                stream.Position = 0;
                IFormatter formatter = new BinaryFormatter();
                graphInfo = (GraphInfo)formatter.Deserialize(stream);
            }

            this.graphInfo = graphInfo;
        }
        catch (Exception e)
        {
            Debug.LogError($"Fail to load graph. {e.Message}");
            this.graphInfo = new GraphInfo
            {
                Nodes = new Dictionary<string, GraphNode>()
            };
        }
    }

    private void SaveGraphInfo()
    {
        if (graphInfo == null)
        {
            Debug.LogWarning("Graph info is null.");
            return;
        }

        using (Stream stream = new FileStream(graphInfoFilePath, FileMode.Create, FileAccess.Write))
        {
            IFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, graphInfo);
        }
    }

    private List<AnchorHandler> GetLocatedAnchors()
    {
        List<AnchorHandler> locatedAnchors = new List<AnchorHandler>();

        foreach (AnchorHandler anchorHandler in existingAnchors.Values)
        {
            if (!anchorHandler.IsLocated)
            {
                continue;
            }

            locatedAnchors.Add(anchorHandler);
        }

        return locatedAnchors;
    }

    private void ResetLocatedNodes(List<AnchorHandler> locatedAnchors)
    {
        foreach (AnchorHandler anchorHandler in locatedAnchors)
        {
            if (!graphInfo.Nodes.ContainsKey(anchorHandler.AnchorId))
            {
                graphInfo.Nodes.Add(anchorHandler.AnchorId, new GraphNode
                {
                    AnchorId = anchorHandler.AnchorId,
                    AnchorName = anchorHandler.AnchorName
                });
            }
            graphInfo.Nodes[anchorHandler.AnchorId].OffsetList = new List<Tuple<string, SerializableVector3>>();
        }
    }

    private void UpdateOffsetList(List<AnchorHandler> locatedAnchors)
    {
        int count = locatedAnchors.Count;

        for (int firstIndex = 0; firstIndex < count - 1; firstIndex++)
        {
            for (int secondIndex = firstIndex + 1; secondIndex < count; secondIndex++)
            {
                AnchorHandler first = locatedAnchors[firstIndex];
                AnchorHandler second = locatedAnchors[secondIndex];
                graphInfo.Nodes[first.AnchorId].OffsetList.Add(
                    new Tuple<string, SerializableVector3>(
                        second.AnchorId,
                        new SerializableVector3(first.Anchor.transform.position - second.Anchor.transform.position)));
                graphInfo.Nodes[second.AnchorId].OffsetList.Add(
                    new Tuple<string, SerializableVector3>(
                        first.AnchorId,
                        new SerializableVector3(second.Anchor.transform.position - first.Anchor.transform.position)));
            }
        }

        foreach (GraphNode node in graphInfo.Nodes.Values)
        {
            node.OffsetList.Sort((e1, e2) =>
                e1.Item2.ToVector3().magnitude.CompareTo(e2.Item2.ToVector3().magnitude));
        }
    }

    private void RemoveOutdatedAnchorsFromGraph()
    {
        List<string> outdatedIds = new List<string>();

        foreach (string id in graphInfo.Nodes.Keys)
        {
            if (!existingAnchors.ContainsKey(id))
            {
                Debug.LogWarning($"Existing anchors no longer have id: {id}. Remove it from graphInfo.");
                outdatedIds.Add(id);
            }
        }

        foreach (string id in outdatedIds)
        {
            graphInfo.Nodes.Remove(id);
        }
    }

    private void UpdateGraphInfoOnCreation(string idCreated)
    {
        if (graphInfo == null)
        {
            graphInfo = new GraphInfo();
        }

        RemoveOutdatedAnchorsFromGraph();

        List<AnchorHandler> locatedAnchors = GetLocatedAnchors();
        // The newly created anchor will always show isLocated = false, so we need to manually add it to the locatedAnchors list.
        AnchorHandler anchorHandler = existingAnchors[idCreated];
        locatedAnchors.Add(anchorHandler);
        ResetLocatedNodes(locatedAnchors);
        UpdateOffsetList(locatedAnchors);
        updateNeighbourTask = UpdateNeighbourOnCreationAsync(anchorHandler);

        foreach (GraphNode node in graphInfo.Nodes.Values)
        {
            Debug.Log("Graph info updated:");
            Debug.Log($"{node.AnchorId}: {node.OffsetList.Count}");
        }

        SaveGraphInfo();
    }

    private async Task UpdateNeighbourOnCreationAsync(AnchorHandler anchorHandler)
    {
        await Task.Delay(TimeSpan.FromSeconds(spatialMeshObserver.UpdateInterval));

        List<string> neighbourIds = new List<string>();

        Vector3 anchorPosition = anchorHandler.AnchorVisualTransform.position;
        float observerBoundDistance = spatialMeshObserver.ObservationExtents.x;
        List<AnchorHandler> surroundingAnchors = new List<AnchorHandler>();
        foreach (AnchorHandler otherAnchorHandler in existingAnchors.Values)
        {
            if (otherAnchorHandler.AnchorId == anchorHandler.AnchorId ||
                otherAnchorHandler.TrackingState == AnchorTrackingState.Lost ||
                Vector3.Distance(otherAnchorHandler.AnchorVisualTransform.position, anchorPosition) > observerBoundDistance)
            {
                continue;
            }

            surroundingAnchors.Add(otherAnchorHandler);
        }

        const float verticalMaxDistance = 100f;
        const float raycastOriginOffset = 0.5f;
        int layerMask = spatialMeshObserver.MeshPhysicsLayerMask;
        bool raycastBottom = true;
        bool raycastTop = true;

        RaycastHit hitFloor;
        if (Physics.Raycast(anchorPosition, Vector3.down, out hitFloor, verticalMaxDistance, layerMask,
            QueryTriggerInteraction.Ignore))
        {
            raycastBottom = hitFloor.distance > raycastOriginOffset;
        }

        RaycastHit hitCeiling;
        if (Physics.Raycast(anchorPosition, Vector3.up, out hitCeiling, verticalMaxDistance, layerMask,
            QueryTriggerInteraction.Ignore))
        {
            raycastTop = hitFloor.distance > raycastOriginOffset;
        }

        foreach (AnchorHandler otherAnchorHandler in surroundingAnchors)
        {
            Vector3 direction = otherAnchorHandler.AnchorVisualTransform.position - anchorPosition;
            if (raycastBottom)
            {
                Vector3 bottomPosition = anchorPosition + Vector3.down * raycastOriginOffset;
                if (CheckNodesBlocked(bottomPosition, direction, layerMask))
                {
                    continue;
                }
            }
            if (raycastTop)
            {
                Vector3 topPosition = anchorPosition + Vector3.up * raycastOriginOffset;
                if (CheckNodesBlocked(topPosition, direction, layerMask))
                {
                    continue;
                }
            }

            if (CheckNodesBlocked(anchorPosition, direction, layerMask))
            {
                continue;
            }

            neighbourIds.Add(otherAnchorHandler.AnchorId);
            GraphNode otherNode = graphInfo.Nodes[otherAnchorHandler.AnchorId];
            if (otherNode.NeighbourIds == null)
            {
                otherNode.NeighbourIds = new List<string>();
            }
            otherNode.NeighbourIds.Add(anchorHandler.AnchorId);
        }

        graphInfo.Nodes[anchorHandler.AnchorId].NeighbourIds = neighbourIds;
        SaveGraphInfo();
    }

    private bool CheckNodesBlocked(Vector3 origin, Vector3 direction, int layerMask)
    {
        float maxDistance = direction.magnitude;
        Vector3 destination = origin + direction;
        return Physics.Raycast(origin, direction, maxDistance, layerMask, QueryTriggerInteraction.Ignore) ||
            Physics.Raycast(destination, -direction, maxDistance, layerMask, QueryTriggerInteraction.Ignore);
    }

    private void UpdateGraphInfoOnRemoval(string idRemoved)
    {
        if (graphInfo == null)
        {
            graphInfo = new GraphInfo();
        }

        if (graphInfo.Nodes.ContainsKey(idRemoved))
        {
            graphInfo.Nodes.Remove(idRemoved);
        }

        RemoveOutdatedAnchorsFromGraph();

        List<AnchorHandler> locatedAnchors = GetLocatedAnchors();
        ResetLocatedNodes(locatedAnchors);

        foreach (GraphNode node in graphInfo.Nodes.Values)
        {
            if (node.NeighbourIds != null)
            {
                node.NeighbourIds.Remove(idRemoved);
            }

            if (node.OffsetList.Count == 0)
            {
                continue;
            }

            List<Tuple<string, SerializableVector3>> offsetListAfterRemoval = new List<Tuple<string, SerializableVector3>>();
            
            foreach (var tuple in node.OffsetList)
            {
                if (tuple.Item1 != idRemoved)
                {
                    offsetListAfterRemoval.Add(tuple);
                }
            }

            node.OffsetList = offsetListAfterRemoval;
        }

        UpdateOffsetList(locatedAnchors);

        SaveGraphInfo();
    }

    public void FindPath()
    {
        Vector3 currentPosition = cameraTransform.position;
        int count = existingAnchors.Count;
        if (count == 0)
        {
            Debug.Log("No anchor exists. Stop finding path.");
            return;
        }

        // Randomly find a destination.
        //int selectedAnchorIndex = UnityEngine.Random.Range(0, count - 1);
        //string endAnchorId = null;
        //int index = 0;
        //foreach (string id in existingAnchors.Keys)
        //{
        //    if (index == selectedAnchorIndex)
        //    {
        //        endAnchorId = id;
        //        break;
        //    }
        //    index++;
        //}

        string endAnchorId = null;
        string endAnchorName = destinationName.Text;
        foreach (AnchorHandler anchorHandler in existingAnchors.Values)
        {
            if (anchorHandler.AnchorName != endAnchorName)
            {
                continue;
            }

            if (anchorHandler.TrackingState == AnchorTrackingState.Lost)
            {
                Debug.LogWarning($"Anchor {endAnchorName} loses tracking. Unable to find path.");
                return;
            }

            endAnchorId = anchorHandler.AnchorId;
            break;
        }

        if (string.IsNullOrEmpty(endAnchorId))
        {
            Debug.LogWarning($"Unable to find anchor name: {endAnchorName}.");
            return;
        }

        pathFinder.Find(currentPosition, endAnchorId);
        List<AnchorHandler> paths = pathFinder.Paths;
        RenderPath(paths);
    }

    public void ClearRenderedPath()
    {
        pathRenderer.positionCount = 0;
    }

    private void RenderPath(List<AnchorHandler> paths)
    {
        int count = paths.Count;
        Vector3[] positions = new Vector3[count];

        for (int index = 0; index < count; index++)
        {
            positions[index] = paths[index].AnchorVisualTransform.position;
        }

        pathRenderer.positionCount = count;
        pathRenderer.SetPositions(positions);
    }

    [Serializable]
    public class GameModeChangedEventType : UnityEvent<GameMode>
    {
    }
}
