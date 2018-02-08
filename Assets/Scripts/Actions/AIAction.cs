using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public abstract class AIAction
{
    protected Tank tank;

    public AIAction(Tank _tank) {
        tank = _tank;
    }

    public abstract void Perform();
}
