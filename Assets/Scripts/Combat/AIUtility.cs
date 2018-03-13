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
    
    public static float CalcTimeToHitPos(Vector2 curFirePos, Vector2 curFireVec, Tank tank, WeaponPartSchematic schematic, Vector2 targetPos, bool ignoreTimeToAlign=false, bool ignoreTimeToMove=false) {
        float timeToHit = 0;

        Vector2 diffVec = targetPos - curFirePos;

        // We have to calculate three things: rotation time, travel time to in range, and bullet travel time.

        // First calculate rotation time.
        if (!ignoreTimeToAlign) {
            timeToHit += tank.CalcTimeToRotate(curFireVec, diffVec);
        }

        if (!ignoreTimeToMove) {
            // Next, calculate travel time.
            float moveAmount = Mathf.Max(diffVec.magnitude - schematic.Range, 0);
            if (moveAmount > 0) {
                Vector2 newPos = diffVec.normalized * moveAmount + (Vector2)tank.transform.position;
                timeToHit += tank.CalcTimeToReachPosWithNoRot(newPos);
            }
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

    public static TankStateInfo CalcPosInFutureWithRequestedDir(Vector2 _requestDir, float timeInSecs, TankStateInfo tankInfo, out List<Vector2> passedPos) {
        TankStateInfo resultInfo = new TankStateInfo(tankInfo);
        Vector2 requestDir = _requestDir;

        float elapsedTime = 0;
        
        float dt = Time.fixedDeltaTime;

        List<Vector2> allPos = new List<Vector2>();
        allPos.Add(resultInfo.Pos);

        while (elapsedTime <= timeInSecs) {
            elapsedTime += dt;

            int[] powerChange = AIUtility.CalcPowerChangeBasedOnRequestDir(requestDir, resultInfo);
            resultInfo.LeftCurPower = powerChange[0];
            resultInfo.RightCurPower = powerChange[1];

            // Pos update
            {
                Vector2 f = TankUtility.CalcAppliedLinearForce(resultInfo);
                float m = resultInfo.Mass;
                float drag = resultInfo.LinearDrag;
                Vector2 a = f / m;
                resultInfo.LinearVel = (resultInfo.LinearVel + a * dt) * (1f / (1f + drag * dt));
                resultInfo.Pos += resultInfo.LinearVel * dt;

                allPos.Add(resultInfo.Pos);
            }

            // rot update
            {
                float torque = TankUtility.CalcAppliedTorque(resultInfo);
                float angularDrag = resultInfo.AngularDrag;
                float angularAccel = torque / resultInfo.Inertia * Mathf.Rad2Deg;

                resultInfo.AngularVel = (resultInfo.AngularVel + angularAccel * dt) * (1f / (1f + angularDrag * dt));
                float rotDiff = resultInfo.AngularVel * dt;
                resultInfo.Rot += rotDiff;

                requestDir = requestDir.Rotate(rotDiff);
            }
        }

        passedPos = allPos;

        return resultInfo;
    }

    public static int[] CalcPowerChangeBasedOnRequestDir(Vector2 requestDir, TankStateInfo stateInfo) {
        const float MinRatioCutOff = 0.4f;
        const float MaxRatioCutoff = 0.7f; // TODO: later probably make a serialized field for easier tweaking and move to AITankController

        const float StartingBackwardArcAngle = 180f; // TODO: later probably make a serialized field for easier tweaking and move to AITankController
        const float StartingForwardArcAngle = 360f - StartingBackwardArcAngle;

        int[] powerChange = new int[2];

        if (requestDir.magnitude == 0) {
            powerChange[0] = Mathf.Sign(stateInfo.LeftCurPower) > 0 ? -1 : 1;
            powerChange[1] = Mathf.Sign(stateInfo.RightCurPower) > 0 ? -1 : 1;
            return powerChange;
        }

        // First calculate forward and backwards arc angle based on speed
        float sqrMaxVelocityMag = Mathf.Pow(stateInfo.TerminalVel, 2);
        float sqrCurVelocity = stateInfo.LinearVel.sqrMagnitude;

        float ratio = Mathf.Clamp(1.0f - sqrCurVelocity / sqrMaxVelocityMag, MinRatioCutOff, MaxRatioCutoff);

        float curBackwardArcAngle = ratio * StartingBackwardArcAngle;
        float curForwardArcAngle = ratio * StartingForwardArcAngle;

        Vector2 forwardVec = stateInfo.ForwardVec;
        Vector2 backwardVec = forwardVec.Rotate(180f);

        List<Vector2> arcVectors = new List<Vector2> {
            forwardVec.Rotate(curForwardArcAngle / 2f),
            forwardVec.Rotate(-curForwardArcAngle / 2f),
            backwardVec.Rotate(curBackwardArcAngle / 2f),
            backwardVec.Rotate(-curBackwardArcAngle / 2f)
        };
        DebugManager.Instance.RegisterObject("actuation_arc_vectors", arcVectors);

        float angleDiffFromFront = Vector2.Angle(forwardVec, requestDir);
        float angleDiffFromBack = Vector2.Angle(backwardVec, requestDir);

        const float sigma = 10f; // TODO: later probably make a serialized field for easier tweaking

        // In this case we want the AI to continue accelerating while going towards the requested direction
        if ((curForwardArcAngle / 2f) >= angleDiffFromFront) {
            float angleToTurn = Vector2.SignedAngle(forwardVec, requestDir);

            if (Mathf.Abs(angleToTurn) > sigma) {
                if (Mathf.Sign(angleToTurn) > 0) {
                    powerChange[0] = 0;
                    powerChange[1] = 1;
                } else {
                    powerChange[0] = 1;
                    powerChange[1] = 0;
                }
            } else {
                powerChange[0] = 1;
                powerChange[1] = 1;
            }

            // In this case we want the tank to start accelerating backwards
        } else if ((curBackwardArcAngle / 2f) >= angleDiffFromBack) {
            float angleToTurn = Vector2.SignedAngle(backwardVec, requestDir);

            if (Mathf.Abs(angleToTurn) > sigma) {
                if (Mathf.Sign(angleToTurn) > 0) {
                    powerChange[0] = -1;
                    powerChange[1] = 0;
                } else {
                    powerChange[0] = 0;
                    powerChange[1] = -1;
                }
            } else {
                powerChange[0] = -1;
                powerChange[1] = -1;
            }

            // In this case we want the tank to start turning
        } else {
            float angleToTurnFromFront = Vector2.SignedAngle(forwardVec, requestDir);
            float angleToTurnFromBack = Vector2.SignedAngle(backwardVec, requestDir);

            bool turningToFront = Mathf.Abs(angleToTurnFromFront) <= Mathf.Abs(angleToTurnFromBack);
            float angle = turningToFront ? angleToTurnFromFront : angleToTurnFromBack;

            powerChange = CalcPowerChangeForRotation(angle);
        }

        return powerChange;
    }

    public static int[] CalcPowerChangeForRotation(float angleChange) {
        int[] powerChange = new int[2];

        if (Mathf.Sign(angleChange) >= 0) {
            powerChange[0] = -1;
            powerChange[1] = 1;
        } else {
            powerChange[0] = 1;
            powerChange[1] = -1;
        }

        return powerChange;
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
}
