using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class EquippedPartsItem : MonoBehaviour 
{
    [SerializeField]
    private PartTypeDisplay partTypeDisplay;

    [SerializeField]
    private PartNameDisplay partNameDisplay;

    public Type PartSchematicType
    {
        get {
            if (PartSchematic != null) {
                return PartSchematic.GetType();
            } else {
                return typeof(WeaponPartSchematic);
            }
        }
    }

    public PartSchematic PartSchematic
    {
        get; private set;
    }

    public int ItemIdx
    {
        get; private set;
    }

    public void Init(PartSchematic schematic, int idx) {
        ItemIdx = idx;

        if (schematic != null) {
            PartSchematic = schematic;
            partTypeDisplay.SetPart(schematic);
            partNameDisplay.SetPart(schematic);

        // This case should only happen with weapons.
        } else {
            partTypeDisplay.SetTextDirectly("Weapon");
            partNameDisplay.SetTextDirectly("Empty");
        }
    }

    public void Cleanup() {
        PartSchematic = null;
        ItemIdx = -1;
        this.GetComponent<Button>().onClick.RemoveAllListeners();
        this.GetComponent<Button>().interactable = true;
    }
}
