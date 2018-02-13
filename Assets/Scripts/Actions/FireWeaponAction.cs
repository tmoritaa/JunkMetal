using System;
using System.Collections.Generic;
using System.Linq;

public class FireWeaponAction : AIAction
{
    private int weaponIdx;

    public FireWeaponAction(int _weaponIdx, Tank _tank) : base(_tank) {
        weaponIdx = _weaponIdx;
    }

    public override void Perform() {
        tank.Turret.Weapons[weaponIdx].FireIfAble();
    }
}
