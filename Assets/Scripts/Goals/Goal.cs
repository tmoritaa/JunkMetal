using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public abstract class Goal
{
    public float Insistence
    {
        get; protected set;
    }

    protected TankController tankController;

    public Goal(TankController _tankController) {
        Insistence = 0;
        tankController = _tankController;
    }

    public abstract void Init();
    public abstract AIAction[] CalcActionsToPerform();
    public abstract void UpdateInsistence();
}
