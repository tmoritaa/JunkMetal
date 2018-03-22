using System;
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

    private WeaponPart[] weapons;

    private Tank owner;

    public HullPart(HullPartSchematic _schematic) {
        Schematic = _schematic;
        LeftCurPower = 0;
        RightCurPower = 0;

        weapons = new WeaponPart[Schematic.OrigWeaponDirs.Length];
        Array.Clear(weapons, 0, weapons.Length);
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

        foreach (WeaponPart weapon in GetAllWeapons()) {
            weapon.HandleInput();
        }
    }

    public void PerformFixedUpdate() {
        foreach (WeaponPart weapon in GetAllWeapons()) {
            weapon.PerformFixedUpdate();
        }
    }

    public void Reset() {
        LeftCurPower = 0;
        RightCurPower = 0;
    }

    public List<WeaponPart> GetAllWeapons() {
        List<WeaponPart> weapons = new List<WeaponPart>();
        foreach (WeaponPart part in this.weapons) {
            if (part != null) {
                weapons.Add(part);
            }
        }

        return weapons;
    }

    public WeaponPart GetWeaponAtIdx(int idx) {
        if (weapons.Length <= idx) {
            return null;
        }

        return weapons[idx];
    }

    public void AddWeaponAtIdx(WeaponPart weapon, int idx) {
        if (weapon.Schematic.Weight <= Schematic.WeaponWeightRestrictions[idx]) {
            weapon.EquipIdx = idx;
            weapons[idx] = weapon;
        }
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
