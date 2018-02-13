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
    private GameObject wallPrefab;

    [SerializeField]
    private Transform wallsRoot;

    [SerializeField]
    private Transform canvasRoot;

    [SerializeField]
    private Transform tankRoot;

    [SerializeField]
    private HumanTankController humanTankContPrefab;

    [SerializeField]
    private AITankController aiTankContPrefab;

    public HumanTankController HumanTankController
    {
        get; private set;
    }

    public AITankController AITankController
    {
        get; private set;
    }

    public TileMap Map
    {
        get; private set;
    }

    void Awake() {
        instance = this;

        generateMapBounds();
        generateTileMap();

        TurretPart playerTurret = new TurretPart(PartsManager.Instance.GetPartFromName<TurretPartSchematic>("Basic Turret"));
        playerTurret.AddWeaponAtIdx(new WeaponPart(PartsManager.Instance.GetPartFromName<WeaponPartSchematic>("Basic Weapon1")), 0);
        playerTurret.AddWeaponAtIdx(new WeaponPart(PartsManager.Instance.GetPartFromName<WeaponPartSchematic>("Basic Weapon2")), 1);

        HumanTankController = Instantiate(humanTankContPrefab, tankRoot, false);
        HumanTankController.Init(
            new Vector3(100, -550, 0),
            new HullPart(PartsManager.Instance.GetPartFromName<HullPartSchematic>("Basic Hull")),
            playerTurret,
            new WheelPart(PartsManager.Instance.GetPartFromName<WheelPartSchematic>("Basic Wheels")));

        TurretPart aiTurret = new TurretPart(PartsManager.Instance.GetPartFromName<TurretPartSchematic>("Basic Turret"));
        aiTurret.AddWeaponAtIdx(new WeaponPart(PartsManager.Instance.GetPartFromName<WeaponPartSchematic>("Basic Weapon1")), 0);

        AITankController = Instantiate(aiTankContPrefab, tankRoot, false);
        AITankController.Init(
            new Vector3(0, -600, 0),
            new HullPart(PartsManager.Instance.GetPartFromName<HullPartSchematic>("Basic Hull")),
            aiTurret,
            new WheelPart(PartsManager.Instance.GetPartFromName<WheelPartSchematic>("Basic Wheels")));
        
        MainCamera.GetComponent<ObjectFollower>().SetObjToFollow(HumanTankController.Tank.gameObject);
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
