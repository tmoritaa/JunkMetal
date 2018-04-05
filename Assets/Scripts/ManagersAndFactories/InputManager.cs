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
        Cancel,
        FireWeapon0 = 100,
        FireWeapon1,
        FireWeapon2,
        FireWeapon3,
    }

    private static InputManager instance = null;

    private Dictionary<KeyType, List<KeyCode>> keyboardBindings = new Dictionary<KeyType, List<KeyCode>>();
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

    public bool IsKeyTypeDown(KeyType kType, bool onlyUp=false) {
        return isKeyboardKeyTypeDown(kType, onlyUp) || isControllerKeyTypeDown(kType, onlyUp);
    }

    private bool isKeyboardKeyTypeDown(KeyType kType, bool onlyUp) {
        bool valid = false;
        foreach (KeyCode kCode in keyboardBindings[kType]) {
            bool b = onlyUp ? Input.GetKeyUp(kCode) : Input.GetKey(kCode);
            if (b) {
                valid = true;
                break;
            }
        }

        return valid;
    }

    private bool isControllerKeyTypeDown(KeyType kType, bool onlyUp) {
        if (kType == KeyType.LeftWheelFwd || kType == KeyType.LeftWheelBack || kType == KeyType.RightWheelFwd || kType == KeyType.RightWheelBack) {
            string keyName = (string)controllerBindings[kType];
            float axisVal = Input.GetAxis(keyName);

            if (kType == KeyType.LeftWheelFwd || kType == KeyType.RightWheelFwd) {
                return axisVal < 0;
            } else {
                return axisVal > 0;
            }
        } else if (kType == KeyType.FireWeapon0 || kType == KeyType.FireWeapon1) {
            string keyName = (string)controllerBindings[kType];
            float axisVal = Input.GetAxis(keyName);
            
            if (kType == KeyType.FireWeapon0) {
                return axisVal < 0;
            } else {
                return axisVal > 0;
            }
        } else {
            KeyCode keyCode = (KeyCode)controllerBindings[kType];
            return onlyUp ? Input.GetKeyUp(keyCode) : Input.GetKey(keyCode);            
        }
    }

    private void initKeyboardBindings() {
        keyboardBindings[KeyType.LeftWheelFwd] = new List<KeyCode>() { KeyCode.W };
        keyboardBindings[KeyType.LeftWheelBack] = new List<KeyCode>() { KeyCode.S };
        keyboardBindings[KeyType.RightWheelFwd] = new List<KeyCode>() { KeyCode.U };
        keyboardBindings[KeyType.RightWheelBack] = new List<KeyCode>() { KeyCode.J };
        keyboardBindings[KeyType.FireWeapon0] = new List<KeyCode>() { KeyCode.P };
        keyboardBindings[KeyType.FireWeapon1] = new List<KeyCode>() { KeyCode.O };
        keyboardBindings[KeyType.FireWeapon2] = new List<KeyCode>() { KeyCode.I };
        keyboardBindings[KeyType.Cancel] = new List<KeyCode>() { KeyCode.Escape, KeyCode.Mouse1 };
    }

    private void initControllerBindings() {
        controllerBindings[KeyType.LeftWheelFwd] = "Left Stick Y Axis";
        controllerBindings[KeyType.LeftWheelBack] = "Left Stick Y Axis";
        controllerBindings[KeyType.RightWheelFwd] = "Right Stick Y Axis";
        controllerBindings[KeyType.RightWheelBack] = "Right Stick Y Axis";
        controllerBindings[KeyType.FireWeapon0] = "Triggers";
        controllerBindings[KeyType.FireWeapon1] = "Triggers";
        controllerBindings[KeyType.FireWeapon2] = KeyCode.JoystickButton5;
        controllerBindings[KeyType.FireWeapon3] = KeyCode.JoystickButton4;
        controllerBindings[KeyType.Cancel] = KeyCode.Joystick1Button1;
    }
}
