using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class ManeuverGoal : Goal
{
    private const float acceptableSafeNodePercent = 0.2f;

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
            Debug.LogWarning("safety man");
            newTargetNode = pickNodeForSafety();
        }

        //// If there's absolutely no nodes to move to (generally happens when in a completely safe zone), then try to keep avg optimal distance
        //if (newTargetNode == null) {
        //    Debug.LogWarning("keep that dist");
        //    Vector2 diffVec = targetTank.transform.position - selfTank.transform.position;
        //    float optimalRangeDiff = diffVec.magnitude - selfTank.CalcAvgOptimalRange();

        //    diffVec = diffVec.normalized * optimalRangeDiff;
        //    Vector2 newPos = diffVec + (Vector2)selfTank.transform.position;

        //    newTargetNode = (ThreatNode)map.PositionToNode(newPos);
        //}

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

    // TODO: currently will cross over risky areas sometimes. Maybe we can make it consider that.
    private ThreatNode pickNodeForSafety() {
        // If the current target node is fine, then don't look for a new one
        if (targetNode != null && (ThreatMap.MaxTimeInSecs - targetNode.TimeForTargetToHitNodeNoReload) / ThreatMap.MaxTimeInSecs < acceptableSafeNodePercent) {
            return targetNode;
        }

        ThreatNode resultNode = null;
        
        Tank selfTank = controller.SelfTank;
        Tank targetTank = controller.TargetTank;

        ThreatMap map = controller.ThreatMap;
        ThreatNode curNode = (ThreatNode)map.PositionToNode(selfTank.transform.position);

        float curTimeThresh = curNode.TimeForTargetToHitNodeNoReload;

        List<ThreatNode> diffNodes = new List<ThreatNode>();
        
        foreach (ThreatNode node in map.NodesMarkedTankToHitNodeNoReload) {
            bool validCondition = node.NodeTraversable() && node.TimeForTargetToHitNodeNoReload > curTimeThresh;

            if (targetNode != null) {
                Vector2 targetNodePos = map.NodeToPosition(targetNode);
                Vector2 curNodePos = map.NodeToPosition(curNode);

                if ((targetNodePos - curNodePos).sqrMagnitude > controller.SqrDistForDistSigma) {
                    Vector2 curVec = targetNodePos - curNodePos;
                    Vector2 newVec = map.NodeToPosition(node) - curNodePos;

                    float angle = Vector2.Angle(curVec, newVec);
                    validCondition &= angle < 90;
                }
            }

            if (validCondition) {
                diffNodes.Add(node);
            }
        }

        // Calculate the average diff thresh among the filtered list
        float avgTimeThresh = 0;
        foreach (ThreatNode node in diffNodes) {
            avgTimeThresh += Mathf.Clamp(node.TimeForTargetToHitNode, 0, ThreatMap.MaxTimeInSecs);
        }
        avgTimeThresh /= diffNodes.Count;

        List<ThreatNode> finalFilteredList = new List<ThreatNode>();

        float step = Mathf.Max(Mathf.Abs(avgTimeThresh - curTimeThresh) / 10f, 0.05f);
        while (finalFilteredList.Count == 0 && avgTimeThresh > curTimeThresh) {
            // Get all nodes above a certain diff threshold
            foreach (ThreatNode node in diffNodes) {
                if (node.NodeTraversable() && node.GetTimeDiffForHittingTarget() > avgTimeThresh) {
                    finalFilteredList.Add(node);
                }
            }

            avgTimeThresh -= step;
        }

        // TODO: currently will not change directions if opponent rotates and self is moving towards risky threats.
        // Should account for that, while not causing swash back problem again
        float lowestCost = 999999;
        float avgOptimalRange = selfTank.CalcAvgOptimalRange();
        foreach (ThreatNode node in finalFilteredList) {
            Vector2 nodePos = map.NodeToPosition(node);

            float distFromPrevTargetNode = (targetNode != null) ? (nodePos - map.NodeToPosition(targetNode)).magnitude : 0;
            float distFromSelfToNode = (nodePos - (Vector2)selfTank.transform.position).magnitude;

            float cost = distFromPrevTargetNode + distFromSelfToNode;
            if (lowestCost > cost) {
                lowestCost = cost;
                resultNode = node;
            }
        }

        return resultNode;
    }

    private ThreatNode pickNodeForGoodVantage() {
        // If the current target node is fine, then don't look for a new one
        if (targetNode != null && (ThreatMap.MaxTimeInSecs - targetNode.GetTimeDiffForHittingTarget()) / ThreatMap.MaxTimeInSecs < acceptableSafeNodePercent) {
            return targetNode;
        }

        ThreatNode resultNode = null;

        Tank selfTank = controller.SelfTank;
        Tank targetTank = controller.TargetTank;

        ThreatMap map = controller.ThreatMap;
        ThreatNode curNode = (ThreatNode)map.PositionToNode(selfTank.transform.position);

        float curTimeDiff = Mathf.Clamp(curNode.GetTimeDiffForHittingTarget(), 0, ThreatMap.MaxTimeInSecs);
        
        List<ThreatNode> diffNodes = new List<ThreatNode>();

        // Get all nodes above a certain diff threshold
        foreach (ThreatNode node in map.NodesMarkedHitTargetFromNode) {
            float disttoTarget = (map.NodeToPosition(node) - (Vector2)targetTank.transform.position).magnitude;

            bool validCondition = node.NodeTraversable() && node.GetTimeDiffForHittingTarget() > curTimeDiff && disttoTarget < node.WeaponToHitTargetFromNode.Schematic.OptimalRange;

            if (targetNode != null) {
                Vector2 targetNodePos = map.NodeToPosition(targetNode);
                Vector2 curNodePos = map.NodeToPosition(curNode);

                if ((targetNodePos - curNodePos).sqrMagnitude > controller.SqrDistForDistSigma) {
                    Vector2 curVec = targetNodePos - curNodePos;
                    Vector2 newVec = map.NodeToPosition(node) - curNodePos;

                    float angle = Vector2.Angle(curVec, newVec);
                    validCondition &= angle < 90;
                }
            }

            if (validCondition) {
                diffNodes.Add(node);
            }
        }

        // Calculate the average diff thresh among the filtered list
        float avgTimeDiff = 0;
        foreach (ThreatNode node in diffNodes) {
            avgTimeDiff += Mathf.Clamp(node.GetTimeDiffForHittingTarget(), 0, ThreatMap.MaxTimeInSecs);
        }
        avgTimeDiff /= diffNodes.Count;

        List<ThreatNode> finalFilteredList = new List<ThreatNode>();

        float step = Mathf.Max(Mathf.Abs(avgTimeDiff - curTimeDiff) / 10f, 0.05f);
        while (finalFilteredList.Count == 0 && avgTimeDiff > curTimeDiff) {
            // Get all nodes above a certain diff threshold
            foreach (ThreatNode node in diffNodes) {
                if (node.NodeTraversable() && node.GetTimeDiffForHittingTarget() > avgTimeDiff) {
                    finalFilteredList.Add(node);
                }
            }

            avgTimeDiff -= step;
        }

        if (finalFilteredList.Count != 0) {
            float lowestCost = 999999;
            foreach (ThreatNode node in finalFilteredList) {
                Vector2 nodePos = map.NodeToPosition(node);
                float distFromPrevTargetNode = (targetNode != null) ? (nodePos - map.NodeToPosition(targetNode)).magnitude : 0;
                float distFromSelfToNode = (nodePos - (Vector2)selfTank.transform.position).magnitude;
                float distFromOptimalRange = Mathf.Abs(node.WeaponToHitTargetFromNode.Schematic.OptimalRange - (nodePos - (Vector2)targetTank.transform.position).magnitude);

                float cost = distFromPrevTargetNode + distFromSelfToNode + distFromOptimalRange;
                if (lowestCost > cost) {
                    lowestCost = cost;
                    resultNode = node;
                }
            }
        }

        return resultNode;
    }
}
