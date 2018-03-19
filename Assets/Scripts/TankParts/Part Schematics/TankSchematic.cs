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

    public WeaponPartSchematic[] WeaponSchematics
    {
        get; set;
    }

    public TankSchematic(HullPartSchematic hull, WeaponPartSchematic[] weapons) {
        HullSchematic = hull;
        WeaponSchematics = weapons;
    }
}
