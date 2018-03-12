using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public partial class Tank
{
    public void PerformActuation(Vector2 requestDir) {
        int[] powerChange = AIUtility.CalcPowerChangeBasedOnRequestDir(requestDir, new TankStateInfo(this));

        this.Hull.PerformPowerChange(powerChange[0], powerChange[1]);
    }
    
    public void PerformRotation(Vector2 alignAngle, Vector2 requestDir) {
        float angle = Vector2.SignedAngle(alignAngle, requestDir);

        int[] powerChange = AIUtility.CalcPowerChangeForRotation(angle);
        Hull.PerformPowerChange(powerChange[0], powerChange[1]);
    }

    public float CalcTimeToRotate(Vector2 from, Vector2 to) {
        float rotationAngle = Vector2.Angle(from, to);

        float r = Hull.Schematic.Size.x / 2f;
        float f = Hull.Schematic.EnergyPower;
        float torque = r * f;
        float angularDrag = body.angularDrag;

        float angularAccel = torque / body.inertia * Mathf.Rad2Deg;

        float newVel = body.angularVelocity;

        float angle = Vector2.SignedAngle(GetForwardVec(), to);

        newVel *= Mathf.Sign(angle);

        float dt = Time.fixedDeltaTime;
        float totalDt = 0;

        float angleToCover = rotationAngle;
        while (angleToCover > 0) {
            totalDt += dt;
            newVel = (newVel + angularAccel * dt) * (1f / (1f + angularDrag * dt));
            angleToCover -= newVel * dt;
        }

        return totalDt;
    }

    public float CalcTimeToReachPosWithNoRot(Vector2 targetPos) {
        Vector2 desiredDir = targetPos - (Vector2)this.transform.position;

        float curVel = Body.velocity.magnitude;
        float angle = Vector2.Angle(Body.velocity, desiredDir);
        if (angle >= 90) {
            curVel *= -1;
        }

        float f = Hull.Schematic.EnergyPower;
        float m = body.mass;
        float drag = body.drag;
        float a = f / m;

        float newVel = curVel;
        float dt = Time.fixedDeltaTime;
        float totalDt = 0;

        float distToTarget = desiredDir.magnitude;
        while (distToTarget > 0) {
            totalDt += dt;
            newVel = (newVel + a * dt) * (1f / (1f + drag * dt));
            distToTarget -= newVel * dt;
        }

        return totalDt;
    }

    public float CalcAvgOptimalRange() {
        float totalRange = 0;
        int count = 0;
        foreach (WeaponPart part in Turret.GetAllWeapons()) {
            totalRange += part.Schematic.OptimalRange;
            count += 1;
        }

        return totalRange / count;
    }
}
