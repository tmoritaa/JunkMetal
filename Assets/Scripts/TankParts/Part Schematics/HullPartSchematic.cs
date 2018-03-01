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

    public override string GetPartTypeString() {
        return "Hull";
    }
}
