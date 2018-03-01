using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

public class OtherPartsItem : MonoBehaviour 
{
    [SerializeField]
    private Text partNameDisplay;

    public PartSchematic Part
    {
        get; private set;
    }

    public void Init(PartSchematic schematic) {
        Part = schematic;
        partNameDisplay.text = Part != null ? Part.Name : "Empty";
    }

    public void Cleanup() {
        this.GetComponent<Button>().onClick.RemoveAllListeners();
        this.GetComponent<Button>().interactable = true;
    }
}
