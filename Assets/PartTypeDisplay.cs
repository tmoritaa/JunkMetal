using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

public class PartTypeDisplay : MonoBehaviour 
{
    [SerializeField]
    private Text partTypeName;

    public void SetPart(PartSchematic part) {
        partTypeName.text = part.GetPartTypeString();
    }

    public void SetTextDirectly(string text) {
        partTypeName.text = text;
    }
}
