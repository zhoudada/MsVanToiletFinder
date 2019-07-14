using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Priority_Queue;

public class PathFinder
{
    public List<AnchorHandler> Paths { get; private set; } = new List<AnchorHandler>();

    private float safeNeighbourDistance = 8f;

    public void Find(Vector3 startPosition, string endNodeId)
    {
        Paths.Clear();
        Dictionary<string, PathNode> allNodes = GetAllNodes();
        if (allNodes.Count == 0)
        {
            return;
        }

        PathNode startNode = GetStartNode(allNodes, startPosition);
        startNode.ActualWeight = 0;
        PathNode endNode = allNodes[endNodeId];
        foreach (PathNode node in allNodes.Values)
        {
            node.UpdateHeuristicWeight(endNode);
        }

        SimplePriorityQueue<PathNode> visitingNodes = new SimplePriorityQueue<PathNode>();
        HashSet<PathNode> visitingNodesHashSet = new HashSet<PathNode>();
        //Dictionary<PathNode, List<string>> neighbourIds = GetNeighbourIds(allNodes);
        Dictionary<PathNode, List<string>> neighbourIds = GetNeighbourIdsFromCache(allNodes);

        visitingNodes.Enqueue(startNode, startNode.Priority);
        visitingNodesHashSet.Add(startNode);

        while (visitingNodes.Count > 0)
        {
            PathNode node = visitingNodes.Dequeue();
            visitingNodesHashSet.Remove(node);
            node.IsVisited = true;

            if (node == endNode)
            {
                Debug.Log("Path found.");
                break;
            }

            foreach (string id in neighbourIds[node])
            {
                PathNode neighbour = allNodes[id];
                if (neighbour.IsVisited)
                {
                    continue;
                }

                if (!visitingNodesHashSet.Contains(neighbour))
                {
                    visitingNodes.Enqueue(neighbour, neighbour.Priority);
                    visitingNodesHashSet.Add(neighbour);
                }
                else
                {
                }

                float distance = node.FindDistance(neighbour);
                if (node.ActualWeight + distance >= neighbour.ActualWeight)
                {
                    continue;
                }

                neighbour.ActualWeight = node.ActualWeight + distance;
                neighbour.Parent = node;
            }
        }

        if (endNode.Parent == null)
        {
            Debug.LogWarning($"Fail to find the path to node {endNode.AnchorHandler.AnchorId}.");
            return;
        }

        Paths = ReconstructPath(endNode);
    }

    private Dictionary<string, PathNode> GetAllNodes()
    {
        Dictionary<string, PathNode> allNodes = new Dictionary<string, PathNode>();
        foreach (AnchorHandler anchorHandler in GameMaster.Instance.ExistingAnchors.Values)
        {
            if (anchorHandler.TrackingState != AnchorTrackingState.Lost)
            {
                allNodes.Add(anchorHandler.AnchorId, new PathNode(anchorHandler));
            }
        }

        return allNodes;
    }

    private PathNode GetStartNode(Dictionary<string, PathNode> allNodes, Vector3 startPosition)
    {
        float minDistance = float.MaxValue;
        PathNode startNode = null;
        foreach (PathNode node in allNodes.Values)
        {
            float distance = node.FindDistance(startPosition);
            if (distance < minDistance)
            {
                minDistance = distance;
                startNode = node;
            }
        }

        return startNode;
    }

    private Dictionary<PathNode, List<string>> GetNeighbourIdsFromCache(Dictionary<string, PathNode> allNodes)
    {
        Dictionary<PathNode, List<string>> allNeighbourIds = new Dictionary<PathNode, List<string>>();
        foreach (PathNode node in allNodes.Values)
        {
            List<string> neighbourIds = GameMaster.Instance.GetNeighbourIds(node.AnchorHandler.AnchorId);
            allNeighbourIds.Add(node, neighbourIds);
        }

        return allNeighbourIds;
    }

    private Dictionary<PathNode, List<string>> GetNeighbourIds(Dictionary<string, PathNode> allNodes)
    {
        Dictionary<PathNode, List<string>> neighbourIds = new Dictionary<PathNode, List<string>>();
        foreach (PathNode node in allNodes.Values)
        {
            neighbourIds.Add(node, new List<string>());
            List<Tuple<string, SerializableVector3>> offsetList = GameMaster.Instance.GetOffsetList(node.AnchorHandler.AnchorId);
            if (offsetList == null)
            {
                continue;
            }
            bool[] sectionFound = new bool[8];
            const int maxOffsetNumber = 6;
            int currentOffsetIndex = 0;
            float closestNeighbourDistance = float.MaxValue;
            List<Tuple<string, Vector3>> relativePositionList = GetRelativePositionList(node, offsetList, allNodes);
            foreach (Tuple<string, Vector3> relativePositionTuple in relativePositionList)
            {
                if (currentOffsetIndex >= maxOffsetNumber)
                {
                    break;
                }
                currentOffsetIndex++;
                Vector3 offset = relativePositionTuple.Item2;
                float distance = offset.magnitude;
                int sectionIndex = ComputeSection(offset, Vector3.up);
                if (sectionFound[sectionIndex])
                {
                    continue;
                }

                if (neighbourIds[node].Count == 0)
                {
                    closestNeighbourDistance = offset.magnitude;
                    sectionFound[sectionIndex] = true;
                    neighbourIds[node].Add(relativePositionTuple.Item1);
                }
                else
                {
                    if (distance > safeNeighbourDistance && distance > 3 * closestNeighbourDistance)
                    {
                        continue;
                    }
                    sectionFound[sectionIndex] = true;
                    neighbourIds[node].Add(relativePositionTuple.Item1);
                }
            }
        }

        return neighbourIds;
    }

    private List<Tuple<string, Vector3>> GetRelativePositionList(PathNode node, List<Tuple<string, SerializableVector3>> offsetList,
        Dictionary<string, PathNode> allNodes)
    {
        List<Tuple<string, Vector3>> relativePositionList = new List<Tuple<string, Vector3>>();
        foreach (var offsetTuple in offsetList)
        {
            string otherId = offsetTuple.Item1;
            Vector3 otherPosition = allNodes[otherId].AnchorHandler.AnchorVisualTransform.position;
            Vector3 relativePosition = otherPosition - node.AnchorHandler.AnchorVisualTransform.position;
            relativePositionList.Add(Tuple.Create(otherId, relativePosition));
        }

        relativePositionList.Sort((e1, e2) => e1.Item2.magnitude.CompareTo(e2.Item2.magnitude));
        return relativePositionList;
    }

    private List<AnchorHandler> ReconstructPath(PathNode endNode)
    {
        List<AnchorHandler> result = new List<AnchorHandler>();
        PathNode node = endNode;

        while (node != null)
        {
            result.Add(node.AnchorHandler);
            node = node.Parent;
        }

        return result;
    }

    private int ComputeSection(Vector3 direction, Vector3 normal)
    {
        Vector3 projectedDirection = Vector3.ProjectOnPlane(direction, normal);
        float x = projectedDirection.x;
        float z = projectedDirection.z;
        if (z >= 0)
        {
            if (x > 0)
            {
                float angle = Mathf.Atan(z / x) * Mathf.Rad2Deg;
                return angle < 45 ? 0 : 1;
            }
            else if (x < 0)
            {
                float angle = Mathf.Atan(-z / x) * Mathf.Rad2Deg;
                return angle < 45 ? 3 : 2;
            }
            else
            {
                // z >= 0, x = 0
                return 1;
            }
        }
        else
        {
            // z < 0
            if (x > 0)
            {
                float angle = Mathf.Atan(-z / x) * Mathf.Rad2Deg;
                return angle < 45 ? 7 : 6;
            }
            else if (x < 0)
            {
                float angle = Mathf.Atan(z / x) * Mathf.Rad2Deg;
                return angle < 45 ? 4 : 5;
            }
            else
            {
                // z < 0, x = 0
                return 6;
            }
        }

        throw new InvalidOperationException($"Unable to determine projected direction: {x}, {z}");
    }

    private class PathNode
    {
        public bool IsVisited { get; set; }
        public AnchorHandler AnchorHandler { get; set; }
        public float ActualWeight { get; set; }
        public float HeuristicWeight { get; set; }
        public PathNode Parent { get; set; }
        public float Priority => ActualWeight + HeuristicWeight;

        public PathNode(AnchorHandler handler)
        {
            IsVisited = false;
            AnchorHandler = handler;
            ActualWeight = float.MaxValue;
            HeuristicWeight = float.MaxValue;
            Parent = null;
        }

        public float FindDistance(Vector3 otherPosition)
        {
            return Vector3.Distance(AnchorHandler.AnchorVisualTransform.position, otherPosition);
        }

        public float FindDistance(PathNode other)
        {
            return FindDistance(other.AnchorHandler.AnchorVisualTransform.position);
        }

        public void UpdateHeuristicWeight(PathNode endNode)
        {
            HeuristicWeight = FindDistance(endNode);
        }
    }
}
