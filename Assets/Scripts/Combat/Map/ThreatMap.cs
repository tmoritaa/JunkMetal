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

    private HashSet<ThreatNode> nodesMarkedTankToHitNodeNoReload = new HashSet<ThreatNode>();
    public HashSet<ThreatNode> NodesMarkedTankToHitNodeNoReload
    {
        get {
            return nodesMarkedTankToHitNodeNoReload;
        }
    }

    public ThreatMap(float _mapWidth, float _mapHeight, float _tileDim, List<Transform> walls = null)
        : base(_mapWidth, _mapHeight, _tileDim, walls) { }

    public ThreatMap(Map map) : base(map) { }

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
        nodesMarkedTankToHitNodeNoReload.Clear();

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

                float timeToHitPos = AIUtility.CalcTimeToHitPos(weapon.OwningTank.transform.position, weapon.CalculateFireVec(), weapon.OwningTank, weapon.Schematic, NodeToPosition(node));
                float timeToHitPosWithReloadTime = timeToHitPos + weapon.CalcTimeToReloaded();

                // If time is over 1 second, we consider it too long and stop searching.
                if (timeToHitPosWithReloadTime <= MaxTimeInSecs || timeToHitPos <= MaxTimeInSecs) {
                    if (node.TimeForTargetToHitNode > timeToHitPosWithReloadTime) {
                        node.TimeForTargetToHitNode = timeToHitPosWithReloadTime;
                    }

                    if (node.TimeForTargetToHitNodeNoReload > timeToHitPos) {
                        node.TimeForTargetToHitNodeNoReload = timeToHitPos;
                    }

                    List<Connection> connections = FindConnectedNodes(node, true);
                    foreach (Connection con in connections) {
                        if (checkedNodes.Find(n => n == con.targetNode) == null && openNodes.Find(n => n == con.targetNode) == null) {
                            openNodes.Add(con.targetNode);
                        }
                    }
                }

                nodesMarkedTankToHitNode.Add(node);
                nodesMarkedTankToHitNodeNoReload.Add(node);
                checkedNodes.Add(node);
                openNodes.RemoveAt(0);
            }
        }   
    }
    
    protected override float calcHeuristicCost(Node node, Node target) {
        List<Connection> connections = FindStraightPath(node, target);

        float totalCost = 0;
        foreach (Connection con in connections) {
            ThreatNode threatNode = (ThreatNode)con.targetNode;
            totalCost += Mathf.Clamp(MaxTimeInSecs - threatNode.TimeForTargetToHitNode, 0, MaxTimeInSecs);
        }

        return totalCost;
    }

    protected override float calculateConnectionCost(Connection connection) {
        ThreatNode threatNode = (ThreatNode)connection.targetNode;
        return Mathf.Clamp(MaxTimeInSecs - threatNode.TimeForTargetToHitNode, 0, MaxTimeInSecs);
    }

    protected override Node createNode(int x, int y, params object[] values) {
        return new ThreatNode(x, y);
    }
}
