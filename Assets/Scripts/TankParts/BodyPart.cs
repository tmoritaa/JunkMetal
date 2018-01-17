using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class BodyPart
{
    public Vector2 Size
    {
        get; private set;
    }

    public int Armour
    {
        get; private set;
    }

    public BodyPart(int armour, Vector2 size) {
        Armour = armour;
        Size = size;
    }
}
