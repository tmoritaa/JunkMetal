using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.SceneManagement;

public class MainScreenHandler : MonoBehaviour 
{
    void Start() {
        PlayerManager.Instance.SavePlayerInfo();
    }

    public void GotoCombat() {
        SceneManager.LoadScene("EnemySelection");
    }

    public void GotoCustomization() {
        SceneManager.LoadScene("Customization");
    }
}
