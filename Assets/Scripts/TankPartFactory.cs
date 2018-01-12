using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class TankPartFactory
{
    public static WheelPart CreateWheelPart(Tank tank) {
        KeyCode forwardKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), GlobalRandom.GetRandomNumber(97, 123).ToString());
        KeyCode backKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), GlobalRandom.GetRandomNumber(97, 123).ToString());
        return CreateWheelPart(tank, forwardKey, backKey);
    }

    public static WheelPart CreateWheelPart(Tank tank, KeyCode forwardKey, KeyCode backKey) {
        return new WheelPart(tank, forwardKey, backKey);
    }

    public static BodyPart CreateBodyPart(Vector2 size) {
        return new BodyPart(size);
    }

    public static MainWeaponPart CreateMainWeaponPart(Tank tank, float shootingForce, float reloadTime, KeyCode shootKey) {
        return new MainWeaponPart(tank, shootingForce, reloadTime, shootKey);
    }
}
