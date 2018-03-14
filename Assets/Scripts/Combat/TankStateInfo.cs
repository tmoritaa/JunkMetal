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
        return OwningTank.Turret.Schematic.OrigWeaponDirs[part.TurretIdx].Rotate(Rot); // TODO: this assumes tank loses turret rotation functionality
    }
}
