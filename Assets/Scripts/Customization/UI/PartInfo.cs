﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

public class PartInfo : MonoBehaviour 
{
    [SerializeField]
    private Text text;

	public void UpdatePartText(PartSchematic selectedPart, PartSchematic equippedPart) {
        if (selectedPart != null) {
            text.text = selectedPart.GetStatString(equippedPart);
        } else {
            text.text = "";
        }
    }
}
