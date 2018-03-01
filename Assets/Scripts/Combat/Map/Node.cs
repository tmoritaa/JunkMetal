using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class Node
{
    public int x
    {
        get; private set;
    }

    public int y
    {
        get; private set;
    }

    public bool fullyBlocked;
    public bool tempBlocked;

    public Node(int _x, int _y) {
        x = _x;
        y = _y;
        fullyBlocked = false;
        tempBlocked = false;
    }

    public virtual bool NodeTraversable() {
        return !fullyBlocked && !tempBlocked;
    }

    public virtual void ResetNodeValues() {
        // Do nothing.
    }
}
