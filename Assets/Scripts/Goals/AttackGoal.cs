using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class AttackGoal : Goal
{
    private const float TimeDiffThreshToFire = 1f;
    private const float AngleThreshToFire = 5f;

    private class WeaponMoveResults
    {
        public float timeEstimate = 99999;
        public AIAction moveAction = null;
        public Vector2 targetPos = new Vector2();
        public WeaponPart weapon;
    }

    public AttackGoal(AITankController controller) : base(controller) {}

    public override void ReInit() {
        // Do nothing.
    }

    public override void UpdateInsistence() {
        Insistence = 0;

        // Based on attackability
        // Calculate threat level of our weapons against target
        //Tank selfTank = controller.Tank;
        //Tank oppTank = controller.TargetTank;
        //foreach (WeaponPart part in selfTank.Turret.Weapons) {
        //    if (part == null) {
        //        continue;
        //    }

        //    Insistence += AIUtility.CalculateRiskValue(part, selfTank, oppTank);
        //}

        //Insistence = 1.0f - Insistence;

        Insistence = 0.8f;

        Debug.Log("Attack goal Insistence=" + Insistence);
    }

    // Three actions: move, aim, and fire
    // For each weapon, perform all move logic and calculations.
    // Move Logic:
    // First, are we in range of the weapon we're trying to fire?
    // If not, move closer to get in range.
    // If we are in range, is the weapon actually aimed at our opponent. For move logic, we'll try to orient ourselves to make aiming faster.
    // - First, calculate travel distance per second of weapon being planned to fire.
    // - Next calculate where we want to shoot based on velocity of opponent and expected travel distance of weapon.
    // - Now we have a location to shoot at.
    // Figure out which maneuver will result in the fastest aim.
    // - First perform transpose solution and come with time estimate or whether it's even possible in current orientation.
    // - Second perform rotation solution and come with time estimate for that.
    // Pick better solution, and perform GoInDir for that.
    // Since we have the time estimate for all weapons, pick one that has shortest time estimate.
    // Then, pick a weapon we plan to fire. This will be our base for all other actions.
    // Aim logic:
    // Just try to aim at calculated fire position.
    // Fire:
    // If fire travel time calculated previously is relatively small (0 - 1 second?), the weapon is in range, and the angle diff is below threshold,
    //      then fire weapon. Maybe we can scale this based on weapon reload time.
    public override AIAction[] CalcActionsToPerform() {
        List<AIAction> actions = new List<AIAction>();

        Tank tank = controller.SelfTank;
        Tank targetTank = controller.TargetTank;

        WeaponMoveResults[] results = new WeaponMoveResults[tank.Turret.Weapons.Length];
        Array.Clear(results, 0, results.Length);

        for (int i = 0; i < tank.Turret.Weapons.Length; ++i) {
            WeaponPart part = tank.Turret.Weapons[i];

            if (part == null) {
                continue;
            }

            Vector2 diffVec = (Vector2)targetTank.transform.position - part.CalculateFirePos();
            float distFromTargetToFirePos = diffVec.magnitude;

            WeaponMoveResults result = new WeaponMoveResults();
            result.weapon = part;

            WeaponPartSchematic schematic = part.Schematic;

            bool inOptimalRange = distFromTargetToFirePos < schematic.OptimalRange;

            Vector2 targetPos = AIUtility.CalculateTargetPosWithWeapon(
                part.Schematic.ShootImpulse, // NOTE: Since bullet mass is always 1, shoot impulse is directly the terminal velocity of the bullet
                part.CalculateFirePos(), 
                tank.transform.position, 
                targetTank.transform.position, 
                targetTank.Body.velocity);
            result.targetPos = targetPos;

            if (!inOptimalRange) {
                Vector2 toTargetVec = targetTank.transform.position - tank.transform.position;
                float distToTarget = toTargetVec.magnitude;

                // If not in range, just do action to get into optimal range
                float travelDist = distToTarget - schematic.OptimalRange;

                // NOTE: we're just going to calculate time to travel distance, without regard for orientation.
                // Later we might have to change this and incorporate orientation. Will have to see how it works overall.
                result.timeEstimate = travelDist / tank.TerminalVelocity;
                result.moveAction = new GoInDirAction(toTargetVec, controller);
            } else if (!part.IsFireable) {
                Vector2 toTargetVec = targetTank.transform.position - tank.transform.position;
                float distToTarget = toTargetVec.magnitude;

                // If weapon is reloading, we want to try to keep optimal distance
                float travelDist = distToTarget - schematic.OptimalRange;

                // NOTE: we're just going to calculate time to travel distance, without regard for orientation.
                // Later we might have to change this and incorporate orientation. Will have to see how it works overall.
                result.timeEstimate = Mathf.Abs(travelDist) / tank.TerminalVelocity;
                result.moveAction = new GoInDirAction(Mathf.Sign(travelDist) * toTargetVec, controller);
            } else {
                // Now we get time estimates for both transpose and rotation solution, and pick the faster one and save that.
                // First calculate the transpose solution.
                Vector2 curWeaponFireVec = part.CalculateFireVec();

                Vector2 targetMovePos;
                // Try tank moving forward, and if that doesn't intersect, try tank moving back. If both don't work, then there's no transpose solution.
                bool intersected = Vector2Extension.LineLineIntersection(tank.transform.position, tank.GetForwardVec(), targetPos, -curWeaponFireVec, out targetMovePos);
                if (!intersected) {
                    intersected = Vector2Extension.LineLineIntersection(tank.transform.position, tank.GetBackwardVec(), targetPos, -curWeaponFireVec, out targetMovePos);
                }

                float transposeTimeEstimate = -1f;
                AIAction transposeAction = null;
                if (intersected) {
                    Vector2 transVec = targetMovePos - (Vector2)tank.transform.position;
                    transposeTimeEstimate = transVec.magnitude / tank.TerminalVelocity;
                    transposeAction = new GoInDirAction(transVec.normalized, controller);
                }

                // Next calculate the rotation solution. This should always exist.
                float rotationTimeEstimate = -1f;
                AIAction rotationAction = null;

                rotationAction = new RotateAction(result.weapon.CalculateFireVec(), diffVec.normalized, controller);
                rotationTimeEstimate = tank.CalcTimeToRotate(part.CalculateFireVec(), diffVec);

                if (transposeTimeEstimate < 0 && rotationTimeEstimate < 0) {
                    Debug.LogWarning("Could not find transpose or rotation solution for attacking. Probably shouldn't happen.");
                } else if (transposeTimeEstimate < 0 && rotationTimeEstimate >= 0) {
                    result.timeEstimate = rotationTimeEstimate;
                    result.moveAction = rotationAction;
                } else if (transposeTimeEstimate >= 0 && rotationTimeEstimate < 0) {
                    result.timeEstimate = transposeTimeEstimate;
                    result.moveAction = transposeAction;
                } else {
                    result.timeEstimate = Mathf.Min(transposeTimeEstimate, rotationTimeEstimate);
                    result.moveAction = (transposeTimeEstimate < rotationTimeEstimate) ? transposeAction : rotationAction;
                }
            }

            // Add time to reload to time estimate.
            result.timeEstimate += part.CalcTimeToReloaded(); 

            results[i] = result;
        }

        // Pick smallest time estimate with action.
        WeaponMoveResults finalResult = null;
        for (int i = 0; i < results.Length; ++i) {
            WeaponMoveResults result = results[i];

            if (finalResult == null || (result != null && finalResult.timeEstimate > result.timeEstimate)) {
                finalResult = result;
            }
        }

        if (finalResult != null) {
            actions.Add(finalResult.moveAction);
        } else {
            Debug.LogWarning("No move action picked.");
        }

        Vector2 aimVec = finalResult.targetPos - (Vector2)tank.transform.position;
        actions.Add(new AimWithWeaponAction(aimVec, finalResult.weapon, controller));

        Vector2 curFireVec = finalResult.weapon.CalculateFireVec();
        Ray ray = new Ray(finalResult.weapon.CalculateFirePos(), curFireVec);
        float shortestDist = Vector3.Cross(ray.direction, (Vector3)(finalResult.targetPos) - ray.origin).magnitude;
        bool canHitIfFired = shortestDist < targetTank.Hull.Schematic.Size.x;

        Vector2 targetVec = (Vector2)targetTank.transform.position - finalResult.weapon.CalculateFirePos();

        bool fireVecFacingTarget = Vector2.Angle(curFireVec, targetVec) < 90f;
        bool inRange = targetVec.magnitude < finalResult.weapon.Schematic.Range;
        if (inRange &&  canHitIfFired && fireVecFacingTarget) {
            actions.Add(new FireWeaponAction(finalResult.weapon.TurretIdx, controller));
        }

        return actions.ToArray();
    }
}
