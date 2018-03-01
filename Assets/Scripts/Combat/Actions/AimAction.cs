using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class AimAction : AIAction
{
    private Vector2 aimDir;

    public AimAction(Vector2 _aimDir, AITankController controller) : base(controller) {
        aimDir = _aimDir;
    }

    public override void Perform() {
        Tank tank = controller.SelfTank;

        float curTurretAngle = tank.Turret.Angle + tank.Body.rotation;
        Vector2 curDir = tank.GetForwardVec().Rotate(tank.Turret.Angle);

        float signedAngle = Vector2.SignedAngle(curDir, aimDir);
        tank.Turret.SetRotDir(Mathf.Sign(signedAngle));
    }
}
