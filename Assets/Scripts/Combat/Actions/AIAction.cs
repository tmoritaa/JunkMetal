using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public abstract class AIAction
{
    protected AITankController controller;

    public AIAction(AITankController _controller) {
        controller = _controller;
    }

    public abstract void Perform();
}
