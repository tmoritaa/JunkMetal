using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class GoInDirAction : AIAction
{
    private Vector2 requestDir;

    public GoInDirAction(Vector2 _requestDir, AITankController controller) : base(controller) {
        requestDir = _requestDir;
    }

    public override void Perform() {
        Tank tank = controller.SelfTank;

        // TODO: Makes sense for now, but later if goals are rewritten to take into account walls, take Avoid Walls from here
        Vector2 newRequestDir = controller.AvoidWalls(requestDir);
        if (Application.isEditor) {
            Debug.DrawLine(tank.transform.position, tank.transform.position + (Vector3)(newRequestDir.normalized * 50), Color.red);
        }

        tank.PerformActuation(newRequestDir.normalized);
    }
}
