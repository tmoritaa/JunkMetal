using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class Connection
{
    public Node srcNode
    {
        get; private set;
    }

    public Node targetNode
    {
        get; private set;
    }

    public Connection(Node _srcNode, Node _tgtNode) {
        srcNode = _srcNode;
        targetNode = _tgtNode;
    }
}