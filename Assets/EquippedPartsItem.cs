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

    public PartSchematic PartSchematic
    {
        get; private set;
    }

    public int ItemIdx
    {
        get; private set;
    }

    public void Init(PartSchematic schematic, string partTypeTextIfNull="") {
        bool isSchematicNull = schematic != null;
        GetComponent<Button>().interactable = isSchematicNull;

        if (isSchematicNull) {
            PartSchematic = schematic;
            partTypeDisplay.SetPart(schematic);
            partNameDisplay.SetPart(schematic);
        } else {
            partTypeDisplay.SetTextDirectly(partTypeTextIfNull);
            partNameDisplay.SetTextDirectly("");
        }
    }
}
