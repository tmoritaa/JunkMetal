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

        public float Cost
        {
            get; private set;
        }

        public CostInfo(LookaheadNode node, float cost) {
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

        float timeForTargetToHitSelf = calcMinTimeForAimerToHitAimee(targetTank.StateInfo, selfTank.StateInfo, targetTank.Turret.GetAllWeapons());
        float timeForSelfToHitTarget = calcMinTimeForAimerToHitAimee(selfTank.StateInfo, targetTank.StateInfo, selfTank.Turret.GetAllWeapons());

        float safeTime = prevBehaviour == Behaviour.GoingforIt ? 0.5f : 1.0f;
        float diff = timeForSelfToHitTarget - timeForTargetToHitSelf;
        bool runaway = timeForTargetToHitSelf < timeForSelfToHitTarget && timeForTargetToHitSelf < safeTime && diff > 0.25f;

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
            float targetTime = calcMinTimeForAimerToHitAimee(targetTank.StateInfo, node.TankInfo, targetTank.Turret.GetAllWeapons());
            float selfTime = calcMinTimeForAimerToHitAimee(node.TankInfo, targetTank.StateInfo, selfTank.Turret.GetAllWeapons());

            float cost = targetTime - selfTime;

            nodeCosts.Add(new CostInfo(node, cost));
        }
        
        DebugManager.Instance.RegisterObject("maneuver_going_cost_infos", nodeCosts);

        CostInfo pickedNode = null;
        foreach (CostInfo info in nodeCosts) {
            if (pickedNode == null || info.Cost > pickedNode.Cost) {
                pickedNode = info;
            }
        }
        
        DebugManager.Instance.RegisterObject("maneuver_best_node", pickedNode.Node);

        return pickedNode.Node.IncomingDir;
    }

    private Vector2 runawayBehaviour(List<LookaheadNode> possibleNodes) {
        Tank targetTank = controller.TargetTank;

        List<CostInfo> nodeCosts = new List<CostInfo>();
        bool reachableFirst = false;
        foreach (LookaheadNode node in possibleNodes) {
            float time = calcMinTimeForAimerToHitAimee(targetTank.StateInfo, node.TankInfo, targetTank.Turret.GetAllWeapons());

            nodeCosts.Add(new CostInfo(node, time));

            if (time > LookaheadTime) {
                reachableFirst = true;
            }
        }

        DebugManager.Instance.RegisterObject("maneuver_runaway_cost_infos", nodeCosts);

        CostInfo pickedInfo = null;
        if (reachableFirst) {
            foreach (CostInfo info in nodeCosts) {
                if (info.Cost > LookaheadTime && (pickedInfo == null || pickedInfo.Cost < info.Cost)) {
                    pickedInfo = info;
                }
            }
        } else {
            foreach (CostInfo info in nodeCosts) {
                if (pickedInfo == null || pickedInfo.Cost < info.Cost) {
                    pickedInfo = info;
                }
            }
        }

        DebugManager.Instance.RegisterObject("maneuver_best_node", pickedInfo.Node);

        return pickedInfo.Node.IncomingDir;
    }

    private float calcMinTimeForAimerToHitAimee(TankStateInfo aimingTankInfo, TankStateInfo aimeeTankInfo, List<WeaponPart> aimerWeapons) {
        float minTime = 9999f;
        foreach (WeaponPart weapon in aimerWeapons) {
            Vector2 fireVec = weapon.OwningTank.Turret.Schematic.OrigWeaponDirs[weapon.TurretIdx].Rotate(aimingTankInfo.Rot);
            float time = AIUtility.CalcTimeToHitPos(aimingTankInfo.Pos, fireVec, aimingTankInfo, weapon.Schematic, aimeeTankInfo.Pos);

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

    private void clearManeuverBehaviourDebugObjects() {
        DebugManager.Instance.RegisterObject("maneuver_runaway_cost_infos", null);
        DebugManager.Instance.RegisterObject("maneuver_going_cost_infos", null);
        DebugManager.Instance.RegisterObject("maneuver_best_node", null);
    }
}
