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

    private enum Behaviour
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

    private Behaviour prevBehaviour = Behaviour.Notset;

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

        if (elapsedTimeSinceLastUpdate >= UpdatePeriod) {
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

        int timeForTargetToHitSelf = calcMinTimeForAimerToHitAimee(targetTank.StateInfo, selfTank.StateInfo, targetTank.Turret.GetAllWeapons());
        int timeForSelfToHitTarget = calcMinTimeForAimerToHitAimee(selfTank.StateInfo, targetTank.StateInfo, selfTank.Turret.GetAllWeapons());

        int safeTime = prevBehaviour == Behaviour.GoingforIt ? 50 : 100;
        int diff = timeForSelfToHitTarget - timeForTargetToHitSelf;
        bool runaway = timeForTargetToHitSelf < timeForSelfToHitTarget && timeForTargetToHitSelf < safeTime && diff > 25;

        List<float> possibleRotAngles = new List<float>() {
                0,
                180f,
                45f,
                135f,
                -45f,
                -135f
            };
        if (!runaway) {
            possibleRotAngles.Add(90f);
            possibleRotAngles.Add(-90f);
        }

        clearManeuverBehaviourDebugObjects();

        LookaheadTree tree = new LookaheadTree();
        tree.PopulateTree(selfTank, map, LookaheadTime, LookaheadTimeStep, possibleRotAngles);

        List<LookaheadNode> possibleNodes = tree.FindAllNodesAtSearchTime(LookaheadTime);
        DebugManager.Instance.RegisterObject("maneuver_nodes_at_searchtime", possibleNodes);

        // First perform general filter
        possibleNodes = filterByPathNotObstructed(possibleNodes);
        DebugManager.Instance.RegisterObject("maneuver_path_unobstructed_filter", possibleNodes);

        possibleNodes = filterByDestNotObstructed(possibleNodes);
        DebugManager.Instance.RegisterObject("maneuver_dest_not_obstructed_filter", possibleNodes);

        Vector2 requestDir = new Vector2();
        if (runaway) {
            Debug.Log("We runnin");

            requestDir = runawayBehaviour(possibleNodes);
            prevBehaviour = Behaviour.Runaway;
        } else {
            Debug.Log("We goin for it");

            requestDir = goingForItBehaviour(possibleNodes);
            prevBehaviour = Behaviour.GoingforIt;
        }

        return requestDir;
    }

    private Vector2 goingForItBehaviour(List<LookaheadNode> possibleNodes) {
        Tank selfTank = controller.SelfTank;
        Tank targetTank = controller.TargetTank;

        List<CostInfo> nodeCosts = new List<CostInfo>();
        foreach (LookaheadNode node in possibleNodes) {
            int targetTime = calcMinTimeForAimerToHitAimee(targetTank.StateInfo, node.TankInfo, targetTank.Turret.GetAllWeapons());
            int selfTime = calcMinTimeForAimerToHitAimee(node.TankInfo, targetTank.StateInfo, selfTank.Turret.GetAllWeapons());

            int cost = targetTime - selfTime;

            nodeCosts.Add(new CostInfo(node, cost));
        }
        
        DebugManager.Instance.RegisterObject("maneuver_going_cost_infos", nodeCosts);

        Debug.Log("Infos=========================================");
        CostInfo bestInfo = null;
        foreach (CostInfo info in nodeCosts) {
            Debug.Log("Info: Pos=" + info.Node.TankInfo.Pos + " Rot=" + info.Node.TankInfo.Rot + " Cost=" + info.Cost);
            if (bestInfo == null || info.Cost > bestInfo.Cost) {
                bestInfo = info;
            }
        }

        bestInfo = handleSameCostCostInfos(bestInfo, nodeCosts);

        Debug.Log("BestInfo: Pos=" + bestInfo.Node.TankInfo.Pos + " Rot=" + bestInfo.Node.TankInfo.Rot + " Cost=" + bestInfo.Cost);

        DebugManager.Instance.RegisterObject("maneuver_best_node", bestInfo.Node);

        return bestInfo.Node.IncomingDir;
    }

    private Vector2 runawayBehaviour(List<LookaheadNode> possibleNodes) {
        Tank targetTank = controller.TargetTank;

        List<CostInfo> nodeCosts = new List<CostInfo>();
        bool reachableFirst = false;
        foreach (LookaheadNode node in possibleNodes) {
            int time = calcMinTimeForAimerToHitAimee(targetTank.StateInfo, node.TankInfo, targetTank.Turret.GetAllWeapons());

            nodeCosts.Add(new CostInfo(node, time));

            if (time > LookaheadTime) {
                reachableFirst = true;
            }
        }

        DebugManager.Instance.RegisterObject("maneuver_runaway_cost_infos", nodeCosts);

        CostInfo bestInfo = null;
        if (reachableFirst) {
            foreach (CostInfo info in nodeCosts) {
                if (info.Cost > LookaheadTime && (bestInfo == null || bestInfo.Cost < info.Cost)) {
                    bestInfo = info;
                }
            }
        } else {
            foreach (CostInfo info in nodeCosts) {
                if (bestInfo == null || bestInfo.Cost < info.Cost) {
                    bestInfo = info;
                }
            }
        }

        bestInfo = handleSameCostCostInfos(bestInfo, nodeCosts);

        DebugManager.Instance.RegisterObject("maneuver_best_node", bestInfo.Node);

        return bestInfo.Node.IncomingDir;
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

    private int calcMinTimeForAimerToHitAimee(TankStateInfo aimingTankInfo, TankStateInfo aimeeTankInfo, List<WeaponPart> aimerWeapons) {
        int minTime = 9999;
        foreach (WeaponPart weapon in aimerWeapons) {
            Vector2 fireVec = weapon.OwningTank.Turret.Schematic.OrigWeaponDirs[weapon.TurretIdx].Rotate(aimingTankInfo.Rot);
            int time = convertFloatSecondToIntCentiSecond(AIUtility.CalcTimeToHitPos(aimingTankInfo.Pos, fireVec, aimingTankInfo, weapon.Schematic, aimeeTankInfo.Pos) + weapon.CalcTimeToReloaded());

            if (time < minTime) {
                minTime = time;
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

    private int convertFloatSecondToIntCentiSecond(float time) {
        return Mathf.RoundToInt(time * 100);
    }

    private void clearManeuverBehaviourDebugObjects() {
        DebugManager.Instance.RegisterObject("maneuver_runaway_cost_infos", null);
        DebugManager.Instance.RegisterObject("maneuver_going_cost_infos", null);
        DebugManager.Instance.RegisterObject("maneuver_best_node", null);
    }
}
