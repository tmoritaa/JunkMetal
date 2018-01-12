using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class MainWeaponPart
{
    private Tank owningTank;
    private float shootForce;
    private float reloadTimeInSec;
    private KeyCode shootKey;

    private float lastShotTime;
    private bool shouldShoot = false;

    public MainWeaponPart(Tank _tank, float _shootForce, float _reloadTime, KeyCode _shootKey) {
        owningTank = _tank;
        shootForce = _shootForce;
        reloadTimeInSec = _reloadTime;
        shootKey = _shootKey;
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

            Vector2 forwardVec = (new Vector2(0, 1)).Rotate(owningTank.Body.rotation);
            bullet.Fire(forwardVec, shootForce);

            lastShotTime = Time.time;
            shouldShoot = false;
        }
    }
}
