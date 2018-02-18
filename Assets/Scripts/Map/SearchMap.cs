using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class SearchMap : Map
{
    public SearchMap(float _mapWidth, float _mapHeight, float _tileDim, List<Transform> walls = null)
        : base(_mapWidth, _mapHeight, _tileDim, walls) {}

    public override void ResetNodeValues() {
        foreach (Node n in MapArray) {
            ((SearchNode)n).searched = false;
        }
    }

    protected override float calcHeuristicCost(Node node, Node target) {
        Vector2 pos = NodeToPosition(node);
        Vector2 targetPos = NodeToPosition(target);

        return (pos - targetPos).sqrMagnitude;
    }

    protected override float calculateConnectionCost(Connection connection) {
        Vector2 pos = NodeToPosition(connection.srcNode);
        Vector2 otherPos = NodeToPosition(connection.targetNode);

        return (pos - otherPos).sqrMagnitude;
    }

    protected override Node createNode(int x, int y, params object[] values) {
        return new SearchNode(x, y);
    }
}
