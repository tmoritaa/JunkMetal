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

    private WeaponPart[] weapons;

    private Tank owningTank;

    private float rotationDir = 0;

    public TurretPart(TurretPartSchematic schematic) {
        Schematic = schematic;

        Angle = 0;
        weapons = new WeaponPart[Schematic.OrigWeaponDirs.Length];
        Array.Clear(weapons, 0, weapons.Length);
    }

    public void SetOwner(Tank tank) {
        owningTank = tank;

        foreach(WeaponPart weapon in GetAllWeapons()) {
            weapon.SetOwner(tank);
        }
    }

    public List<WeaponPart> GetAllWeapons() {
        List<WeaponPart> weapons = new List<WeaponPart>();
        foreach (WeaponPart part in this.weapons) {
            if (part != null) {
                weapons.Add(part);
            }
        }

        return weapons;
    }

    public WeaponPart GetWeaponAtIdx(int idx) {
        if (weapons.Length <= idx) {
            return null;
        }

        return weapons[idx];
    }

    public void AddWeaponAtIdx(WeaponPart weapon, int idx) {
        if (weapon.Schematic.Weight <= Schematic.WeaponWeightRestrictions[idx]) {
            weapon.TurretIdx = idx;
            weapons[idx] = weapon;
        }
    }

    public void HandleInput() {
        float rotDir = 0;

        if (InputManager.Instance.IsKeyTypeDown(InputManager.KeyType.TurretCCW)) {
            rotDir = 1.0f;
        } else if (InputManager.Instance.IsKeyTypeDown(InputManager.KeyType.TurretCW)) {
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
        float timeDelta = Time.fixedDeltaTime;

        if (Mathf.Abs(Schematic.RotPerSecond) > 0 && Math.Abs(rotationDir) > 0) {
            float angle = rotationDir * Schematic.RotPerSecond * timeDelta;
            owningTank.TurretGO.transform.Rotate(new Vector3(0, 0, angle));
            Angle += angle;
        }

        foreach (WeaponPart weapon in GetAllWeapons()) {
            weapon.PerformFixedUpdate();
        }
    }

    public void Reset() {
        rotationDir = 0;
    }
}
