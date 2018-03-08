using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class TankParSchematictFactory
{
    public static HullPartSchematic CreateHullPartSchematic(string name, int armour, Vector2 size, int energyPower, int weight) {
        return new HullPartSchematic(name, armour, size, energyPower, weight);
    }

    public static TurretPartSchematic CreateTurretPartSchematic(string name, int armour, float rotPerSecond, int weight, Vector2[] weaponDirs, Vector2[] weaponFireOffset, int[] weightRestrict) {
        return new TurretPartSchematic(name, armour, rotPerSecond, weight, weaponDirs, weaponFireOffset, weightRestrict);
    }

    public static WeaponPartSchematic CreateWeaponPartSchematic(string name, float shootingImpulse, float recoilImpulse, float reloadTime, float range, int weight, Bullet.BulletTypes bulletType, int damage) {
        return new WeaponPartSchematic(name, shootingImpulse, recoilImpulse, reloadTime, range, weight, bulletType, damage);
    }
}
