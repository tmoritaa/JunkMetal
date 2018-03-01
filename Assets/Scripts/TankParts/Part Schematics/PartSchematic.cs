using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public abstract class PartSchematic
{
    public enum PartType
    {
        Hull,
        Turret,
        Wheels,
        Weapon
    }

    public string Name
    {
        get; protected set;
    }

    public PartType PType
    {
        get; protected set;
    }

    public PartSchematic(string name, PartType pType) {
        Name = name;
        PType = pType;
    }
}
