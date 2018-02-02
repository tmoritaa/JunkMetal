using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class TurretPartSchematic : PartSchematic
{
    // Should be between 100 - 300
    public float Weight
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

    public float[] WeaponWeightRestrictions
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

    public TurretPartSchematic(string name, int _armour, float _rotPerTimestep, float _weight, Vector2[] _weaponDirs, float[] _weaponWeightRestrict, KeyCode _leftTurnKey, KeyCode _rightTurnKey) : base(name) {
        Name = name;
        Armour = _armour;
        RotPerTimeStep = _rotPerTimestep;
        LeftTurnKey = _leftTurnKey;
        RightTurnKey = _rightTurnKey;
        Weight = _weight;

        OrigWeaponDirs = _weaponDirs;
        WeaponWeightRestrictions = _weaponWeightRestrict;
    }
}
