using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class RotateAction : AIAction
{
    private Vector2 requestDir;
    private Vector2 alignAngle;

    public RotateAction(Vector2 _alignAngle, Vector2 _requestDir, Tank _tank) : base(_tank) {
        requestDir = _requestDir;
        alignAngle = _alignAngle;
    }

    public override void Perform() {
        tank.PerformRotation(alignAngle, requestDir);
    }
}
