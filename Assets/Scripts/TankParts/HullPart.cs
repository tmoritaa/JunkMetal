using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class HullPart
{
    // Should be between 100 - 400
    public float Weight
    {
        get; private set;
    }

    public Vector2 Size
    {
        get; private set;
    }

    public int Armour
    {
        get; private set;
    }

    // Should be between 500000 - 1000000
    public float MoveForce
    {
        get; private set;
    }

    public HullPart(int armour, Vector2 size, float moveForce, float weight) {
        Armour = armour;
        Size = size;
        MoveForce = moveForce;
        Weight = weight;
    }
}
