using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class TankParSchematictFactory
{
    public static HullPartSchematic CreateHullPartSchematic(string name, int armour, int energyPower, int weight, Vector2[] weaponDirs, Vector2[] weaponPos, int[] weightRestrict) {
        return new HullPartSchematic(name, armour, energyPower, weight, weaponDirs, weaponPos, weightRestrict);
    }

    public static WeaponPartSchematic CreateWeaponPartSchematic(string name, float shootingImpulse, float recoilImpulse, float reloadTime, float range, int weight, Bullet.BulletTypes bulletType, int damage) {
        return new WeaponPartSchematic(name, shootingImpulse, recoilImpulse, reloadTime, range, weight, bulletType, damage);
    }
}
