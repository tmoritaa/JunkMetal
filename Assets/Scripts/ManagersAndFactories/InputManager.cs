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
    private Dictionary<KeyType, object> controllerBindings = new Dictionary<KeyType, object>();

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
        initControllerBindings();
    }

    public bool IsKeyTypeDown(KeyType kType) {
        return isKeyboardKeyTypeDown(kType) || isControllerKeyTypeDown(kType);
    }

    private bool isKeyboardKeyTypeDown(KeyType kType) {
        KeyCode kCode = keyboardBindings[kType];
        return Input.GetKey(kCode);
    }

    private bool isControllerKeyTypeDown(KeyType kType) {
        if (kType == KeyType.LeftWheelFwd || kType == KeyType.LeftWheelBack || kType == KeyType.RightWheelFwd || kType == KeyType.RightWheelBack) {
            string keyName = (string)controllerBindings[kType];
            float axisVal = Input.GetAxis(keyName);

            if (kType == KeyType.LeftWheelFwd || kType == KeyType.RightWheelFwd) {
                return axisVal < 0;
            } else {
                return axisVal > 0;
            }
        } else {
            KeyCode keyCode = (KeyCode)controllerBindings[kType];
            return Input.GetKey(keyCode);            
        }
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

    private void initControllerBindings() {
        controllerBindings[KeyType.LeftWheelFwd] = "Left Stick Y Axis";
        controllerBindings[KeyType.LeftWheelBack] = "Left Stick Y Axis";
        controllerBindings[KeyType.RightWheelFwd] = "Right Stick Y Axis";
        controllerBindings[KeyType.RightWheelBack] = "Right Stick Y Axis";
        controllerBindings[KeyType.TurretCW] = KeyCode.JoystickButton5;
        controllerBindings[KeyType.TurretCCW] = KeyCode.JoystickButton4;
        controllerBindings[KeyType.FireWeapon0] = KeyCode.JoystickButton2;
        controllerBindings[KeyType.FireWeapon1] = KeyCode.JoystickButton3;
        controllerBindings[KeyType.FireWeapon2] = KeyCode.JoystickButton1;
    }
}
