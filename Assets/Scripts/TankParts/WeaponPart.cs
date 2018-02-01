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

    private Tank owningTank;
    private float shootForce;
    private float reloadTimeInSec;
    private KeyCode shootKey;

    private float lastShotTime;
    private bool shouldShoot = false;

    public WeaponPart(Tank _tank, float _shootForce, float _reloadTime, float _weight, KeyCode _shootKey) {
        owningTank = _tank;
        shootForce = _shootForce;
        reloadTimeInSec = _reloadTime;

        shootKey = _shootKey;

        Weight = _weight;

        lastShotTime = -10000;
    }

    public void HandleInput() {
        if (Input.GetKey(shootKey) && (lastShotTime + reloadTimeInSec) <= Time.time) {
            shouldShoot = true;
        }
    }
    
    public void PerformFixedUpdate() {
        if (shouldShoot) {
            Bullet bullet = BulletFactory.Instance.CreateBullet(owningTank);

            Vector2 fireVec = owningTank.Turret.ForwardVec.Rotate(owningTank.Body.rotation);

            bullet.Fire(fireVec, shootForce);

            lastShotTime = Time.time;
            shouldShoot = false;
        }
    }
}
