using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class TankPartFactory
{
    public static WheelPart CreateWheelPart(Tank tank, float energyInc, float energyDec, float weight, KeyCode leftForwardKey=KeyCode.None, KeyCode leftBackKey=KeyCode.None, 
        KeyCode rightForwardKey = KeyCode.None, KeyCode rightBackKey = KeyCode.None) {

        if (leftForwardKey == KeyCode.None) {
            leftForwardKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), GlobalRandom.GetRandomNumber(97, 123).ToString());
        }

        if (leftBackKey == KeyCode.None) {
            leftBackKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), GlobalRandom.GetRandomNumber(97, 123).ToString());
        }

        if (rightForwardKey == KeyCode.None) {
            rightForwardKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), GlobalRandom.GetRandomNumber(97, 123).ToString());
        }

        if (rightBackKey == KeyCode.None) {
            rightBackKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), GlobalRandom.GetRandomNumber(97, 123).ToString());
        }

        return new WheelPart(tank, energyInc, energyDec, weight, leftForwardKey, leftBackKey, rightForwardKey, rightBackKey);
    }

    public static HullPart CreateHullPart(int armour, Vector2 size, float moveForce, float weight) {
        return new HullPart(armour, size, moveForce, weight);
    }

    public static TurretPart CreateTurretPart(Tank tank, float rotPerTimeStep, float weight, Vector2[] weaponDirs, float[] weightRestrict, 
        KeyCode leftTurnKey = KeyCode.None, KeyCode rightTurnKey = KeyCode.None) {

        if (leftTurnKey == KeyCode.None) {
            leftTurnKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), GlobalRandom.GetRandomNumber(97, 123).ToString());
        }

        if (rightTurnKey == KeyCode.None) {
            rightTurnKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), GlobalRandom.GetRandomNumber(97, 123).ToString());
        }

        return new TurretPart(tank, rotPerTimeStep, weight, weaponDirs, weightRestrict, leftTurnKey, rightTurnKey);
    }

    public static WeaponPart CreateMainWeaponPart(Tank tank, float shootingForce, float reloadTime, float range, float weight, KeyCode shootKey=KeyCode.None) 
    {
        if (shootKey == KeyCode.None) {
            shootKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), GlobalRandom.GetRandomNumber(97, 123).ToString());
        }

        return new WeaponPart(tank, shootingForce, reloadTime, range, weight, shootKey);
    }
}
