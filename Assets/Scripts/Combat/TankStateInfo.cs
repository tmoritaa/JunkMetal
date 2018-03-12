using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public struct TankStateInfo
{
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

    public TankStateInfo(float[] curPower, float mass, Vector2 pos, Vector2 linearVel, float rot, float linearDrag, float angularVel, float angularDrag, float inertia, float energyPower, Vector2 size) {
        LeftCurPower = curPower[0];
        RightCurPower = curPower[1];
        Mass = mass;
        Pos = pos;
        LinearVel = linearVel;
        LinearDrag = linearDrag;
        AngularVel = angularVel;
        AngularDrag = angularDrag;
        Rot = rot;
        Inertia = inertia;
        EnergyPower = energyPower;
        Size = size;
    }

    public TankStateInfo(Tank tank) {
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
}
