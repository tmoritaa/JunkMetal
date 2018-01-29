using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class EnginePart
{
    public float MoveForce
    {
        get; private set;
    }

    public EnginePart(float moveForce) {
        MoveForce = moveForce;
    }
}
