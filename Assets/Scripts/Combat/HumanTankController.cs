using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class HumanTankController : TankController
{
    public override void Init(Vector2 startPos, TankSchematic tankSchematic) {
        base.Init(startPos, tankSchematic);

        this.gameObject.layer = 9; // Player layer
    }

    void Update() {
        SelfTank.HandleInput();
    }
}
