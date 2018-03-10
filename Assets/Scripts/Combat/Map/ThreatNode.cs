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
    public int TimeDiffDangerLevel = -1;
    public int TankToNodeDangerLevel = -1;

    public ThreatNode(int x, int y) : base(x, y) {
        TimeToHitTargetFromNode = 9999;
        TimeForTargetToHitNode = 9999;
        TimeForTargetToHitNodeNoReload = 9999;
        WeaponToHitTargetFromNode = null;
        Marked = false;
        TimeDiffDangerLevel = -1;
        TankToNodeDangerLevel = -1;
    }

    public override void ResetNodeValues() {
        TimeToHitTargetFromNode = 9999;
        TimeForTargetToHitNode = 9999;
        TimeForTargetToHitNodeNoReload = 9999;
        WeaponToHitTargetFromNode = null;
        Marked = false;
        TimeDiffDangerLevel = -1;
        TankToNodeDangerLevel = -1;
    }

    public float GetTimeDiffForHittingTarget() {
        return TimeForTargetToHitNode - TimeToHitTargetFromNode;
    }
}
