using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public abstract class CustomizationState : State
{
    protected CustomizationHandler handler;

    public CustomizationState(CustomizationHandler _handler) {
        handler = _handler;
    }
}
