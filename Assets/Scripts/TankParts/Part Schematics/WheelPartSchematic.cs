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

    public WheelPartSchematic(string name, float _energyInc, float _energyDec, int _weight) : base(name, PartType.Wheels) {
        Name = name;
        EnergyInc = _energyInc;
        EnergyDec = _energyDec;
        Weight = _weight;
    }

    public override string GetStatString(PartSchematic diffSchem) {
        bool showDiff = diffSchem != null;

        string retStr = string.Empty;

        if (showDiff) {
            WheelPartSchematic diffHull = (WheelPartSchematic)diffSchem;

            retStr = string.Format("{0}\nEnergy Inc.: {1} => {2}\nEnergy Dec.: {3} => {4}\nWeight: {5} => {6}",
                Name, diffHull.EnergyInc, EnergyInc, diffHull.EnergyDec, EnergyDec, diffHull.Weight, Weight);
        } else {
            retStr = string.Format("{0}\nEnergy Inc.: {1}\nEnergy Dec.: {2}\nWeight: {3}",
                Name, EnergyInc, EnergyDec, Weight);
        }

        return retStr;
    }
}
