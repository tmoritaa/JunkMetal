using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class EnemyInfo
{
    public string Name
    {
        get; private set;
    }

    public TankSchematic TankSchem
    {
        get; private set;
    }

    public EnemyInfo(string name, TankSchematic tankSchem) {
        Name = name;
        TankSchem = tankSchem;
    }
}
