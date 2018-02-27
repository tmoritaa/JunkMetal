using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.SceneManagement;

public class MainScreenManager : MonoBehaviour 
{
	public void GotoCombat() {
        SceneManager.LoadScene("Combat");
    }

    public void GotoCustomization() {
        SceneManager.LoadScene("Customization");
    }
}
