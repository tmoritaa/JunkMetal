using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class SearchMap : Map
{
    public SearchMap(float _mapWidth, float _mapHeight, float _tileDim, List<Transform> walls = null)
        : base(_mapWidth, _mapHeight, _tileDim, walls) {}

    public SearchMap(Map map) : base(map) 
        { }

    public override void ResetNodeValues() {
        foreach (Node n in MapArray) {
            ((SearchNode)n).searched = false;
        }
    }

    protected override Node createNode(int x, int y, params object[] values) {
        return new SearchNode(x, y);
    }
}
