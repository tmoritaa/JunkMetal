using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class AimWithWeaponAction : AIAction
{
    private Vector2 aimDir;
    private WeaponPart weapon;

    public AimWithWeaponAction(Vector2 _aimDir, WeaponPart _weapon, Tank tank) : base(tank) {
        aimDir = _aimDir;
        weapon = _weapon;
    }

    public override void Perform() {
        Vector2 curDir = weapon.CalculateFireVec();

        float signedAngle = Vector2.SignedAngle(curDir, aimDir);
        tank.Turret.SetRotDir(Mathf.Sign(signedAngle));
    }
}
