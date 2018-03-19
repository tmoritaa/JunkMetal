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

    public Vector2 Size
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

    public Vector2[] OrigWeaponFirePosOffset
    {
        get; private set;
    }

    public int[] WeaponWeightRestrictions
    {
        get; private set;
    }

    public HullPartSchematic(string name, int armour, Vector2 size, int energyPower, int weight, Vector2[] _weaponDirs, Vector2[] _weaponFireOffset, int[] _weaponWeightRestrict) : base(name, PartType.Hull) {
        Name = name;
        Armour = armour;
        Size = size;
        EnergyPower = energyPower;
        Weight = weight;
        OrigWeaponDirs = _weaponDirs;
        OrigWeaponFirePosOffset = _weaponFireOffset;
        WeaponWeightRestrictions = _weaponWeightRestrict;
    }

    public override string GetStatString(PartSchematic diffSchem) {
        bool showDiff = diffSchem != null;

        string retStr = string.Empty;

        if (showDiff) {
            HullPartSchematic diffHull = (HullPartSchematic)diffSchem;

            string diffWeightRestrictStr = "[" + String.Join(", ", new List<int>(diffHull.WeaponWeightRestrictions).ConvertAll(i => i.ToString()).ToArray()) + "]";
            string weightRestrictStr = "[" + String.Join(", ", new List<int>(WeaponWeightRestrictions).ConvertAll(i => i.ToString()).ToArray()) + "]";

            retStr = string.Format("{0}\nArmour: {1} => {2}\nSize: {3} => {4}\nEnergy Power: {5} => {6}\n Weight: {7} => {8}\nWeapon Weight Restrictions: {9} => {10}",
                Name, diffHull.Armour, Armour, diffHull.Size, Size, diffHull.EnergyPower, EnergyPower, diffHull.Weight, Weight, diffWeightRestrictStr, weightRestrictStr);
        } else {
            string weightRestrictStr = "[" + String.Join(", ", new List<int>(WeaponWeightRestrictions).ConvertAll(i => i.ToString()).ToArray()) + "]";
            retStr = string.Format("{0}\nArmour: {1}\nSize: {2}\nEnergy Power: {3}\n Weight: {4}\nWeapon Weight Restrictions: {5}",
                Name, Armour, Size, EnergyPower, Weight, weightRestrictStr);
        }

        return retStr;
    }
}
