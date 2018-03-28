using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public abstract class PartSchematic
{
    public enum PartType
    {
        Hull,
        Weapon
    }

    public enum WeaponTier
    {
        Light,
        Medium,
        Heavy,
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

    public abstract string GetStatString(PartSchematic equippedSchem);

    protected string getColorBasedChangeInVal<T>(T firstParam, T secondParam, bool greaterIsBetter = true) where T : IComparable {
        string positiveColor = "<color=#00ff00ff>";
        string negativeColor = "<color=#ff0000ff>";
        string neutralColor = "<color=#ffffffff>";

        int compareVal = firstParam.CompareTo(secondParam);

        if (compareVal > 0) {
            return greaterIsBetter ? negativeColor : positiveColor;
        } else if (compareVal < 0) {
            return greaterIsBetter ? positiveColor : negativeColor;
        } else {
            return neutralColor;
        }
    }
}
