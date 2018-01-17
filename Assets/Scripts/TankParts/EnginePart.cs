using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class EnginePart
{
    public float MoveForce
    {
        get; private set;
    }

    public float WheelEnergyInc
    {
        get; private set;
    }

    public float WheelEnergyDec
    {
        get; private set;
    }

    public EnginePart(float moveForce, float wheelEnergyInc, float wheelEnergyDec) {
        MoveForce = moveForce;
        WheelEnergyInc = wheelEnergyInc;
        WheelEnergyDec = wheelEnergyDec;
    }
}
