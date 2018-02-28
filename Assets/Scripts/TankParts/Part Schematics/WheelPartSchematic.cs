using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

public class WheelPartSchematic : PartSchematic
{
    // Should be between 50 - 200
    public int Weight
    {
        get; private set;
    }

    public float EnergyInc
    {
        get; private set;
    }

    public float EnergyDec
    {
        get; private set;
    }

    public WheelPartSchematic(string name, float _energyInc, float _energyDec, int _weight) : base(name) {
        Name = name;
        EnergyInc = _energyInc;
        EnergyDec = _energyDec;
        Weight = _weight;
    }

    public override string GetPartTypeString() {
        return "Wheels";
    }
}
