using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class HumanTankController : TankController
{
    public override void Init(Vector2 startPos, float startRot, TankSchematic tankSchematic) {
        base.Init(startPos, startRot, tankSchematic);

        SelfTank.gameObject.layer = 9; // Player layer
    }

    protected override void Update() {
        base.Update();

        if (!CombatHandler.Instance.DisableMovement) {
            SelfTank.HandleInput();
        }
    }
}
