using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class TankSchematic
{
    public HullPartSchematic HullSchematic
    {
        get; set;
    }

    public TurretPartSchematic TurretSchematic
    {
        get; set;
    }

    public WeaponPartSchematic[] WeaponSchematics
    {
        get; set;
    }

    public TankSchematic(HullPartSchematic hull, TurretPartSchematic turret, WeaponPartSchematic[] weapons) {
        HullSchematic = hull;
        TurretSchematic = turret;
        WeaponSchematics = weapons;
    }
}
