using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class WeaponPart
{
    // Should be between 10 - 100. Only used for weight restrictions on turrets
    public float Weight
    {
        get; private set;
    }

    public int TurretIdx
    {
        get; set;
    }

    private Tank owningTank;
    private float shootForce;
    private float reloadTimeInSec;
    private float range;
    private Bullet.BulletTypes bulletType;
    private int damage;

    private KeyCode shootKey;

    private float lastShotTime;
    private bool shouldShoot = false;

    public WeaponPart(Tank _tank, float _shootForce, float _reloadTime, float _range, float _weight, Bullet.BulletTypes _bulletType, int _damage, KeyCode _shootKey) {
        owningTank = _tank;
        shootForce = _shootForce;
        reloadTimeInSec = _reloadTime;
        Weight = _weight;
        range = _range;
        bulletType = _bulletType;
        damage = _damage;
        shootKey = _shootKey;

        lastShotTime = -10000;
        TurretIdx = -1;
    }

    public void HandleInput() {
        if (Input.GetKey(shootKey) && (lastShotTime + reloadTimeInSec) <= Time.time) {
            shouldShoot = true;
        }
    }
    
    public void PerformFixedUpdate() {
        if (shouldShoot) {
            Bullet bullet = BulletFactory.Instance.CreateBullet(owningTank, bulletType);

            Vector2 fireVec = owningTank.Turret.OrigWeaponDirs[TurretIdx].Rotate(owningTank.Turret.Angle + owningTank.Body.rotation);

            bullet.Fire(fireVec, shootForce, range, damage);

            lastShotTime = Time.time;
            shouldShoot = false;
        }
    }
}
