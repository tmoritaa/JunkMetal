using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class TankSchematic
{
    public HullPartSchematic HullSchematic
    {
        get; set;
    }

    public TurretPartSchematic TurretSchematic
    {
        get; set;
    }

    public WheelPartSchematic WheelSchematic
    {
        get; set;
    }

    public WeaponPartSchematic[] WeaponSchematics
    {
        get; set;
    }

    public TankSchematic(HullPartSchematic hull, TurretPartSchematic turret, WheelPartSchematic wheel, WeaponPartSchematic[] weapons) {
        HullSchematic = hull;
        TurretSchematic = turret;
        WheelSchematic = wheel;
        WeaponSchematics = weapons;
    }

    // TODO: super hacky. Once Tank schematic is rewritten to not use PartSchematics, rewrite this shit.
    public void UpdateTankSchematic(PartSchematic newPart, PartSchematic oldPart, int weaponIdx) {
        // Do nothing if the part types don't match.
        if (oldPart != null && oldPart.GetType() != newPart.GetType()) {
            return;
        }

        Type partType = newPart.GetType();
        if (partType == typeof(HullPartSchematic)) {
            HullSchematic = (HullPartSchematic)newPart;
        } else if (partType == typeof(TurretPartSchematic)) {
            TurretPartSchematic oldTurret = (TurretPartSchematic)oldPart;
            TurretPartSchematic newTurret = (TurretPartSchematic)newPart;
            if (oldTurret.OrigWeaponDirs.Length != newTurret.OrigWeaponDirs.Length) {
                WeaponPartSchematic[] oldWeapons = WeaponSchematics;
                WeaponSchematics = new WeaponPartSchematic[newTurret.OrigWeaponDirs.Length];

                int minLength = Mathf.Min(oldTurret.OrigWeaponDirs.Length, newTurret.OrigWeaponDirs.Length);
                for (int i = 0; i < minLength; ++i) {
                    WeaponSchematics[i] = oldWeapons[i];
                }
            }

            TurretSchematic = newTurret;
        } else if (partType == typeof(WheelPartSchematic)) {
            WheelSchematic = (WheelPartSchematic)newPart;
        } else if (partType == typeof(WeaponPartSchematic)) {
            WeaponSchematics[weaponIdx] = (WeaponPartSchematic)newPart;
        }
    }
}
