using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class ThreatNode : Node
{
    public float TimeForTargetToHitNode;
    public float TimeToHitTargetFromNode;
    public float TimeForTargetToHitNodeNoReload;

    public WeaponPart WeaponToHitTargetFromNode;

    public bool Marked = false;

    public ThreatNode(int x, int y) : base(x, y) {
        TimeToHitTargetFromNode = 9999;
        TimeForTargetToHitNode = 9999;
        TimeForTargetToHitNodeNoReload = 9999;
        WeaponToHitTargetFromNode = null;
        Marked = false;
    }

    public override void ResetNodeValues() {
        TimeToHitTargetFromNode = 9999;
        TimeForTargetToHitNode = 9999;
        TimeForTargetToHitNodeNoReload = 9999;
        WeaponToHitTargetFromNode = null;
        Marked = false;
    }

    public float GetTimeDiffForHittingTarget() {
        return TimeForTargetToHitNode - TimeToHitTargetFromNode;
    }
}
