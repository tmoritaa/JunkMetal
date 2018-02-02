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

    [SerializeField]
    private float mapWidth = 500;

    [SerializeField]
    private float mapHeight = 500;

    [SerializeField]
    private float tileDim = 25;

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

        TurretPart playerTurret = TankPartFactory.CreateTurretPart(PlayerTank, 1, 200,
            new Vector2[] { new Vector2(0, 1), new Vector2(0, -1) },
            new float[] { 100, 100 },
            KeyCode.T, KeyCode.Y);
        playerTurret.AddWeaponAtIdx(TankPartFactory.CreateWeaponPart(PlayerTank, 50000, 1, 500, 50, Bullet.BulletTypes.Normal, 10, KeyCode.P), 0);
        playerTurret.AddWeaponAtIdx(TankPartFactory.CreateWeaponPart(PlayerTank, 50000, 1, 100, 50, Bullet.BulletTypes.Normal, 10, KeyCode.O), 1);

        PlayerTank.Init(
            Tank.PlayerTypes.Human,
            TankPartFactory.CreateHullPart(100, new Vector2(50, 50), debugMoveForce, 250),
            playerTurret,
            TankPartFactory.CreateWheelPart(PlayerTank, 0.1f, 0.05f, 125, KeyCode.W, KeyCode.S, KeyCode.I, KeyCode.K));

        AiTank = Instantiate(tankPrefab);
        AiTank.transform.SetParent(canvasRoot.transform, false);
        AiTank.transform.position = new Vector3(0, -300, 0);

        TurretPart aiTurret = TankPartFactory.CreateTurretPart(PlayerTank, 1, 200,
            new Vector2[] { new Vector2(0, 1) },
            new float[] { 100 },
            KeyCode.T, KeyCode.Y);
        aiTurret.AddWeaponAtIdx(TankPartFactory.CreateWeaponPart(AiTank, 50000, 1, 500, 50, Bullet.BulletTypes.Normal, 10, KeyCode.P), 0);

        AiTank.Init(
            Tank.PlayerTypes.AI,
            TankPartFactory.CreateHullPart(100, new Vector2(50, 50), debugMoveForce, 300),
            aiTurret,
            TankPartFactory.CreateWheelPart(PlayerTank, 0.1f, 0.05f, 50, KeyCode.W, KeyCode.S, KeyCode.I, KeyCode.K));

        MainCamera.GetComponent<ObjectFollower>().SetObjToFollow(PlayerTank.gameObject);

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
        for (int x = -1; x <= 1; x += 2) {
            float xPos = x * mapWidth / 2f - x * tileDim / 2f;

            GameObject wall = Instantiate(wallPrefab);
            wall.transform.SetParent(wallsRoot, false);

            wall.transform.localPosition = new Vector3(xPos, 0, 0);

            Vector2 size = new Vector2(tileDim, mapHeight + tileDim * 2f);

            wall.GetComponent<BoxCollider2D>().size = size;
            wall.GetComponent<RectTransform>().sizeDelta = size; 
        }

        for (int y = -1; y <= 1; y += 2) {
            float yPos = y * mapHeight / 2f - y * tileDim / 2f;

            GameObject wall = Instantiate(wallPrefab);
            wall.transform.SetParent(wallsRoot, false);

            wall.transform.localPosition = new Vector3(0, yPos, 0);

            Vector2 size = new Vector2(mapWidth + tileDim * 2f, tileDim);

            wall.GetComponent<BoxCollider2D>().size = size;
            wall.GetComponent<RectTransform>().sizeDelta = size;
        }
    }
}
