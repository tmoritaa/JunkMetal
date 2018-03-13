﻿using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class HullPart
{
    public HullPartSchematic Schematic
    {
        get; private set;
    }

    private enum Side
    {
        left,
        right,
    }

    public int LeftCurPower
    {
        get; private set;
    }

    public int RightCurPower
    {
        get; private set;
    }

    public HullPart(HullPartSchematic _schematic) {
        Schematic = _schematic;
        LeftCurPower = 0;
        RightCurPower = 0;
    }

    public void HandleInput() {
        int leftChangeDir = 0;
        int rightChangeDir = 0;

        if (InputManager.Instance.IsKeyTypeDown(InputManager.KeyType.LeftWheelFwd)) {
            leftChangeDir += 1;
        }
        if (InputManager.Instance.IsKeyTypeDown(InputManager.KeyType.LeftWheelBack)) {
            leftChangeDir -= 1;
        }

        if (InputManager.Instance.IsKeyTypeDown(InputManager.KeyType.RightWheelFwd)) {
            rightChangeDir += 1;
        }
        if (InputManager.Instance.IsKeyTypeDown(InputManager.KeyType.RightWheelBack)) {
            rightChangeDir -= 1;
        }

        PerformPowerChange(leftChangeDir, rightChangeDir);
    }

    public void Reset() {
        LeftCurPower = 0;
        RightCurPower = 0;
    }

    public void PerformPowerChange(int leftChangeDir, int rightChangeDir) {
        performPowerChangeForSide(Side.left, leftChangeDir);
        performPowerChangeForSide(Side.right, rightChangeDir);
    }

    private void performPowerChangeForSide(Side side, int changeDir) {
        int power = (side == Side.left) ? LeftCurPower : RightCurPower;

        bool handled = false;

        // Add power increase and clamp based on key input.
        if (changeDir > 0) {
            power += 1;
            handled = true;
        } else if (changeDir < 0) {
            power -= 1;
            handled = true;
        }

        power = Math.Min(Math.Max(-1, power), 1);

        if (!handled && Mathf.Abs(power) > 0) {
            power = 0;
        }

        if (side == Side.left) {
            LeftCurPower = power;
        } else {
            RightCurPower = power;
        }
    }
}
