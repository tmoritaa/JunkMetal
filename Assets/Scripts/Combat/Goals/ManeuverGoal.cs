using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class ManeuverGoal : Goal
{
    public class CostInfo
    {
        public LookaheadNode Node
        {
            get; private set;
        }

        public int Cost
        {
            get; private set;
        }

        public CostInfo(LookaheadNode node, int cost) {
            Node = node;
            Cost = cost;
        }
    }

    public enum Behaviour
    {
        Runaway,
        GoingforIt,
        Notset,
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

        Map map = controller.Map;

        elapsedTimeSinceLastUpdate += Time.deltaTime;

        if (elapsedTimeSinceLastUpdate >= UpdatePeriod) {
            Vector2 requestedDir = pickDirToPursue();

            forwardVecWhenUpdate = controller.SelfTank.GetForwardVec();
            prevMoveDir = requestedDir;
            elapsedTimeSinceLastUpdate = 0;
        }

        float angleDiffSinceUpdate = Vector2.SignedAngle(forwardVecWhenUpdate, controller.SelfTank.GetForwardVec());
        actions.Add(new GoInDirAction(prevMoveDir/*.Rotate(angleDiffSinceUpdate)*/, controller));

        CombatDebugHandler.Instance.RegisterObject("maneuver_move_dir", prevMoveDir/*.Rotate(angleDiffSinceUpdate)*/);

        return actions.ToArray();
    }

    private Vector2 pickDirToPursue() {
        Tank selfTank = controller.SelfTank;
        Tank targetTank = controller.TargetTank;

        Map map = controller.Map;

        WeaponPart targetWeapon;
        WeaponPart selfWeapon;
        int timeForTargetToHitSelf = calcMinTimeForAimerToHitAimee(targetTank.StateInfo, selfTank.StateInfo, targetTank.Hull.GetAllWeapons(), out targetWeapon);
        int timeForSelfToHitTarget = calcMinTimeForAimerToHitAimee(selfTank.StateInfo, targetTank.StateInfo, selfTank.Hull.GetAllWeapons(), out selfWeapon);

        int weaponDmgDiff = selfWeapon.Schematic.Damage - targetWeapon.Schematic.Damage;
        float stepRatio = weaponDmgDiff / 10f;

        int thresh = 50 + Mathf.RoundToInt(stepRatio * 25);
        if (weaponDmgDiff > 0) {
            thresh = Math.Min(thresh, 100);
        } else {
            thresh = Math.Max(thresh, -100);
        }

        int diff = timeForSelfToHitTarget - timeForTargetToHitSelf;
        
        bool runaway = diff > thresh && timeForTargetToHitSelf < 150;

        string debugOutput = "diff=" + diff + " thresh=" + thresh + " runningAway?=" + runaway.ToString();
        Debug.Log(debugOutput);

        List<float> possibleRotAngles = new List<float>() {
                0,
                180f,
                45f,
                135f,
                -45f,
                -135f,
                90f,
                -90f
            };

        clearManeuverBehaviourDebugObjects();

        LookaheadTree tree = new LookaheadTree();
        tree.PopulateTree(selfTank, map, LookaheadTime, LookaheadTimeStep, possibleRotAngles);

        List<LookaheadNode> possibleNodes = tree.FindAllNodesAtSearchTime(LookaheadTime);
        CombatDebugHandler.Instance.RegisterObject("maneuver_nodes_at_searchtime", possibleNodes);

        // First perform general filter
        possibleNodes = filterByPathNotObstructed(possibleNodes);
        CombatDebugHandler.Instance.RegisterObject("maneuver_path_unobstructed_filter", possibleNodes);

        possibleNodes = filterByDestNotObstructed(possibleNodes);
        CombatDebugHandler.Instance.RegisterObject("maneuver_dest_not_obstructed_filter", possibleNodes);

        //possibleNodes = filterByTooCloseToTarget(possibleNodes);
        //CombatDebugHandler.Instance.RegisterObject("maneuver_too_close_filter", possibleNodes);

        Vector2 requestDir = new Vector2();
        if (runaway) {
            requestDir = runawayBehaviour(possibleNodes);
            controller.PrevManeuverBehaviour = Behaviour.Runaway;
        } else {
            requestDir = goingForItBehaviour(possibleNodes);
            controller.PrevManeuverBehaviour = Behaviour.GoingforIt;
        }

        return requestDir;
    }

    private Vector2 goingForItBehaviour(List<LookaheadNode> possibleNodes) {
        Tank selfTank = controller.SelfTank;
        Tank targetTank = controller.TargetTank;

        List<CostInfo> nodeCosts = new List<CostInfo>();

        bool overlapNodeExists = false;
        Vector2 diffVec = targetTank.transform.position - selfTank.transform.position;
        List<bool> overlaps = new List<bool>();

        // TODO: should probably change later to pick one with fastest time to hit opp.
        if (diffVec.magnitude < selfTank.CalcAvgRange()) {
            foreach (LookaheadNode node in possibleNodes) {
                bool nodeOverlaps = false;
                foreach (WeaponPart part in selfTank.Hull.GetAllWeapons()) {
                    if (node.HasOverlappedTargetWithWeapon(targetTank.transform.position, part)) {
                        overlapNodeExists = true;
                        nodeOverlaps = true;
                        break;
                    }
                }

                overlaps.Add(nodeOverlaps);
            }
            Debug.Assert(possibleNodes.Count == overlaps.Count, "Possible nodes and overlap count don't match. Should never happen");
        }

        for (int i = 0; i < possibleNodes.Count; ++i) {
            LookaheadNode node = possibleNodes[i];

            WeaponPart notUsed;
            int targetTime = calcMinTimeForAimerToHitAimee(targetTank.StateInfo, node.TankInfo, targetTank.Hull.GetAllWeapons(), out notUsed);
            int selfTime = calcMinTimeForAimerToHitAimee(node.TankInfo, targetTank.StateInfo, selfTank.Hull.GetAllWeapons(), out notUsed);

            int cost = targetTime - selfTime;

            bool overlapCondition = true;
            if (overlapNodeExists) {
                overlapCondition = overlaps[i];
            }

            if (overlapCondition) {
                nodeCosts.Add(new CostInfo(node, cost));
            }
        }
        
        CombatDebugHandler.Instance.RegisterObject("maneuver_going_cost_infos", nodeCosts);

        CostInfo bestInfo = null;
        foreach (CostInfo info in nodeCosts) {
            if (bestInfo == null || info.Cost > bestInfo.Cost) {
                bestInfo = info;
            }
        }

        bestInfo = handleSameCostCostInfos(bestInfo, nodeCosts);

        CombatDebugHandler.Instance.RegisterObject("maneuver_best_node", bestInfo.Node);

        return bestInfo.Node.GetNodeOneStepAfterRoot().IncomingDir;
    }

    private Vector2 runawayBehaviour(List<LookaheadNode> possibleNodes) {
        Tank targetTank = controller.TargetTank;
        Tank selfTank = controller.SelfTank;

        float dist = (targetTank.transform.position - selfTank.transform.position).magnitude;

        float maxRange = selfTank.Hull.GetMaxRange();
        bool onlyForward = maxRange < dist;
        bool allWeaponsReloading = selfTank.Hull.IsAllWeaponsReloading();

        List<CostInfo> nodeCosts = new List<CostInfo>();
        foreach (LookaheadNode node in possibleNodes) {
            WeaponPart notUsed;
            int targetTime = calcMinTimeForAimerToHitAimee(targetTank.StateInfo, node.TankInfo, targetTank.Hull.GetAllWeapons(), out notUsed);

            int cost = targetTime;
            
            float futureDist = ((Vector2)targetTank.transform.position - node.TankInfo.Pos).magnitude;
            if ((onlyForward && dist > futureDist) || (!onlyForward && futureDist < maxRange) || allWeaponsReloading) {
                nodeCosts.Add(new CostInfo(node, cost));
            }
        }

        // If no nodes fit condition, go over again and just calculate cost for each node.
        if (nodeCosts.Count == 0) {
            foreach (LookaheadNode node in possibleNodes) {
                WeaponPart notUsed;
                int targetTime = calcMinTimeForAimerToHitAimee(targetTank.StateInfo, node.TankInfo, targetTank.Hull.GetAllWeapons(), out notUsed);

                int cost = targetTime;

                nodeCosts.Add(new CostInfo(node, cost));
            }
        }

        CombatDebugHandler.Instance.RegisterObject("maneuver_runaway_cost_infos", nodeCosts);

        CostInfo bestInfo = null;
        foreach (CostInfo info in nodeCosts) {
            if (bestInfo == null || bestInfo.Cost < info.Cost) {
                bestInfo = info;
            }
        }

        bestInfo = handleSameCostCostInfos(bestInfo, nodeCosts);

        CombatDebugHandler.Instance.RegisterObject("maneuver_best_node", bestInfo.Node);

        return bestInfo.Node.GetNodeOneStepAfterRoot().IncomingDir;
    }

    private CostInfo handleSameCostCostInfos(CostInfo bestInfo, List<CostInfo> allCostInfos) {
        CostInfo pickedInfo = bestInfo;

        List<CostInfo> sameCosts = new List<CostInfo>();
        foreach (CostInfo info in allCostInfos) {
            if (info.Cost == bestInfo.Cost) {
                sameCosts.Add(info);
            }
        }

        if (sameCosts.Count > 1) {
            float smallestAngleDiff = 9999f;

            foreach (CostInfo info in sameCosts) {
                float angleDiff = Vector2.Angle(prevMoveDir, info.Node.IncomingDir);

                if (angleDiff < smallestAngleDiff) {
                    smallestAngleDiff = angleDiff;
                    pickedInfo = info;
                }
            }
        }

        return pickedInfo;
    }

    private int calcMinTimeForAimerToHitAimee(TankStateInfo aimingTankInfo, TankStateInfo aimeeTankInfo, List<WeaponPart> aimerWeapons, out WeaponPart outWeapon) {
        int minTime = 99999;
        outWeapon = null;
        foreach (WeaponPart weapon in aimerWeapons) {
            Vector2 fireVec = weapon.OwningTank.Hull.Schematic.OrigWeaponDirs[weapon.EquipIdx].Rotate(aimingTankInfo.Rot);
            int time = convertFloatSecondToIntCentiSecond(AIUtility.CalcTimeToHitPos(aimingTankInfo.Pos, fireVec, aimingTankInfo, weapon.Schematic, aimeeTankInfo.Pos) + weapon.CalcTimeToReloaded());

            if (time < minTime) {
                minTime = time;
                outWeapon = weapon;
            }
        }

        return minTime;
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
        Map map = controller.Map;

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

    private List<LookaheadNode> filterByTooCloseToTarget(List<LookaheadNode> nodes) {
        List<LookaheadNode> filteredNode = new List<LookaheadNode>();

        Vector2 targetTankPos = controller.TargetTank.transform.position;
        foreach (LookaheadNode node in nodes) {
            float diff = (targetTankPos - node.TankInfo.Pos).magnitude;

            if (diff > 50f) {
                filteredNode.Add(node);
            }
        }

        if (filteredNode.Count == 0) {
            filteredNode = nodes;
        }

        return filteredNode;
    }

    private int convertFloatSecondToIntCentiSecond(float time) {
        return Mathf.RoundToInt(time * 100);
    }

    private void clearManeuverBehaviourDebugObjects() {
        CombatDebugHandler.Instance.RegisterObject("maneuver_runaway_cost_infos", null);
        CombatDebugHandler.Instance.RegisterObject("maneuver_going_cost_infos", null);
        CombatDebugHandler.Instance.RegisterObject("maneuver_best_node", null);
    }
}
