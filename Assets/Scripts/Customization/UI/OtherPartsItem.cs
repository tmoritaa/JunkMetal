using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class OtherPartsItem : MonoBehaviour, ISelectHandler
{
    [SerializeField]
    private Text partNameDisplay;

    public PartSchematic Part
    {
        get; private set;
    }

    private CustomizationHandler handler;

    public void Init(PartSchematic schematic, CustomizationHandler _handler) {
        Part = schematic;
        partNameDisplay.text = Part != null ? Part.Name : "Empty";
        handler = _handler;
    }

    public void Cleanup() {
        this.GetComponent<Button>().onClick.RemoveAllListeners();
        this.GetComponent<Button>().interactable = true;
    }

    public void OnSelect(BaseEventData eventData) {
        handler.UpdatePartInfo(Part);
    }
}
