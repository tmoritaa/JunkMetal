using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameStartHandler : MonoBehaviour 
{
    [SerializeField]
    private Button continueButton;

    void Awake() {
        if (!PlayerManager.Instance.PlayerSaveExists()) {
            continueButton.gameObject.SetActive(false);
        }
    }

    public void StartNewGame() {
        PlayerManager.Instance.ClearSavedPlayerInfo();
        PlayerManager.Instance.LoadPlayerInfo();

        gotoMainGameScreen();
    }

    public void ContinueGame() {
        PlayerManager.Instance.LoadPlayerInfo();

        gotoMainGameScreen();
    }

    public void ExitGame() {
        Debug.Log("Game Exited");
        Application.Quit();
    }

    private void gotoMainGameScreen() {
        SceneManager.LoadScene("Main");
    }
}
