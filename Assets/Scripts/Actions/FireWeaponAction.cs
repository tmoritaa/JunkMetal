using System;
using System.Collections.Generic;
using System.Linq;

public class FireWeaponAction : AIAction
{
    private int weaponIdx;

    public FireWeaponAction(int _weaponIdx, AITankController _controller) : base(_controller) {
        weaponIdx = _weaponIdx;
    }

    public override void Perform() {
        controller.Tank.Turret.Weapons[weaponIdx].FireIfAble();
    }
}
