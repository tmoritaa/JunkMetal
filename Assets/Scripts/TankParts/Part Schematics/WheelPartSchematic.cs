using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

public class WheelPartSchematic : PartSchematic
{
    // Should be between 50 - 200
    public int Weight
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

    public KeyCode LeftForwardKey
    {
        get; private set;
    }
    public KeyCode LeftBackwardKey
    {
        get; private set;
    }
    public KeyCode RightForwardKey
    {
        get; private set;
    }
    public KeyCode RightBackwardKey
    {
        get; private set;
    }

    public WheelPartSchematic(string name, float _energyInc, float _energyDec, int _weight, KeyCode _leftForwardKey, KeyCode _leftBackwardKey, KeyCode _rightForwardKey, KeyCode _rightBackwardKey) : base(name) {
        Name = name;
        EnergyInc = _energyInc;
        EnergyDec = _energyDec;
        Weight = _weight;

        LeftForwardKey = _leftForwardKey;
        LeftBackwardKey = _leftBackwardKey;
        RightForwardKey = _rightForwardKey;
        RightBackwardKey = _rightBackwardKey;
    }
}
