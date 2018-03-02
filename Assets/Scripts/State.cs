using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public abstract class State
{
    public abstract void Start();
    public abstract void End();
    public abstract void PerformUpdate();
}
