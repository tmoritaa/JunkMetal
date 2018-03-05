﻿using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class ManeuverGoal : Goal
{
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

        // Get all nodes with a diff above a certain threshold
        float diffThresh = Mathf.Clamp(curNode.GetTimeDiffForHittingTarget(), 0, ThreatMap.MaxTimeInSecs);

        List<ThreatNode> diffNodes = new List<ThreatNode>();

        foreach (ThreatNode node in map.NodesMarkedHitTargetFromNode) {
            if (node.NodeTraversable() && node.GetTimeDiffForHittingTarget() > diffThresh) {
                diffNodes.Add(node);
            }
        }

        ThreatNode newTargetNode = null;
        if (diffNodes.Count != 0) {
            float lowestCost = 999999;
            foreach (ThreatNode node in diffNodes) {
                Vector2 nodePos = map.NodeToPosition(node);
                float diffDistToOptimalRange = Mathf.Abs(node.WeaponToHitTargetFromNode.Schematic.OptimalRange - (nodePos - (Vector2)targetTank.transform.position).magnitude);
                float distFromSelfToNode = (nodePos - (Vector2)selfTank.transform.position).magnitude;

                float cost = diffDistToOptimalRange + distFromSelfToNode;
                if (lowestCost > cost) {
                    lowestCost = cost;
                    newTargetNode = node;
                }
            }
        } else {
            // If no diff nodes (occurs when no weapons to fire), just move to a safer space
            diffThresh = curNode.TimeForTargetToHitNodeNoReload;

            diffNodes.Clear();
            foreach (ThreatNode node in map.NodesMarkedTankToHitNodeNoReload) {
                if (node.NodeTraversable() && node.TimeForTargetToHitNodeNoReload > diffThresh) {
                    diffNodes.Add(node);
                }
            }

            float lowestCost = 999999;
            float avgOptimalRange = selfTank.CalcAvgOptimalRange();
            foreach (ThreatNode node in diffNodes) {
                Vector2 nodePos = map.NodeToPosition(node);
                float diffDistToOptimalRange = Mathf.Abs(avgOptimalRange - (nodePos - (Vector2)targetTank.transform.position).magnitude);
                float distFromSelfToNode = (nodePos - (Vector2)selfTank.transform.position).magnitude;

                float cost = diffDistToOptimalRange + distFromSelfToNode;
                if (lowestCost > cost) {
                    lowestCost = cost;
                    newTargetNode = node;
                }
            }
        }

        // If there's absolutely no nodes to move to (generally happens when in a completely safe zone), then try to keep avg optimal distance
        if (newTargetNode == null) {
            Vector2 diffVec = targetTank.transform.position - selfTank.transform.position;
            float optimalRangeDiff = diffVec.magnitude - selfTank.CalcAvgOptimalRange();

            diffVec = diffVec.normalized * optimalRangeDiff;
            Vector2 newPos = diffVec + (Vector2)selfTank.transform.position;

            newTargetNode = (ThreatNode)map.PositionToNode(newPos);
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
}