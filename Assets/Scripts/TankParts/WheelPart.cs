using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class WheelPart
{
    // Should be between 50 - 200
    public float Weight
    {
        get; private set;
    }

    public float LeftCurPower
    {
        get; private set;
    }

    public float RightCurPower
    {
        get; private set;
    }

    public float EnergyInc
    {
        get; private set;
    }

    public float EnergyDec
    {
        get; private set;
    }

    private enum Side
    {
        left,
        right,
    }

    private Tank owningTank;

    private KeyCode leftForwardKey;
    private KeyCode leftBackwardKey;
    private KeyCode rightForwardKey;
    private KeyCode rightBackwardKey;

    public WheelPart(Tank _tank, float _energyInc, float _energyDec, float _weight, KeyCode _leftForwardKey, KeyCode _leftBackwardKey, KeyCode _rightForwardKey, KeyCode _rightBackwardKey) {
        owningTank = _tank;

        EnergyInc = _energyInc;
        EnergyDec = _energyDec;
        Weight = _weight;

        leftForwardKey = _leftForwardKey;
        leftBackwardKey = _leftBackwardKey;
        rightForwardKey = _rightForwardKey;
        rightBackwardKey = _rightBackwardKey;

        Debug.Log("Wheel keys: Lforward=" + leftForwardKey + " LBackwards=" + leftBackwardKey + " Rforward = " + rightForwardKey + " RBackwards = " + rightBackwardKey);
    }

    public void HandleInput() {
        int leftChangeDir = 0;
        int rightChangeDir = 0;

        if (Input.GetKey(leftForwardKey)) {
            leftChangeDir += 1;
        }
        if (Input.GetKey(leftBackwardKey)) {
            leftChangeDir -= 1;
        }

        if (Input.GetKey(rightForwardKey)) {
            rightChangeDir += 1;
        }
        if (Input.GetKey(rightBackwardKey)) {
            rightChangeDir -= 1;
        }

        PerformPowerChange(leftChangeDir, rightChangeDir);
    }

    public void PerformPowerChange(int leftChangeDir, int rightChangeDir) {
        performPowerChangeForSide(Side.left, leftChangeDir);
        performPowerChangeForSide(Side.right, rightChangeDir);
    }

    private void performPowerChangeForSide(Side side, int changeDir) {
        float power = (side == Side.left) ? LeftCurPower : RightCurPower;

        bool handled = false;

        // Add power increase and clamp based on key input.
        if (changeDir > 0) {
            power += EnergyInc;
            handled = true;
        } else if (changeDir < 0) {
            power -= EnergyInc;
            handled = true;
        }

        if (!handled && Mathf.Abs(power) > 0) {
            power = Mathf.Sign(power) * (Mathf.Abs(power) - EnergyDec);
        }
        power = Mathf.Clamp(power, -1.0f, 1.0f);

        // Force 0 if below sigma.
        if (Mathf.Abs(power) < 0.001f) {
            power = 0;
        }

        if (side == Side.left) {
            LeftCurPower = power;
        } else {
            RightCurPower = power;
        }
    }
}
