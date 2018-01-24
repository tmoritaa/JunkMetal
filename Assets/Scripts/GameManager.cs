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
    private float debugMoveForce = 15000f;

    [SerializeField]
    private bool actuationDebugOn = true;
    public bool ActuationDebugOn
    {
        get {
            return actuationDebugOn;
        }
    }

    [SerializeField]
    private bool avoidWallsDebugOn = true;
    public bool AvoidWallsDebugOn
    {
        get {
            return avoidWallsDebugOn;
        }
    }

    [SerializeField]
    private Tank tankPrefab;

    [SerializeField]
    private GameObject canvasRoot;

    private Tank playerTank;
    private Tank aiTank;

    void Awake() {
        instance = this;

        playerTank = Instantiate(tankPrefab);
        playerTank.transform.SetParent(canvasRoot.transform, false);
        playerTank.transform.position = new Vector3(0, -100, 0);

        playerTank.Init(
            Tank.PlayerTypes.Human,
            TankPartFactory.CreateBodyPart(100, new Vector2(100, 50)),
            TankPartFactory.CreateEnginePart(debugMoveForce, 0.1f, 0.05f),
            TankPartFactory.CreateMainWeaponPart(playerTank, 50000, 1, 1, KeyCode.P, KeyCode.T, KeyCode.Y),
            TankPartFactory.CreateWheelPart(playerTank, KeyCode.W, KeyCode.S),
            TankPartFactory.CreateWheelPart(playerTank, KeyCode.I, KeyCode.K));

        aiTank = Instantiate(tankPrefab);
        aiTank.transform.SetParent(canvasRoot.transform, false);
        aiTank.transform.position = new Vector3();

        aiTank.Init(
            Tank.PlayerTypes.AI,
            TankPartFactory.CreateBodyPart(100, new Vector2(100, 50)),
            TankPartFactory.CreateEnginePart(debugMoveForce, 0.1f, 0.05f),
            TankPartFactory.CreateMainWeaponPart(aiTank, 50000, 1, 1, KeyCode.P, KeyCode.T, KeyCode.Y),
            TankPartFactory.CreateWheelPart(aiTank, KeyCode.W, KeyCode.S),
            TankPartFactory.CreateWheelPart(aiTank, KeyCode.I, KeyCode.K));

        MainCamera.GetComponent<ObjectFollower>().SetObjToFollow(aiTank.gameObject);
    }

    void Update() {
        if (Input.GetMouseButton(0)) {
            aiTank.TargetPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }
    }

    void OnDrawGizmos() {
        if (Application.isPlaying) {
            Color color = Gizmos.color;

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(aiTank.TargetPos, 30);

            Gizmos.color = color;
        }
    }
}
