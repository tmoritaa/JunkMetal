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

        // Reset debug information
        DebugManager.Instance.RegisterObject("maneuver_path_unobstructed_filter", new List<LookaheadNode>());
        DebugManager.Instance.RegisterObject("maneuver_dest_not_obstructed_filter", new List<LookaheadNode>());
        DebugManager.Instance.RegisterObject("maneuver_in_range_filter", new List<LookaheadNode>());
        DebugManager.Instance.RegisterObject("maneuver_does_not_cross_fire_filter", new List<LookaheadNode>());
        DebugManager.Instance.RegisterObject("maneuver_angle_from_fire_filter", new List<LookaheadNode>());
        DebugManager.Instance.RegisterObject("maneuver_aim_filter", new List<LookaheadNode>());
        DebugManager.Instance.RegisterObject("maneuver_optimal_range_filter", new List<LookaheadNode>());

        // TODO: for now, just use first weapon. Later should be closest weapon
        float angleDiff = Vector2.Angle(selfTank.transform.position - targetTank.transform.position, targetTank.Turret.GetAllWeapons()[0].CalculateFireVec());
        bool weaponFirable = targetTank.Turret.GetAllWeapons()[0].IsFireable;

        Func<List<LookaheadNode>, bool, List<LookaheadNode>>[] filterOrFindFuncs;
        Debug.Log(angleDiff);
        if (angleDiff < 5f && weaponFirable) {
            // Super dangerous. We just prioritize angle from Fire vec
            filterOrFindFuncs = new Func<List<LookaheadNode>, bool, List<LookaheadNode>>[] {
                filterByPathNotObstructed,
                filterByDestNotObstructed,
                filterByDoesNotCrossTargetFireVec,
                filterByAngleToTargetFireVec
            };
        } else if (angleDiff < 25f && weaponFirable) {
            // Not too dangerous, but still should run away while trying to aim
            filterOrFindFuncs = new Func<List<LookaheadNode>, bool, List<LookaheadNode>>[] {
                filterByPathNotObstructed,
                filterByDestNotObstructed,
                filterByDoesNotCrossTargetFireVec,
                filterByWithinRange,
                filterByAngleToTargetFireVec,
                filterByAim,
                findNodeWithBestOptimalRangeDist
            };
        } else {
            // Now we should be relatively safe
            filterOrFindFuncs = new Func<List<LookaheadNode>, bool, List<LookaheadNode>>[] {
                filterByPathNotObstructed,
                filterByDestNotObstructed,
                filterByWithinRange,
                filterByAim,
                findNodeWithBestOptimalRangeDist
            };
        }

        Vector2 requestDir = new Vector2();
        foreach (Func<List<LookaheadNode>, bool, List<LookaheadNode>> func in filterOrFindFuncs) {
            nodes = func(nodes, func == filterOrFindFuncs.Last());

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
            Debug.LogWarning("Searching for direction to go to using future prediction has resulted in too much or nothing. Should never happen");
        }

        return requestDir;
    }

    private List<LookaheadNode> filterByPathNotObstructed(List<LookaheadNode> nodes, bool lastFunc) {
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
    
    private List<LookaheadNode> filterByDestNotObstructed(List<LookaheadNode> nodes, bool lastFunc) {
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

    private List<LookaheadNode> filterByWithinRange(List<LookaheadNode> nodes, bool lastFunc) {
        if (lastFunc) {
            Debug.Log("Filter by within range is the last filter function. Should never happen");
        }

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

    private List<LookaheadNode> filterByDoesNotCrossTargetFireVec(List<LookaheadNode> nodes, bool lastFunc) {
        if (lastFunc) {
            Debug.Log("Filter by does not cross target fire vec is the last filter function. Should never happen");
        }

        Tank selfTank = controller.SelfTank;
        Tank targetTank = controller.TargetTank;
        Vector2 fireVec = targetTank.Turret.GetAllWeapons()[0].CalculateFireVec(); // TODO: for now. Should really be closest fire vec.

        List<LookaheadNode> filteredNodes = new List<LookaheadNode>();
        foreach (LookaheadNode node in nodes) {
            float angleFromCur = Vector2.SignedAngle(selfTank.transform.position - targetTank.transform.position, fireVec);
            float angleFromFuture = Vector2.SignedAngle(node.TankInfo.Pos - (Vector2)targetTank.transform.position, fireVec);

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

    private List<LookaheadNode> filterByAngleToTargetFireVec(List<LookaheadNode> nodes, bool lastFunc) {
        float largestAngle = 0;

        Tank targetTank = controller.TargetTank;
        Vector2 fireVec = targetTank.Turret.GetAllWeapons()[0].CalculateFireVec(); // TODO: for now. Should really be closest fire vec.

        LookaheadNode bestNode = null;
        foreach (LookaheadNode node in nodes) {
            float angle = Vector2.Angle(node.TankInfo.Pos - (Vector2)targetTank.transform.position, fireVec);

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
                float angle = Vector2.Angle(node.TankInfo.Pos - (Vector2)targetTank.transform.position, fireVec);

                if (largestAngle <= (angle + largestAngle / 3f)) {
                    filteredNode.Add(node);
                }
            }
        }
        
        DebugManager.Instance.RegisterObject("maneuver_angle_from_fire_filter", filteredNode);

        return filteredNode;
    }

    private List<LookaheadNode> filterByAim(List<LookaheadNode> nodes, bool lastFunc) {
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

        if (lastFunc) {
            filteredNodes.Add(bestNode);
        } else {
            foreach (LookaheadNode node in nodes) {
                Vector2 toTargetVec = (Vector2)targetTank.transform.position - node.TankInfo.Pos;

                Vector2 originFireVec = part.OwningTank.Turret.Schematic.OrigWeaponDirs[part.TurretIdx];
                float range = part.Schematic.Range;
                Vector2 curFireVec = originFireVec.Rotate(node.TankInfo.Rot); // TODO: this is assuming turret loses rotation functionality. If we keep it in the end, don't forget to adjust this.

                float angleDiff = Vector2.Angle(toTargetVec, curFireVec);

                if (angleDiff - smallestAngleDiffInRange / 2f <= smallestAngleDiffInRange) {
                    filteredNodes.Add(node);
                }
            }
        }

        if (filteredNodes.Count == 0) {
            Debug.LogWarning("Aim filter resulted in no options");
            filteredNodes = nodes;
        }
        
        DebugManager.Instance.RegisterObject("maneuver_aim_filter", filteredNodes);

        return filteredNodes;
    }

    private List<LookaheadNode> findNodeWithBestOptimalRangeDist(List<LookaheadNode> nodes, bool lastFunc) {
        if (!lastFunc) {
            Debug.LogWarning("Best optimal range dist find is not last filter function. Should never happen");
        }

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

        DebugManager.Instance.RegisterObject("maneuver_optimal_range_filter", filteredList);

        return filteredList;
    }
}
