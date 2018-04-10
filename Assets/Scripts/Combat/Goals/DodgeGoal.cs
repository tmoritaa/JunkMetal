using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class DodgeGoal : Goal
{
    private const float LookaheadTime = 0.5f;
    private const float LookaheadTimeStep = 0.5f;
    private const float WaitPeriod = 0.5f;

    private float timeSinceLastJet = -9999;

    public DodgeGoal(AITankController _tankController) : base(_tankController) {
    }

    public override void ReInit() {
        // Do nothing.
    }

    public override void UpdateInsistence() {
        Insistence = 0;

        // If dodged recently, don't dodge so soon.
        if (Time.time - timeSinceLastJet < WaitPeriod) {
            return;
        }

        Tank selfTank = controller.SelfTank;

        bool shouldDodge = false;

        // First calculate all bullet trajectories and check if they'll hit us in 0.5 seconds
        foreach (Bullet bullet in BulletInstanceHandler.Instance.BulletInstances) {
            Vector2 targetPos = selfTank.transform.position;
            Vector2 curFireVec = bullet.Body.velocity.normalized;
            Vector2 curFirePos = bullet.Body.position;
            Ray ray = new Ray(curFirePos, curFireVec);
            float shortestDist = Vector3.Cross(ray.direction, (Vector3)(targetPos) - ray.origin).magnitude;
            bool canHitIfFired = shortestDist < selfTank.Hull.Schematic.Size.x / 2f;

            Vector2 targetVec = targetPos - curFirePos;

            float distTravelledByBullet = (bullet.Body.velocity * 0.5f).magnitude;

            bool fireVecFacingTarget = Vector2.Angle(curFireVec, targetVec) < 90f;
            bool inRange = targetVec.magnitude < distTravelledByBullet;
            if (inRange && canHitIfFired && fireVecFacingTarget) {
                shouldDodge = true;
                break;
            }
        }

        if (!shouldDodge) {
            Tank oppTank = controller.TargetTank;
            shouldDodge = AIUtility.IsInOpponentFireVec(selfTank.StateInfo, oppTank.StateInfo, oppTank.Hull.GetAllWeapons());
        }

        if (shouldDodge) {
            Insistence = 75;
        }
    }

    public override AIAction[] CalcActionsToPerform() {
        Tank selfTank = controller.SelfTank;
        Tank targetTank = controller.TargetTank;
        Map map = controller.Map;

        List<TreeSearchMoveInfo> possibleMoves = new List<TreeSearchMoveInfo>();
        possibleMoves.Add(new TreeSearchMoveInfo(new Vector2(0, 1), true));
        possibleMoves.Add(new TreeSearchMoveInfo(new Vector2(0, -1), true));
        possibleMoves.Add(new TreeSearchMoveInfo(new Vector2(0, 1).Rotate(90f), true));
        possibleMoves.Add(new TreeSearchMoveInfo(new Vector2(0, 1).Rotate(-90f), true));

        LookaheadTree tree = new LookaheadTree();
        tree.PopulateTree(selfTank, map, LookaheadTime, LookaheadTimeStep, possibleMoves);

        List<LookaheadNode> possibleNodes = tree.FindAllNodesAtSearchTime(LookaheadTime);
        
        possibleNodes = AIUtility.FilterByPathNotObstructed(possibleNodes);

        possibleNodes = AIUtility.FilterByDestNotObstructed(possibleNodes, map);

        possibleNodes = AIUtility.FilterByPassingBullet(possibleNodes, map);

        possibleNodes = AIUtility.FilterByAwayFromWall(possibleNodes, selfTank.StateInfo);

        LookaheadNode bestNode = null;
        float bestCost = 0;
        foreach (LookaheadNode node in possibleNodes) {
            WeaponPart notUsed;
            int targetTime = AIUtility.CalcMinTimeForAimerToHitAimee(targetTank.StateInfo, node.TankInfo, targetTank.Hull.GetAllWeapons(), out notUsed);
            int selfTime = AIUtility.CalcMinTimeForAimerToHitAimee(node.TankInfo, targetTank.StateInfo, selfTank.Hull.GetAllWeapons(), out notUsed);

            float cost = targetTime - selfTime;

            if (bestNode == null || bestCost < cost) {
                bestNode = node;
                bestCost = cost;
            }
        }

        timeSinceLastJet = Time.time;

        List<AIAction> actions = new List<AIAction>();

        actions.Add(new JetInDirAction(bestNode.IncomingDir, controller));

        return actions.ToArray();
    }
}
