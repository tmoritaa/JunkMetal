using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class WheelPart
{
    private enum Side
    {
        left,
        right,
    }

    public WheelPartSchematic Schematic
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

    public WheelPart(WheelPartSchematic schematic) {
        Schematic = schematic;
        LeftCurPower = 0;
        RightCurPower = 0;
    }

    public void HandleInput() {
        int leftChangeDir = 0;
        int rightChangeDir = 0;

        if (Input.GetKey(Schematic.LeftForwardKey)) {
            leftChangeDir += 1;
        }
        if (Input.GetKey(Schematic.LeftBackwardKey)) {
            leftChangeDir -= 1;
        }

        if (Input.GetKey(Schematic.RightForwardKey)) {
            rightChangeDir += 1;
        }
        if (Input.GetKey(Schematic.RightBackwardKey)) {
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
            power += Schematic.EnergyInc;
            handled = true;
        } else if (changeDir < 0) {
            power -= Schematic.EnergyInc;
            handled = true;
        }

        if (!handled && Mathf.Abs(power) > 0) {
            power = Mathf.Sign(power) * (Mathf.Abs(power) - Schematic.EnergyDec);
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
