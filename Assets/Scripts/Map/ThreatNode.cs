using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class ThreatNode : Node
{
    public float ThreatValue;

    public ThreatNode(int x, int y) : base(x, y) {
        ThreatValue = 0;
    }
}
