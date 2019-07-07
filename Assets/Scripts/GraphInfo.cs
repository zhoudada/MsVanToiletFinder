using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GraphInfo
{
    public Dictionary<string, GraphNode> Nodes = new Dictionary<string, GraphNode>();

    public void Reset()
    {
        Nodes.Clear();
    }
}
