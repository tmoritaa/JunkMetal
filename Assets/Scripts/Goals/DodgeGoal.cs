﻿using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class DodgeGoal : Goal
{
    const float HitRiskBase = 10f;

    private struct RiskInfo
    {
        public WeaponPart part;
        public Vector2 fireVec;
        public float riskValue;
    }

    public DodgeGoal(AITankController _tankController) : base(_tankController) {
    }

    public override void Init() {
        // Do nothing.
    }

    public override void UpdateInsistence() {
        // TODO: for now.
        Insistence = 100;
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
        
        Tank tank = controller.Tank;
        Tank oppTank = controller.TargetTank;

        Vector2 oppToSelfVec = tank.transform.position - oppTank.transform.position;

        List<RiskInfo> riskInfos = new List<RiskInfo>();
        foreach(WeaponPart part in oppTank.Turret.Weapons) {
            if (part == null) {
                continue;
            }

            Vector2 fireVec = part.CalculateFireVec();

            float angle = Vector2.Angle(fireVec, oppToSelfVec);

            if (part.IsFireable && angle < 90 && oppToSelfVec.magnitude < part.Schematic.Range) {
                RiskInfo riskInfo = new RiskInfo();
                riskInfo.part = part;
                riskInfo.fireVec = fireVec;
                riskInfo.riskValue = calculateRiskValue(riskInfo.part, riskInfo.fireVec, tank, oppTank);
                riskInfos.Add(riskInfo);
            }
        }

        riskInfos.Sort(
            (RiskInfo x, RiskInfo y) => {
                return (x.riskValue > y.riskValue) ? -1 : 1;
            });

        Vector2 finalDir = new Vector2();
        bool solutionFound = false;
        Vector2 toTargetVec = oppTank.transform.position - tank.transform.position;
        while (riskInfos.Count > 0 && !solutionFound) {
            Vector2 potentialDir = new Vector2();
            float maxRisk = 0;
            foreach (RiskInfo riskInfo in riskInfos) {
                Vector2 perpVec = riskInfo.fireVec.Perp();

                // If perp vector is pointing towards opp, flip it.
                if (Vector2.Angle(toTargetVec, perpVec) < 90) {
                    perpVec = -perpVec;
                }

                potentialDir += perpVec.normalized;
                potentialDir.Normalize();

                maxRisk = Mathf.Max(maxRisk, riskInfo.riskValue);
            }

            // If resulting dir is prominent enough, or if there's only one risk, add a vector to try to keep distance with the opponent.
            // The adding of distance keeping we might have to make more complicated
            if (potentialDir.magnitude > 0.5f || riskInfos.Count == 1) {
                // Calculate approach vector to try to keep average distance.
                float distToTarget = toTargetVec.magnitude;
                float travelDist = distToTarget - tank.GetAvgRangeOfWeapons();
                Vector2 approachDir = (Mathf.Sign(travelDist) * toTargetVec).normalized;

                Debug.Log(maxRisk);
                float dodgeRatio = Mathf.Clamp01(maxRisk / HitRiskBase);
                potentialDir = potentialDir * dodgeRatio + approachDir * (1.0f - dodgeRatio);
                potentialDir.Normalize();

                finalDir = potentialDir;
                solutionFound = true;
            } else {
                riskInfos.RemoveAt(riskInfos.Count - 1);
            }
        }

        actions.Add(new GoInDirAction(finalDir, controller));

        return actions.ToArray();
    }

    private float calculateRiskValue(WeaponPart weaponPart, Vector2 fireVec, Tank selfTank, Tank oppTank) {
        const float DmgToArmourRatioThreshold = 0.2f;
        const float TimeToHitRatioForRisk = 0.3f;
        const float ShotAngleRatioForRisk = 1.0f - TimeToHitRatioForRisk;
        const float MaxTimeInSecThresh = 1.0f;
        const float MaxAngleThresh = 90f;
        const float DmgRiskBase = 100f;

        float riskValue = 0;

        // Consider damage to quantify whether we can be hit, or absolutely cannot be hit. We can use the ratio of damage to current health for this
        // For angle and speed, this should let us calculate how likely it is for the shot to hit us if shot right now. Represent this as a percentage and we're good to go.

        float damageRisk = (((float)weaponPart.Schematic.Damage / selfTank.CurArmour) > DmgToArmourRatioThreshold) ? DmgRiskBase : 0;

        Vector2 diffVec = selfTank.transform.position - oppTank.transform.position;
        float angleDiff = Vector2.Angle(fireVec, diffVec);

        float shotTerminalVel = weaponPart.Schematic.ShootImpulse;
        float timeToHit = diffVec.magnitude / shotTerminalVel;

        // For time to hit, we consider 1 second the max.
        // For angleDiff, we consider 45 degrees the max.
        float hitChanceRisk = (Mathf.Max(0, MaxTimeInSecThresh - timeToHit) / MaxTimeInSecThresh * HitRiskBase * TimeToHitRatioForRisk) + 
            (Mathf.Max(0, (MaxAngleThresh - angleDiff)) / MaxAngleThresh * HitRiskBase * ShotAngleRatioForRisk);

        riskValue = damageRisk + hitChanceRisk;

        return riskValue;
    }
}
