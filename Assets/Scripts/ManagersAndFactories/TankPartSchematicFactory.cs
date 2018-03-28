using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class TankParSchematictFactory
{
    public static HullPartSchematic CreateHullPartSchematic(string name, int armour, int energyPower, Vector2 size, int weight, float angularDrag, Vector2[] weaponDirs, Vector2[] weaponPos, PartSchematic.WeaponTier[] tierRestrictions) {
        return new HullPartSchematic(name, armour, energyPower, size, weight, angularDrag, weaponDirs, weaponPos, tierRestrictions);
    }

    public static WeaponPartSchematic CreateWeaponPartSchematic(string name, float shootingImpulse, float recoilImpulse, float hitImpulse, float reloadTime, float range, PartSchematic.WeaponTier weaponTier, Bullet.BulletTypes bulletType, int damage) {
        return new WeaponPartSchematic(name, shootingImpulse, recoilImpulse, hitImpulse, reloadTime, range, weaponTier, bulletType, damage);
    }
}
