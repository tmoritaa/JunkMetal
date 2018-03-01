using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class TurretPartSchematic : PartSchematic
{
    // Should be between 100 - 300
    public int Weight
    {
        get; private set;
    }

    public int Armour
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

    public float RotPerSecond
    {
        get; private set;
    }

    public TurretPartSchematic(string name, int _armour, float _rotPerSecond, int _weight, Vector2[] _weaponDirs, Vector2[] _weaponFireOffset, int[] _weaponWeightRestrict) : base(name, PartType.Turret) {
        Name = name;
        Armour = _armour;
        RotPerSecond = _rotPerSecond;
        Weight = _weight;

        OrigWeaponDirs = _weaponDirs;
        OrigWeaponFirePosOffset = _weaponFireOffset;
        WeaponWeightRestrictions = _weaponWeightRestrict;
    }

    public override string GetPartTypeString() {
        return "Turret";
    }
}
