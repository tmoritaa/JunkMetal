using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class WeaponPartSchematic : PartSchematic
{
    // Should be between 10 - 100. Only used for weight restrictions on turrets
    public int Weight
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

    public float ReloadTimeInSec
    {
        get; private set;
    }

    public float Range
    {
        get; private set;
    }

    public float OptimalRange
    {
        get {
            return Range * 0.9f;
        }
    }

    public float ThreatRange
    {
        get {
            return Range * 1.5f;
        }
    }

    public Bullet.BulletTypes BulletType
    {
        get; private set;
    }

    public int Damage
    {
        get; private set;
    }

    public WeaponPartSchematic(string name, float _shootImpulse, float _recoilImpulse, float _reloadTime, float _range, int _weight, Bullet.BulletTypes _bulletType, int _damage) : base(name, PartType.Weapon) {
        Name = name;
        ShootImpulse = _shootImpulse;
        RecoilImpulse = _recoilImpulse;
        ReloadTimeInSec = _reloadTime;
        Weight = _weight;
        Range = _range;
        BulletType = _bulletType;
        Damage = _damage;
    }

    public override string GetStatString(PartSchematic diffSchem) {
        bool showDiff = diffSchem != null;

        string retStr = string.Empty;

        if (showDiff) {
            WeaponPartSchematic diffHull = (WeaponPartSchematic)diffSchem;

            retStr = string.Format("{0}\nShoot Strength: {1} => {2}\nShoot Recoil: {3} => {4}\nRange: {5} => {6}\nBullet Type: {7} => {8}\nDamage: {9} => {10}\nReload Time:{11} => {12}\nWeight: {13} => {14}",
                Name, diffHull.ShootImpulse, ShootImpulse, diffHull.RecoilImpulse, RecoilImpulse, diffHull.Range, Range, 
                diffHull.BulletType, BulletType, diffHull.Damage, Damage, diffHull.ReloadTimeInSec, ReloadTimeInSec, diffHull.Weight, Weight);
        } else {
            retStr = string.Format("{0}\nShoot Strength: {1}\nShoot Recoil: {2}\nRange: {3}\nBullet Type: {4}\nDamage: {5}\nReload Time:{6}\nWeight: {7}",
                Name, ShootImpulse, RecoilImpulse, Range, BulletType, Damage, ReloadTimeInSec, Weight);
        }

        return retStr;
    }
}
