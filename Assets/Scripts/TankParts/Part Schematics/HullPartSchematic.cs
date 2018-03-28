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

    public Vector2 Size
    {
        get; private set;
    }

    public float AngularDrag
    {
        get; private set;
    }

    public Vector2[] OrigWeaponDirs
    {
        get; private set;
    }

    public Vector2[] OrigWeaponPos
    {
        get; private set;
    }

    public WeaponTier[] WeaponTierRestrictions
    {
        get; private set;
    }

    public HullPartSchematic(string name, int armour, int energyPower, Vector2 size, int weight, float angularDrag, Vector2[] _weaponDirs, Vector2[] _weaponPos, WeaponTier[] _weaponWeightRestrict) : base(name, PartType.Hull) {
        Name = name;
        Armour = armour;
        EnergyPower = energyPower;
        Weight = weight;
        Size = size;
        AngularDrag = angularDrag;
        OrigWeaponDirs = _weaponDirs;
        OrigWeaponPos = _weaponPos;
        WeaponTierRestrictions = _weaponWeightRestrict;
    }

    public override string GetStatString(PartSchematic diffSchem) {
        bool showDiff = diffSchem != null;

        string retStr = string.Empty;

        if (showDiff) {
            HullPartSchematic diffHull = (HullPartSchematic)diffSchem;

            string diffWeightRestrictStr = "(" + String.Join(", ", new List<WeaponTier>(diffHull.WeaponTierRestrictions).ConvertAll(i => i.ToString()).ToArray()) + ")";
            string weightRestrictStr = "(" + String.Join(", ", new List<WeaponTier>(WeaponTierRestrictions).ConvertAll(i => i.ToString()).ToArray()) + ")";

            string armorStr = string.Format("Armour: {0} => {2}{1}</color>", diffHull.Armour, Armour, getColorBasedChangeInVal(diffHull.Armour, Armour));
            string energyPowerStr = string.Format("Move Force: {0} => {2}{1}</color>", diffHull.EnergyPower, EnergyPower, getColorBasedChangeInVal(diffHull.EnergyPower, EnergyPower));
            string weightStr = string.Format("Weight: {0} => {2}{1}</color>", diffHull.Weight, Weight, getColorBasedChangeInVal(diffHull.Weight, Weight, false));
            string angularDragStr = string.Format("Angular Drag: {0} => {2}{1}</color>", diffHull.AngularDrag, AngularDrag, getColorBasedChangeInVal(diffHull.AngularDrag, AngularDrag, false));

            retStr = string.Format("{0}\n{1}\n{2}\n{3}\n{4}\nWeapon Weight Restrictions: {5} => {6}",
                Name, armorStr, energyPowerStr, weightStr, angularDragStr, diffWeightRestrictStr, weightRestrictStr);
        } else {
            string weightRestrictStr = "[" + String.Join(", ", new List<WeaponTier>(WeaponTierRestrictions).ConvertAll(i => i.ToString()).ToArray()) + "]";
            retStr = string.Format("{0}\nArmour: {1}\nEnergy Power: {2}\n Weight: {3}\nAngular Drag: {4}\nWeapon Weight Restrictions: {5}",
                Name, Armour, EnergyPower, Weight, AngularDrag, weightRestrictStr);
        }

        return retStr;
    }
}
