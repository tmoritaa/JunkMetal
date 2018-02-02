using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class PartSchematic
{
    public string Name
    {
        get; protected set;
    }

    public PartSchematic(string name) {
        Name = name;
    }
}
