using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public partial class Tank
{
    public void HandleInput() {
        if (initialized) {
            Wheels.HandleInput();
            Turret.HandleInput();
        }
    }
}
