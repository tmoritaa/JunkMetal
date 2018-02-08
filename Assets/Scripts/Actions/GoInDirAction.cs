using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class GoInDirAction : AIAction
{
    private Vector2 requestDir;

    public GoInDirAction(Vector2 _requestDir, Tank tank) : base(tank) {
        requestDir = _requestDir;
    }

    public override void Perform() {
        if (Application.isEditor) {
            Debug.DrawLine(tank.transform.position, tank.transform.position + (Vector3)(requestDir * 50), Color.red);
        }

        tank.PerformActuation(requestDir.normalized);
    }
}
