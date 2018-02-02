using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class TurretPart
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

    public float Angle
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

    private Tank owningTank;

    private float rotPerTimeStep;

    private KeyCode leftTurnKey;
    private KeyCode rightTurnKey;

    private float rotDir = 0;
    
    private WeaponPart[] weapons;

    public TurretPart(Tank _tank, float _rotPerTimestep, float _weight, Vector2[] _weaponDirs, float[] _weaponWeightRestrict, KeyCode _leftTurnKey, KeyCode _rightTurnKey) {
        owningTank = _tank;
        rotPerTimeStep = _rotPerTimestep;
        leftTurnKey = _leftTurnKey;
        rightTurnKey = _rightTurnKey;
        Weight = _weight;
        Angle = 0;

        OrigWeaponDirs = _weaponDirs;
        WeaponWeightRestrictions = _weaponWeightRestrict;

        weapons = new WeaponPart[OrigWeaponDirs.Length];
        Array.Clear(weapons, 0, weapons.Length);
    }

    public void AddWeaponAtIdx(WeaponPart weapon, int idx) {
        if (weapon.Weight <= WeaponWeightRestrictions[idx]) {
            weapon.TurretIdx = idx;
            weapons[idx] = weapon;
        }
    }

    public void HandleInput() {
        if (Input.GetKey(leftTurnKey)) {
            rotDir = 1.0f;
        } else if (Input.GetKey(rightTurnKey)) {
            rotDir = -1.0f;
        } else {
            rotDir = 0;
        }

        foreach (WeaponPart weapon in weapons) {
            if (weapon != null) {
                weapon.HandleInput();
            }
        }
    }

    public void PerformFixedUpdate() {
        if (Mathf.Abs(rotPerTimeStep) > 0 && Math.Abs(rotDir) > 0) {
            float angle = rotDir * rotPerTimeStep;
            owningTank.TurretGO.transform.Rotate(new Vector3(0, 0, angle));
            Angle += angle;
        }

        foreach (WeaponPart weapon in weapons) {
            if (weapon != null) {
                weapon.PerformFixedUpdate();
            }
        }
    }
}
