using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GraphNode
{
    public string AnchorId;
    public string AnchorName;
    public List<Tuple<string, SerializableVector3>> OffsetList;
    public List<string> NeighbourIds;
}
