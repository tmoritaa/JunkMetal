using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class TankParSchematictFactory
{
    public static WheelPartSchematic CreateWheelPartSchematic(string name, float energyInc, float energyDec, int weight, KeyCode leftForwardKey=KeyCode.None, KeyCode leftBackKey=KeyCode.None, 
        KeyCode rightForwardKey = KeyCode.None, KeyCode rightBackKey = KeyCode.None) {

        if (leftForwardKey == KeyCode.None) {
            leftForwardKey = (KeyCode)Enum.Parse(typeof(KeyCode), GlobalRandom.GetRandomNumber(97, 123).ToString());
        }

        if (leftBackKey == KeyCode.None) {
            leftBackKey = (KeyCode)Enum.Parse(typeof(KeyCode), GlobalRandom.GetRandomNumber(97, 123).ToString());
        }

        if (rightForwardKey == KeyCode.None) {
            rightForwardKey = (KeyCode)Enum.Parse(typeof(KeyCode), GlobalRandom.GetRandomNumber(97, 123).ToString());
        }

        if (rightBackKey == KeyCode.None) {
            rightBackKey = (KeyCode)Enum.Parse(typeof(KeyCode), GlobalRandom.GetRandomNumber(97, 123).ToString());
        }

        return new WheelPartSchematic(name, energyInc, energyDec, weight, leftForwardKey, leftBackKey, rightForwardKey, rightBackKey);
    }

    public static HullPartSchematic CreateHullPartSchematic(string name, int armour, Vector2 size, int energyPower, int weight) {
        return new HullPartSchematic(name, armour, size, energyPower, weight);
    }

    public static TurretPartSchematic CreateTurretPartSchematic(string name, int armour, float rotPerTimeStep, int weight, Vector2[] weaponDirs, int[] weightRestrict, 
        KeyCode leftTurnKey = KeyCode.None, KeyCode rightTurnKey = KeyCode.None, KeyCode[] shootKeys = null) {

        if (shootKeys == null) {
            for (int i = 0; i < weaponDirs.Length; ++i) {
                shootKeys[i] = (KeyCode)Enum.Parse(typeof(KeyCode), GlobalRandom.GetRandomNumber(97, 123).ToString());
            }
        }

        if (leftTurnKey == KeyCode.None) {
            leftTurnKey = (KeyCode)Enum.Parse(typeof(KeyCode), GlobalRandom.GetRandomNumber(97, 123).ToString());
        }

        if (rightTurnKey == KeyCode.None) {
            rightTurnKey = (KeyCode)Enum.Parse(typeof(KeyCode), GlobalRandom.GetRandomNumber(97, 123).ToString());
        }

        return new TurretPartSchematic(name, armour, rotPerTimeStep, weight, weaponDirs, weightRestrict, leftTurnKey, rightTurnKey, shootKeys);
    }

    public static WeaponPartSchematic CreateWeaponPartSchematic(string name, float shootingImpulse, float shootingBackForce, float reloadTime, float range, int weight, Bullet.BulletTypes bulletType, int damage) 
    {
        return new WeaponPartSchematic(name, shootingImpulse, shootingBackForce, reloadTime, range, weight, bulletType, damage);
    }
}
