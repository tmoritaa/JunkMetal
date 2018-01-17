using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class TankPartFactory
{
    public static WheelPart CreateWheelPart(Tank tank, KeyCode forwardKey=KeyCode.None, KeyCode backKey=KeyCode.None) {
        if (forwardKey == KeyCode.None) {
            forwardKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), GlobalRandom.GetRandomNumber(97, 123).ToString());
        }

        if (backKey == KeyCode.None) {
            backKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), GlobalRandom.GetRandomNumber(97, 123).ToString());
        }
        
        return new WheelPart(tank, forwardKey, backKey);
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

    public static EnginePart CreateEnginePart(float moveForce, float wheelEnergyInc, float wheelEnergyDec) {
        return new EnginePart(moveForce, wheelEnergyInc, wheelEnergyDec);
    }
}
