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

    public BodyPart(Vector2 size) {
        Size = size;
    }
}
