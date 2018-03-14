using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class ManeuverGoal : Goal
{
    private struct CostInfo
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

        DebugManager.Instance.RegisterObject("maneuver_move_dir", prevMoveDir.Rotate(angleDiffSinceUpdate));

        return actions.ToArray();
    }

    private Vector2 pickDirToPursue() {
        Tank selfTank = controller.SelfTank;
        Tank targetTank = controller.TargetTank;

        ThreatMap map = controller.ThreatMap;

        // TODO: for now, just use first weapon. Later should be closest weapon
        Vector2 targetToSelfVec = selfTank.transform.position - targetTank.transform.position;

        float angleDiff = Vector2.Angle(targetToSelfVec, targetTank.Turret.GetAllWeapons()[0].CalculateFireVec());
        float targetWeaponReloadTime = targetTank.Turret.GetAllWeapons()[0].CalcTimeToReloaded();
        bool inTargetRange = targetToSelfVec.magnitude < targetTank.Turret.GetAllWeapons()[0].Schematic.ThreatRange;
        bool inSelfRange = targetToSelfVec.magnitude < selfTank.Turret.GetAllWeapons()[0].Schematic.Range;
        float selfWeaponReloadTime = selfTank.Turret.GetAllWeapons()[0].CalcTimeToReloaded();

        bool shouldDodge = angleDiff < 20f && inTargetRange && (targetWeaponReloadTime < 0.5f || selfWeaponReloadTime >= 0.5f);
        bool shouldDodgeAim = !shouldDodge && angleDiff < 50f && inTargetRange && (targetWeaponReloadTime < 0.5f || selfWeaponReloadTime >= 0.5f);
        bool shouldAimOpt = !shouldDodge && !shouldDodgeAim && inSelfRange;
        bool shouldApproachAim = !shouldDodge && !shouldDodgeAim && !shouldAimOpt;

        List<float> possibleRotAngles = new List<float>() {
            0,
            180f,
            45f,
            135f,
            -45f,
            -135f
        };
        if (shouldAimOpt) {
            possibleRotAngles.Add(90f);
            possibleRotAngles.Add(-90f);
        }

        LookaheadTree tree = new LookaheadTree();
        tree.PopulateTree(selfTank, map, LookaheadTime, LookaheadTimeStep, possibleRotAngles);
        
        List<LookaheadNode> nodes = tree.FindAllNodesAtSearchTime(LookaheadTime);
        DebugManager.Instance.RegisterObject("maneuver_nodes_at_searchtime", nodes);

        // First perform general filter
        nodes = filterByPathNotObstructed(nodes);
        DebugManager.Instance.RegisterObject("maneuver_path_unobstructed_filter", nodes);

        nodes = filterByDestNotObstructed(nodes);
        DebugManager.Instance.RegisterObject("maneuver_dest_not_obstructed_filter", nodes);

        clearManeuverBehaviourDebugObjects();

        Vector2 requestDir = new Vector2();
        if (shouldDodge) {
            // We want to dodge straight up
            requestDir = calcDodgeDir(nodes);
            Debug.Log("dodge");
        } else if (shouldDodgeAim) {
            // We want to dodge-aim
            // Prioritize dodging, while aiming as well if dodge results are similar
            requestDir = calcDodgeAimDir(nodes);
            Debug.Log("dodge-aim");
        } else if (shouldAimOpt) {
            // We want to aim-dodge
            // Prioritize aiming, while approaching optimal dist as well if aim results are similar
            requestDir = calcAimOptDistDir(nodes);
            Debug.Log("aim-optdist");
        } else if (shouldApproachAim) {
            // We want to approach-aim
            // Prioritize approaching, while also aiming if aim results are similar
            requestDir = calcApproach(nodes);
            Debug.Log("approach");
        }

        return requestDir;
    }

    private void clearManeuverBehaviourDebugObjects() {
        DebugManager.Instance.RegisterObject("maneuver_dodge_largest_angle_diff_filter", null);
        DebugManager.Instance.RegisterObject("maneuver_dodge_best_node", null);
        DebugManager.Instance.RegisterObject("maneuver_dodge_aim_dangerous_filter", null);
        DebugManager.Instance.RegisterObject("maneuver_dodge_aim_angle_diff_filter", null);
        DebugManager.Instance.RegisterObject("maneuver_dodge_aim_aim_filter", null);
        DebugManager.Instance.RegisterObject("maneuver_dodge_aim_best_node", null);
        DebugManager.Instance.RegisterObject("maneuver_aim_opt_dangerous_filter", null);
        DebugManager.Instance.RegisterObject("maneuver_aim_opt_aim_filter", null);
        DebugManager.Instance.RegisterObject("maneuver_aim_opt_best_node", null);
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

        nodes = filterByTargetFireDist(nodes);
        DebugManager.Instance.RegisterObject("maneuver_dodge_largest_angle_diff_filter", nodes);

        LookaheadNode bestNode = findBestNodeWithOptimalRangeDist(nodes);
        
        if (Vector2.Angle(bestNode.IncomingDir, prevMoveDir) >= 90f) {
            bestNode = findBestNodeByTargetFireDist(nodes);
        }

        DebugManager.Instance.RegisterObject("maneuver_dodge_best_node", bestNode);

        if (bestNode == null) {
            return prevMoveDir;
        }

        return bestNode.IncomingDir;
    }

    // We still want to dodge, but also aim if possible
    // also in this state, we never want to return to the dodge state.
    private Vector2 calcDodgeAimDir(List<LookaheadNode> _nodes) {
        List<LookaheadNode> nodes = _nodes;

        // First, remove all nodes that would return us to the dodge state
        nodes = filterByDangerousNodes(nodes);
        DebugManager.Instance.RegisterObject("maneuver_dodge_aim_dangerous_filter", nodes);

        // Next find good nodes that increase angle between target fire vec.
        nodes = filterByAngleToTargetFireVec(nodes);
        DebugManager.Instance.RegisterObject("maneuver_dodge_aim_angle_diff_filter", nodes);

        // Next find good nodes that get us closer to optimal range distance 
        nodes = filterByOptimalRangeDist(nodes);
        DebugManager.Instance.RegisterObject("maneuver_dodge_aim_opt_dist_filter", nodes);

        // If still multiple exist, find good nodes for aiming
        nodes = filterbyAim(nodes);
        DebugManager.Instance.RegisterObject("maneuver_dodge_aim_aim_filter", nodes);

        // Finally, find the best node with the largest angle increase between target fire vec.
        LookaheadNode bestNode = findBestNodeWithAngleToTargetFireVec(nodes);
        DebugManager.Instance.RegisterObject("maneuver_dodge_aim_best_node", bestNode);

        return bestNode.IncomingDir;
    }

    // We now want to aim, while also trying to get to optimal range
    private Vector2 calcAimOptDistDir(List<LookaheadNode> _nodes) {
        List<LookaheadNode> nodes = _nodes;

        // First, remove all nodes that would return us to the dodge state. We still want to do this
        nodes = filterByDangerousNodes(nodes);
        DebugManager.Instance.RegisterObject("maneuver_aim_opt_dangerous_filter", nodes);

        // Next find nodes that let us aim well
        nodes = filterbyAim(nodes);
        DebugManager.Instance.RegisterObject("maneuver_aim_opt_aim_filter", nodes);

        // Finally, find the best node that results in the closest optimal range dist
        LookaheadNode bestNode = null;
        if (!IsAllNodesWithinOptimalDistSigma(nodes)) {
            bestNode = findBestNodeWithOptimalRangeDist(nodes);
        } else {
            bestNode = findBestNodeForAim(nodes);
        }

        DebugManager.Instance.RegisterObject("maneuver_aim_opt_best_node", bestNode);

        return bestNode.IncomingDir;
    }

    // For approach aim, first we filter dangerous nodes.
    // Then we filter nodes based on dist from optimal dist
    // Finally we pick node with best aim if optimal dist is not so different.
    private Vector2 calcApproach(List<LookaheadNode> _nodes) {
        List<LookaheadNode> nodes = _nodes;

        // Next we filter nodes based on dist from optimal dist
        nodes = filterByOptimalRangeDist(nodes);
        DebugManager.Instance.RegisterObject("maneuver_approach_opt_range_filter", nodes);

        Tank targetTank = controller.TargetTank;
        Vector2 targetTankPos = targetTank.transform.position;
        WeaponPart part = targetTank.Turret.GetAllWeapons()[0];
        Vector2 fireVec = part.CalculateFireVec();

        Vector2 targetToSelfVec = (Vector2)controller.SelfTank.transform.position - targetTankPos;
        float angleToFireVec = Vector2.Angle(targetToSelfVec, fireVec);

        LookaheadNode bestNode;
        if (targetToSelfVec.magnitude > part.Schematic.ThreatRange * 1.2f) {
            bestNode = findBestNodeWithOptimalRangeDist(nodes);
        } else if (angleToFireVec < 20f) {
            bestNode = findBestNodeWithAngleToTargetFireVec(nodes);
        } else {
            bestNode = findBestNodeForAim(nodes);
        }
        
        DebugManager.Instance.RegisterObject("maneuver_approach_best_node", bestNode);

        return bestNode.IncomingDir;
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

        return filteredNode;
    }

    private List<LookaheadNode> filterByDangerousNodes(List<LookaheadNode> nodes) {
        Tank targetTank = controller.TargetTank;
        Vector2 targetTankPos = targetTank.transform.position;
        Vector2 fireVec = targetTank.Turret.GetAllWeapons()[0].CalculateFireVec();
        float range = targetTank.Turret.GetAllWeapons()[0].Schematic.ThreatRange;

        List<LookaheadNode> filteredNodes = new List<LookaheadNode>();
        foreach (LookaheadNode node in nodes) {
            Vector2 diffVec = node.TankInfo.Pos - targetTankPos;
            float angleDiff = Vector2.Angle(diffVec, fireVec);

            if (!(angleDiff < 20f && range > diffVec.magnitude)) {
                filteredNodes.Add(node);
            }
        }

        if (filteredNodes.Count == 0) {
            filteredNodes = nodes;
        }

        return filteredNodes;
    }

    private List<LookaheadNode> filterByAngleToTargetFireVec(List<LookaheadNode> nodes) {
        Tank targetTank = controller.TargetTank;
        Vector2 targetTankPos = targetTank.transform.position;
        Vector2 fireVec = targetTank.Turret.GetAllWeapons()[0].CalculateFireVec();

        // Calc angle diff for each node.
        List<CostInfo> infos = new List<CostInfo>();
        foreach (LookaheadNode node in nodes) {
            float angleDiff = Vector2.Angle(node.TankInfo.Pos - targetTankPos, fireVec);

            infos.Add(new CostInfo(node, angleDiff));
        }

        float largestAngleDiff = infos.Max(c => c.Cost);
        // Pick nodes that are within acceptable diff of largest angle diff
        List<LookaheadNode> filteredNodes = new List<LookaheadNode>();
        foreach (CostInfo info in infos) {
            if (info.Cost + largestAngleDiff / 4f > largestAngleDiff) {
                filteredNodes.Add(info.Node);
            }
        }

        return filteredNodes;
    }

    private LookaheadNode findBestNodeWithAngleToTargetFireVec(List<LookaheadNode> nodes) {
        Tank targetTank = controller.TargetTank;
        Vector2 targetTankPos = targetTank.transform.position;
        Vector2 fireVec = targetTank.Turret.GetAllWeapons()[0].CalculateFireVec();

        // Calc angle diff for each node.
        LookaheadNode bestNode = null;
        float largestAngleDiff = -1;
        foreach (LookaheadNode node in nodes) {
            float angleDiff = Vector2.Angle(node.TankInfo.Pos - targetTankPos, fireVec);

            if (largestAngleDiff < angleDiff) {
                largestAngleDiff = angleDiff;
                bestNode = node;
            }
        }

        return bestNode;
    }

    private LookaheadNode findBestNodeForAim(List<LookaheadNode> nodes) {
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

        LookaheadNode bestNode = null;
        if (filteredNodes.Count > 0) {
            bestNode = filteredNodes[0];
        } else {
            float smallestAngleDiff = 9999;
            foreach (LookaheadNode node in nodes) {
                Vector2 toTargetVec = (Vector2)targetTank.transform.position - node.TankInfo.Pos;

                float range = part.Schematic.Range;
                Vector2 curFireVec = node.TankInfo.CalculateFireVecOfWeapon(part);

                float angleDiff = Vector2.Angle(toTargetVec, curFireVec);

                if (angleDiff < smallestAngleDiff) {
                    smallestAngleDiff = angleDiff;
                    bestNode = node;
                }
            }
        }

        return bestNode;
    }

    private List<LookaheadNode> filterbyAim(List<LookaheadNode> nodes) {
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

        if (filteredNodes.Count == 0) {
            List<CostInfo> infos = new List<CostInfo>();
            foreach (LookaheadNode node in nodes) {
                Vector2 toTargetVec = (Vector2)targetTank.transform.position - node.TankInfo.Pos;

                float range = part.Schematic.Range;
                Vector2 curFireVec = node.TankInfo.CalculateFireVecOfWeapon(part);

                float angleDiff = Vector2.Angle(toTargetVec, curFireVec);

                infos.Add(new CostInfo(node, angleDiff));
            }

            float smallestAngleDiffInRange = infos.Min(c => c.Cost);

            foreach (CostInfo info in infos) {
                if (info.Cost - smallestAngleDiffInRange / 4f <= smallestAngleDiffInRange) {
                    filteredNodes.Add(info.Node);
                }
            }
        }
        
        return filteredNodes;
    }
    
    private LookaheadNode findBestNodeWithOptimalRangeDist(List<LookaheadNode> nodes) {
        LookaheadNode bestNode = null;

        Vector2 targetTankPos = controller.TargetTank.transform.position;
        float optimalRange = controller.SelfTank.Turret.GetAllWeapons()[0].Schematic.OptimalRange;

        float closestDist = 9999f;
        foreach (LookaheadNode node in nodes) {
            float distToOptimal = Mathf.Abs(optimalRange - (node.TankInfo.Pos - targetTankPos).magnitude);

            if (closestDist > distToOptimal) {
                closestDist = distToOptimal;
                bestNode = node;
            }
        }

        return bestNode;
    }
    
    private List<LookaheadNode> filterByOptimalRangeDist(List<LookaheadNode> nodes) {
        Vector2 targetTankPos = controller.TargetTank.transform.position;
        float optimalRange = controller.SelfTank.Turret.GetAllWeapons()[0].Schematic.OptimalRange;

        // Calc angle diff for each node.
        List<CostInfo> infos = new List<CostInfo>();
        foreach (LookaheadNode node in nodes) {
            float distDiff = Mathf.Abs((targetTankPos - node.TankInfo.Pos).magnitude - optimalRange);

            infos.Add(new CostInfo(node, distDiff));
        }

        float smallestDistDiff = infos.Min(c => c.Cost);
        // Pick nodes that are within acceptable diff of smallest dist diff
        List<LookaheadNode> filteredNodes = new List<LookaheadNode>();
        foreach (CostInfo info in infos) {
            if (info.Cost - smallestDistDiff / 4f <= smallestDistDiff) {
                filteredNodes.Add(info.Node);
            }
        }

        return filteredNodes;
    }

    private LookaheadNode findBestNodeByTargetFireDist(List<LookaheadNode> nodes) {
        float maxDist = -1;
        LookaheadNode bestNode = null;
        foreach (LookaheadNode node in nodes) {
            float dist;
            bool success = calcClosestDistFromFireVec(node.TankInfo, out dist);

            if (success && dist > maxDist) {
                maxDist = dist;
                bestNode = node;
            }
        }

        return bestNode;
    }

    private List<LookaheadNode> filterByTargetFireDist(List<LookaheadNode> nodes) {
        // Calc angle diff for each node.
        List<CostInfo> infos = new List<CostInfo>();
        foreach (LookaheadNode node in nodes) {
            float cost = 0;
            bool success = calcClosestDistFromFireVec(node.TankInfo, out cost);

            if (success) {
                infos.Add(new CostInfo(node, cost));
            }
        }

        List<LookaheadNode> filteredNodes = new List<LookaheadNode>();
        if (infos.Count > 0) {
            float maxDist = infos.Max(c => c.Cost);

            foreach (CostInfo info in infos) {
                if (info.Cost + maxDist / 4f > maxDist) {
                    filteredNodes.Add(info.Node);
                }
            }
        } else {
            filteredNodes = nodes;
        }

        return nodes;
    }

    private bool IsAllNodesWithinOptimalDistSigma(List<LookaheadNode> nodes) {
        bool withinDistSigma = true;
        const float sigma = 50f;

        Vector2 targetTankPos = controller.TargetTank.transform.position;
        float optimalRange = controller.SelfTank.Turret.GetAllWeapons()[0].Schematic.OptimalRange;
        foreach (LookaheadNode node in nodes) {
            float distToOptimal = Mathf.Abs(optimalRange - (node.TankInfo.Pos - targetTankPos).magnitude);

            if (distToOptimal > sigma) {
                withinDistSigma = false;
                break;
            }
        }

        return withinDistSigma;
    }

    private bool calcClosestDistFromFireVec(TankStateInfo tankInfo, out float lowestDistFromFireVec) {
        Tank targetTank = controller.TargetTank;

        Vector2 targetToSelfVec = tankInfo.Pos - (Vector2)targetTank.transform.position;
        lowestDistFromFireVec = 9999;
        Vector2 closestFireVec = new Vector2();

        bool success = false;
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
                    success = true;
                }
            }
        }

        return success;
    }
}
