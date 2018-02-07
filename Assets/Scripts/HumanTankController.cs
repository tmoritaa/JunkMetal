using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class HumanTankController : TankController
{
    public override void Init(Vector2 startPos, HullPart _body, TurretPart _turret, WheelPart _wheels) {
        base.Init(startPos, _body, _turret, _wheels);

        this.gameObject.layer = 9; // Player layer
    }

    void Update() {
        Tank.HandleInput();
    }
}
