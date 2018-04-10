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

    private const float UpdatePeriod = 0.1f;
    private const float LookaheadTime = 0.5f;
    private const float LookaheadTimeStep = 0.5f;

    private Vector2 prevMoveDir = new Vector2();
    private bool prevMoveIsJet = false;

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
        Insistence = 50;
    }

    public override AIAction[] CalcActionsToPerform() {
        List<AIAction> actions = new List<AIAction>();

        Map map = controller.Map;

        elapsedTimeSinceLastUpdate += Time.deltaTime;

        if (elapsedTimeSinceLastUpdate >= UpdatePeriod) {
            LookaheadNode requestNode = pickDirToPursue();

            forwardVecWhenUpdate = controller.SelfTank.GetForwardVec();
            prevMoveDir = requestNode.IncomingDir;
            prevMoveIsJet = requestNode.IncomingWasJet;

            elapsedTimeSinceLastUpdate = 0;
        }

        if (prevMoveIsJet) {
            if (elapsedTimeSinceLastUpdate == 0) {
                actions.Add(new JetInDirAction(prevMoveDir, controller));

                CombatDebugHandler.Instance.RegisterObject("maneuver_move_dir", prevMoveDir);
            }
        } else {
            float angleDiffSinceUpdate = Vector2.SignedAngle(forwardVecWhenUpdate, controller.SelfTank.GetForwardVec());
            actions.Add(new GoInDirAction(prevMoveDir.Rotate(angleDiffSinceUpdate), controller));

            CombatDebugHandler.Instance.RegisterObject("maneuver_move_dir", prevMoveDir.Rotate(angleDiffSinceUpdate));
        }
        
        return actions.ToArray();
    }

    private LookaheadNode pickDirToPursue() {
        Tank selfTank = controller.SelfTank;
        Tank targetTank = controller.TargetTank;

        Map map = controller.Map;

        WeaponPart targetWeapon;
        WeaponPart selfWeapon;
        int timeForTargetToHitSelf = AIUtility.CalcMinTimeForAimerToHitAimee(targetTank.StateInfo, selfTank.StateInfo, targetTank.Hull.GetAllWeapons(), out targetWeapon);
        int timeForSelfToHitTarget = AIUtility.CalcMinTimeForAimerToHitAimee(selfTank.StateInfo, targetTank.StateInfo, selfTank.Hull.GetAllWeapons(), out selfWeapon);

        int weaponDmgDiff = selfWeapon.Schematic.Damage - targetWeapon.Schematic.Damage;
        float stepRatio = weaponDmgDiff / 10f;

        int thresh = 50 + Mathf.RoundToInt(stepRatio * 25);
        if (weaponDmgDiff > 0) {
            thresh = Math.Min(thresh, 100);
        } else {
            thresh = Math.Max(thresh, -100);
        }

        int diff = timeForSelfToHitTarget - timeForTargetToHitSelf;
        
        bool runaway = diff > thresh && timeForTargetToHitSelf < 100;

        Debug.Log(runaway ? "running away" : "Going for it");

        List<TreeSearchMoveInfo> possibleMoves = new List<TreeSearchMoveInfo>();

        possibleMoves.Add(new TreeSearchMoveInfo(new Vector2(0, 1), false));
        possibleMoves.Add(new TreeSearchMoveInfo(new Vector2(0, -1), false));
        possibleMoves.Add(new TreeSearchMoveInfo(new Vector2(0, 1).Rotate(45f), false));
        possibleMoves.Add(new TreeSearchMoveInfo(new Vector2(0, 1).Rotate(135f), false));
        possibleMoves.Add(new TreeSearchMoveInfo(new Vector2(0, 1).Rotate(-45f), false));
        possibleMoves.Add(new TreeSearchMoveInfo(new Vector2(0, 1).Rotate(-135f), false));

        if (!runaway) {
            possibleMoves.Add(new TreeSearchMoveInfo(new Vector2(0, 1).Rotate(90f), false));
            possibleMoves.Add(new TreeSearchMoveInfo(new Vector2(0, 1).Rotate(-90f), false));
        }

        float dist = (selfTank.transform.position - targetTank.transform.position).magnitude;
        if (!runaway && selfTank.Hull.EnergyAvailableForUsage(selfTank.Hull.Schematic.JetEnergyUsage) && selfTank.FindMaxWeaponRange() >= dist) {
            possibleMoves.Add(new TreeSearchMoveInfo(new Vector2(0, 1), true));
            possibleMoves.Add(new TreeSearchMoveInfo(new Vector2(0, -1), true));
            possibleMoves.Add(new TreeSearchMoveInfo(new Vector2(0, 1).Rotate(90f), true));
            possibleMoves.Add(new TreeSearchMoveInfo(new Vector2(0, 1).Rotate(-90f), true));
        }

        clearManeuverBehaviourDebugObjects();

        LookaheadTree tree = new LookaheadTree();
        tree.PopulateTree(selfTank, map, LookaheadTime, LookaheadTimeStep, possibleMoves);

        List<LookaheadNode> possibleNodes = tree.FindAllNodesAtSearchTime(LookaheadTime);
        CombatDebugHandler.Instance.RegisterObject("maneuver_nodes_at_searchtime", possibleNodes);

        // First perform general filter
        possibleNodes = AIUtility.FilterByPathNotObstructed(possibleNodes);
        CombatDebugHandler.Instance.RegisterObject("maneuver_path_unobstructed_filter", possibleNodes);

        possibleNodes = AIUtility.FilterByDestNotObstructed(possibleNodes, map);
        CombatDebugHandler.Instance.RegisterObject("maneuver_dest_not_obstructed_filter", possibleNodes);

        possibleNodes = AIUtility.FilterByPassingBullet(possibleNodes, map);
        CombatDebugHandler.Instance.RegisterObject("maneuver_bullet_filter", possibleNodes);

        possibleNodes = AIUtility.FilterByAwayFromWall(possibleNodes, selfTank.StateInfo);
        CombatDebugHandler.Instance.RegisterObject("maneuver_away_from_wall_filter", possibleNodes);

        LookaheadNode requestNode;
        if (runaway) {
            requestNode = runawayBehaviour(possibleNodes);
            controller.PrevManeuverBehaviour = Behaviour.Runaway;
        } else {
            requestNode = goingForItBehaviour(possibleNodes);
            controller.PrevManeuverBehaviour = Behaviour.GoingforIt;
        }

        return requestNode;
    }

    private LookaheadNode goingForItBehaviour(List<LookaheadNode> possibleNodes) {
        Tank selfTank = controller.SelfTank;
        Tank targetTank = controller.TargetTank;

        List<CostInfo> nodeCosts = new List<CostInfo>();

        bool overlapNodeExists = false;
        Vector2 diffVec = targetTank.transform.position - selfTank.transform.position;
        List<bool> overlaps = new List<bool>();

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

        for (int i = 0; i < possibleNodes.Count; ++i) {
            LookaheadNode node = possibleNodes[i];

            WeaponPart notUsed;
            int targetTime = AIUtility.CalcMinTimeForAimerToHitAimee(targetTank.StateInfo, node.TankInfo, targetTank.Hull.GetAllWeapons(), out notUsed);
            int selfTime = AIUtility.CalcMinTimeForAimerToHitAimee(node.TankInfo, targetTank.StateInfo, selfTank.Hull.GetAllWeapons(), out notUsed);

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

        return bestInfo.Node.GetNodeOneStepAfterRoot();
    }

    private LookaheadNode runawayBehaviour(List<LookaheadNode> possibleNodes) {
        Tank targetTank = controller.TargetTank;
        Tank selfTank = controller.SelfTank;

        float dist = (targetTank.transform.position - selfTank.transform.position).magnitude;

        float maxRange = selfTank.Hull.GetMaxRange() * 1.5f;
        bool onlyCloser = maxRange < dist;
        bool allWeaponsReloading = selfTank.Hull.IsAllWeaponsReloading();

        List<CostInfo> nodeCosts = new List<CostInfo>();
        bool withFilter = true;

        while (nodeCosts.Count == 0) {
            foreach (LookaheadNode node in possibleNodes) {
                WeaponPart notUsed;
                int targetTime = AIUtility.CalcMinTimeForAimerToHitAimee(targetTank.StateInfo, node.TankInfo, targetTank.Hull.GetAllWeapons(), out notUsed);

                Vector2 incomingDir = node.GetNodeOneStepAfterRoot().IncomingDir;

                int cost = targetTime;
                float futureDist = ((Vector2)targetTank.transform.position - node.TankInfo.Pos).magnitude;
                if (!withFilter || ((onlyCloser && dist > futureDist) || (!onlyCloser && futureDist < maxRange) || allWeaponsReloading)) {
                    nodeCosts.Add(new CostInfo(node, cost));
                }
            }

            withFilter = false;
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

        return bestInfo.Node.GetNodeOneStepAfterRoot();
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

    private void clearManeuverBehaviourDebugObjects() {
        CombatDebugHandler.Instance.RegisterObject("maneuver_runaway_cost_infos", null);
        CombatDebugHandler.Instance.RegisterObject("maneuver_going_cost_infos", null);
        CombatDebugHandler.Instance.RegisterObject("maneuver_best_node", null);
    }
}
