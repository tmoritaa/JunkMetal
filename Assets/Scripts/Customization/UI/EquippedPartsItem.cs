using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class EquippedPartsItem : MonoBehaviour, ISelectHandler
{
    [SerializeField]
    private Text partTypeDisplay;

    [SerializeField]
    private Text partNameDisplay;

    public PartSlot Slot
    {
        get; private set;
    }

    private CustomizationHandler handler;

    public void Init(PartSlot slot, CustomizationHandler _handler) {
        Slot = slot;
        handler = _handler;

        partTypeDisplay.text = Slot.PartType.ToString();
        partNameDisplay.text = Slot.Part != null ? Slot.Part.Name : "Empty";
    }

    public void Cleanup() {
        Slot = null;
        this.GetComponent<Button>().onClick.RemoveAllListeners();
        this.GetComponent<Button>().interactable = true;
    }

    public void OnSelect(BaseEventData eventData) {
        handler.UpdatePartInfo(Slot.Part);
    }
}
