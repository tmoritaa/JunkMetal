using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class TankUtility
{
    public static Vector2 CalcAppliedLinearForce(TankStateInfo stateInfo) {
        Vector3 leftForceVec = stateInfo.ForwardVec * stateInfo.LeftCurPower * stateInfo.EnergyPower / 2f;
        Vector3 rightForceVec = stateInfo.ForwardVec * stateInfo.RightCurPower * stateInfo.EnergyPower / 2f;

        Vector2 linearForce = rightForceVec + leftForceVec;

        return linearForce;
    }

    public static float CalcAppliedTorque(TankStateInfo stateInfo) {
        Vector3 leftForceVec = stateInfo.ForwardVec * stateInfo.LeftCurPower * stateInfo.EnergyPower / 2f;
        Vector3 rightForceVec = stateInfo.ForwardVec * stateInfo.RightCurPower * stateInfo.EnergyPower / 2f;

        float width = stateInfo.Size.x;
        Vector3 rightR = new Vector2(width / 2f, 0).Rotate(stateInfo.Rot);
        Vector3 rightTorque = Vector3.Cross(rightR, rightForceVec);

        Vector3 leftR = new Vector2(-width / 2f, 0).Rotate(stateInfo.Rot);
        Vector3 leftTorque = Vector3.Cross(leftR, leftForceVec);

        return rightTorque.z + leftTorque.z;
    }
}
