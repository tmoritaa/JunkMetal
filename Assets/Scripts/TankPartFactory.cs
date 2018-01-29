using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class TankPartFactory
{
    public static WheelPart CreateWheelPart(Tank tank, float energyInc, float energyDec, KeyCode leftForwardKey=KeyCode.None, KeyCode leftBackKey=KeyCode.None, 
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

        return new WheelPart(tank, energyInc, energyDec, leftForwardKey, leftBackKey, rightForwardKey, rightBackKey);
    }

    public static BodyPart CreateBodyPart(int armour, Vector2 size) {
        return new BodyPart(armour, size);
    }

    public static MainWeaponPart CreateMainWeaponPart(Tank tank, float shootingForce, float reloadTime, float rotPerTimeStep,
        KeyCode shootKey=KeyCode.None, 
        KeyCode leftTurnKey=KeyCode.None, 
        KeyCode rightTurnKey=KeyCode.None) 
    {
        if (shootKey == KeyCode.None) {
            shootKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), GlobalRandom.GetRandomNumber(97, 123).ToString());
        }

        if (leftTurnKey == KeyCode.None) {
            leftTurnKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), GlobalRandom.GetRandomNumber(97, 123).ToString());
        }

        if (rightTurnKey == KeyCode.None) {
            rightTurnKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), GlobalRandom.GetRandomNumber(97, 123).ToString());
        }

        return new MainWeaponPart(tank, shootingForce, reloadTime, rotPerTimeStep, shootKey, leftTurnKey, rightTurnKey);
    }

    public static EnginePart CreateEnginePart(float moveForce) {
        return new EnginePart(moveForce);
    }
}
