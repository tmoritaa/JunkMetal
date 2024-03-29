﻿using System;
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

    private Vector2 origWeaponFireOffset;

    private float lastShotTime;
    private bool shouldShoot = false;

    public WeaponPart(WeaponPartSchematic schematic, Vector2 weaponFireOffset, Tank owner) {
        Schematic = schematic;

        origWeaponFireOffset = weaponFireOffset;
        lastShotTime = -10000;
        EquipIdx = -1;
        OwningTank = owner;
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
        if (shouldShoot && OwningTank.Hull.EnergyAvailableForUsage(Schematic.EnergyUsage)) {
            Bullet bullet = BulletInstanceHandler.Instance.CreateBullet(Schematic.BulletType);
            bullet.Init(OwningTank);

            Vector2 fireVec = CalculateFireVec();
            Vector2 fireOffset = CalculateFireOffset();

            bullet.Fire(fireVec, fireOffset, Schematic);

            CombatAnimationHandler.Instance.InstantiatePrefab("fire_smoke", (Vector2)OwningTank.transform.position + fireOffset, bullet.Body.rotation);

            OwningTank.DisableMovementForSeconds(OwningTank.StopMoveInSecondsWhenFired);

            OwningTank.Hull.ModEnergy(-Schematic.EnergyUsage);

            lastShotTime = Time.time;
        }
        shouldShoot = false;
    }

    public Vector2 CalculateFireVec() {
        return OwningTank.Hull.Schematic.OrigWeaponDirs[EquipIdx].Rotate(OwningTank.Body.rotation);
    }

    public Vector2 CalculateFirePos() {
        Vector2 offset = CalculateFireOffset();
        return (Vector2)OwningTank.transform.position + offset;
    }

    public Vector2 CalculateFireOffset() {
        return OwningTank.Hull.Schematic.OrigWeaponPos[EquipIdx].Rotate(OwningTank.Body.rotation) + origWeaponFireOffset.Rotate(OwningTank.Body.rotation);
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
