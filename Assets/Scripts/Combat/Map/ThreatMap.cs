using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

public class ThreatMap : Map
{
    public class Cluster
    {
        public List<ThreatNode> Nodes
        {
            get; private set;
        }

        public int DangerLevel
        {
            get; private set;
        }

        private bool isCenterPosSaved = false;
        private Vector2 savedCenterPos = new Vector2();

        public Cluster(List<ThreatNode> nodes, int dangerLevel) {
            Nodes = nodes;
            DangerLevel = dangerLevel;
        }

        // TODO: later, make it not pass a threat map, but threat node should hold own reference of owner map.
        public Vector2 CalcCenterPos(ThreatMap map) {
            if (isCenterPosSaved) {
                return savedCenterPos;
            }

            Vector2 pos = new Vector2();

            foreach (ThreatNode node in Nodes) {
                pos += map.NodeToPosition(node);
            }

            pos /= Nodes.Count;

            savedCenterPos = pos;
            isCenterPosSaved = true;

            return pos;
        }
    }

    public const float MaxTimeInSecs = 1f;

    public const int MaxThreatLevel = 2;

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

                float timeToHitPos = AIUtility.CalcTimeToHitPos(nodePos, weapon.CalculateFireVec(), weapon.OwningTank, weapon.Schematic, targetTank.transform.position, true);
                timeToHitPos += weapon.CalcTimeToReloaded();

                bool marked = false;
                if (timeToHitPos < MaxTimeInSecs) {
                    if (node.TimeToHitTargetFromNode > timeToHitPos) {
                        node.TimeToHitTargetFromNode = timeToHitPos;
                        node.WeaponToHitTargetFromNode = weapon;
                        marked = true;
                    }

                    List<Connection> connections = FindConnectedNodes(node, true);
                    foreach (Connection con in connections) {
                        if (checkedNodes.Find(n => n == con.targetNode) == null && openNodes.Find(n => n == con.targetNode) == null) {
                            openNodes.Add(con.targetNode);
                        }
                    }
                }

                if (marked) {
                    nodesMarkedHitTargetFromNode.Add(node);
                    node.Marked = true;
                }

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
                node.Marked = true;
                openNodes.RemoveAt(0);
            }
        }   
    }
    
    public void MarkDangerousNodes() {
        foreach (ThreatNode node in nodesMarkedHitTargetFromNode) {
            float ratio = Mathf.Clamp(node.GetTimeDiffForHittingTarget(), 0, MaxTimeInSecs) / MaxTimeInSecs;
            node.TimeDiffDangerLevel = (int)Mathf.Clamp(ratio / (1f / (MaxThreatLevel + 1)), 0, MaxThreatLevel);
        }

        foreach (ThreatNode node in nodesMarkedTankToHitNode) {
            float ratio = node.TimeForTargetToHitNode / MaxTimeInSecs;
            node.TankToNodeDangerLevel = (int)Mathf.Clamp(ratio / (1f / (MaxThreatLevel + 1)), 0, MaxThreatLevel);
        }
    }

    public List<Cluster> FindClusters(List<ThreatNode> _nodes, Func<ThreatNode, float> valFunc) {
        List<ThreatNode> workingSet = _nodes;
        List<Cluster> clusters = new List<Cluster>();

        while (workingSet.Count > 0) {
            List<ThreatNode> checkedNodes = new List<ThreatNode>();

            List<ThreatNode> openNodes = new List<ThreatNode>();
            openNodes.Add(workingSet[0]);
            int searchDangerLevel = openNodes[0].TimeDiffDangerLevel;

            while (openNodes.Count > 0) {
                ThreatNode node = openNodes[0];

                if (node.TimeDiffDangerLevel == searchDangerLevel) {
                    List<Connection> connections = FindConnectedNodes(node, true);
                    foreach (Connection con in connections) {
                        if (checkedNodes.Find(n => n == con.targetNode) == null && openNodes.Find(n => n == con.targetNode) == null) {
                            openNodes.Add((ThreatNode)con.targetNode);
                        }
                    }
                }
                
                checkedNodes.Add(node);
                openNodes.RemoveAt(0);
            }

            checkedNodes = checkedNodes.FindAll(n => n.TimeDiffDangerLevel == searchDangerLevel);

            clusters.Add(new Cluster(checkedNodes, searchDangerLevel));
            workingSet = workingSet.Except(checkedNodes).ToList();
        }

        return clusters;
    }

    //protected override float calcHeuristicCost(Node node, Node target) {
    //    List<Connection> connections = FindStraightPath(node, target);

    //    float totalCost = 0;
    //    foreach (Connection con in connections) {
    //        ThreatNode threatNode = (ThreatNode)con.targetNode;
    //        totalCost += Mathf.Clamp(MaxTimeInSecs - threatNode.TimeForTargetToHitNode, 0, MaxTimeInSecs);
    //    }

    //    return totalCost;
    //}

    //protected override float calculateConnectionCost(Connection connection) {
    //    ThreatNode threatNode = (ThreatNode)connection.targetNode;
    //    return Mathf.Clamp(MaxTimeInSecs - threatNode.TimeForTargetToHitNode, 0, MaxTimeInSecs);
    //}

    protected override Node createNode(int x, int y, params object[] values) {
        return new ThreatNode(x, y);
    }
}
