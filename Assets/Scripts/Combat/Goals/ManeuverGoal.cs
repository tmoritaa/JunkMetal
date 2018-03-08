using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class ManeuverGoal : Goal
{
    // TODO: only for debugging. Remove once done.
    public List<ThreatNode> DebugDiffNodes = null;

    public Vector2 CenterOfZone = new Vector2();

    private struct CostInfo
    {
        public ThreatNode node;
        public float cost;

        public CostInfo(ThreatNode n, float _cost) {
            node = n;
            cost = _cost;
        }
    }

    private const float acceptableSafeNodePercent = 0.1f;

    private ThreatNode targetNode = null;
    private List<Node> path = new List<Node>();
    // TODO: for debugging only.
    public List<Node> Path
    {
        get {
            return path;
        }
    }

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

        Tank targetTank = controller.TargetTank;
        Tank selfTank = controller.SelfTank;

        ThreatMap map = controller.ThreatMap;
        ThreatNode curNode = (ThreatNode)map.PositionToNode(selfTank.transform.position);

        ThreatNode newTargetNode = null;

        newTargetNode = pickNodeForGoodVantage();

        // If no good vantage points, just move to a safer position taking into account optimal range
        if (newTargetNode == null) {
            newTargetNode = pickNodeForSafety();
        }

        Vector2 diffDir = (targetTank.transform.position - selfTank.transform.position).normalized;
        bool shouldFollowPath = true;
        if (newTargetNode != null && targetNode != newTargetNode) {
            targetNode = newTargetNode;
            
            path = map.FindPath(selfTank.transform.position, map.NodeToPosition(targetNode));
            AIUtility.SmoothPath(path, selfTank);

            if (targetNode.WeaponToHitTargetFromNode != null) {
                actions.Add(new AimWithWeaponAction(diffDir, targetNode.WeaponToHitTargetFromNode, controller));
            } else {
                actions.Add(new AimAction(diffDir, controller));
            }
        } else if (targetNode == null) {
            Debug.LogWarning("Closest safe node not found. Default to just approaching opponent");
            actions.Add(new GoInDirAction(diffDir, controller));
            actions.Add(new AimAction(diffDir, controller));
            shouldFollowPath = false;
        }

        if (shouldFollowPath) {
            Vector2 curPos = selfTank.transform.position;
            float sqrDistSigma = controller.SqrDistForDistSigma;
            if (path.Count > 0 && (curPos - map.NodeToPosition(path[0])).sqrMagnitude < sqrDistSigma) {
                path.RemoveAt(0);
                AIUtility.SmoothPath(path, controller.SelfTank);
            }

            Vector2 target = (path.Count > 0) ? map.NodeToPosition(path[0]) : map.NodeToPosition(targetNode);

            Vector2 dir = (target - curPos).normalized;
            actions.Add(new GoInDirAction(dir, controller));
        }

        return actions.ToArray();
    }

    private ThreatNode pickNodeForSafety() {
        Tank selfTank = controller.SelfTank;
        Tank targetTank = controller.TargetTank;

        ThreatMap map = controller.ThreatMap;
        ThreatNode curNode = (ThreatNode)map.PositionToNode(selfTank.transform.position);

        ThreatNode resultNode = null;

        float curTimeDiff = (targetNode != null) ? Mathf.Clamp(targetNode.TimeForTargetToHitNodeNoReload, 0, ThreatMap.MaxTimeInSecs)
            : Mathf.Clamp(curNode.TimeForTargetToHitNodeNoReload, 0, ThreatMap.MaxTimeInSecs);

        curTimeDiff -= 0.025f;
        curTimeDiff = Mathf.Clamp(curTimeDiff, 0, ThreatMap.MaxTimeInSecs);

        List<ThreatNode> diffNodes = new List<ThreatNode>();

        // Get all nodes above a certain diff threshold
        foreach (ThreatNode node in map.NodesMarkedHitTargetFromNode) {
            if (node.NodeTraversable() && node.TimeForTargetToHitNodeNoReload > curTimeDiff) {
                diffNodes.Add(node);
            }
        }

        if (diffNodes.Count != 0) {
            List<CostInfo> costs = new List<CostInfo>();
            foreach (ThreatNode node in diffNodes) {
                Vector2 nodePos = map.NodeToPosition(node);

                float approxTravelDist = (targetNode != null) ? (nodePos - map.NodeToPosition(targetNode)).magnitude : (nodePos - (Vector2)selfTank.transform.position).magnitude;
                float distFromOptimalRange = Mathf.Abs(selfTank.CalcAvgOptimalRange() - (nodePos - (Vector2)targetTank.transform.position).magnitude);

                float cost = approxTravelDist + distFromOptimalRange;
                costs.Add(new CostInfo(node, cost));
            }

            if (costs.Count > 0) {
                float costSigma = 50f;

                float lowestCost = costs[0].cost;
                costs.OrderBy(c => c.cost);

                if (targetNode != null) {
                    foreach (CostInfo costInfo in costs) {
                        if (costInfo.cost < lowestCost + costSigma && costInfo.node.TimeForTargetToHitNodeNoReload > targetNode.TimeForTargetToHitNodeNoReload) {
                            resultNode = costInfo.node;
                            break;
                        }
                    }
                }

                if (resultNode == null) {
                    resultNode = costs[0].node;
                }
            }
        }

        if (targetNode != null && resultNode != null) {
            float timeDiff = Mathf.Abs(targetNode.TimeForTargetToHitNodeNoReload - resultNode.TimeForTargetToHitNodeNoReload);
            float distDiff = (map.NodeToPosition(targetNode) - map.NodeToPosition(curNode)).magnitude;
            if (timeDiff < 0.025f && distDiff >= 100f) {
                resultNode = targetNode;
            }
        }

        DebugDiffNodes = diffNodes;

        return resultNode;
    }

    private ThreatNode pickNodeForGoodVantage() {
        CenterOfZone = new Vector2();

        Tank selfTank = controller.SelfTank;
        Tank targetTank = controller.TargetTank;

        ThreatMap map = controller.ThreatMap;
        ThreatNode curNode = (ThreatNode)map.PositionToNode(selfTank.transform.position);

        ThreatNode resultNode = null;

        if (targetNode != null && targetNode.TimeDiffDangerous) {
            targetNode = null;
        }

        if (curNode.TimeDiffDangerous && targetNode == null) {
            Debug.Log("On dangerous. Find safe node");

            // In this case, we want to find a closer node that goes through minimal risk
            List<CostInfo> distsFromCurNode = new List<CostInfo>();
            foreach (ThreatNode node in map.NodesMarkedHitTargetFromNode) {
                float distToNode = selfTank.CalcTimeToReachPosWithNoRot(map.NodeToPosition(node));
                float distOfNodeToTarget = (map.NodeToPosition(node) - (Vector2)targetTank.transform.position).magnitude;

                bool blocked = false;
                List<Connection> cons = map.FindStraightPath(curNode, node, out blocked);

                if (!node.TimeDiffDangerous && distOfNodeToTarget < node.WeaponToHitTargetFromNode.Schematic.Range && !blocked) {
                    distsFromCurNode.Add(new CostInfo(node, distToNode));
                }
            }

            distsFromCurNode = distsFromCurNode.OrderBy(c => c.cost).ToList();
            const float FilterNum = 10;
            if (distsFromCurNode.Count > FilterNum) {
                distsFromCurNode.RemoveRange(10, distsFromCurNode.Count - 10);
            }

            List<CostInfo> riskToNode = new List<CostInfo>();
            foreach (CostInfo info in distsFromCurNode) {
                ThreatNode node = info.node;

                bool blocked = false;
                List<Connection> cons = map.FindStraightPath(curNode, node, out blocked);

                float cost = 0;
                foreach (Connection con in cons) {
                    cost += Mathf.Clamp(ThreatMap.MaxTimeInSecs - ((ThreatNode)con.targetNode).TimeForTargetToHitNode, 0, ThreatMap.MaxTimeInSecs);
                }

                riskToNode.Add(new CostInfo(node, cost));
            }

            riskToNode = riskToNode.OrderBy(c => c.cost).ToList();

            if (riskToNode.Count > 0) {
                resultNode = riskToNode[0].node;
            }

            // TODO: only for debugging
            DebugDiffNodes = new List<ThreatNode>();
            riskToNode.ForEach(c => DebugDiffNodes.Add(c.node));
        } else if (!curNode.TimeDiffDangerous) {
            Debug.Log("On safe. Find safer node");

            // If we're already in a safe position, then find a node that is also safe but has optimal range.
            Vector2 centerOfZone = map.FindCenterOfZone(curNode);
            CenterOfZone = centerOfZone;

            List<CostInfo> optNodes = new List<CostInfo>();
            foreach (ThreatNode node in map.NodesMarkedHitTargetFromNode) {
                if (node.TimeDiffDangerous || node == curNode) {
                    continue;
                }

                bool invalid = false;
                List<Connection> cons = map.FindStraightPath(curNode, node, out invalid);

                foreach (Connection con in cons) {
                    if (((ThreatNode)con.targetNode).TimeDiffDangerous) {
                        invalid = true;
                        break;
                    }
                }

                if (invalid) {
                    continue;
                }

                Vector2 toCenterOfZoneVec = centerOfZone - map.NodeToPosition(curNode);

                Vector2 dirToNode = map.NodeToPosition(node) - map.NodeToPosition(curNode);
                float distToNode = dirToNode.magnitude;
                float distToOptimalRange = Mathf.Abs(node.WeaponToHitTargetFromNode.Schematic.OptimalRange - (map.NodeToPosition(curNode) - (Vector2)targetTank.transform.position).magnitude);

                if (Vector2.Angle(dirToNode, toCenterOfZoneVec) < 90) {
                    float cost = distToNode + distToOptimalRange;
                    optNodes.Add(new CostInfo(node, cost));
                }
            }

            optNodes = optNodes.OrderBy(c => c.cost).ToList();

            if (optNodes.Count > 0) {
                resultNode = optNodes[0].node;
            }            

            DebugDiffNodes = new List<ThreatNode>();
            optNodes.ForEach(c => DebugDiffNodes.Add(c.node));
        } else {
            Debug.Log("Dangerous but have target node");

            resultNode = targetNode;
        }
            
        return resultNode;
    }
}
