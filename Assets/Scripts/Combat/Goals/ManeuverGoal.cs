﻿using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class ManeuverGoal : Goal
{
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

        newTargetNode = pickNodeForPursue(map.NodesMarkedHitTargetFromNode, (ThreatNode n) => { return n.TimeDiffDangerous; });

        // If no good vantage points, just move to a safer position taking into account optimal range
        if (newTargetNode == null) {
            newTargetNode = pickNodeForPursue(map.NodesMarkedTankToHitNodeNoReload, (ThreatNode n) => { return n.StrictlyDangerous; });
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

        DebugManager.Instance.RegisterObject("maneuver_path", path);

        return actions.ToArray();
    }

    private ThreatNode pickNodeForPursue(HashSet<ThreatNode> markedNodes, Func<ThreatNode, bool> getDangerBoolFunc) {
        Tank selfTank = controller.SelfTank;
        Tank targetTank = controller.TargetTank;

        ThreatMap map = controller.ThreatMap;
        ThreatNode curNode = (ThreatNode)map.PositionToNode(selfTank.transform.position);

        ThreatNode resultNode = null;

        bool targetNodeNoLongerSafe = targetNode == null || (targetNode != null && getDangerBoolFunc(targetNode));
        float distToTargetNode = (targetNode != null) ? (map.NodeToPosition(curNode) - map.NodeToPosition(targetNode)).magnitude : 0;

        if (getDangerBoolFunc(curNode) && targetNodeNoLongerSafe) {
            // In this case, we want to find a closer node that goes through minimal risk
            List<CostInfo> distsFromCurNode = new List<CostInfo>();
            foreach (ThreatNode node in markedNodes) {
                float distToNode = selfTank.CalcTimeToReachPosWithNoRot(map.NodeToPosition(node));

                float distOfNodeToTarget = (map.NodeToPosition(node) - (Vector2)targetTank.transform.position).magnitude;

                bool blocked = false;
                List<Connection> cons = map.FindStraightPath(curNode, node, out blocked);

                if (!getDangerBoolFunc(node) && distOfNodeToTarget < node.WeaponToHitTargetFromNode.Schematic.Range && !blocked) {
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

            List<ThreatNode> debugDiffNodes = new List<ThreatNode>();
            riskToNode.ForEach(c => debugDiffNodes.Add(c.node));
            DebugManager.Instance.RegisterObject("maneuver_nodes", debugDiffNodes);
        } else if (!getDangerBoolFunc(curNode) && distToTargetNode < 50f) {
            // If we're already in a safe position, then find a node that is also safe but has optimal range.
            List<CostInfo> optNodes = new List<CostInfo>();
            foreach (ThreatNode node in markedNodes) {
                if (getDangerBoolFunc(node)) {
                    continue;
                }

                bool invalid = false;
                List<Connection> cons = map.FindStraightPath(curNode, node, out invalid);

                foreach (Connection con in cons) {
                    if (getDangerBoolFunc((ThreatNode)con.targetNode)) {
                        invalid = true;
                        break;
                    }
                }

                if (invalid) {
                    continue;
                }

                Vector2 toCenterOfZoneVec = map.NodeToPosition(curNode) - (Vector2)targetTank.transform.position;
                if (toCenterOfZoneVec.magnitude > node.WeaponToHitTargetFromNode.Schematic.OptimalRange) {
                    toCenterOfZoneVec *= -1;
                }

                Vector2 dirToNode = map.NodeToPosition(node) - map.NodeToPosition(curNode);
                float distToOptimalRange = Mathf.Abs(node.WeaponToHitTargetFromNode.Schematic.OptimalRange - (map.NodeToPosition(node) - (Vector2)targetTank.transform.position).magnitude);
                
                if (Vector2.Angle(dirToNode, toCenterOfZoneVec) < 90) {
                    float cost = distToOptimalRange;
                    optNodes.Add(new CostInfo(node, cost));
                }
            }

            optNodes = optNodes.OrderBy(c => c.cost).ToList();

            if (optNodes.Count > 0) {
                resultNode = optNodes[0].node;
            }

            List<ThreatNode> debugDiffNodes = new List<ThreatNode>();
            optNodes.ForEach(c => debugDiffNodes.Add(c.node));
            DebugManager.Instance.RegisterObject("maneuver_nodes", debugDiffNodes);
        } else {
            resultNode = targetNode;
        }
            
        return resultNode;
    }
}
