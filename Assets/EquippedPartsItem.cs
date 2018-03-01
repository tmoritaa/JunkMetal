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
    private Text partTypeDisplay;

    [SerializeField]
    private Text partNameDisplay;

    public PartSlot Slot
    {
        get; private set;
    }

    public void Init(PartSlot slot) {
        Slot = slot;

        partTypeDisplay.text = Slot.PartType.ToString();
        partNameDisplay.text = Slot.Part != null ? Slot.Part.Name : "Empty";
    }

    // TODO: only temporary. Once owned items uses a different class than this, we can remove this.
    public void Init(PartSchematic schematic) {
        Slot = new PartSlot(schematic, PartSchematic.PartType.Weapon, -1);

        partTypeDisplay.text = Slot.PartType.ToString();
        partNameDisplay.text = Slot.Part != null ? Slot.Part.Name : "Empty";
    }

    public void Cleanup() {
        Slot = null;
        this.GetComponent<Button>().onClick.RemoveAllListeners();
        this.GetComponent<Button>().interactable = true;
    }
}
