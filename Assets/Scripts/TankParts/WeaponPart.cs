using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class WeaponPart
{
    public WeaponPartSchematic Schematic
    {
        get; private set;
    }

    public int TurretIdx
    {
        get; set;
    }

    public bool IsFireable {
        get {
            return Schematic.ReloadTimeInSec < (Time.time - lastShotTime);
        }
    }

    private Tank owningTank;
    
    private float lastShotTime;
    private bool shouldShoot = false;

    public WeaponPart(WeaponPartSchematic schematic) {
        Schematic = schematic;
        
        lastShotTime = -10000;
        TurretIdx = -1;
    }

    public void SetOwner(Tank tank) {
        owningTank = tank;
    }

    public void HandleInput() {
        KeyCode shootKey = owningTank.Turret.Schematic.ShootKeys[TurretIdx];

        if (Input.GetKey(shootKey) && (lastShotTime + Schematic.ReloadTimeInSec) <= Time.time) {
            FireIfAble();
        }
    }

    public void FireIfAble() {
        if (IsFireable) {
            shouldShoot = true;
        }
    }
    
    public void PerformFixedUpdate() {
        if (shouldShoot) {
            Bullet bullet = BulletFactory.Instance.CreateBullet(owningTank, Schematic.BulletType);

            Vector2 fireVec = CalculateFireVec();

            bullet.Fire(fireVec, Schematic.ShootImpulse, Schematic.ShootBackForce, Schematic.Range, Schematic.Damage);

            lastShotTime = Time.time;
            shouldShoot = false;
        }
    }

    public Vector2 CalculateFireVec() {
        return owningTank.Turret.Schematic.OrigWeaponDirs[TurretIdx].Rotate(owningTank.Turret.Angle + owningTank.Body.rotation);
    }

    public float CalcTimeToReloaded() {
        float timeDiff = Time.time - lastShotTime;

        return Schematic.ReloadTimeInSec - timeDiff;
    }
}
