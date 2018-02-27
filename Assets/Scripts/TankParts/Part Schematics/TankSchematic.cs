using System;
using System.Collections.Generic;
using System.Linq;

public class TankSchematic
{
    public HullPartSchematic HullSchematic
    {
        get; private set;
    }

    public TurretPartSchematic TurretSchematic
    {
        get; private set;
    }

    public WheelPartSchematic WheelSchematic
    {
        get; private set;
    }

    public WeaponPartSchematic[] WeaponSchematics
    {
        get; private set;
    }

    public TankSchematic(HullPartSchematic hull, TurretPartSchematic turret, WheelPartSchematic wheel, WeaponPartSchematic[] weapons) {
        HullSchematic = hull;
        TurretSchematic = turret;
        WheelSchematic = wheel;
        WeaponSchematics = weapons;
    }
}
