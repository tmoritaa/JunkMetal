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
        JetLeft,
        JetRight,
        JetUp,
        JetDown,
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
        keyboardBindings[KeyType.RightWheelFwd] = new List<KeyCode>() { KeyCode.E };
        keyboardBindings[KeyType.RightWheelBack] = new List<KeyCode>() { KeyCode.D };
        keyboardBindings[KeyType.JetLeft] = new List<KeyCode>() { KeyCode.F };
        keyboardBindings[KeyType.JetRight] = new List<KeyCode>() { KeyCode.H };
        keyboardBindings[KeyType.JetUp] = new List<KeyCode>() { KeyCode.T };
        keyboardBindings[KeyType.JetDown] = new List<KeyCode>() { KeyCode.G };
        keyboardBindings[KeyType.FireWeapon0] = new List<KeyCode>() { KeyCode.U };
        keyboardBindings[KeyType.FireWeapon1] = new List<KeyCode>() { KeyCode.I };
        keyboardBindings[KeyType.FireWeapon2] = new List<KeyCode>() { KeyCode.O };
        keyboardBindings[KeyType.FireWeapon3] = new List<KeyCode>() { KeyCode.P };
        keyboardBindings[KeyType.Cancel] = new List<KeyCode>() { KeyCode.Escape, KeyCode.Mouse1 };
    }

    private void initControllerBindings() {
        controllerBindings[KeyType.LeftWheelFwd] = "Left Stick Y Axis";
        controllerBindings[KeyType.LeftWheelBack] = "Left Stick Y Axis";
        controllerBindings[KeyType.RightWheelFwd] = "Right Stick Y Axis";
        controllerBindings[KeyType.RightWheelBack] = "Right Stick Y Axis";
        controllerBindings[KeyType.JetLeft] = KeyCode.Joystick1Button2;
        controllerBindings[KeyType.JetRight] = KeyCode.Joystick1Button1;
        controllerBindings[KeyType.JetUp] = KeyCode.Joystick1Button3;
        controllerBindings[KeyType.JetDown] = KeyCode.Joystick1Button0;
        controllerBindings[KeyType.FireWeapon0] = "Triggers";
        controllerBindings[KeyType.FireWeapon1] = "Triggers";
        controllerBindings[KeyType.FireWeapon2] = KeyCode.JoystickButton5;
        controllerBindings[KeyType.FireWeapon3] = KeyCode.JoystickButton4;
        controllerBindings[KeyType.Cancel] = KeyCode.Joystick1Button1;
    }
}
