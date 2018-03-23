using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class TankParSchematictFactory
{
    public static HullPartSchematic CreateHullPartSchematic(string name, int armour, int energyPower, Vector2 size, int weight, Vector2[] weaponDirs, Vector2[] weaponPos, int[] weightRestrict) {
        return new HullPartSchematic(name, armour, energyPower, size, weight, weaponDirs, weaponPos, weightRestrict);
    }

    public static WeaponPartSchematic CreateWeaponPartSchematic(string name, float shootingImpulse, float recoilImpulse, float hitImpulse, float reloadTime, float range, int weight, Bullet.BulletTypes bulletType, int damage) {
        return new WeaponPartSchematic(name, shootingImpulse, recoilImpulse, hitImpulse, reloadTime, range, weight, bulletType, damage);
    }
}
