using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class AIUtility
{
    // Used http://danikgames.com/blog/how-to-intersect-a-moving-target-in-2d/ as a reference.
    public static Vector2 CalculateTargetPosWithWeapon(float weaponTerminalVel, Vector2 weaponFirePos, Vector2 weaponOwnerPos, Vector2 targetTankPos, Vector2 targetTankVel) {
        Vector2 diffVec = targetTankPos- weaponFirePos;
        Vector2 abVec = diffVec.normalized;

        Vector2 targetVel = targetTankVel;
        Vector2 uj = (Vector2.Dot(targetVel, abVec) / abVec.magnitude) * abVec;
        Vector2 ui = targetVel - uj;

        Vector2 vi = ui;

        float timeToHit = 999;
        Vector2 v;
        // Corner case: if it turns out the weapons is too slow to ever catch the target, we just kind of try.
        if (ui.magnitude > weaponTerminalVel) {
            v = targetVel.normalized * weaponTerminalVel;
        } else {
            Vector2 vj = abVec * Mathf.Sqrt(weaponTerminalVel * weaponTerminalVel - vi.sqrMagnitude);
            v = vi + vj;
            timeToHit = diffVec.magnitude / (vj.magnitude - uj.magnitude);
        }

        Vector2 targetPos = weaponOwnerPos + v * timeToHit;

        return targetPos;
    }
    
    public static float CalcTimeToHitPos(Vector2 curFirePos, Vector2 curFireVec, Tank tank, WeaponPartSchematic schematic, Vector2 targetPos) {
        float timeToHit = 0;

        Vector2 diffVec = targetPos - curFirePos;

        // We have to calculate three things: rotation time, travel time to in range, and bullet travel time.

        // First calculate rotation time.
        timeToHit += tank.CalcTimeToRotate(curFireVec, diffVec);

        // Next, calculate travel time.
        // TODO: Implement. Have to finish.
        float moveAmount = Mathf.Max(diffVec.magnitude - schematic.Range, 0);
        if (moveAmount > 0) {
            Vector2 newPos = diffVec.normalized * moveAmount + (Vector2)tank.transform.position;
            timeToHit += tank.CalcTimeToReachPosWithNoRot(newPos);
        }

        // Finally, calculate bullet travel time.
        timeToHit += Mathf.Min(schematic.Range, diffVec.magnitude) / schematic.ShootImpulse;

        return timeToHit;
    }

    public static float CalcTimeToReachPos(Tank tank, Vector2 targetPos) {
        Vector2 curPos = tank.transform.position;

        Vector2 diffVec = targetPos - curPos;

        float timeToReach = 0;

        timeToReach += Mathf.Min(tank.CalcTimeToRotate(tank.GetForwardVec(), diffVec), tank.CalcTimeToRotate(tank.GetBackwardVec(), diffVec));
        timeToReach += tank.CalcTimeToReachPosWithNoRot(targetPos);

        return timeToReach;
    }

    // TODO: probably won't be used once AttackGoal and DodgeGoal are rewritten using threat maps. Check later and confirm that it's not used.
    public static float CalculateRiskValue(WeaponPart weaponPart, Tank targetedTank, Tank targetingTank) {
        const float ThreatRangeMod = 1.1f;

        float riskValue = 0;

        Vector2 fireVec = weaponPart.CalculateFireVec();
        Vector2 oppToSelfVec = targetedTank.transform.position - targetingTank.transform.position;
        float angle = Vector2.Angle(fireVec, oppToSelfVec);

        if (weaponPart.IsFireable && angle < 90 && oppToSelfVec.magnitude < weaponPart.Schematic.Range * ThreatRangeMod) {
            const float TimeToHitRatioForRisk = 0.2f;
            const float ShotAngleRatioForRisk = 0.4f;
            const float DamageRatioForRisk = 1.0f - TimeToHitRatioForRisk - ShotAngleRatioForRisk;
            const float MaxTimeInSecThresh = 1.0f;
            const float MaxAngleThresh = 90f;

            // Consider damage to quantify whether we can be hit, or absolutely cannot be hit. We can use the ratio of damage to current health for this
            // For angle and speed, this should let us calculate how likely it is for the shot to hit us if shot right now. Represent this as a percentage and we're good to go.

            float damageRisk = Mathf.Clamp01((float)weaponPart.Schematic.Damage / targetedTank.CurArmour) * DamageRatioForRisk;

            Vector2 diffVec = targetedTank.transform.position - targetingTank.transform.position;
            float angleDiff = Vector2.Angle(fireVec, diffVec);
            float angleRisk = Mathf.Max(0, (MaxAngleThresh - angleDiff)) / MaxAngleThresh * ShotAngleRatioForRisk;

            float shotTerminalVel = weaponPart.Schematic.ShootImpulse;
            float timeToHit = diffVec.magnitude / shotTerminalVel;
            float shotSpeedRisk = Mathf.Max(0, MaxTimeInSecThresh - timeToHit) / MaxTimeInSecThresh * TimeToHitRatioForRisk;

            riskValue = damageRisk + angleRisk + shotSpeedRisk;
        }

        return riskValue;
    }

    public static void SmoothPath(List<Node> path, Tank tank) {
        const int WallBit = 8;
        const int PlayerBit = 9;
        const int LayerMask = 1 << WallBit | 1 << PlayerBit;

        int removeCount = 0;
        for (int i = 0; i < path.Count; ++i) {
            Node node = path[i];

            Vector2 leftVec = tank.GetForwardVec().Rotate(-90).normalized;
            Vector2 rightVec = tank.GetForwardVec().Rotate(90).normalized;

            Vector2 pos = CombatManager.Instance.Map.NodeToPosition(node);
            Vector2 diffVec = pos - (Vector2)tank.transform.position;

            RaycastHit2D leftHit = Physics2D.Raycast((Vector2)tank.transform.position + (leftVec * (tank.Hull.Schematic.Size.x / 2f)), diffVec.normalized, diffVec.magnitude, LayerMask);
            RaycastHit2D rightHit = Physics2D.Raycast((Vector2)tank.transform.position + (rightVec * (tank.Hull.Schematic.Size.x / 2f)), diffVec.normalized, diffVec.magnitude, LayerMask);

            // If collision, stop
            if (leftHit.collider != null || rightHit.collider != null) {
                break;
            }

            removeCount = i;
        }

        if (removeCount > 0) {
            path.RemoveRange(0, removeCount);
        }
    }
}
