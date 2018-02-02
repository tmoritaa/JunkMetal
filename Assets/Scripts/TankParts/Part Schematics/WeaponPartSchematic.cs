using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class WeaponPartSchematic : PartSchematic
{
    // Should be between 10 - 100. Only used for weight restrictions on turrets
    public float Weight
    {
        get; private set;
    }

    public float ShootForce
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

    public WeaponPartSchematic(string name, float _shootForce, float _reloadTime, float _range, float _weight, Bullet.BulletTypes _bulletType, int _damage) : base(name) {
        Name = name;
        ShootForce = _shootForce;
        ReloadTimeInSec = _reloadTime;
        Weight = _weight;
        Range = _range;
        BulletType = _bulletType;
        Damage = _damage;
    }
}
