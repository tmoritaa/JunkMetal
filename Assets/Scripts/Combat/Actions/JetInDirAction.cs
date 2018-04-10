using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class JetInDirAction : AIAction
{
    Vector2 dir;

    public JetInDirAction(Vector2 _dir, AITankController _controller) : base(_controller) {
        dir = _dir;
    }

    public override void Perform() {
        controller.SelfTank.PerformJet(dir);
        controller.SelfTank.PerformActuation(new Vector2());
    }
}
