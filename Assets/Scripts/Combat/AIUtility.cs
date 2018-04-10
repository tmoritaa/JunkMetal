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
    
    public static float CalcTimeToHitPos(Vector2 curPos, Vector2 curFireVec, TankStateInfo aimingTankInfo, WeaponPartSchematic schematic, Vector2 targetPos) {
        float timeToHit = 0;

        Vector2 diffVec = targetPos - curPos;

        // We have to calculate three things: rotation time, travel time to in range, and bullet travel time.

        // First calculate rotation time.
        timeToHit += aimingTankInfo.CalcTimeToRotate(curFireVec, diffVec);

        // Next, calculate travel time.
        float moveAmount = Mathf.Max(diffVec.magnitude - schematic.Range, 0);
        if (moveAmount > 0) {
            Vector2 newPos = diffVec.normalized * moveAmount + curPos;
            timeToHit += aimingTankInfo.CalcTimeToReachPosWithNoRot(newPos);
        }

        // Finally, calculate bullet travel time.
        timeToHit += Mathf.Min(schematic.Range, diffVec.magnitude) / schematic.ShootImpulse;

        return timeToHit;
    }

    public static float CalcTimeToReachPos(TankStateInfo tankInfo, Vector2 targetPos) {
        Vector2 curPos = tankInfo.Pos;

        Vector2 diffVec = targetPos - curPos;

        float timeToReach = 0;

        timeToReach += Mathf.Min(tankInfo.CalcTimeToRotate(tankInfo.ForwardVec, diffVec), tankInfo.CalcTimeToRotate(tankInfo.ForwardVec.Rotate(180f), diffVec));
        timeToReach += tankInfo.CalcTimeToReachPosWithNoRot(targetPos);

        return timeToReach;
    }

    public static TankStateInfo CalcPosInFutureWithRequestedPowerChange(int[] powerChange, float timeInSecs, TankStateInfo tankInfo, out List<Vector2> passedPos) {
        Vector2 forwardVec = tankInfo.ForwardVec;

        if (powerChange[0] == powerChange[1]) {
            forwardVec *= powerChange[0];
        } else if (powerChange[0] == 0 || powerChange[1] == 0) {
            if (powerChange[0] != 0) {
                forwardVec = forwardVec.Rotate(Mathf.Sign(powerChange[0]) > 0 ? -45f : -135f);
            } else {
                forwardVec = forwardVec.Rotate(Mathf.Sign(powerChange[1]) > 0 ? 45f : 135f);
            }
        } else {
            if (powerChange[0] > 0) {
                forwardVec = forwardVec.Rotate(-90f);
            } else {
                forwardVec = forwardVec.Rotate(90f);
            }
        }

        return CalcPosInFutureWithRequestedDir(forwardVec, timeInSecs, tankInfo, out passedPos);
    }

    public static TankStateInfo CalcPosInFutureWithRequestedDirJets(Vector2 _requestDir, float timeInSecs, TankStateInfo tankInfo, out List<Vector2> passedPos) {
        TankStateInfo resultInfo = new TankStateInfo(tankInfo);
        Vector2 requestDir = _requestDir;

        float elapsedTime = 0;

        float dt = Time.fixedDeltaTime;

        List<Vector2> allPos = new List<Vector2>();
        allPos.Add(resultInfo.Pos);

        float jetImpulseMag = resultInfo.OwningTank.Hull.Schematic.JetImpulse;
        resultInfo.LinearVel += requestDir.normalized * (jetImpulseMag / resultInfo.Mass);

        while (elapsedTime <= timeInSecs) {
            elapsedTime += dt;

            // Pos update
            {
                float m = resultInfo.Mass;
                float drag = resultInfo.LinearDrag;
                resultInfo.LinearVel = resultInfo.LinearVel * (1f / (1f + drag * dt));
                resultInfo.Pos += resultInfo.LinearVel * dt;

                allPos.Add(resultInfo.Pos);
            }
        }

        passedPos = allPos;

        return resultInfo;
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

    // TODO: In general, later make serialized field if these values feel like they need tweaking
    public static int[] CalcPowerChangeBasedOnRequestDir(Vector2 requestDir, TankStateInfo stateInfo) {
        const float MinRatioCutOff = 0.4f;
        const float MaxRatioCutoff = 0.7f; 

        const float StartingBackwardArcAngle = 180f;
        const float StartingForwardArcAngle = 360f - StartingBackwardArcAngle;

        int[] powerChange = new int[2];

        if (requestDir.magnitude == 0) {
            if (stateInfo.LeftCurPower != 0) {
                powerChange[0] = Mathf.Sign(stateInfo.LeftCurPower) > 0 ? -1 : 1;
            }

            if (stateInfo.RightCurPower != 0) {
                powerChange[1] = Mathf.Sign(stateInfo.RightCurPower) > 0 ? -1 : 1;
            }
            
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
        CombatDebugHandler.Instance.RegisterObject("actuation_arc_vectors", arcVectors);

        float angleDiffFromFront = Vector2.Angle(forwardVec, requestDir);
        float angleDiffFromBack = Vector2.Angle(backwardVec, requestDir);

        const float sigma = 10f;

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

    public static void SmoothPath(Map map, List<Node> path, Tank tank) {
        const int WallBit = 8;
        const int PlayerBit = 9;
        const int LayerMask = 1 << WallBit | 1 << PlayerBit;

        int removeCount = 0;
        for (int i = 0; i < path.Count; ++i) {
            Node node = path[i];

            Vector2 leftVec = tank.GetForwardVec().Rotate(-90).normalized;
            Vector2 rightVec = tank.GetForwardVec().Rotate(90).normalized;

            Vector2 pos = map.NodeToPosition(node);
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

    public static List<LookaheadNode> FilterByPathNotObstructed(List<LookaheadNode> nodes) {
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

    public static List<LookaheadNode> FilterByDestNotObstructed(List<LookaheadNode> nodes, Map map) {
        List<LookaheadNode> filteredNode = new List<LookaheadNode>();

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

    public static List<LookaheadNode> FilterByPassingBullet(List<LookaheadNode> nodes, Map map) {
        List<LookaheadNode> filteredNodes = new List<LookaheadNode>();

        foreach (LookaheadNode node in nodes) {
            Vector2 startPos = map.NodeToPosition(node.PassedNodes[0]);
            Vector2 endPos = node.TankInfo.Pos;

            bool hitSomething = false;
            foreach (Bullet bullet in BulletInstanceHandler.Instance.BulletInstances) {
                Vector2 bulletPos = bullet.transform.position;

                if (bulletPos.x < Mathf.Min(startPos.x, endPos.x) || bulletPos.x > Mathf.Max(startPos.x, endPos.x)
                    || bulletPos.y < Mathf.Min(startPos.y, endPos.y) || bulletPos.y > Mathf.Max(startPos.y, endPos.y)) {
                    continue;
                }

                BoxCollider2D bulletCollider = bullet.Collider;
                Vector2 bulletLBPos = bulletPos + bulletCollider.offset - (bulletCollider.size / 2f);
                Vector2 bulletRTPos = bulletPos + bulletCollider.offset + (bulletCollider.size / 2f);
                foreach (Node mapNode in node.PassedNodes) {
                    Vector2 mapNodePos = map.NodeToPosition(mapNode);

                    if (mapNodePos.x < Mathf.Min(bulletLBPos.x, bulletRTPos.x) || mapNodePos.x > Mathf.Max(bulletLBPos.x, bulletRTPos.x)
                    || mapNodePos.y < Mathf.Min(bulletLBPos.y, bulletRTPos.y) || mapNodePos.y > Mathf.Max(bulletLBPos.y, bulletRTPos.y)) {
                        continue;
                    } else {
                        hitSomething = true;
                        break;
                    }
                }

                if (hitSomething) {
                    break;
                }
            }

            if (!hitSomething) {
                filteredNodes.Add(node);
            }
        }

        if (filteredNodes.Count == 0) {
            filteredNodes = nodes;
        }

        return filteredNodes;
    }

    public static bool IsInOpponentFireVec(TankStateInfo selfTank, TankStateInfo oppTank, List<WeaponPart> oppWeapons) {
        bool canBeHit = false;

        foreach (WeaponPart weapon in oppWeapons) {
            if (weapon.CalcTimeToReloaded() > 0.25f) {
                continue;
            }

            Vector2 targetPos = selfTank.Pos;
            Vector2 curFireVec = weapon.CalculateFireVec();
            Ray ray = new Ray(weapon.CalculateFirePos(), curFireVec);
            float shortestDist = Vector3.Cross(ray.direction, (Vector3)(targetPos) - ray.origin).magnitude;
            bool canHitIfFired = shortestDist < oppTank.Size.x;

            Vector2 targetVec = targetPos - weapon.CalculateFirePos();

            bool fireVecFacingTarget = Vector2.Angle(curFireVec, targetVec) < 90f;
            bool inRange = targetVec.magnitude < weapon.Schematic.Range;
            if (inRange && canHitIfFired && fireVecFacingTarget) {
                canBeHit = true;
                break;
            }
        }

        return canBeHit;
    }

    public static List<LookaheadNode> FilterByAwayFromWall(List<LookaheadNode> nodes, TankStateInfo selfTankInfo) {
        List<LookaheadNode> filteredNodes = new List<LookaheadNode>();

        List<Vector2> hitWallDirs;
        bool closeToWall = checkIfCloseToWall(selfTankInfo, out hitWallDirs);

        if (closeToWall) {
            foreach (LookaheadNode node in nodes) {
                Vector2 dir = node.GetNodeOneStepAfterRoot().IncomingDir;

                bool isHitWallDir = false;
                foreach (Vector2 hitWallDir in hitWallDirs) {
                    float angle = Vector2.Angle(dir, hitWallDir);

                    if (angle < 90f) {
                        isHitWallDir = true;
                        break;
                    }
                }

                if (!isHitWallDir) {
                    filteredNodes.Add(node);
                }
            }
        }

        if (filteredNodes.Count == 0) {
            filteredNodes = nodes;
        }

        return filteredNodes;
    }

    public static int CalcMinTimeForAimerToHitAimee(TankStateInfo aimingTankInfo, TankStateInfo aimeeTankInfo, List<WeaponPart> aimerWeapons, out WeaponPart outWeapon) {
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

    private static int convertFloatSecondToIntCentiSecond(float time) {
        return Mathf.RoundToInt(time * 100);
    }

    private static bool checkIfCloseToWall(TankStateInfo selfTankInfo, out List<Vector2> wallDirections) {
        wallDirections = new List<Vector2>();

        Vector2 centerPt = selfTankInfo.Pos;

        Vector2[] checkDirs = new Vector2[] { new Vector2(0, 1), new Vector2(0, -1), new Vector2(1, 0), new Vector2(-1, 0) };

        foreach (Vector2 dir in checkDirs) {
            RaycastHit2D hitResult = Physics2D.Raycast(centerPt, dir, selfTankInfo.TerminalVel / 2f);

            if (hitResult.collider != null) {
                wallDirections.Add(dir);
            }
        }

        return wallDirections.Count > 0;
    }
}
