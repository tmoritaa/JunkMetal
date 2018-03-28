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

    public float ShootImpulse
    {
        get; private set;
    }

    public float RecoilImpulse
    {
        get; private set;
    }

    public float HitImpulse
    {
        get; private set;
    }

    public float ReloadTimeInSec
    {
        get; private set;
    }

    public float Range
    {
        get; private set;
    }

    public Bullet.BulletTypes BulletType
    {
        get; private set;
    }

    public int Damage
    {
        get; private set;
    }

    public WeaponPartSchematic(string name, float _shootImpulse, float _recoilImpulse, float _hitImpulse, float _reloadTime, float _range, WeaponTier _tier, Bullet.BulletTypes _bulletType, int _damage) : base(name, PartType.Weapon) {
        Name = name;
        ShootImpulse = _shootImpulse;
        RecoilImpulse = _recoilImpulse;
        HitImpulse = _hitImpulse;
        ReloadTimeInSec = _reloadTime;
        Tier = _tier;
        Range = _range;
        BulletType = _bulletType;
        Damage = _damage;
    }

    public override string GetStatString(PartSchematic diffSchem) {
        bool showDiff = diffSchem != null;

        string retStr = string.Empty;

        if (showDiff) {
            WeaponPartSchematic diffWeapon = (WeaponPartSchematic)diffSchem;

            string damageStr = string.Format("Damage: {0} => {2}{1}</color>", diffWeapon.Damage, Damage, getColorBasedChangeInVal(diffWeapon.Damage, Damage));
            string rangeStr = string.Format("Range: {0} => {2}{1}</color>", diffWeapon.Range, Range, getColorBasedChangeInVal(diffWeapon.Range, Range));
            string reloadTimeStr = string.Format("Reload Time: {0} => {2}{1}</color>", diffWeapon.ReloadTimeInSec, ReloadTimeInSec, getColorBasedChangeInVal(diffWeapon.ReloadTimeInSec, ReloadTimeInSec, false));
            string fireStrengthStr = string.Format("Fire Strength: {0} => {2}{1}</color>", diffWeapon.ShootImpulse, ShootImpulse, getColorBasedChangeInVal(diffWeapon.ShootImpulse, ShootImpulse));
            string recoilStr = string.Format("Recoil: {0} => {2}{1}</color>", diffWeapon.RecoilImpulse, RecoilImpulse, getColorBasedChangeInVal(diffWeapon.RecoilImpulse, RecoilImpulse, false));
            string tierStr = string.Format("Tier: {0} => {1}", diffWeapon.Tier, Tier);

            retStr = string.Format("{0}\nBullet Type: {1} => {2}\n{3}\n{4}\n{5}\n{6}\n{7}\n{8}",
                Name, diffWeapon.BulletType, BulletType, damageStr, rangeStr, reloadTimeStr, fireStrengthStr, recoilStr, tierStr);
        } else {
            retStr = string.Format("{0}\nBullet Type: {1}\nDamage: {2}\nRange: {3}\nReload Time:{4}\nFire Strength: {5}\nRecoil: {6}\nTier: {7}",
                Name, BulletType, Damage, Range, ReloadTimeInSec, ShootImpulse, RecoilImpulse, Tier);
        }

        return retStr;
    }
}
