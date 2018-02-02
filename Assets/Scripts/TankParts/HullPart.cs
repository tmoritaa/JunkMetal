using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class HullPart
{
    public HullPartSchematic Schematic
    {
        get; private set;
    }

    public HullPart(HullPartSchematic _schematic) {

        Schematic = _schematic;
    }
}
