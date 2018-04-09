using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class PlayerStatArea : MonoBehaviour 
{
    [SerializeField]
    private bool displayEnergy;

    [SerializeField]
    private GameObject energyDisplay;

	void Start()
	{
        energyDisplay.SetActive(displayEnergy);
	}
}
