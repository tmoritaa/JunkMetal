using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class TankPartHighlighter : MonoBehaviour 
{
    [SerializeField]
    private TankGOConstructor tankGOConstructor;

    public void UpdateTankDisplay(TankSchematic schematic) {
        tankGOConstructor.Clear();
        tankGOConstructor.Init(schematic);
    }
}
