using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class TurretPart
{
    public TurretPartSchematic Schematic
    {
        get; private set;
    }

    public float Angle
    {
        get; private set;
    }

    private Tank owningTank;

    private float rotDir = 0;
    
    private WeaponPart[] weapons;

    private KeyCode[] shootKeys;

    public TurretPart(TurretPartSchematic schematic) {
        Schematic = schematic;

        Angle = 0;
        weapons = new WeaponPart[Schematic.OrigWeaponDirs.Length];
        Array.Clear(weapons, 0, weapons.Length);
    }

    public void SetOwner(Tank tank) {
        owningTank = tank;

        foreach(WeaponPart weapon in weapons) {
            if (weapon != null) {
                weapon.SetOwner(tank);
            }
        }
    }

    public void AddWeaponAtIdx(WeaponPart weapon, int idx) {
        if (weapon.Schematic.Weight <= Schematic.WeaponWeightRestrictions[idx]) {
            weapon.TurretIdx = idx;
            weapons[idx] = weapon;
        }
    }

    public void HandleInput() {
        if (Input.GetKey(Schematic.LeftTurnKey)) {
            rotDir = 1.0f;
        } else if (Input.GetKey(Schematic.RightTurnKey)) {
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
        if (Mathf.Abs(Schematic.RotPerTimeStep) > 0 && Math.Abs(rotDir) > 0) {
            float angle = rotDir * Schematic.RotPerTimeStep;
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
