using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class ManeuverGoal : Goal
{
    const float UpdatePeriod = 0.25f;

    private Vector2 prevMoveDir = new Vector2();

    private Vector2 forwardVecWhenUpdate = new Vector2();

    private float elapsedTimeSinceLastUpdate = 100f;

    public ManeuverGoal(AITankController _tankController) : base(_tankController) {
    }

    public override void ReInit() {
        // Do nothing.
    }

    public override void UpdateInsistence() {
        // For now, though since AttackGoal will set insistence above 50 when it can attack, maybe this is fine.
        Insistence = 50;
    }

    public override AIAction[] CalcActionsToPerform() {
        List<AIAction> actions = new List<AIAction>();

        ThreatMap map = controller.ThreatMap;

        elapsedTimeSinceLastUpdate += Time.deltaTime;

        if (elapsedTimeSinceLastUpdate > UpdatePeriod) {
            Vector2 requestedDir = pickDirToPursue();

            // TODO: if no time diff nodes, then should do different logic

            forwardVecWhenUpdate = controller.SelfTank.GetForwardVec();
            prevMoveDir = requestedDir;
            elapsedTimeSinceLastUpdate = 0;
        }

        float angleDiffSinceUpdate = Vector2.SignedAngle(forwardVecWhenUpdate, controller.SelfTank.GetForwardVec());

        actions.Add(new GoInDirAction(prevMoveDir.Rotate(angleDiffSinceUpdate), controller));

        return actions.ToArray();
    }

    private Vector2 pickDirToPursue() {
        Tank selfTank = controller.SelfTank;
        Tank targetTank = controller.TargetTank;

        ThreatMap map = controller.ThreatMap;
        ThreatNode curNode = (ThreatNode)map.PositionToNode(selfTank.transform.position);

        const float lookaheadTime = 1.0f;
        const float timeStep = 0.5f;

        LookaheadTree tree = new LookaheadTree();
        tree.PopulateTree(selfTank, map, lookaheadTime, timeStep);

        List<LookaheadNode> nodes = tree.FindAllNodesAtSearchTime(lookaheadTime);

        DebugManager.Instance.RegisterObject("maneuver_nodes_at_searchtime", nodes);

        nodes = filterByPathNotObstructed(nodes);
        nodes = filterByDestNotObstructed(nodes);
        nodes = filterByDistToFireVec(nodes);
        nodes = filterByWithinRange(nodes);
        nodes = filterByTraverseDangerousNodes(nodes);
        
        Func<List<LookaheadNode>, List<LookaheadNode>>[] filterOrFindFuncs = new Func<List<LookaheadNode>, List<LookaheadNode>>[]
        {
            filterByClusterDist,
            filterByAim,
            findNodeWithBestOptimalRangeDist
        };

        Vector2 requestDir = new Vector2();
        foreach (Func<List<LookaheadNode>, List<LookaheadNode>> func in filterOrFindFuncs) {
            nodes = func(nodes);

            if (nodes.Count == 1) {
                LookaheadNode bestNode = nodes[0];

                Vector2 dir = new Vector2();
                LookaheadNode curSearchNode = bestNode;
                while (curSearchNode.ParentNode != null) {
                    dir = curSearchNode.IncomingDir;
                    curSearchNode = curSearchNode.ParentNode;
                }

                requestDir = dir;
            }
        }

        // Something terrible has happened
        if (nodes.Count != 1) {
            Debug.LogWarning("Searching for direction to go to using future prediction has resulted in too much or nothing. Should never happen");
        }

        return requestDir;
    }

    private List<LookaheadNode> filterByPathNotObstructed(List<LookaheadNode> nodes) {
        List<LookaheadNode> filteredNode = new List<LookaheadNode>();

        foreach (LookaheadNode node in nodes) {
            if (node.PathNotObstructed()) {
                filteredNode.Add(node);
            }
        }

        if (filteredNode.Count == 0) {
            filteredNode = nodes;
        }

        DebugManager.Instance.RegisterObject("maneuver_path_unobstructed_filter", filteredNode);

        return filteredNode;
    }

    private List<LookaheadNode> filterByDestNotObstructed(List<LookaheadNode> nodes) {
        List<LookaheadNode> filteredNode = new List<LookaheadNode>();
        ThreatMap map = controller.ThreatMap;

        foreach (LookaheadNode node in nodes) {
            if (map.PositionToNode(node.TankInfo.Pos).NodeTraversable()) {
                filteredNode.Add(node);
            }
        }

        if (filteredNode.Count == 0) {
            filteredNode = nodes;
        }

        DebugManager.Instance.RegisterObject("maneuver_dest_not_obstructed_filter", filteredNode);

        return filteredNode;
    }

    private List<LookaheadNode> filterByTraverseDangerousNodes(List<LookaheadNode> nodes) {
        List<LookaheadNode> filteredNode = new List<LookaheadNode>();

        foreach (LookaheadNode node in nodes) {
            if (node.PathFromRootDoesNotCrossDangerNode()) {
                filteredNode.Add(node);
            }
        }

        if (filteredNode.Count == 0) {
            filteredNode = nodes;
        }

        DebugManager.Instance.RegisterObject("maneuver_dangerous_node_filter", filteredNode);

        return filteredNode;
    }

    private List<LookaheadNode> filterByDistToFireVec(List<LookaheadNode> nodes) {
        List<LookaheadNode> filteredNode = new List<LookaheadNode>();

        const float minDistFromFireVec = 100f;
        foreach (LookaheadNode node in nodes) {
            float lowestDist = 0;
            calcDodgeVec(node.TankInfo, out lowestDist);

            if (lowestDist > minDistFromFireVec) {
                filteredNode.Add(node);
            }
        }

        if (filteredNode.Count == 0) {
            filteredNode = nodes;
        }

        DebugManager.Instance.RegisterObject("maneuver_dist_from_fire_filter", filteredNode);

        return filteredNode;
    }

    private List<LookaheadNode> filterByWithinRange(List<LookaheadNode> nodes) {
        List<LookaheadNode> filteredNode = new List<LookaheadNode>();

        Tank selfTank = controller.SelfTank;
        Tank targetTank = controller.TargetTank;

        // TODO: later should use main weapon system. For now this is fine
        float range = selfTank.Turret.GetAllWeapons()[0].Schematic.Range;
        foreach (LookaheadNode node in nodes) {
            float distFromTarget = (node.TankInfo.Pos - (Vector2)targetTank.transform.position).magnitude;

            if (distFromTarget < range) {
                filteredNode.Add(node);
            }
        }

        if (filteredNode.Count == 0) {
            filteredNode = nodes;
        }

        DebugManager.Instance.RegisterObject("maneuver_in_range_filter", filteredNode);

        return filteredNode;
    }

    private List<LookaheadNode> filterByAim(List<LookaheadNode> nodes) {
        Tank selfTank = controller.SelfTank;
        Tank targetTank = controller.TargetTank;

        WeaponPart part = selfTank.Turret.GetAllWeapons()[0]; // TODO: for now. Later should use main weapon system

        List<LookaheadNode> filteredNodes = new List<LookaheadNode>();
        // Check for overlap.
        foreach (LookaheadNode node in nodes) {
            if (node.HasOverlappedTargetWithWeapon(targetTank.transform.position, part)) {
                filteredNodes.Add(node);
            }
        }

        // If no overlap, check for closest within sigma
        if (filteredNodes.Count == 0) {
            float smallestAngleDiffInRange = 9999;
            LookaheadNode bestNode = null;
            foreach (LookaheadNode node in nodes) {
                Vector2 toTargetVec = (Vector2)targetTank.transform.position - node.TankInfo.Pos;

                Vector2 originFireVec = part.OwningTank.Turret.Schematic.OrigWeaponDirs[part.TurretIdx];
                float range = part.Schematic.Range;
                Vector2 curFireVec = originFireVec.Rotate(node.TankInfo.Rot); // TODO: this is assuming turret loses rotation functionality. If we keep it in the end, don't forget to adjust this.

                float angleDiff = Vector2.Angle(toTargetVec, curFireVec);

                if (angleDiff < smallestAngleDiffInRange) {
                    bestNode = node;
                    smallestAngleDiffInRange = angleDiff;
                }
            }

            if (bestNode != null) {
                filteredNodes.Add(bestNode);
            }
        }

        if (filteredNodes.Count == 0) {
            Debug.LogWarning("Aim filter resulted in no options");
            filteredNodes = nodes;
        }

        DebugManager.Instance.RegisterObject("maneuver_aim_filter", filteredNodes);

        return filteredNodes;
    }

    private List<LookaheadNode> filterByClusterDist(List<LookaheadNode> nodes) {
        Tank selfTank = controller.SelfTank;
        Tank targetTank = controller.TargetTank;

        ThreatMap map = controller.ThreatMap;

        // TODO: later should use main weapon system. For now this is fine
        float range = selfTank.Turret.GetAllWeapons()[0].Schematic.Range;

        List<ThreatNode> nodesInRange = new List<ThreatNode>();

        foreach (ThreatNode node in map.NodesMarkedHitTargetFromNode) {
            float dist = ((Vector2)targetTank.transform.position - map.NodeToPosition(node)).magnitude;

            if (dist < range) {
                nodesInRange.Add(node);
            }
        }

        List<ThreatMap.Cluster> clusters = map.FindClusters(nodesInRange, (ThreatNode n) => { return n.GetTimeDiffForHittingTarget(); });

        clusters = clusters.FindAll(c => c.DangerLevel > 1);

        if (clusters.Count == 0 || (clusters.Count == 1 && clusters[0].DangerLevel == 0)) {
            Debug.LogWarning("No clusters found or all clusters are dangerous. Should never happen.");
            return nodes;
        }

        float closestNodeDist = 9999;
        LookaheadNode closestNode = null;
        foreach (LookaheadNode node in nodes) {
            foreach (ThreatMap.Cluster cluster in clusters) {
                float dist = (cluster.CalcCenterPos(map) - node.TankInfo.Pos).magnitude;

                if (dist < closestNodeDist) {
                    closestNodeDist = dist;
                    closestNode = node;
                }
            }
        }

        HashSet<LookaheadNode> nodesWithinRange = new HashSet<LookaheadNode>();
        foreach (LookaheadNode node in nodes) {
            foreach (ThreatMap.Cluster cluster in clusters) {
                float dist = (cluster.CalcCenterPos(map) - node.TankInfo.Pos).magnitude;

                if (dist < closestNodeDist + 150f) {
                    nodesWithinRange.Add(node);
                }
            }
        }

        // If there's no nodes, then we just want to use the closest node
        if (nodesWithinRange.Count == 0) {
            if (closestNode == null) {
                Debug.LogWarning("Optimal range filter resulted in no closest node. Should never happen");
            }
            nodesWithinRange = new HashSet<LookaheadNode>() { closestNode };
        }

        DebugManager.Instance.RegisterObject("maneuver_cluster_filter", nodesWithinRange.ToList());

        return nodesWithinRange.ToList();
    }

    private List<LookaheadNode> findNodeWithBestOptimalRangeDist(List<LookaheadNode> nodes) {
        Tank selfTank = controller.SelfTank;
        Tank targetTank = controller.TargetTank;

        WeaponPart part = selfTank.Turret.GetAllWeapons()[0]; // TODO: for now. Later should use main weapon system
        float optimalRange = part.Schematic.OptimalRange;

        LookaheadNode bestNode = null;
        float smallestOptimalRangedist = 9999;
        foreach (LookaheadNode node in nodes) {
            float distFromTarget = (node.TankInfo.Pos - (Vector2)targetTank.transform.position).magnitude;

            float optimalRangeDiff = Mathf.Abs(distFromTarget - optimalRange);

            if (optimalRangeDiff < smallestOptimalRangedist) {
                bestNode = node;
                smallestOptimalRangedist = optimalRangeDiff;
            }
        }

        List<LookaheadNode> filteredList = new List<LookaheadNode>() { bestNode };
        DebugManager.Instance.RegisterObject("maneuver_optimal_range_filter", filteredList.ToList());

        return filteredList;
    }

    private Vector2 calcDodgeVec(TankStateInfo tankInfo, out float lowestDistFromFireVec) {
        Tank targetTank = controller.TargetTank;

        ThreatMap map = controller.ThreatMap;
        ThreatNode curNode = (ThreatNode)map.PositionToNode(tankInfo.Pos);

        Vector2 targetToSelfVec = map.NodeToPosition(curNode) - (Vector2)targetTank.transform.position;
        lowestDistFromFireVec = 9999;
        Vector2 closestFireVec = new Vector2();
        foreach (WeaponPart part in targetTank.Turret.GetAllWeapons()) {
            Vector2 fireVec = part.CalculateFireVec();

            float angle = Vector2.Angle(fireVec, targetToSelfVec);

            if (angle < 90f) {
                // Calc dist from fire vector to self.
                Ray ray = new Ray(targetTank.transform.position, fireVec);
                float distToSelfFromFireVec = Vector3.Cross(ray.direction, (Vector3)tankInfo.Pos - ray.origin).magnitude;

                if (distToSelfFromFireVec < lowestDistFromFireVec) {
                    closestFireVec = fireVec;
                    lowestDistFromFireVec = distToSelfFromFireVec;
                }
            }
        }

        Vector2 dodgeVec = closestFireVec.Perp();
        // Next calculate moving perpendicular from fire vec
        // If perp vector is pointing towards opp, flip it.
        if (Vector2.Angle(targetToSelfVec, dodgeVec) >= 90) {
            dodgeVec = -dodgeVec;
        }

        return dodgeVec;
    }
}
