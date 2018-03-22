using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class TankStateInfo
{
    public Tank OwningTank
    {
        get; private set;
    }

    public Vector2 ForwardVec
    {
        get {
            return new Vector2(0, 1).Rotate(Rot);
        }
    }

    public float TerminalVel
    {
        get {
            return ((EnergyPower / LinearDrag)) / Mass;
        }
    }

    public float LeftCurPower;

    public float RightCurPower;

    public Vector2 Pos;

    public Vector2 LinearVel;

    public float AngularVel;

    public float Rot;

    public float Mass
    {
        get; private set;
    }

    public float LinearDrag
    {
        get; private set;
    }

    public float AngularDrag
    {
        get; private set;
    }

    public float Inertia
    {
        get; private set;
    }

    public float EnergyPower
    {
        get; private set;
    }

    public Vector2 Size
    {
        get; private set;
    }

    public TankStateInfo(Tank tank) {
        OwningTank = tank;
        LeftCurPower = tank.Hull.LeftCurPower;
        RightCurPower = tank.Hull.RightCurPower;
        Mass = tank.Body.mass;
        Pos = tank.Body.position;
        LinearVel = tank.Body.velocity;
        LinearDrag = tank.Body.drag;
        AngularVel = tank.Body.angularVelocity;
        AngularDrag = tank.Body.angularDrag;
        Rot = tank.Body.rotation;
        Inertia = tank.Body.inertia;
        EnergyPower = tank.Hull.Schematic.EnergyPower;
        Size = tank.Hull.Schematic.Size;
    }

    public TankStateInfo(TankStateInfo stateInfo) {
        OwningTank = stateInfo.OwningTank;
        LeftCurPower = stateInfo.LeftCurPower;
        RightCurPower = stateInfo.RightCurPower;
        Mass = stateInfo.Mass;
        Pos = stateInfo.Pos;
        LinearVel = stateInfo.LinearVel;
        LinearDrag = stateInfo.LinearDrag;
        AngularVel = stateInfo.AngularVel;
        AngularDrag = stateInfo.AngularDrag;
        Rot = stateInfo.Rot;
        Inertia = stateInfo.Inertia;
        EnergyPower = stateInfo.EnergyPower;
        Size = stateInfo.Size;
    }

    public Vector2 CalculateFireVecOfWeapon(WeaponPart part) {
        return OwningTank.Hull.Schematic.OrigWeaponDirs[part.EquipIdx].Rotate(Rot);
    }

    public float CalcTimeToRotate(Vector2 from, Vector2 to) {
        float rotationAngle = Vector2.Angle(from, to);

        float r = Size.x / 2f;
        float f = EnergyPower;
        float torque = r * f;
        float angularDrag = AngularDrag;

        float angularAccel = torque / Inertia * Mathf.Rad2Deg;

        float newVel = AngularVel;

        float angle = Vector2.SignedAngle(ForwardVec, to);

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
        Vector2 desiredDir = targetPos - Pos;

        float curVel = LinearVel.magnitude;
        float angle = Vector2.Angle(LinearVel, desiredDir);
        if (angle >= 90) {
            curVel *= -1;
        }

        float f = EnergyPower;
        float m = Mass;
        float drag = LinearDrag;
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
}
