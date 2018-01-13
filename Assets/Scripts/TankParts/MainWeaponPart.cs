using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class MainWeaponPart
{
    private Tank owningTank;
    private float shootForce;
    private float reloadTimeInSec;
    private float rotPerTimeStep;
    private KeyCode shootKey;
    private KeyCode leftTurnKey;
    private KeyCode rightTurnKey;

    private float lastShotTime;
    private bool shouldShoot = false;
    private Vector2 forwardVec;

    private float rotDir = 0;

    public MainWeaponPart(Tank _tank, float _shootForce, float _reloadTime, float _rotPerTimestep, KeyCode _shootKey, KeyCode _leftTurnKey, KeyCode _rightTurnKey) {
        owningTank = _tank;
        shootForce = _shootForce;
        reloadTimeInSec = _reloadTime;
        rotPerTimeStep = _rotPerTimestep;

        shootKey = _shootKey;
        leftTurnKey = _leftTurnKey;
        rightTurnKey = _rightTurnKey;

        forwardVec = new Vector2(0, 1).Rotate(owningTank.Body.rotation).normalized;
        lastShotTime = -10000;
    }

    public void HandleInput() {
        if (Input.GetKey(shootKey) && (lastShotTime + reloadTimeInSec) <= Time.time) {
            shouldShoot = true;
        }

        if (Input.GetKey(leftTurnKey)) {
            rotDir = 1.0f;
        } else if (Input.GetKey(rightTurnKey)) {
            rotDir = -1.0f;
        } else {
            rotDir = 0;
        }
    }
    
    public void PerformFixedUpdate() {
        if (shouldShoot) {
            Bullet bullet = BulletFactory.Instance.CreateBullet(owningTank);

            Vector2 fireVec = forwardVec.Rotate(owningTank.Body.rotation);

            bullet.Fire(fireVec, shootForce);

            lastShotTime = Time.time;
            shouldShoot = false;
        }

        if (Math.Abs(rotDir) > 0) {
            float angle = rotDir * rotPerTimeStep;
            owningTank.CannonGO.transform.Rotate(new Vector3(0, 0, angle));
            forwardVec = forwardVec.Rotate(angle);
        }
    }
}
