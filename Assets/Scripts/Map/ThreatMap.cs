using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

public class ThreatMap : Map
{
    public const float MaxTimeInSecs = 1f;

    private HashSet<ThreatNode> nodesMarkedHitTargetFromNode = new HashSet<ThreatNode>();
    public HashSet<ThreatNode> NodesMarkedHitTargetFromNode
    {
        get {
            return nodesMarkedHitTargetFromNode;
        }
    }

    private HashSet<ThreatNode> nodesMarkedTankToHitNode = new HashSet<ThreatNode>();
    public HashSet<ThreatNode> NodesMarkedTankToHitNode
    {
        get {
            return nodesMarkedTankToHitNode;
        }
    }

    public ThreatMap(float _mapWidth, float _mapHeight, float _tileDim, List<Transform> walls = null)
        : base(_mapWidth, _mapHeight, _tileDim, walls) { }

    public ThreatMap(Map map) : base(map) { }

    protected override float calcHeuristicCost(Node node, Node target) {
        List<Connection> connections = FindStraightPath(node, target);

        float totalCost = 0;
        foreach(Connection con in connections) {
            ThreatNode threatNode = (ThreatNode)con.targetNode;
            totalCost += Mathf.Clamp(MaxTimeInSecs - threatNode.TimeForTargetToHitNode, 0, MaxTimeInSecs);
        }

        return totalCost;
    }

    protected override float calculateConnectionCost(Connection connection) {
        ThreatNode threatNode = (ThreatNode)connection.targetNode;
        return Mathf.Clamp(MaxTimeInSecs - threatNode.TimeForTargetToHitNode, 0, MaxTimeInSecs);
    }

    public void UpdateTimeToHitTargetFromNode(Tank selfTank, Tank targetTank) {
        nodesMarkedHitTargetFromNode.Clear();

        foreach (WeaponPart weapon in selfTank.Turret.GetAllWeapons()) {
            List<Node> checkedNodes = new List<Node>();
            List<Node> openNodes = new List<Node>();
            openNodes.Add(PositionToNode(targetTank.transform.position));
            Node startNode = PositionToNode(weapon.CalculateFirePos());
            openNodes.Add(startNode);
            // Also add surrounding nodes
            {
                List<Connection> connections = FindConnectedNodes(startNode, true);
                foreach (Connection con in connections) {
                    if (checkedNodes.Find(n => n == con.targetNode) == null && openNodes.Find(n => n == con.targetNode) == null) {
                        openNodes.Add(con.targetNode);
                    }
                }
            }

            while (openNodes.Count > 0) {
                ThreatNode node = (ThreatNode)openNodes[0];
                Vector2 nodePos = NodeToPosition(node);

                float timeToHitPos = AIUtility.CalcTimeToHitPos(nodePos, weapon.CalculateFireVec(), weapon.OwningTank, weapon.Schematic, targetTank.transform.position);
                timeToHitPos += weapon.CalcTimeToReloaded();
                if (timeToHitPos < MaxTimeInSecs) {
                    if (node.TimeToHitTargetFromNode > timeToHitPos) {
                        node.TimeToHitTargetFromNode = timeToHitPos;
                        node.WeaponToHitTargetFromNode = weapon;
                    }

                    List<Connection> connections = FindConnectedNodes(node, true);
                    foreach (Connection con in connections) {
                        if (checkedNodes.Find(n => n == con.targetNode) == null && openNodes.Find(n => n == con.targetNode) == null) {
                            openNodes.Add(con.targetNode);
                        }
                    }
                }

                nodesMarkedHitTargetFromNode.Add(node);
                checkedNodes.Add(node);
                openNodes.RemoveAt(0);
            }
        }
    }

    public void UpdateTimeForTankToHitNode(Tank tank) {
        nodesMarkedTankToHitNode.Clear();

        foreach (WeaponPart weapon in tank.Turret.GetAllWeapons()) {
            List<Node> checkedNodes = new List<Node>();
            List<Node> openNodes = new List<Node>();

            Node startNode = PositionToNode(weapon.CalculateFirePos());
            openNodes.Add(startNode);
            // Also add surrounding nodes
            {
                List<Connection> connections = FindConnectedNodes(startNode, true);
                foreach (Connection con in connections) {
                    if (checkedNodes.Find(n => n == con.targetNode) == null && openNodes.Find(n => n == con.targetNode) == null) {
                        openNodes.Add(con.targetNode);
                    }
                }
            }
            
            while (openNodes.Count > 0) {
                ThreatNode node = (ThreatNode)openNodes[0];

                float timeToHitPos = AIUtility.CalcTimeToHitPos(weapon.CalculateFirePos(), weapon.CalculateFireVec(), weapon.OwningTank, weapon.Schematic, NodeToPosition(node));
                timeToHitPos += weapon.CalcTimeToReloaded();
                // If time is over 1 second, we consider it too long and stop searching.
                if (timeToHitPos < MaxTimeInSecs) {
                    if (node.TimeForTargetToHitNode > timeToHitPos) {
                        node.TimeForTargetToHitNode = timeToHitPos;
                    }

                    List<Connection> connections = FindConnectedNodes(node, true);
                    foreach (Connection con in connections) {
                        if (checkedNodes.Find(n => n == con.targetNode) == null && openNodes.Find(n => n == con.targetNode) == null) {
                            openNodes.Add(con.targetNode);
                        }
                    }
                }

                nodesMarkedTankToHitNode.Add(node);
                checkedNodes.Add(node);
                openNodes.RemoveAt(0);
            }
        }   
    }

    protected override Node createNode(int x, int y, params object[] values) {
        return new ThreatNode(x, y);
    }
}
