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

    public float RotPerTimeStep
    {
        get; private set;
    }

    public KeyCode LeftTurnKey
    {
        get; private set;
    }

    public KeyCode RightTurnKey
    {
        get; private set;
    }

    public KeyCode[] ShootKeys
    {
        get; private set;
    }

    public TurretPartSchematic(string name, int _armour, float _rotPerTimestep, int _weight, Vector2[] _weaponDirs, Vector2[] _weaponFireOffset, int[] _weaponWeightRestrict, KeyCode _leftTurnKey, KeyCode _rightTurnKey, KeyCode[] _shootKeys) : base(name) {
        Name = name;
        Armour = _armour;
        RotPerTimeStep = _rotPerTimestep;
        LeftTurnKey = _leftTurnKey;
        RightTurnKey = _rightTurnKey;
        ShootKeys = _shootKeys;
        Weight = _weight;

        OrigWeaponDirs = _weaponDirs;
        OrigWeaponFirePosOffset = _weaponFireOffset;
        WeaponWeightRestrictions = _weaponWeightRestrict;
    }
}
