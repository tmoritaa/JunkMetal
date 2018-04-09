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

    public float CurEnergy
    {
        get; private set;
    }

    private Dictionary<InputManager.KeyType, bool> jetUsageForFixedUpdate = new Dictionary<InputManager.KeyType, bool>();

    private WeaponPart[] weapons;

    private Tank owner;

    public HullPart(HullPartSchematic _schematic, Tank _owner) {
        Schematic = _schematic;
        owner = _owner;
        LeftCurPower = 0;
        RightCurPower = 0;
        CurEnergy = Schematic.Energy;

        weapons = new WeaponPart[Schematic.OrigWeaponDirs.Length];
        Array.Clear(weapons, 0, weapons.Length);

        jetUsageForFixedUpdate[InputManager.KeyType.JetLeft] = false;
        jetUsageForFixedUpdate[InputManager.KeyType.JetRight] = false;
        jetUsageForFixedUpdate[InputManager.KeyType.JetUp] = false;
        jetUsageForFixedUpdate[InputManager.KeyType.JetDown] = false;
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

        handleJetInput();

        foreach (WeaponPart weapon in GetAllWeapons()) {
            weapon.HandleInput();
        }
    }

    public void PerformFixedUpdate() {
        foreach (WeaponPart weapon in GetAllWeapons()) {
            weapon.PerformFixedUpdate();
        }

        bool activated = activateJetsIfRequested();

        if (!activated) {
            refreshEnergy(Time.fixedDeltaTime);
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
        if (weapon.Schematic.Tier <= Schematic.WeaponTierRestrictions[idx]) {
            weapon.EquipIdx = idx;
            weapons[idx] = weapon;
        }
    }

    public float GetMaxRange() {
        return GetAllWeapons().Max(w => w.Schematic.Range);
    }

    public bool IsAllWeaponsReloading() {
        bool noReload = true;
        foreach (WeaponPart part in GetAllWeapons()) {
            noReload = part.CalcTimeToReloaded() == 0;

            if (!noReload) {
                break;
            }
        }

        return !noReload;
    }

    public void PerformPowerChange(int leftChangeDir, int rightChangeDir) {
        performPowerChangeForSide(Side.left, leftChangeDir);
        performPowerChangeForSide(Side.right, rightChangeDir);
    }

    public void ModEnergy(float energyModVal) {
        CurEnergy = Mathf.Clamp(CurEnergy + energyModVal, 0, Schematic.Energy);
    }


    public bool EnergyAvailableForUsage(float energyVal) {
        return CurEnergy >= energyVal;
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

    private bool activateJetsIfRequested() {
        bool activated = false;

        InputManager.KeyType[] kTypes = new InputManager.KeyType[] { InputManager.KeyType.JetLeft, InputManager.KeyType.JetRight, InputManager.KeyType.JetUp, InputManager.KeyType.JetDown };
        Vector2[] jetDirs = new Vector2[] { new Vector2(-1, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(0, -1) };

        Vector2[] curDirs = new Vector2[] { owner.GetForwardVec(), owner.GetBackwardVec(), owner.GetForwardVec().Rotate(90f), owner.GetForwardVec().Rotate(-90f) };
        for (int i = 0; i < kTypes.Length; ++i) {
            InputManager.KeyType key = kTypes[i];
            Vector2 jetDir = jetDirs[i];

            if (jetUsageForFixedUpdate[key] && EnergyAvailableForUsage(Schematic.JetEnergyUsage)) {
                float minAngle = 9999f;
                Vector2 minDir = new Vector2();
                foreach (Vector2 curDir in curDirs) {
                    float angle = Vector2.Angle(curDir, jetDir);

                    if (minAngle > angle) {
                        minDir = curDir;
                        minAngle = angle;
                    }
                }

                owner.Body.AddForce(minDir.normalized * Schematic.JetImpulse, ForceMode2D.Impulse);
                ModEnergy(-Schematic.JetEnergyUsage);

                activated = true;
            }

            jetUsageForFixedUpdate[key] = false;
        }

        return activated;
    }

    private void refreshEnergy(float timeDelta) {
        float refreshRate = Schematic.EnergyRefreshPerSec * timeDelta;
        ModEnergy(refreshRate);
    }

    private void handleJetInput() {
        if (InputManager.Instance.IsKeyTypeDown(InputManager.KeyType.JetLeft, true)) {
            jetUsageForFixedUpdate[InputManager.KeyType.JetLeft] = true;
        }
        if (InputManager.Instance.IsKeyTypeDown(InputManager.KeyType.JetRight, true)) {
            jetUsageForFixedUpdate[InputManager.KeyType.JetRight] = true;
        }
        if (InputManager.Instance.IsKeyTypeDown(InputManager.KeyType.JetUp, true)) {
            jetUsageForFixedUpdate[InputManager.KeyType.JetUp] = true;
        }
        if (InputManager.Instance.IsKeyTypeDown(InputManager.KeyType.JetDown, true)) {
            jetUsageForFixedUpdate[InputManager.KeyType.JetDown] = true;
        }
    }
}
