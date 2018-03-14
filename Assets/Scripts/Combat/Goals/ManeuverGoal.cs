using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class ManeuverGoal : Goal
{
    public struct CostInfo
    {
        public LookaheadNode Node
        {
            get; private set;
        }

        public float Cost
        {
            get; private set;
        }

        public CostInfo(LookaheadNode node, float cost) {
            Node = node;
            Cost = cost;
        }
    }

    const float UpdatePeriod = 0.1f;

    const float LookaheadTime = 0.5f;
    const float LookaheadTimeStep = 0.5f;

    private Vector2 prevMoveDir = new Vector2();

    private Vector2 forwardVecWhenUpdate = new Vector2();

    private float elapsedTimeSinceLastUpdate = 100f;

    public ManeuverGoal(AITankController _tankController) : base(_tankController) {
    }

    public override void ReInit() {
        prevMoveDir = new Vector2();
        forwardVecWhenUpdate = new Vector2();
        elapsedTimeSinceLastUpdate = 100f;
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

        LookaheadTree tree = new LookaheadTree();
        tree.PopulateTree(selfTank, map, LookaheadTime, LookaheadTimeStep, new Vector2());

        List<LookaheadNode> nodes = tree.FindAllNodesAtSearchTime(LookaheadTime);

        DebugManager.Instance.RegisterObject("maneuver_nodes_at_searchtime", nodes);
        // TODO: for now, just use first weapon. Later should be closest weapon
        Vector2 targetToSelfVec = selfTank.transform.position - targetTank.transform.position;

        float angleDiff = Vector2.Angle(targetToSelfVec, targetTank.Turret.GetAllWeapons()[0].CalculateFireVec());
        float targetWeaponReloadTime = targetTank.Turret.GetAllWeapons()[0].CalcTimeToReloaded();
        bool inTargetRange = targetToSelfVec.magnitude < targetTank.Turret.GetAllWeapons()[0].Schematic.Range * 1.1f;
        bool inSelfRange = targetToSelfVec.magnitude < selfTank.Turret.GetAllWeapons()[0].Schematic.Range;
        float selfWeaponReloadTime = selfTank.Turret.GetAllWeapons()[0].CalcTimeToReloaded();

        // First perform general filter
        nodes = filterByPathNotObstructed(nodes);
        nodes = filterByDestNotObstructed(nodes);

        Vector2 requestDir = new Vector2();
        if (angleDiff < 20f && inTargetRange && (targetWeaponReloadTime < 0.5f || selfWeaponReloadTime >= 0.5f)) {
            // We want to dodge straight up
            requestDir = calcDodgeDir(nodes);
            Debug.Log("dodge");
        } else if (angleDiff < 50f && inTargetRange && (targetWeaponReloadTime < 0.5f || selfWeaponReloadTime >= 0.5f)) {
            // We want to dodge-aim
            // Prioritize dodging, while aiming as well if dodge results are similar
            requestDir = calcDodgeAimDir(nodes);
            Debug.Log("dodge-aim");
        } else if (inSelfRange) {
            // We want to aim-dodge
            // Prioritize aiming, while dodging as well if aim results are similar
            requestDir = calcAimDodgeDir(nodes);
            Debug.Log("aim-dodge");
        } else {
            // We want to approach-aim
            // Prioritize approaching, while also aiming if aim results are similar
            requestDir = calcApproachAimDir(nodes);
            Debug.Log("approach-aim");
        }

        return requestDir;
    }

    // We just want to dodge. 
    // First filter any nodes that crossed fire vector. If they all do, then forget about it.
    // Then calc angle diff for each node.
    // Then pick nodes that result in largest angle diff. If angle diff between nodes is less than sigma, then keep those too. Note that we prioritize current direction.
    // Finally if we have multiple similar nodes, then pick node that gets us closest to optimal range
    private Vector2 calcDodgeDir(List<LookaheadNode> _nodes) {
        List<LookaheadNode> nodes = _nodes;

        Tank targetTank = controller.TargetTank;
        Vector2 targetTankPos = targetTank.transform.position;
        Vector2 fireVec = targetTank.Turret.GetAllWeapons()[0].CalculateFireVec();

        // Filter any nodes that cross fire vector.
        // NOTE: If we're up against a wall, sometimes we do want to move cross towards the fire vec. For now ignore case.
        nodes = filterByDoesNotCrossTargetFireVec(nodes);
        DebugManager.Instance.RegisterObject("maneuver_dodge_does_not_cross_fire_filter", nodes);

        // Calc angle diff for each node.
        List<CostInfo> infos = new List<CostInfo>();
        foreach (LookaheadNode node in nodes) {
            float angleDiff = Vector2.Angle(node.TankInfo.Pos - targetTankPos, fireVec);

            infos.Add(new CostInfo(node, angleDiff));
        }

        float largestAngleDiff = infos.Max(c => c.Cost);
        // Pick nodes that are within acceptable diff of largest angle diff
        List<CostInfo> filteredInfos = new List<CostInfo>();
        foreach (CostInfo info in infos) {
            if (info.Cost + largestAngleDiff / 4f > largestAngleDiff) {
                filteredInfos.Add(info);
            }
        }
        DebugManager.Instance.RegisterObject("maneuver_dodge_largest_angle_diff_filter", filteredInfos);

        float optimalRange = controller.SelfTank.Turret.GetAllWeapons()[0].Schematic.OptimalRange;
        LookaheadNode bestNode = null;
        float closestDist = 9999f;
        foreach (CostInfo info in filteredInfos) {
            float distToOptimal = Mathf.Abs(optimalRange - (info.Node.TankInfo.Pos - targetTankPos).magnitude);

            if (closestDist > distToOptimal) {
                closestDist = distToOptimal;
                bestNode = info.Node;
            }

        }
        DebugManager.Instance.RegisterObject("maneuver_dodge_best_node", bestNode);

        return bestNode.IncomingDir;
    }

    private Vector2 calcDodgeAimDir(List<LookaheadNode> nodes) {
        return new Vector2();
    }

    private Vector2 calcAimDodgeDir(List<LookaheadNode> nodes) {
        return new Vector2();
    }

    private Vector2 calcApproachAimDir(List<LookaheadNode> nodes) {
        return new Vector2();
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

    private List<LookaheadNode> filterByDoesNotCrossTargetFireVec(List<LookaheadNode> nodes) {
        Tank targetTank = controller.TargetTank;

        ThreatMap map = controller.ThreatMap;

        List<LookaheadNode> filteredNodes = new List<LookaheadNode>();
        foreach (LookaheadNode node in nodes) {
            if (!node.CrossesOppFireVector(targetTank.transform.position, targetTank.Turret.GetAllWeapons()[0], map)) {
                filteredNodes.Add(node);
            }
        }

        if (filteredNodes.Count == 0) {
            filteredNodes = nodes;
        }

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
}
