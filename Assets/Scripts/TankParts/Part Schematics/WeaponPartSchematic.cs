using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class WeaponPartSchematic : PartSchematic
{
    public WeaponTier Tier
    {
        get; private set;
    }

    public float ReloadTimeInSec
    {
        get; private set;
    }

    public Bullet.BulletTypes BulletType
    {
        get; private set;
    }

    public float ShootImpulse
    {
        get {
            return (float)BulletInfos["shoot_impulse"];
        }
    }

    public float Range
    {
        get {
            return (float)BulletInfos["range"];
        }
    }

    public int Damage
    {
        get {
            return (int)BulletInfos["damage"];
        }
    }

    public Dictionary<string, object> BulletInfos
    {
        get; private set;
    }

    public WeaponPartSchematic(string name, float _reloadTime, WeaponTier _tier, Bullet.BulletTypes _bulletType, Dictionary<string, object> bulletInfos) : base(name, PartType.Weapon) {
        Name = name;
        BulletInfos = bulletInfos;
        ReloadTimeInSec = _reloadTime;
        Tier = _tier;
        BulletType = _bulletType;
    }

    public override string GetStatString(PartSchematic diffSchem) {
        bool showDiff = diffSchem != null;

        string retStr = string.Empty;

        if (showDiff) {
            WeaponPartSchematic diffWeapon = (WeaponPartSchematic)diffSchem;

            string damageStr = string.Format("Damage:\n{0} => {2}{1}</color>", diffWeapon.Damage, Damage, getColorBasedChangeInVal(diffWeapon.Damage, Damage));
            string rangeStr = string.Format("Range:\n{0} => {2}{1}</color>", diffWeapon.Range, Range, getColorBasedChangeInVal(diffWeapon.Range, Range));
            string reloadTimeStr = string.Format("Reload Time:\n{0} => {2}{1}</color>", diffWeapon.ReloadTimeInSec, ReloadTimeInSec, getColorBasedChangeInVal(diffWeapon.ReloadTimeInSec, ReloadTimeInSec, false));
            string fireStrengthStr = string.Format("Fire Strength:\n{0} => {2}{1}</color>", diffWeapon.ShootImpulse, ShootImpulse, getColorBasedChangeInVal(diffWeapon.ShootImpulse, ShootImpulse));
            string tierStr = string.Format("Tier:\n{0} => {1}", diffWeapon.Tier, Tier);

            retStr = string.Format("Bullet Type:\n{0} => {1}\n{2}\n{3}\n{4}\n{5}\n{6}",
                diffWeapon.BulletType, BulletType, damageStr, rangeStr, reloadTimeStr, fireStrengthStr, tierStr);
        } else {
            retStr = string.Format("Bullet Type:\n{0}\nDamage:\n{1}\nRange:\n{2}\nReload Time:\n{3}\nFire Strength:\n{4}\nTier:\n{5}",
                BulletType, Damage, Range, ReloadTimeInSec, ShootImpulse, Tier);
        }

        return retStr;
    }
}
