using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class TankPartFactory
{
    public static WheelPart CreateWheelPart(Tank tank) {
        KeyCode forwardKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), GlobalRandom.GetRandomNumber(97, 123).ToString());
        KeyCode backKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), GlobalRandom.GetRandomNumber(97, 123).ToString());
        return new WheelPart(tank, forwardKey, backKey);
    }

}
