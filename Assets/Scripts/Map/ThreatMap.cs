using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

public class ThreatMap : Map
{
    public ThreatMap(float _mapWidth, float _mapHeight, float _tileDim, List<Transform> walls = null)
        : base(_mapWidth, _mapHeight, _tileDim, walls) { }

    public ThreatMap(Map map) : base(map) { }

    public override void ResetNodeValues() {
        foreach (Node n in MapArray) {
            ((ThreatNode)n).ThreatValue = 0;
        }
    }

    public void UpdateThreats(Tank oppTank) {
        foreach (WeaponPart weapon in oppTank.Turret.GetAllWeapons()) {
            List<Node> checkedNodes = new List<Node>();
            List<Node> openNodes = new List<Node>();
            openNodes.Add(PositionToNode(weapon.CalculateFirePos()));
            while (openNodes.Count > 0) {
                ThreatNode node = (ThreatNode)openNodes[0];

                float threatVal = 1f - AIUtility.CalcThreatValueAtPos(weapon, NodeToPosition(node));
                // If time is over 1 second, we consider it too long and stop searching.
                if (threatVal > 0) {
                    if (node.ThreatValue < threatVal) {
                        node.ThreatValue = threatVal;
                    }

                    List<Connection> connections = FindConnectedNodes(node, true);
                    foreach (Connection con in connections) {
                        if (checkedNodes.Find(n => n == con.targetNode) == null && openNodes.Find(n => n == con.targetNode) == null) {
                            openNodes.Add(con.targetNode);
                        }
                    }
                }

                checkedNodes.Add(node);
                openNodes.RemoveAt(0);
            }
        }   
    }

    protected override Node createNode(int x, int y, params object[] values) {
        return new ThreatNode(x, y);
    }
}
