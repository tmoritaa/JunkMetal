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

    [SerializeField]
    TankPartHighlighter tankHighlighter;

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

        initEquippedPartsViaPlayerSchematic();

        updateTankDisplayToCurrent();

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
        if (isValidTankSchematic()) {
            updatePlayerTankSchematic();
            SceneManager.LoadScene("Main");
        }
    }

    public void UpdateEquippedParts(PartSchematic newPart) {
        PartSlot curPickedSlot = PickedPartsItem.Slot;
        PartSchematic oldPart = curPickedSlot.Part;
        curPickedSlot.UpdatePart(newPart);

        if (newPart != null && newPart.PType == PartSchematic.PartType.Hull) {
            HullPartSchematic oldHull = (HullPartSchematic)oldPart;
            HullPartSchematic newHull = (HullPartSchematic)newPart;

            if (oldHull.OrigWeaponDirs.Length != newHull.OrigWeaponDirs.Length) {
                int lengthDiff = newHull.OrigWeaponDirs.Length - oldHull.OrigWeaponDirs.Length;

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
            
            int count = 0;
            foreach (PartSlot slot in EquippedParts) {
                if (slot.PartType == PartSchematic.PartType.Weapon) {
                    PartSchematic.WeaponTier tier = newHull.WeaponTierRestrictions[count];
                    if (slot.Part != null && ((WeaponPartSchematic)slot.Part).Tier > tier) {
                        slot.UpdatePart(null);
                    }

                    count += 1;
                }
            }
        }

        updateTankDisplayToCurrent();
    }

    public void UpdatePartInfo(PartSchematic part) {
        PartSchematic equipPart = PickedPartsItem != null ? PickedPartsItem.Slot.Part : null;
        partInfo.UpdatePartText(part, equipPart);
    }

    private void updateTankDisplayToCurrent() {
        TankSchematic tankSchem = partSlotsToTankSchematic();

        tankHighlighter.UpdateTankDisplay(tankSchem);
    }

    private bool isValidTankSchematic() {
        int weaponCount = 0;
        foreach (PartSlot slot in EquippedParts) {
            switch (slot.PartType) {
                case PartSchematic.PartType.Weapon:
                    weaponCount += slot.Part != null ? 1 : 0;
                    break;
            }
        }

        return weaponCount > 0;
    }

    private TankSchematic partSlotsToTankSchematic() {
        HullPartSchematic hullSchem = null;
        List<WeaponPartSchematic> weaponSchems = new List<WeaponPartSchematic>();

        foreach (PartSlot slot in EquippedParts) {
            switch (slot.PartType) {
                case PartSchematic.PartType.Hull:
                    hullSchem = (HullPartSchematic)slot.Part;
                    break;
                case PartSchematic.PartType.Weapon:
                    weaponSchems.Add((WeaponPartSchematic)slot.Part);
                    break;
            }
        }

        Debug.Assert(hullSchem != null && weaponSchems.Count > 0, "PartSlots to Tank Schematic resulted in invalid tank schematic. Should never happen.");

        return new TankSchematic(hullSchem, weaponSchems.ToArray());
    }

    private void updatePlayerTankSchematic() {
        PlayerManager.Instance.UpdateTankSchematic(partSlotsToTankSchematic());
    }

    private void initEquippedPartsViaPlayerSchematic() {
        EquippedParts = new List<PartSlot>();

        TankSchematic playerSchematic = PlayerManager.Instance.TankSchematic;

        List<PartSchematic> schematics = new List<PartSchematic> {
            playerSchematic.HullSchematic,
        };
        schematics.AddRange(playerSchematic.WeaponSchematics);

        for (int i = 0; i < schematics.Count; ++i) {
            PartSchematic schematic = schematics[i];

            PartSchematic.PartType pType = (schematic != null) ? schematic.PType : PartSchematic.PartType.Weapon;
            
            EquippedParts.Add(new PartSlot(schematic, pType, i));
        }
    }
}
