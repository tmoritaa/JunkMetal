using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class GameManager : MonoBehaviour 
{
    private static GameManager instance;
    public static GameManager Instance
    {
        get {
            return instance;
        }
    }

    private Camera mainCamera = null;
    public Camera MainCamera
    {
        get {
            if (mainCamera == null) {
                mainCamera = Camera.main;
            }

            return mainCamera;
        }
    }

    [SerializeField]
    private Tank tankPrefab;

    [SerializeField]
    private GameObject canvasRoot;

    private Tank playerTank;

    void Awake() {
        instance = this;

        playerTank = Instantiate(tankPrefab);
        playerTank.transform.SetParent(canvasRoot.transform, false);
        playerTank.transform.position = new Vector3();

        playerTank.Init(
            TankPartFactory.CreateBodyPart(100, new Vector2(50, 50)),
            TankPartFactory.CreateMainWeaponPart(playerTank, 50000, 1, 1, KeyCode.P, KeyCode.T, KeyCode.Y),
            TankPartFactory.CreateWheelPart(playerTank, KeyCode.W, KeyCode.S),
            TankPartFactory.CreateWheelPart(playerTank, KeyCode.I, KeyCode.K));

        MainCamera.GetComponent<ObjectFollower>().SetObjToFollow(playerTank.gameObject);
    }
}
