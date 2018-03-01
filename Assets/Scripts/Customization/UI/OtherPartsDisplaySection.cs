using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

public class OtherPartsDisplaySection : MonoBehaviour 
{
    [SerializeField]
    private Text headerText;

	public void UpdateHeaderText(PartSlot partSlot) {
        headerText.text = partSlot.PartType.ToString();
    }
}
