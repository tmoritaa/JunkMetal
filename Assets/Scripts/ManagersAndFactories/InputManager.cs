using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class InputManager
{
    public enum KeyType
    {
        LeftWheelFwd,
        LeftWheelBack,
        RightWheelFwd,
        RightWheelBack,
        TurretCW,
        TurretCCW,
        FireWeapon0 = 100,
        FireWeapon1,
        FireWeapon2,
    }

    private static InputManager instance = null;

    private Dictionary<KeyType, KeyCode> keyboardBindings = new Dictionary<KeyType, KeyCode>();

    public static InputManager Instance {
        get {
            if (instance == null) {
                instance = new InputManager();
            }

            return instance;
        }
    }

    private InputManager() {
        initKeyboardBindings();
    }

    public KeyCode GetKeyCodeForKeyboard(KeyType kType) {
        return keyboardBindings[kType];
    }

    private void initKeyboardBindings() {
        keyboardBindings[KeyType.LeftWheelFwd] = KeyCode.W;
        keyboardBindings[KeyType.LeftWheelBack] = KeyCode.S;
        keyboardBindings[KeyType.RightWheelFwd] = KeyCode.U;
        keyboardBindings[KeyType.RightWheelBack] = KeyCode.J;
        keyboardBindings[KeyType.TurretCW] = KeyCode.T;
        keyboardBindings[KeyType.TurretCCW] = KeyCode.R;
        keyboardBindings[KeyType.FireWeapon0] = KeyCode.P;
        keyboardBindings[KeyType.FireWeapon1] = KeyCode.O;
        keyboardBindings[KeyType.FireWeapon2] = KeyCode.I;
    }
}
