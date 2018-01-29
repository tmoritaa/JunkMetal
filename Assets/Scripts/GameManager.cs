﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class GameManager : MonoBehaviour 
{
    [SerializeField]
    private float mapWidth = 500;

    [SerializeField]
    private float mapHeight = 500;

    [SerializeField]
    private float tileDim = 25;

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
    private GameObject wallPrefab;

    [SerializeField]
    private Transform wallsRoot;

    [SerializeField]
    private GameObject canvasRoot;

    [SerializeField]
    private float debugMoveForce = 15000f;

    public Tank PlayerTank
    {
        get; private set;
    }

    public Tank AiTank
    {
        get; private set;
    }

    public TileMap Map
    {
        get; private set;
    }

    void Awake() {
        instance = this;

        PlayerTank = Instantiate(tankPrefab);
        PlayerTank.transform.SetParent(canvasRoot.transform, false);
        PlayerTank.transform.position = new Vector3(0, -600, 0);

        PlayerTank.Init(
            Tank.PlayerTypes.Human,
            TankPartFactory.CreateBodyPart(100, new Vector2(100, 50)),
            TankPartFactory.CreateEnginePart(debugMoveForce, 0.1f, 0.05f),
            TankPartFactory.CreateMainWeaponPart(PlayerTank, 50000, 1, 1, KeyCode.P, KeyCode.T, KeyCode.Y),
            TankPartFactory.CreateWheelPart(PlayerTank, KeyCode.W, KeyCode.S, KeyCode.I, KeyCode.K));

        AiTank = Instantiate(tankPrefab);
        AiTank.transform.SetParent(canvasRoot.transform, false);
        AiTank.transform.position = new Vector3(0, -300, 0);

        AiTank.Init(
            Tank.PlayerTypes.AI,
            TankPartFactory.CreateBodyPart(100, new Vector2(100, 50)),
            TankPartFactory.CreateEnginePart(debugMoveForce, 0.1f, 0.05f),
            TankPartFactory.CreateMainWeaponPart(AiTank, 50000, 1, 1, KeyCode.P, KeyCode.T, KeyCode.Y),
            TankPartFactory.CreateWheelPart(PlayerTank, KeyCode.W, KeyCode.S, KeyCode.I, KeyCode.K));

        MainCamera.GetComponent<ObjectFollower>().SetObjToFollow(AiTank.gameObject);

        generateMapBounds();
        generateTileMap();
    }

    private void generateTileMap() {
        // Note that this is temporary. Once map loading is implemented, we can just keep the walls generated in a list during map generation, and go through those.
        List<Transform> walls = new List<Transform>();
        for (int i = 0; i < wallsRoot.childCount; ++i) {
            walls.Add(wallsRoot.GetChild(i));
        }

        Map = new TileMap(mapWidth, mapHeight, tileDim, walls);
    }

    private void generateMapBounds() {
        const float defaultVal = 100;

        for (int x = -1; x <= 1; x += 2) {
            float xPos = x * mapWidth / 2f + x * defaultVal / 2f;

            GameObject wall = Instantiate(wallPrefab);
            wall.transform.SetParent(wallsRoot, false);

            wall.transform.localPosition = new Vector3(xPos, 0, 0);

            Vector2 size = new Vector2(defaultVal, mapHeight + defaultVal * 2f);

            wall.GetComponent<BoxCollider2D>().size = size;
            wall.GetComponent<RectTransform>().sizeDelta = size; 
        }

        for (int y = -1; y <= 1; y += 2) {
            float yPos = y * mapHeight / 2f + y * defaultVal / 2f;

            GameObject wall = Instantiate(wallPrefab);
            wall.transform.SetParent(wallsRoot, false);

            wall.transform.localPosition = new Vector3(0, yPos, 0);

            Vector2 size = new Vector2(mapWidth + defaultVal * 2f, defaultVal);

            wall.GetComponent<BoxCollider2D>().size = size;
            wall.GetComponent<RectTransform>().sizeDelta = size;
        }
    }
}
