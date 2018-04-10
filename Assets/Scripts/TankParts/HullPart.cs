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

    private List<Vector2> jetUsageDirs = new List<Vector2>();

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

    public void RequestJetDir(Vector2 dir) {
        jetUsageDirs.Add(dir);
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

        HullPrefabInfo hullPrefabInfo = PartPrefabManager.Instance.GetHullPrefabInfoViaName(Schematic.Name);
        
        Vector2[] curDirs = new Vector2[] { owner.GetForwardVec(), owner.GetBackwardVec(), owner.GetForwardVec().Rotate(90f), owner.GetForwardVec().Rotate(-90f) };
        string[] curDirJetOffsetNames = new string[] { "bot", "top", "right", "left" };
        foreach (Vector2 jetDir in jetUsageDirs) {
            if (EnergyAvailableForUsage(Schematic.JetEnergyUsage)) {
                float minAngle = 9999f;
                Vector2 minDir = new Vector2();
                string dirJetOffsetName = "";
                for (int i = 0; i < curDirs.Length; ++i) {
                    Vector2 curDir = curDirs[i];

                    float angle = Vector2.Angle(curDir, jetDir);

                    if (minAngle > angle) {
                        minDir = curDir;
                        minAngle = angle;
                        dirJetOffsetName = curDirJetOffsetNames[i];
                    }
                }

                owner.Body.AddForce(minDir.normalized * Schematic.JetImpulse, ForceMode2D.Impulse);

                Vector2 jetOffset = hullPrefabInfo.JetOffsets[dirJetOffsetName];
                float jetAngle = Vector2.SignedAngle(owner.GetForwardVec(), minDir);
                CombatAnimationHandler.Instance.InstantiatePrefab("jet_cloud", jetOffset, jetAngle, owner.JetRoot, true);

                ModEnergy(-Schematic.JetEnergyUsage);

                activated = true;
            }
        }

        jetUsageDirs.Clear();

        return activated;
    }

    private void refreshEnergy(float timeDelta) {
        float refreshRate = Schematic.EnergyRefreshPerSec * timeDelta;
        ModEnergy(refreshRate);
    }

    private void handleJetInput() {
        if (InputManager.Instance.IsKeyTypeDown(InputManager.KeyType.JetLeft, true)) {
            RequestJetDir(new Vector2(-1, 0));
        }
        if (InputManager.Instance.IsKeyTypeDown(InputManager.KeyType.JetRight, true)) {
            RequestJetDir(new Vector2(1, 0));
        }
        if (InputManager.Instance.IsKeyTypeDown(InputManager.KeyType.JetUp, true)) {
            RequestJetDir(new Vector2(0, 1));
        }
        if (InputManager.Instance.IsKeyTypeDown(InputManager.KeyType.JetDown, true)) {
            RequestJetDir(new Vector2(0, -1));
        }
    }
}
