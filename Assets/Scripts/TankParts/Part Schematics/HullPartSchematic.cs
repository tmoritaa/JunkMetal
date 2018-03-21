using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class HullPartSchematic : PartSchematic
{
    public int Weight
    {
        get; private set;
    }

    public int Armour
    {
        get; private set;
    }

    public int EnergyPower
    {
        get; private set;
    }

    public Vector2[] OrigWeaponDirs
    {
        get; private set;
    }

    public int[] WeaponWeightRestrictions
    {
        get; private set;
    }

    public HullPartSchematic(string name, int armour, int energyPower, int weight, Vector2[] _weaponDirs, int[] _weaponWeightRestrict) : base(name, PartType.Hull) {
        Name = name;
        Armour = armour;
        EnergyPower = energyPower;
        Weight = weight;
        OrigWeaponDirs = _weaponDirs;
        WeaponWeightRestrictions = _weaponWeightRestrict;
    }

    public override string GetStatString(PartSchematic diffSchem) {
        bool showDiff = diffSchem != null;

        string retStr = string.Empty;

        if (showDiff) {
            HullPartSchematic diffHull = (HullPartSchematic)diffSchem;

            string diffWeightRestrictStr = "[" + String.Join(", ", new List<int>(diffHull.WeaponWeightRestrictions).ConvertAll(i => i.ToString()).ToArray()) + "]";
            string weightRestrictStr = "[" + String.Join(", ", new List<int>(WeaponWeightRestrictions).ConvertAll(i => i.ToString()).ToArray()) + "]";

            retStr = string.Format("{0}\nArmour: {1} => {2}\nEnergy Power: {3} => {4}\n Weight: {5} => {6}\nWeapon Weight Restrictions: {7} => {8}",
                Name, diffHull.Armour, Armour, diffHull.EnergyPower, EnergyPower, diffHull.Weight, Weight, diffWeightRestrictStr, weightRestrictStr);
        } else {
            string weightRestrictStr = "[" + String.Join(", ", new List<int>(WeaponWeightRestrictions).ConvertAll(i => i.ToString()).ToArray()) + "]";
            retStr = string.Format("{0}\nArmour: {1}\nEnergy Power: {2}\n Weight: {3}\nWeapon Weight Restrictions: {4}",
                Name, Armour, EnergyPower, Weight, weightRestrictStr);
        }

        return retStr;
    }
}
