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

    public WeaponPart[] Weapons
    {
        get; private set;
    }

    private Tank owningTank;

    private float rotationDir = 0;

    private KeyCode[] shootKeys;

    public TurretPart(TurretPartSchematic schematic) {
        Schematic = schematic;

        Angle = 0;
        Weapons = new WeaponPart[Schematic.OrigWeaponDirs.Length];
        Array.Clear(Weapons, 0, Weapons.Length);
    }

    public void SetOwner(Tank tank) {
        owningTank = tank;

        foreach(WeaponPart weapon in GetAllWeapons()) {
            weapon.SetOwner(tank);
        }
    }

    public List<WeaponPart> GetAllWeapons() {
        List<WeaponPart> weapons = new List<WeaponPart>();
        foreach (WeaponPart part in Weapons) {
            if (part != null) {
                weapons.Add(part);
            }
        }

        return weapons;
    }

    public WeaponPart GetWeaponAtIdx(int idx) {
        if (Weapons.Length <= idx) {
            return null;
        }

        return Weapons[idx];
    }

    public void AddWeaponAtIdx(WeaponPart weapon, int idx) {
        if (weapon.Schematic.Weight <= Schematic.WeaponWeightRestrictions[idx]) {
            weapon.TurretIdx = idx;
            Weapons[idx] = weapon;
        }
    }

    public void HandleInput() {
        float rotDir = 0;

        if (Input.GetKey(Schematic.LeftTurnKey)) {
            rotDir = 1.0f;
        } else if (Input.GetKey(Schematic.RightTurnKey)) {
            rotDir = -1.0f;
        }
        SetRotDir(rotDir);

        foreach (WeaponPart weapon in GetAllWeapons()) {
            weapon.HandleInput();
        }
    }

    public void SetRotDir(float f) {
        rotationDir = f;
    }

    public void PerformFixedUpdate() {
        if (Mathf.Abs(Schematic.RotPerTimeStep) > 0 && Math.Abs(rotationDir) > 0) {
            float angle = rotationDir * Schematic.RotPerTimeStep;
            owningTank.TurretGO.transform.Rotate(new Vector3(0, 0, angle));
            Angle += angle;
        }

        foreach (WeaponPart weapon in GetAllWeapons()) {
            weapon.PerformFixedUpdate();
        }
    }
}
