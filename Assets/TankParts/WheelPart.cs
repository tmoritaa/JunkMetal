using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class WheelPart
{
    public float CurPower
    {
        get; private set;
    }

    private Tank owningTank;

    private KeyCode forwardKey;
    private KeyCode backwardKey;

    public WheelPart(Tank _tank, KeyCode _forwardKey, KeyCode _backwardKey) {
        owningTank = _tank;
        forwardKey = _forwardKey;
        backwardKey = _backwardKey;

        Debug.Log("Wheel keys: forward=" + forwardKey + " backwards=" + backwardKey);
    }

    public void HandleInput() {
        bool handled = false;

        // Add power increase and clamp based on key input.
        if (Input.GetKey(forwardKey)) {
            CurPower += owningTank.PowerIncPerTS;
            handled = true;
        }
        if (Input.GetKey(backwardKey)) {
            CurPower -= owningTank.PowerIncPerTS;
            handled = true;
        }
        if (!handled && Mathf.Abs(CurPower) > 0) {
            CurPower = Mathf.Sign(CurPower) * (Mathf.Abs(CurPower) - owningTank.PowerDeterPerTS);
        }
        CurPower = Mathf.Clamp(CurPower, -1.0f, 1.0f);

        // Force 0 if below sigma.
        if (Mathf.Abs(CurPower) < 0.001f) {
            CurPower = 0;
        }
    }
}
