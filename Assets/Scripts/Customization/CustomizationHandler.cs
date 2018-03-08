using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.SceneManagement;

public class CustomizationHandler : MonoBehaviour 
{
    public enum StateType
    {
        EquippedItemSelect,
        OtherItemSelect,
    }

    [SerializeField]
    private SimpleObjectPool equippedPartsItemPool;
    public SimpleObjectPool EquippedPartsItemPool
    {
        get {
            return equippedPartsItemPool;
        }
    }

    [SerializeField]
    private Transform equippedPartsItemRoot;
    public Transform EquippedPartsItemRoot
    {
        get {
            return equippedPartsItemRoot;
        }
    }

    [SerializeField]
    private SimpleObjectPool otherPartsItemPool;
    public SimpleObjectPool OtherPartsItemPool
    {
        get {
            return otherPartsItemPool;
        }
    }

    [SerializeField]
    private OtherPartsDisplaySection otherPartsDisplaySection;
    public OtherPartsDisplaySection OtherPartsDisplaySection
    {
        get {
            return otherPartsDisplaySection;
        }
    }

    [SerializeField]
    private Transform otherPartsItemsRoot;
    public Transform OtherPartsItemsRoot
    {
        get {
            return otherPartsItemsRoot;
        }
    }

    [SerializeField]
    PartInfo partInfo;

    [HideInInspector]
    public EquippedPartsItem PickedPartsItem;

    public List<PartSlot> EquippedParts
    {
        get; private set;
    }

    private Dictionary<StateType, State> states = new Dictionary<StateType, State>();
    private State curState;
    private StateType curStateType;

	void Start()
	{
        states[StateType.EquippedItemSelect] = new SelectEquippedItemsState(this);
        states[StateType.OtherItemSelect] = new SelectOtherItemsState(this);

        curStateType = StateType.EquippedItemSelect;
        curState = states[curStateType];

        initEquippedParts();

        curState.Start();
    }

    void Update() {
        curState.PerformUpdate();
    }

    public void GotoState(StateType stateType) {
        curState.End();

        curStateType = stateType;
        curState = states[curStateType];

        curState.Start();
    }

    public void BackToMain() {
        updatePlayerTankSchematic();
        SceneManager.LoadScene("Main");
    }

    public void UpdateEquippedParts(PartSchematic newPart) {
        PartSlot curPickedSlot = PickedPartsItem.Slot;
        PartSchematic oldPart = curPickedSlot.Part;
        curPickedSlot.UpdatePart(newPart);

        if (newPart.PType == PartSchematic.PartType.Turret) {
            TurretPartSchematic oldTurret = (TurretPartSchematic)oldPart;
            TurretPartSchematic newTurret = (TurretPartSchematic)newPart;

            if (oldTurret.OrigWeaponDirs.Length != newTurret.OrigWeaponDirs.Length) {
                int lengthDiff = newTurret.OrigWeaponDirs.Length - oldTurret.OrigWeaponDirs.Length;

                if (lengthDiff > 0) {
                    for (int i = 0; i < lengthDiff; ++i) {
                        EquippedParts.Add(new PartSlot(null, PartSchematic.PartType.Weapon, EquippedParts.Count));
                    }
                } else if (lengthDiff < 0) {
                    for (int i = 0; i < Math.Abs(lengthDiff); ++i) {
                        EquippedParts.RemoveAt(EquippedParts.Count - 1);
                    }
                }
            }
        }
    }

    public void UpdatePartInfo(PartSchematic part) {
        PartSchematic equipPart = PickedPartsItem != null ? PickedPartsItem.Slot.Part : null;
        partInfo.UpdatePartText(part, equipPart);
    }

    private void updatePlayerTankSchematic() {
        TankSchematic schem = PlayerManager.Instance.TankSchematic;

        int weaponCount = 0;
        foreach(PartSlot slot in EquippedParts) {
            switch (slot.PartType) {
                case PartSchematic.PartType.Hull:
                    schem.HullSchematic = (HullPartSchematic)slot.Part;
                    break;
                case PartSchematic.PartType.Turret:
                    schem.TurretSchematic = (TurretPartSchematic)slot.Part;
                    schem.WeaponSchematics = new WeaponPartSchematic[schem.TurretSchematic.OrigWeaponDirs.Length];
                    break;
                case PartSchematic.PartType.Weapon:
                    schem.WeaponSchematics[weaponCount] = (WeaponPartSchematic)slot.Part;
                    weaponCount += 1;
                    break;
            }
        }
    }

    private void initEquippedParts() {
        EquippedParts = new List<PartSlot>();

        TankSchematic playerSchematic = PlayerManager.Instance.TankSchematic;

        List<PartSchematic> schematics = new List<PartSchematic> {
            playerSchematic.HullSchematic,
            playerSchematic.TurretSchematic
        };
        schematics.AddRange(playerSchematic.WeaponSchematics);

        for (int i = 0; i < schematics.Count; ++i) {
            PartSchematic schematic = schematics[i];

            PartSchematic.PartType pType = (schematic != null) ? schematic.PType : PartSchematic.PartType.Weapon;
            
            EquippedParts.Add(new PartSlot(schematic, pType, i));
        }
    }
}
