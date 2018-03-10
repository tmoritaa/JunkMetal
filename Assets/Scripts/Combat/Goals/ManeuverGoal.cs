using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class ManeuverGoal : Goal
{
    private Vector2 prevMoveDir = new Vector2();

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

        Vector2 newMoveDir = new Vector2();

        newMoveDir = pickDirToPursue(map.NodesMarkedHitTargetFromNode, (ThreatNode n) => { return n.TimeDiffDangerLevel; });

        // If no good vantage points, just move to a safer position taking into account optimal range
        //if (newTargetNode == null) {
        //    newTargetNode = pickDirToPursue(map.NodesMarkedTankToHitNodeNoReload, (ThreatNode n) => { return n.TankToNodeDangerLevel; });
        //}

        if (newMoveDir.magnitude > 0) {
            prevMoveDir = newMoveDir;   
        }

        actions.Add(new GoInDirAction(prevMoveDir, controller));
        DebugManager.Instance.RegisterObject("maneuver_move_dir", prevMoveDir);

        return actions.ToArray();
    }

    private Vector2 calcAimVec(Vector2 moveDir) {
        Tank selfTank = controller.SelfTank;
        Tank targetTank = controller.TargetTank;

        Vector2 toTargetVec = targetTank.transform.position - selfTank.transform.position;

        Vector2 fireVec = new Vector2();
        // TODO: later pick from main weapons
        foreach (WeaponPart part in selfTank.Turret.GetAllWeapons()) {
            if (!part.IsFireable) {
                continue;
            }

            fireVec = part.CalculateFireVec();
        }

        Vector2 aimDir = moveDir;
        float rotAngle = Vector2.SignedAngle(fireVec, toTargetVec);

        if (fireVec.magnitude > 0) {
            aimDir = fireVec.Rotate(90 * Mathf.Sign(rotAngle)).normalized;
        }

        return aimDir.normalized;
    }

    private Vector2 pickDirToPursue(HashSet<ThreatNode> markedNodes, Func<ThreatNode, int> getDangerLevelFunc) {
        Tank selfTank = controller.SelfTank;
        Tank targetTank = controller.TargetTank;

        ThreatMap map = controller.ThreatMap;
        ThreatNode curNode = (ThreatNode)map.PositionToNode(selfTank.transform.position);

        List<Vector2> possibleDirs = new List<Vector2> {
            selfTank.GetForwardVec(),
            selfTank.GetBackwardVec(),
            selfTank.GetForwardVec().Rotate(45f),
            selfTank.GetForwardVec().Rotate(135f),
            selfTank.GetForwardVec().Rotate(-45f),
            selfTank.GetForwardVec().Rotate(-135f),
        };

        if (curNode.TimeDiffDangerLevel == ThreatMap.MaxThreatLevel) {
            possibleDirs.Add(selfTank.GetForwardVec().Rotate(90f));
            possibleDirs.Add(selfTank.GetForwardVec().Rotate(-90f));
        }

        Func<List<Vector2>, List<Vector2>>[] filterFuncs = new Func<List<Vector2>, List<Vector2>>[] {
            filterByDodgeNecessity,
            filterByAimRequirements,
            filterBySafestClusterDir,
            filterByPrevMoveDir };

        foreach (Func<List<Vector2>, List<Vector2>> filterFunc in filterFuncs) {
            possibleDirs = filterFunc(possibleDirs);

            if (possibleDirs.Count == 1) {
                return possibleDirs[0];
            }
        }

        // If we reached this point, that means there's multiple directions still viable. In this case, just pick the first one.
        // We might have to change this logic.
        return possibleDirs[0];
    }

    private List<Vector2> filterByDodgeNecessity(List<Vector2> possibleDirs) {
        float closestDistFromFireVec = 0;
        Vector2 dodgeVec = calcDodgeVec(out closestDistFromFireVec);

        List<Vector2> filteredList = new List<Vector2>();
        if (closestDistFromFireVec < 100f) {
            foreach (Vector2 vec in possibleDirs) {
                if (Vector2.Angle(vec, dodgeVec) < 90f) {
                    filteredList.Add(vec);
                }

                // Exception case. In this case we can accept either dodge vec direction
                if (closestDistFromFireVec == 0) {
                    dodgeVec *= -1;
                    if (Vector2.Angle(vec, dodgeVec) < 90f) {
                        filteredList.Add(vec);
                    }
                }
            }
        }

        if (filteredList.Count == 0) {
            filteredList = possibleDirs;
        }

        DebugManager.Instance.RegisterObject("maneuver_dodge_filter_vecs", filteredList);

        return filteredList;
    }

    private List<Vector2> filterByAimRequirements(List<Vector2> possibleDirs) {
        Tank selfTank = controller.SelfTank;
        Tank targetTank = controller.TargetTank;

        List<Vector2> filteredList = new List<Vector2>();
        // TODO: Should do aiming for all main weapons, but for now just do it for first weapon.
        if (selfTank.Turret.GetAllWeapons()[0].CalcTimeToReloaded() < 0.5f) {
            WeaponPart aimWeapon = selfTank.Turret.GetAllWeapons()[0];
            Vector2 fireVec = aimWeapon.CalculateFireVec();

            Vector2 selfToTargetVec = targetTank.transform.position - selfTank.transform.position;

            float angle = Vector2.SignedAngle(fireVec.normalized, selfToTargetVec.normalized);
            bool cwRot = angle < 0;
            const float angleSigma = 5f;

            List<Vector2> alignedVecs = new List<Vector2>(); // TODO: only for debugging. Remove later.
            if (Mathf.Abs(angle) >= angleSigma) {
                foreach (Vector2 vec in possibleDirs) {
                    Vector2 alignedVec = vec.Rotate(Vector2.SignedAngle(selfTank.GetForwardVec(), new Vector2(0, 1))).normalized;
                    alignedVecs.Add(alignedVec);

                    float xSign = Mathf.Abs(alignedVec.x) < 0.01f ? 0 : Mathf.Sign(alignedVec.x);
                    float ySign = Mathf.Abs(alignedVec.y) < 0.01f ? 0 : Mathf.Sign(alignedVec.y);

                    bool validCondition = (cwRot && xSign != 0 && ySign != 0 && xSign == ySign || xSign > 0 && ySign == 0) || (!cwRot && xSign != 0 && ySign != 0 && xSign != ySign || xSign < 0 && ySign == 0);

                    if (validCondition) {
                        filteredList.Add(vec);
                    }
                }
            }
            DebugManager.Instance.RegisterObject("maneuver_aligned_vecs", alignedVecs);
        }

        if (filteredList.Count == 0) {
            filteredList = possibleDirs;
        }

        DebugManager.Instance.RegisterObject("maneuver_aim_filter_vecs", filteredList);

        return filteredList;
    }

    private List<Vector2> filterBySafestClusterDir(List<Vector2> possibleDirs) {
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
            return possibleDirs;
        }

        int maxDangerLevel = clusters.Max(c => c.DangerLevel);
        clusters = clusters.FindAll(c => c.DangerLevel == maxDangerLevel);

        float minDist = 99999;
        ThreatMap.Cluster minDistCluster = null;
        foreach (ThreatMap.Cluster cluster in clusters) {
            float dist = (cluster.CalcCenterPos(map) - (Vector2)selfTank.transform.position).magnitude;

            if (dist < minDist) {
                minDist = dist;
                minDistCluster = cluster;
            }
        }

        Vector2 clusterCenter = minDistCluster.CalcCenterPos(map);

        Vector2 dirToCluster = clusterCenter - (Vector2)selfTank.transform.position;

        List<Vector2> filteredList = new List<Vector2>();

        foreach (Vector2 vec in possibleDirs) {
            float angle = Vector2.Angle(vec, dirToCluster);

            if (angle < 90f) {
                filteredList.Add(vec);
            }
        }

        if (filteredList.Count == 0) {
            Debug.LogWarning("Safest cluster dist filtering resulting in no good directions");
            filteredList = possibleDirs;
        }

        DebugManager.Instance.RegisterObject("maneuver_picked_cluster", minDistCluster);
        DebugManager.Instance.RegisterObject("maneuver_cluster_filter_vecs", filteredList);

        return filteredList;
    }

    private List<Vector2> filterByPrevMoveDir(List<Vector2> possibleDirs) {
        if (prevMoveDir.magnitude == 0) {
            Debug.Log("FilterByPrevMoveDir returned given list");
            DebugManager.Instance.RegisterObject("maneuver_prev_dir_filter_vecs", possibleDirs);
            return possibleDirs;
        }

        List<Vector2> filteredList = new List<Vector2>();
        foreach (Vector2 vec in possibleDirs) {
            float angle = Vector2.Angle(vec, prevMoveDir);

            if (angle < 90f) {
                filteredList.Add(vec);
            }
        }

        if (filteredList.Count == 0) {
            filteredList = possibleDirs;
        }

        DebugManager.Instance.RegisterObject("maneuver_prev_dir_filter_vecs", filteredList);

        return filteredList;
    }
    
    private Vector2 calcDodgeVec(out float lowestDistFromFireVec) {
        Tank selfTank = controller.SelfTank;
        Tank targetTank = controller.TargetTank;

        ThreatMap map = controller.ThreatMap;
        ThreatNode curNode = (ThreatNode)map.PositionToNode(selfTank.transform.position);

        Vector2 targetToSelfVec = map.NodeToPosition(curNode) - (Vector2)targetTank.transform.position;
        lowestDistFromFireVec = 9999;
        Vector2 closestFireVec = new Vector2();
        foreach (WeaponPart part in targetTank.Turret.GetAllWeapons()) {
            Vector2 fireVec = part.CalculateFireVec();

            float angle = Vector2.Angle(fireVec, targetToSelfVec);

            if (angle < 90f) {
                // Calc dist from fire vector to self.
                Ray ray = new Ray(targetTank.transform.position, fireVec);
                float distToSelfFromFireVec = Vector3.Cross(ray.direction, selfTank.transform.position - ray.origin).magnitude;

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

    //private ThreatNode pickNodeForPursue(HashSet<ThreatNode> markedNodes, Func<ThreatNode, int> getDangerLevelFunc) {
    //    Tank selfTank = controller.SelfTank;
    //    Tank targetTank = controller.TargetTank;

    //    ThreatMap map = controller.ThreatMap;
    //    ThreatNode curNode = (ThreatNode)map.PositionToNode(selfTank.transform.position);

    //    ThreatNode resultNode = null;

    //    float distToTargetNode = (targetNode != null) ? (map.NodeToPosition(curNode) - map.NodeToPosition(targetNode)).magnitude : 0;

    //    bool findNewTargetNode = targetNode == null || getDangerLevelFunc(curNode) >= getDangerLevelFunc(targetNode) || distToTargetNode < 100f;
    //    if (findNewTargetNode) {
    //        List<ThreatNode> saferNodes = new List<ThreatNode>();
    //        foreach (ThreatNode node in markedNodes) {
    //            if (getDangerLevelFunc(node) > getDangerLevelFunc(curNode)) {
    //                saferNodes.Add(node);
    //            }
    //        }

    //        Vector2 targetToSelfVec = map.NodeToPosition(curNode) - (Vector2)targetTank.transform.position;
    //        float lowestDistFromFireVec = 9999;
    //        Vector2 closestFireVec = new Vector2();
    //        foreach (WeaponPart part in targetTank.Turret.GetAllWeapons()) {
    //            Vector2 fireVec = part.CalculateFireVec();

    //            float angle = Vector2.Angle(fireVec, targetToSelfVec);

    //            if (angle < 90f) {
    //                // Calc dist from fire vector to self.
    //                Ray ray = new Ray(targetTank.transform.position, fireVec);
    //                float distToSelfFromFireVec = Vector3.Cross(ray.direction, selfTank.transform.position - ray.origin).magnitude;

    //                if (distToSelfFromFireVec < lowestDistFromFireVec) {
    //                    closestFireVec = fireVec;
    //                    lowestDistFromFireVec = distToSelfFromFireVec;
    //                }
    //            }
    //        }

    //        Vector2 dodgeVec = closestFireVec.Perp();
    //        // Next calculate moving perpendicular from fire vec
    //        // If perp vector is pointing towards opp, flip it.
    //        if (Vector2.Angle(targetToSelfVec, dodgeVec) >= 90) {
    //            dodgeVec = -dodgeVec;
    //        }

    //        List<ThreatNode> filteredNodes = new List<ThreatNode>();

    //        Func<Node, bool>[] conditionFuncs = new Func<Node, bool>[] {
    //            (Node node) => {
    //                float distFromTarget = (map.NodeToPosition(node) - (Vector2)targetTank.transform.position).magnitude;
    //                // TODO: later, implement system where we pick a main weapon we're aiming with, and use the optimal range of that.
    //                float optRange = selfTank.CalcAvgOptimalRange();

    //                bool blocked;
    //                map.FindStraightPath(curNode, node, out blocked);

    //                bool validCondition = Mathf.Abs(distFromTarget - optRange) < 100f && !blocked;

    //                if (lowestDistFromFireVec < 100f) {
    //                    Vector2 selfToNode = map.NodeToPosition(node) - map.NodeToPosition(curNode);
    //                    float angle = Vector2.Angle(selfToNode, dodgeVec);
    //                    validCondition &= angle < 90f;
    //                }

    //                return validCondition;
    //            },
    //            (Node node) => {
    //                float distFromTarget = (map.NodeToPosition(node) - (Vector2)targetTank.transform.position).magnitude;

    //                bool blocked;
    //                map.FindStraightPath(curNode, node, out blocked);

    //                bool validCondition = !blocked;

    //                if (lowestDistFromFireVec < 100f) {
    //                    Vector2 selfToNode = map.NodeToPosition(node) - map.NodeToPosition(curNode);
    //                    float angle = Vector2.Angle(selfToNode, dodgeVec);
    //                    validCondition &= angle < 90f;
    //                }

    //                return validCondition;
    //            },
    //            (Node node) => {
    //                float distFromTarget = (map.NodeToPosition(node) - (Vector2)targetTank.transform.position).magnitude;

    //                bool blocked;
    //                map.FindStraightPath(curNode, node, out blocked);

    //                bool validCondition = !blocked;

    //                return validCondition;
    //            }
    //        };

    //        foreach (Func<Node, bool> func in conditionFuncs) {
    //            foreach (ThreatNode node in saferNodes) {
    //                if (func(node)) {
    //                    filteredNodes.Add(node);
    //                }
    //            }

    //            if (filteredNodes.Count > 0) {
    //                break;
    //            }
    //        }

    //        float lowestDistFromCurPos = 9999;
    //        ThreatNode nodeToMove = null;
    //        foreach (ThreatNode node in filteredNodes) {
    //            float dist = (map.NodeToPosition(node) - (Vector2)selfTank.transform.position).magnitude;

    //            if (dist < lowestDistFromCurPos) {
    //                nodeToMove = node;
    //                lowestDistFromCurPos = dist;
    //            }
    //        }

    //        resultNode = nodeToMove;

    //        DebugManager.Instance.RegisterObject("maneuver_nodes", filteredNodes);
    //    } else {
    //        resultNode = targetNode;
    //    }

    //    return resultNode;
    //}
}
