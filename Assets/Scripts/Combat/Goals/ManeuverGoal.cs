using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class ManeuverGoal : Goal
{
    // TODO: only for debugging. Remove once done.
    public List<ThreatNode> DebugDiffNodes = null;

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

                float cost = approxTravelDist + distFromOptimalRange ;
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
        Tank selfTank = controller.SelfTank;
        Tank targetTank = controller.TargetTank;

        ThreatMap map = controller.ThreatMap;
        ThreatNode curNode = (ThreatNode)map.PositionToNode(selfTank.transform.position);

        ThreatNode resultNode = null;

        float curTimeDiff = (targetNode != null) ? Mathf.Clamp(targetNode.GetTimeDiffForHittingTarget(), 0, ThreatMap.MaxTimeInSecs) 
            : Mathf.Clamp(curNode.GetTimeDiffForHittingTarget(), 0, ThreatMap.MaxTimeInSecs);

        curTimeDiff -= 0.025f;
        curTimeDiff = Mathf.Clamp(curTimeDiff, 0, ThreatMap.MaxTimeInSecs);

        List<ThreatNode> diffNodes = new List<ThreatNode>();

        // Get all nodes above a certain diff threshold
        foreach (ThreatNode node in map.NodesMarkedHitTargetFromNode) {
            float disttoTarget = (map.NodeToPosition(node) - (Vector2)targetTank.transform.position).magnitude;
            if (node.NodeTraversable() && node.GetTimeDiffForHittingTarget() > curTimeDiff && disttoTarget < node.WeaponToHitTargetFromNode.Schematic.Range) {
                diffNodes.Add(node);
            }
        }

        if (diffNodes.Count != 0) {
            List<CostInfo> costs = new List<CostInfo>();
            foreach (ThreatNode node in diffNodes) {
                Vector2 nodePos = map.NodeToPosition(node);

                float approxTravelDist = (targetNode != null) ? (nodePos - map.NodeToPosition(targetNode)).magnitude : (nodePos - (Vector2)selfTank.transform.position).magnitude;
                float distFromOptimalRange = Mathf.Abs(node.WeaponToHitTargetFromNode.Schematic.OptimalRange - (nodePos - (Vector2)targetTank.transform.position).magnitude);

                float cost = approxTravelDist * 0.45f + distFromOptimalRange * 0.55f;
                costs.Add(new CostInfo(node, cost));
            }

            if (costs.Count > 0) {
                float costSigma = 100f;

                costs = costs.OrderBy(c => c.cost).ToList();
                float lowestCost = costs[0].cost;

                if (targetNode != null) {
                    foreach (CostInfo costInfo in costs) {
                        float curNodeTimeDiff = costInfo.node.GetTimeDiffForHittingTarget();
                        float targetNodeTimeDiff = targetNode.GetTimeDiffForHittingTarget();
                        if (costInfo.cost < lowestCost + costSigma && curNodeTimeDiff > targetNodeTimeDiff) {
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
            float timeDiff = Mathf.Abs(targetNode.GetTimeDiffForHittingTarget() - resultNode.GetTimeDiffForHittingTarget());
            float distDiff = (map.NodeToPosition(targetNode) - map.NodeToPosition(curNode)).magnitude;
            float otherDistDiff = (map.NodeToPosition(targetNode) - map.NodeToPosition(resultNode)).magnitude;
            if (timeDiff < 0.025f && distDiff >= 100f && otherDistDiff < 100f) {
                resultNode = targetNode;
            }
        }

        DebugDiffNodes = diffNodes;
            
        return resultNode;
    }
}
