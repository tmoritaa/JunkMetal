using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class TankParSchematictFactory
{
    public static HullPartSchematic CreateHullPartSchematic(string name, int armour, int enginePower, Vector2 size, int weight, float angularDrag, float energy, float energyRefresh, float jetImpulse, float jetEnergyUsage, Vector2[] weaponDirs, Vector2[] weaponPos, PartSchematic.WeaponTier[] tierRestrictions) {
        return new HullPartSchematic(name, armour, enginePower, size, weight, angularDrag, energy, energyRefresh, jetImpulse, jetEnergyUsage, weaponDirs, weaponPos, tierRestrictions);
    }

    public static WeaponPartSchematic CreateWeaponPartSchematic(string name, float reloadTime, int energyUsage, PartSchematic.WeaponTier weaponTier, Bullet.BulletTypes bulletType, Dictionary<string, object> bulletInfos) {
        return new WeaponPartSchematic(name, reloadTime, energyUsage, weaponTier, bulletType, bulletInfos);
    }
}
