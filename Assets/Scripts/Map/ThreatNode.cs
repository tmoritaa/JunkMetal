using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class ThreatNode : Node
{
    public float TimeForTargetToHitNode;
    public float TimeToHitTargetFromNode;

    public WeaponPart WeaponToHitTargetFromNode;

    public ThreatNode(int x, int y) : base(x, y) {
        TimeToHitTargetFromNode = 9999;
        TimeForTargetToHitNode = 9999;
    }

    public override void ResetNodeValues() {
        TimeToHitTargetFromNode = 9999;
        TimeForTargetToHitNode = 9999;
    }

    public float GetTimeDiffForHittingTarget() {
        return TimeForTargetToHitNode - TimeToHitTargetFromNode;
    }
}
