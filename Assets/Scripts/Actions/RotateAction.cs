using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class RotateAction : AIAction
{
    private Vector2 requestDir;
    private Vector2 alignAngle;

    public RotateAction(Vector2 _alignAngle, Vector2 _requestDir, AITankController _controller) : base(_controller) {
        requestDir = _requestDir;
        alignAngle = _alignAngle;
    }

    public override void Perform() {
        controller.SelfTank.PerformRotation(alignAngle, requestDir);
    }
}
