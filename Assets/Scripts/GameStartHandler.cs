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

    [SerializeField]
    private List<Button> mainButtons;

    [SerializeField]
    private GameObject confirmationGO;

    [SerializeField]
    private Button confirmationNoButton;
    
    void Start() {
        confirmationGO.SetActive(false);

        if (!PlayerManager.Instance.PlayerSaveExists()) {
            continueButton.gameObject.SetActive(false);
            mainButtons[0].Select();
        } else {
            continueButton.Select();
        }
    }

    public void StartNewGamePressed() {
        if (PlayerManager.Instance.PlayerSaveExists()) {
            confirmationGO.SetActive(true);
            mainButtons.ForEach(b => b.interactable = false);
            confirmationNoButton.Select();
        } else {
            startNewGame();
        }
    }

    public void ContinueGamePressed() {
        PlayerManager.Instance.LoadPlayerInfo();

        gotoMainGameScreen();
    }

    public void ExitGame() {
        Debug.Log("Game Exited");
        Application.Quit();
    }

    public void YesButtonPressed() {
        startNewGame();
    }

    public void NoButtonPressed() {
        mainButtons.ForEach(b => b.interactable = true);
        confirmationGO.SetActive(false);
        continueButton.Select();
    }

    private void gotoMainGameScreen() {
        SceneManager.LoadScene("Main");
    }

    private void startNewGame() {
        PlayerManager.Instance.ClearSavedPlayerInfo();
        PlayerManager.Instance.LoadPlayerInfo();

        gotoMainGameScreen();
    }
}
