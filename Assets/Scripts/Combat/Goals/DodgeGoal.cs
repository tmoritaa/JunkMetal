using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class DodgeGoal : Goal
{
    private struct RiskInfo
    {
        public WeaponPart weapon;
        public Vector2 fireVec;
        public float riskValue;
    }

    public DodgeGoal(AITankController _tankController) : base(_tankController) {
    }

    public override void ReInit() {
        // Do nothing.
    }

    public override void UpdateInsistence() {
        Insistence = 0;

        // Should be based on attackability of opponent
        // Calculate threat level of target weapons against us
        Tank selfTank = controller.SelfTank;
        Tank oppTank = controller.TargetTank;
        foreach (WeaponPart part in selfTank.Turret.GetAllWeapons()) {
            if (part == null) {
                continue;
            }

            Insistence += AIUtility.CalculateRiskValue(part, selfTank, oppTank);
        }

        Debug.Log("Dodge goal Insistence=" + Insistence);
    }

    // Get all fire vectors of opponent that are in range, are facing us, and are fireable
    // Then for each one, calculate angle from us to fire vec
    // Calculate risk of each fire vec
    // Calculate movement vec for each risk fire vec
    // If adding each one results in one direction, do that.
    // If adding each results in not moving, then pick the least risky one, and don't consider that one, and redo direction additions with the fire vecs left
    // Once picked move vector to dodge risk, blend in movement vector to stay in avg range of weapons on tank
    public override AIAction[] CalcActionsToPerform() {
        List<AIAction> actions = new List<AIAction>();
        
        Tank selfTank = controller.SelfTank;
        Tank oppTank = controller.TargetTank;

        Vector2 oppToSelfVec = selfTank.transform.position - oppTank.transform.position;

        List<RiskInfo> riskInfos = new List<RiskInfo>();
        foreach(WeaponPart part in oppTank.Turret.GetAllWeapons()) {
            float riskValue = AIUtility.CalculateRiskValue(part, selfTank, oppTank);
            if (riskValue > 0) {
                RiskInfo riskInfo = new RiskInfo();
                riskInfo.weapon = part;
                riskInfo.fireVec = part.CalculateFireVec();;
                riskInfo.riskValue = riskValue;
                riskInfos.Add(riskInfo);
            }
        }

        riskInfos.OrderByDescending(ri => ri.riskValue);
        
        Vector2 finalMoveVec = new Vector2();
        bool solutionFound = false;
        Vector2 toTargetVec = oppTank.transform.position - selfTank.transform.position;
        while (riskInfos.Count > 0 && !solutionFound) {
            Vector2 potentialDir = new Vector2();
            float maxRisk = 0;
            foreach (RiskInfo riskInfo in riskInfos) {
                // Calculate whether moving away from target or moving perpendicular from fire vec would be faster
                float runAwayTimeEstimate = 9999f;
                float runSideTimeEstimate = 9999f;

                // First calculate moving away from target
                Vector2 awayFromTarget = -toTargetVec;
                float distToEscape = riskInfo.weapon.Schematic.Range - awayFromTarget.magnitude;
                // Only consider running away if we're actually in range to get hit. Should probably always be true.
                if (distToEscape > 0) {
                    float angle = Mathf.Min(Vector2.Angle(selfTank.GetForwardVec(), awayFromTarget), Vector2.Angle(selfTank.GetBackwardVec(), awayFromTarget));
                    runAwayTimeEstimate = distToEscape / selfTank.StateInfo.TerminalVel;
                }

                // Next calculate moving perpendicular from fire vec
                Vector2 perpVec = riskInfo.fireVec.Perp();

                // If perp vector is pointing towards opp, flip it.
                if (Vector2.Angle(toTargetVec, perpVec) < 90) {
                    perpVec = -perpVec;
                }
                
                runSideTimeEstimate = Mathf.Min(selfTank.CalcTimeToRotate(selfTank.GetForwardVec(), perpVec), selfTank.CalcTimeToRotate(selfTank.GetBackwardVec(), perpVec));

                Vector2 moveVec = (runAwayTimeEstimate > runSideTimeEstimate) ? perpVec : awayFromTarget;

                potentialDir += moveVec.normalized;
                potentialDir.Normalize();

                maxRisk = Mathf.Max(maxRisk, riskInfo.riskValue);
            }

            // If resulting dir is prominent enough, or if there's only one risk, that's the direction we want to escape
            if (potentialDir.magnitude > 0.5f || riskInfos.Count == 1) {
                finalMoveVec = potentialDir;
                solutionFound = true;
            } else {
                riskInfos.RemoveAt(riskInfos.Count - 1);
            }
        }

        actions.Add(new GoInDirAction(finalMoveVec, controller));

        return actions.ToArray();
    }
}
