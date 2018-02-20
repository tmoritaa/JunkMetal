using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class SearchNode : Node
{
    public bool searched;

    public SearchNode(int _x, int _y, bool _searched = false) : base(_x, _y) {
        searched = _searched;
    }

    public override bool NodeTraversable() {
        return base.NodeTraversable() && !searched;
    }

    public override void ResetNodeValues() {
        searched = false;
    }
}
