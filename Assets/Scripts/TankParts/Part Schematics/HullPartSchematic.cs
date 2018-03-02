using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class HullPartSchematic : PartSchematic
{
    // Should be between 100 - 400
    public int Weight
    {
        get; private set;
    }

    public Vector2 Size
    {
        get; private set;
    }

    public int Armour
    {
        get; private set;
    }

    // Should be between 500000 - 1000000
    public int EnergyPower
    {
        get; private set;
    }

    public HullPartSchematic(string name, int armour, Vector2 size, int energyPower, int weight) : base(name, PartType.Hull) {

        Name = name;
        Armour = armour;
        Size = size;
        EnergyPower = energyPower;
        Weight = weight;
    }

    public override string GetStatString(PartSchematic diffSchem) {
        bool showDiff = diffSchem != null;

        string retStr = string.Empty;

        if (showDiff) {
            HullPartSchematic diffHull = (HullPartSchematic)diffSchem;

            retStr = string.Format("{0}\nArmour: {1} => {2}\nSize: {3} => {4}\nEnergy Power: {5} => {6}\n Weight: {7} => {8}",
                Name, diffHull.Armour, Armour, diffHull.Size, Size, diffHull.EnergyPower, EnergyPower, diffHull.Weight, Weight);
        } else {
            retStr = string.Format("{0}\nArmour: {1}\nSize: {2}\nEnergy Power: {3}\n Weight: {4}",
                Name, Armour, Size, EnergyPower, Weight);
        }

        return retStr;
    }
}
