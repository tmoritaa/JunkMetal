using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

public class PartNameDisplay : MonoBehaviour 
{
    [SerializeField]
    private Text partNameText;

	public void SetPart(PartSchematic part) {
        partNameText.text = part.Name;
    }

    public void SetTextDirectly(string text) {
        partNameText.text = text;
    }
}
