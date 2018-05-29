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
    private Dictionary<KeyType, List<object>> controllerBindings = new Dictionary<KeyType, List<object>>();

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
        bool valid = false;
        if (kType == KeyType.LeftWheelFwd || kType == KeyType.LeftWheelBack || kType == KeyType.RightWheelFwd || kType == KeyType.RightWheelBack || kType == KeyType.FireWeapon0 || kType == KeyType.FireWeapon1) {
            foreach (object binding in controllerBindings[kType]) {
                string keyName = (string)binding;
                float axisVal = Input.GetAxis(keyName);

                if (kType == KeyType.LeftWheelFwd || kType == KeyType.RightWheelFwd || kType == KeyType.FireWeapon0) {
                    valid = axisVal < 0;
                } else {
                    valid = axisVal > 0;
                }

                if (valid) {
                    break;
                }
            }
        } else if (kType == KeyType.JetDown || kType == KeyType.JetUp || kType == KeyType.JetLeft || kType == KeyType.JetRight) {
            foreach (object binding in controllerBindings[kType]) {
                if (binding.GetType() == typeof(string)) {
                    string keyName = (string)binding;
                    float axisVal = Input.GetAxis(keyName);

                    if (kType == KeyType.JetLeft || kType == KeyType.JetDown) {
                        valid = axisVal < 0;
                    } else {
                        valid = axisVal > 0;
                    }

                    if (valid) {
                        break;
                    }
                } else {
                    KeyCode keyCode = (KeyCode)binding;
                    valid = onlyUp ? Input.GetKeyUp(keyCode) : Input.GetKey(keyCode);

                    if (valid) {
                        break;
                    }
                }
            }
        } else {
            foreach (object binding in controllerBindings[kType]) {
                KeyCode keyCode = (KeyCode)binding;
                valid = onlyUp ? Input.GetKeyUp(keyCode) : Input.GetKey(keyCode);

                if (valid) {
                    break;
                }
            }
            
        }

        return valid;
    }

    private void initKeyboardBindings() {
        keyboardBindings[KeyType.LeftWheelFwd] = new List<KeyCode>() { KeyCode.W };
        keyboardBindings[KeyType.LeftWheelBack] = new List<KeyCode>() { KeyCode.S };
        keyboardBindings[KeyType.RightWheelFwd] = new List<KeyCode>() { KeyCode.E };
        keyboardBindings[KeyType.RightWheelBack] = new List<KeyCode>() { KeyCode.D };
        keyboardBindings[KeyType.JetLeft] = new List<KeyCode>() { KeyCode.G };
        keyboardBindings[KeyType.JetRight] = new List<KeyCode>() { KeyCode.J };
        keyboardBindings[KeyType.JetUp] = new List<KeyCode>() { KeyCode.Y };
        keyboardBindings[KeyType.JetDown] = new List<KeyCode>() { KeyCode.H };
        keyboardBindings[KeyType.FireWeapon0] = new List<KeyCode>() { KeyCode.U };
        keyboardBindings[KeyType.FireWeapon1] = new List<KeyCode>() { KeyCode.I };
        keyboardBindings[KeyType.FireWeapon2] = new List<KeyCode>() { KeyCode.O };
        keyboardBindings[KeyType.FireWeapon3] = new List<KeyCode>() { KeyCode.P };
        keyboardBindings[KeyType.Cancel] = new List<KeyCode>() { KeyCode.Escape, KeyCode.Mouse1 };
    }

    private void initControllerBindings() {
        controllerBindings[KeyType.LeftWheelFwd] = new List<object>() { "Left Stick Y Axis" };
        controllerBindings[KeyType.LeftWheelBack] = new List<object>() { "Left Stick Y Axis" };
        controllerBindings[KeyType.RightWheelFwd] = new List<object>() { "Right Stick Y Axis" };
        controllerBindings[KeyType.RightWheelBack] = new List<object>() { "Right Stick Y Axis" };
        controllerBindings[KeyType.JetLeft] = new List<object>() { "Analog Horizontal", KeyCode.Joystick1Button2 };
        controllerBindings[KeyType.JetRight] = new List<object>() { "Analog Horizontal", KeyCode.Joystick1Button1 };
        controllerBindings[KeyType.JetUp] = new List<object>() { "Analog Vertical", KeyCode.Joystick1Button3 };
        controllerBindings[KeyType.JetDown] = new List<object>() { "Analog Vertical", KeyCode.Joystick1Button0 };
        controllerBindings[KeyType.FireWeapon0] = new List<object>() { "Triggers" };
        controllerBindings[KeyType.FireWeapon1] = new List<object>() { "Triggers" };
        controllerBindings[KeyType.FireWeapon2] = new List<object>() { KeyCode.JoystickButton5 };
        controllerBindings[KeyType.FireWeapon3] = new List<object>() { KeyCode.JoystickButton4 };
        controllerBindings[KeyType.Cancel] = new List<object>() { KeyCode.Joystick1Button1 };
    }
}
