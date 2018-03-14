using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class ManeuverGoal : Goal
{
    const float UpdatePeriod = 0.25f;

    const float LookaheadTime = 1.0f;
    const float LookaheadTimeStep = 0.5f;

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

        float angleDiffSinceUpdate = Vector2.SignedAngle(forwardVecWhenUpdate, controller.SelfTank.GetForwardVec());
        Vector2 requestDir = prevMoveDir.Rotate(angleDiffSinceUpdate);
        if (elapsedTimeSinceLastUpdate > UpdatePeriod) {
            Vector2 requestedDir = pickDirToPursue();

            // TODO: if no time diff nodes, then should do different logic

            forwardVecWhenUpdate = controller.SelfTank.GetForwardVec();
            prevMoveDir = requestedDir;
            elapsedTimeSinceLastUpdate = 0;
        }

        actions.Add(new GoInDirAction(requestDir, controller));

        return actions.ToArray();
    }

    private Vector2 pickDirToPursue() {
        Tank selfTank = controller.SelfTank;
        Tank targetTank = controller.TargetTank;

        ThreatMap map = controller.ThreatMap;
        ThreatNode curNode = (ThreatNode)map.PositionToNode(selfTank.transform.position);

        LookaheadTree tree = new LookaheadTree();
        tree.PopulateTree(selfTank, map, LookaheadTime, LookaheadTimeStep, prevMoveDir);

        TankStateInfo futureTargetTank = targetTank.StateInfo;
        DebugManager.Instance.RegisterObject("maneuver_future_target_info", futureTargetTank);

        List<LookaheadNode> nodes = tree.FindAllNodesAtSearchTime(LookaheadTime);

        DebugManager.Instance.RegisterObject("maneuver_nodes_at_searchtime", nodes);

        // Reset debug information
        DebugManager.Instance.RegisterObject("maneuver_path_unobstructed_filter", new List<LookaheadNode>());
        DebugManager.Instance.RegisterObject("maneuver_dest_not_obstructed_filter", new List<LookaheadNode>());
        DebugManager.Instance.RegisterObject("maneuver_in_range_filter", new List<LookaheadNode>());
        DebugManager.Instance.RegisterObject("maneuver_does_not_cross_fire_filter", new List<LookaheadNode>());
        DebugManager.Instance.RegisterObject("maneuver_angle_from_fire_filter", new List<LookaheadNode>());
        DebugManager.Instance.RegisterObject("maneuver_aim_filter", new List<LookaheadNode>());
        DebugManager.Instance.RegisterObject("maneuver_optimal_range_filter", new List<LookaheadNode>());

        // TODO: for now, just use first weapon. Later should be closest weapon
        float angleDiff = Vector2.Angle((Vector2)selfTank.transform.position - futureTargetTank.Pos, futureTargetTank.CalculateFireVecOfWeapon(targetTank.Turret.GetAllWeapons()[0]));
        bool targetWeaponFireable = targetTank.Turret.GetAllWeapons()[0].IsFireable;
        bool inTargetRange = (futureTargetTank.Pos - (Vector2)selfTank.transform.position).magnitude < targetTank.Turret.GetAllWeapons()[0].Schematic.Range;
        bool inSelfRange = (futureTargetTank.Pos - (Vector2)selfTank.transform.position).magnitude < selfTank.Turret.GetAllWeapons()[0].Schematic.Range;

        Func<List<LookaheadNode>, TankStateInfo, bool, List<LookaheadNode>>[] filterOrFindFuncs;
        if (angleDiff < 10f && targetWeaponFireable && inTargetRange) {
            Debug.Log("super dangerous");

            // Super dangerous. We just prioritize angle from Fire vec
            filterOrFindFuncs = new Func<List<LookaheadNode>, TankStateInfo, bool, List<LookaheadNode>>[] {
                filterByPathNotObstructed,
                filterByDestNotObstructed,
                //filterByDoesNotCrossTargetFireVec,
                filterByAngleToTargetFireVec
            };
        } else if (angleDiff < 35f && targetWeaponFireable && inTargetRange) {
            Debug.Log("kind of dangerous");

            // Not too dangerous, but still should run away while trying to aim
            filterOrFindFuncs = new Func<List<LookaheadNode>, TankStateInfo, bool, List<LookaheadNode>>[] {
                filterByPathNotObstructed,
                filterByDestNotObstructed,
                filterByDoesNotCrossTargetFireVec,
                filterByAngleToTargetFireVec,
                filterByAim,
                findNodeWithBestOptimalRangeDist
            };
        } else if (inSelfRange) {
            Debug.Log("pretty safe and in range to attack");

            // Now we should be relatively safe
            filterOrFindFuncs = new Func<List<LookaheadNode>, TankStateInfo, bool, List<LookaheadNode>>[] {
                filterByPathNotObstructed,
                filterByDestNotObstructed,
                filterByAim
                //findNodeWithBestOptimalRangeDist
            };
        } else {
            Debug.Log("pretty safe but not in range to attack");

            // Now we should be relatively safe
            filterOrFindFuncs = new Func<List<LookaheadNode>, TankStateInfo, bool, List<LookaheadNode>>[] {
                filterByPathNotObstructed,
                filterByDestNotObstructed,
                findNodeWithBestOptimalRangeDist,
                filterByAim
            };
        }

        Vector2 requestDir = new Vector2();
        foreach (Func<List<LookaheadNode>, TankStateInfo, bool, List<LookaheadNode>> func in filterOrFindFuncs) {
            nodes = func(nodes, futureTargetTank, func == filterOrFindFuncs.Last());

            if (nodes.Count == 1) {
                LookaheadNode bestNode = nodes[0];

                Vector2 dir = new Vector2();
                LookaheadNode curSearchNode = bestNode;
                while (curSearchNode.ParentNode != null) {
                    dir = curSearchNode.IncomingDir;
                    curSearchNode = curSearchNode.ParentNode;
                }

                requestDir = dir;
            } else if (nodes.Count == 0) {
                Debug.LogWarning("Filter func returned list of zero. Should never happen");
            }
        }

        // Something terrible has happened
        if (nodes.Count != 1) {
            Debug.LogWarning("Searching for direction to go to using future prediction has resulted in too much or nothing. Should never happen. Count=" + nodes.Count);
        }

        return requestDir;
    }

    private List<LookaheadNode> filterByPathNotObstructed(List<LookaheadNode> nodes, TankStateInfo targetTankInfo, bool lastFunc) {
        if (lastFunc) {
            Debug.Log("Filter by path not obstructed is the last filter function. Should never happen");
        }

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
    
    private List<LookaheadNode> filterByDestNotObstructed(List<LookaheadNode> nodes, TankStateInfo targetTankInfo, bool lastFunc) {
        if (lastFunc) {
            Debug.Log("Filter by dest not obstructed is the last filter function. Should never happen");
        }

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

    private List<LookaheadNode> filterByWithinRange(List<LookaheadNode> nodes, TankStateInfo targetTankInfo, bool lastFunc) {
        if (lastFunc) {
            Debug.Log("Filter by within range is the last filter function. Should never happen");
        }

        List<LookaheadNode> filteredNode = new List<LookaheadNode>();

        Tank selfTank = controller.SelfTank;

        // TODO: later should use main weapon system. For now this is fine
        float range = selfTank.Turret.GetAllWeapons()[0].Schematic.Range;
        foreach (LookaheadNode node in nodes) {
            float distFromTarget = (node.TankInfo.Pos - targetTankInfo.Pos).magnitude;

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

    private List<LookaheadNode> filterByDoesNotCrossTargetFireVec(List<LookaheadNode> nodes, TankStateInfo targetTankInfo, bool lastFunc) {
        if (lastFunc) {
            Debug.Log("Filter by does not cross target fire vec is the last filter function. Should never happen");
        }

        Tank selfTank = controller.SelfTank;
        Tank targetTank = controller.TargetTank;

        Vector2 fireVec = targetTankInfo.CalculateFireVecOfWeapon(targetTank.Turret.GetAllWeapons()[0]); // TODO: for now. Should really be closest fire vec.

        List<LookaheadNode> filteredNodes = new List<LookaheadNode>();
        foreach (LookaheadNode node in nodes) {
            float angleFromCur = Vector2.SignedAngle((Vector2)selfTank.transform.position - targetTankInfo.Pos, fireVec);
            float angleFromFuture = Vector2.SignedAngle(node.TankInfo.Pos - targetTankInfo.Pos, fireVec);

            if (Mathf.Sign(angleFromCur) == Mathf.Sign(angleFromFuture)) {
                filteredNodes.Add(node);
            }
        }

        if (filteredNodes.Count == 0) {
            filteredNodes = nodes;
        }
        
        DebugManager.Instance.RegisterObject("maneuver_does_not_cross_fire_filter", filteredNodes);

        return filteredNodes;
    }

    private List<LookaheadNode> filterByAngleToTargetFireVec(List<LookaheadNode> nodes, TankStateInfo targetTankInfo, bool lastFunc) {
        float largestAngle = 0;

        Tank targetTank = controller.TargetTank;
        Vector2 fireVec = targetTankInfo.CalculateFireVecOfWeapon(targetTank.Turret.GetAllWeapons()[0]); // TODO: for now. Should really be closest fire vec.

        LookaheadNode bestNode = null;
        foreach (LookaheadNode node in nodes) {
            float angle = Vector2.Angle(node.TankInfo.Pos - targetTankInfo.Pos, fireVec);

            if (angle > largestAngle) {
                bestNode = node;
                largestAngle = angle;
            }
        }

        List<LookaheadNode> filteredNode = new List<LookaheadNode>();
        if (lastFunc) {
            filteredNode.Add(bestNode);
        } else {
            foreach (LookaheadNode node in nodes) {
                float angle = Vector2.Angle(node.TankInfo.Pos - targetTankInfo.Pos, fireVec);

                if (largestAngle <= (angle + largestAngle / 3f)) {
                    filteredNode.Add(node);
                }
            }
        }
        
        DebugManager.Instance.RegisterObject("maneuver_angle_from_fire_filter", filteredNode);

        return filteredNode;
    }

    private List<LookaheadNode> filterByAim(List<LookaheadNode> nodes, TankStateInfo targetTankInfo, bool lastFunc) {
        Tank selfTank = controller.SelfTank;
        Tank targetTank = controller.TargetTank;

        WeaponPart part = selfTank.Turret.GetAllWeapons()[0]; // TODO: for now. Later should use main weapon system

        List<LookaheadNode> filteredNodes = new List<LookaheadNode>();
        // Check for overlap.
        foreach (LookaheadNode node in nodes) {
            if (node.HasOverlappedTargetWithWeapon(targetTankInfo.Pos, part)) {
                filteredNodes.Add(node);
            }
        }

        float smallestAngleDiffInRange = 9999;
        LookaheadNode bestNode = null;
        foreach (LookaheadNode node in nodes) {
            Vector2 toTargetVec = targetTankInfo.Pos - node.TankInfo.Pos;

            float range = part.Schematic.Range;
            Vector2 curFireVec = node.TankInfo.CalculateFireVecOfWeapon(part);

            float angleDiff = Vector2.Angle(toTargetVec, curFireVec);

            if (angleDiff < smallestAngleDiffInRange) {
                bestNode = node;
                smallestAngleDiffInRange = angleDiff;
            }
        }

        if (lastFunc) {
            if (filteredNodes.Count == 0) {
                filteredNodes.Add(bestNode);
            } else {
                filteredNodes.RemoveRange(1, filteredNodes.Count - 1);
            }
        } else {
            foreach (LookaheadNode node in nodes) {
                Vector2 toTargetVec = targetTankInfo.Pos - node.TankInfo.Pos;

                float range = part.Schematic.Range;
                Vector2 curFireVec = node.TankInfo.CalculateFireVecOfWeapon(part);

                float angleDiff = Vector2.Angle(toTargetVec, curFireVec);

                if (angleDiff - smallestAngleDiffInRange / 2f <= smallestAngleDiffInRange) {
                    filteredNodes.Add(node);
                }
            }
        }
        
        DebugManager.Instance.RegisterObject("maneuver_aim_filter", filteredNodes);

        return filteredNodes;
    }

    private List<LookaheadNode> findNodeWithBestOptimalRangeDist(List<LookaheadNode> nodes, TankStateInfo targetTankInfo, bool lastFunc) {
        Tank selfTank = controller.SelfTank;

        WeaponPart part = selfTank.Turret.GetAllWeapons()[0]; // TODO: for now. Later should use main weapon system
        float optimalRange = part.Schematic.OptimalRange;

        LookaheadNode bestNode = null;
        float smallestOptimalRangedist = 9999;
        foreach (LookaheadNode node in nodes) {
            float distFromTarget = (node.TankInfo.Pos - targetTankInfo.Pos).magnitude;

            float optimalRangeDiff = Mathf.Abs(distFromTarget - optimalRange);

            if (optimalRangeDiff < smallestOptimalRangedist) {
                bestNode = node;
                smallestOptimalRangedist = optimalRangeDiff;
            }
        }

        List<LookaheadNode> filteredList = new List<LookaheadNode>();
        if (lastFunc) {
            filteredList.Add(bestNode);
        } else {
            foreach (LookaheadNode node in nodes) {
                float distFromTarget = (node.TankInfo.Pos - targetTankInfo.Pos).magnitude;
                float optimalRangeDiff = Mathf.Abs(distFromTarget - optimalRange);

                if (optimalRangeDiff - smallestOptimalRangedist / 3f < smallestOptimalRangedist) {
                    filteredList.Add(node);
                }
            }
        }
        
        DebugManager.Instance.RegisterObject("maneuver_optimal_range_filter", filteredList);

        return filteredList;
    }

    private TankStateInfo calcFutureTargetTankInfo() {
        Tank targetTank = controller.TargetTank;

        List<Vector2> passedPos;
        TankStateInfo targetTankInfo = AIUtility.CalcPosInFutureWithRequestedPowerChange(
            new int[] { targetTank.Hull.LeftCurPower, targetTank.Hull.RightCurPower },
            UpdatePeriod,
            targetTank.StateInfo,
            out passedPos);

        return targetTankInfo;
    }
}
