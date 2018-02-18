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

    public bool blocked;

    public Node(int _x, int _y) {
        x = _x;
        y = _y;
        blocked = false;
    }

    public virtual bool NodeTraversable() {
        return !blocked;
    }
}
