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

    public int EquipIdx
    {
        get; set;
    }

    public bool IsFireable {
        get {
            return Schematic.ReloadTimeInSec < (Time.time - lastShotTime);
        }
    }

    public Tank OwningTank
    {
        get; private set;
    }
    
    private float lastShotTime;
    private bool shouldShoot = false;

    public WeaponPart(WeaponPartSchematic schematic) {
        Schematic = schematic;
        
        lastShotTime = -10000;
        EquipIdx = -1;
    }

    public void SetOwner(Tank tank) {
        OwningTank = tank;
    }

    public void HandleInput() {
        if (InputManager.Instance.IsKeyTypeDown((InputManager.KeyType)(100 + EquipIdx)) && (lastShotTime + Schematic.ReloadTimeInSec) <= Time.time) {
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
            Bullet bullet = BulletFactory.Instance.CreateBullet(OwningTank, Schematic.BulletType);

            Vector2 fireVec = CalculateFireVec();
            Vector2 fireOffset = CalculateFireOffset();

            bullet.Fire(fireVec, fireOffset, Schematic.ShootImpulse, Schematic.RecoilImpulse, Schematic.Range, Schematic.Damage);

            lastShotTime = Time.time;
            shouldShoot = false;
        }
    }

    public Vector2 CalculateFireVec() {
        return OwningTank.Hull.Schematic.OrigWeaponDirs[EquipIdx].Rotate(OwningTank.Body.rotation);
    }

    public Vector2 CalculateFirePos() {
        Vector2 offset = CalculateFireOffset();
        return (Vector2)OwningTank.transform.position + offset;
    }

    public Vector2 CalculateFireOffset() {
        return OwningTank.Hull.Schematic.OrigWeaponFirePosOffset[EquipIdx].Rotate(OwningTank.Body.rotation);
    }

    public float CalcTimeToReloaded() {
        float timeDiff = Time.time - lastShotTime;

        return Mathf.Max(0, Schematic.ReloadTimeInSec - timeDiff);
    }

    public float CalcRatioToReloaded() {
        float timeDiff = Time.time - lastShotTime;
        return timeDiff / Schematic.ReloadTimeInSec;
    }
}
