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

    public void PerformJet(Vector2 dir) {
        this.Hull.RequestJetDir(dir);
    }

    public float FindMaxWeaponRange() {
        float maxRange = 0;
        foreach (WeaponPart part in Hull.GetAllWeapons()) {
            if (maxRange < part.Schematic.Range) {
                maxRange = part.Schematic.Range;
            }
        }

        return maxRange;
    }
}
