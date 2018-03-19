using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public partial class Tank
{
    public void PerformActuation(Vector2 requestDir) {
        int[] powerChange = AIUtility.CalcPowerChangeBasedOnRequestDir(requestDir, new TankStateInfo(this));

        this.Hull.PerformPowerChange(powerChange[0], powerChange[1]);
    }
    
    public void PerformRotation(Vector2 alignAngle, Vector2 requestDir) {
        float angle = Vector2.SignedAngle(alignAngle, requestDir);

        int[] powerChange = AIUtility.CalcPowerChangeForRotation(angle);
        Hull.PerformPowerChange(powerChange[0], powerChange[1]);
    }

    public float CalcAvgRange() {
        float totalRange = 0;
        int count = 0;
        foreach (WeaponPart part in Hull.GetAllWeapons()) {
            totalRange += part.Schematic.Range;
            count += 1;
        }

        return totalRange / count;
    }
}
