using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

public class DeathScreen : MonoBehaviour 
{
    [SerializeField]
    private Text text;

    [SerializeField]
    private Button continueButton;
	
	public void SetupDeathScreen(Tank wonTank) {
        if (wonTank == CombatHandler.Instance.AITankController.SelfTank) {
            text.text = "You have lost";
        } else {
            text.text = "You have won";
        }

        continueButton.Select();
    }
}
