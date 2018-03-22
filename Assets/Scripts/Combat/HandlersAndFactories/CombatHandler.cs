using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.SceneManagement;

public class CombatHandler : MonoBehaviour 
{
    private static CombatHandler instance;
    public static CombatHandler Instance
    {
        get {
            return instance;
        }
    }

    [SerializeField]
    private float mapWidth = 500;
    public float MapWidth
    {
        get {
            return mapWidth;
        }
    }

    [SerializeField]
    private float mapHeight = 500;
    public float MapHeight
    {
        get {
            return mapHeight;
        }
    }

    [SerializeField]
    private float tileDim = 25;
    public float TileDim
    {
        get {
            return tileDim;
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
    private DeathScreen deathScreen;

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

    public bool DisableMovement
    {
        get; private set;
    }

    void Awake() {
        instance = this;

        deathScreen.gameObject.SetActive(false);

        List<Transform> walls = generateMapBounds();

        HumanTankController = Instantiate(humanTankContPrefab, tankRoot, false);
        HumanTankController.Init(
            new Vector3(300, -800, 0),
            0,
            PlayerManager.Instance.TankSchematic);

        Dictionary<string, object> data = DataPasser.Instance.RetrieveData();
        TankSchematic enemyTankSchem = ((EnemyInfo)data["Opponent"]).TankSchem;
        AITankController = Instantiate(aiTankContPrefab, tankRoot, false);
        AITankController.Init(
            new Vector3(300, -600, 0),
            180f,
            enemyTankSchem,
            HumanTankController.SelfTank,
            walls);
        
        MainCamera.GetComponent<ObjectFollower>().SetObjToFollow(HumanTankController.SelfTank.gameObject);

        DisableMovement = false;
    }

    public void DeathOccurred(Tank diedTank) {
        deathScreen.gameObject.SetActive(true);

        Tank wonTank = (diedTank == AITankController.SelfTank) ? HumanTankController.SelfTank : AITankController.SelfTank;
        deathScreen.SetupDeathScreen(wonTank);
        DisableMovement = true;
    }

    public void ReturnToMainScreen() {
        SceneManager.LoadScene("Main");
    }

    private List<Transform> generateMapBounds() {
        List<Transform> walls = new List<Transform>();

        float segmentWidth = mapWidth / 3f;
        float segmentHeight = mapHeight / 3f;

        for (int x = -1; x <= 1; x += 2) {
            float xPos = x * mapWidth / 2f - x * tileDim / 2f;

            GameObject wall = Instantiate(wallPrefab, wallsRoot, false);
            wall.transform.localPosition = new Vector3(xPos, 0, 0);

            Vector2 size = new Vector2(tileDim, segmentHeight + tileDim * 2f);

            wall.GetComponent<BoxCollider2D>().size = size;
            wall.GetComponent<RectTransform>().sizeDelta = size;

            walls.Add(wall.transform);
        }

        for (int y = -1; y <= 1; y += 2) {
            float yPos = y * mapHeight / 2f - y * tileDim / 2f;

            GameObject wall = Instantiate(wallPrefab, wallsRoot, false);
            wall.transform.localPosition = new Vector3(0, yPos, 0);

            Vector2 size = new Vector2(segmentWidth + tileDim * 2f, tileDim);

            wall.GetComponent<BoxCollider2D>().size = size;
            wall.GetComponent<RectTransform>().sizeDelta = size;

            walls.Add(wall.transform);
        }

        float diagHeight = tileDim;
        float diagWidth = Mathf.Sqrt(segmentWidth * segmentWidth + segmentHeight * segmentHeight) - tileDim;
        for (int y = -1; y <= 1; y += 2) {
            for (int x = -1; x <= 1; x += 2) {
                Vector2 corner = new Vector2(x * segmentWidth / 2f, y * mapHeight / 2f);
                Vector2 otherCorner = new Vector2(x * mapWidth / 2f, y * segmentHeight / 2f);

                Vector2 centerPt = (corner + otherCorner) / 2f;

                GameObject wall = Instantiate(wallPrefab, wallsRoot, false);
                wall.transform.localPosition = centerPt;

                Vector2 size = new Vector2(diagWidth, diagHeight);

                wall.GetComponent<BoxCollider2D>().size = size;
                wall.GetComponent<RectTransform>().sizeDelta = size;

                if (x == y) {
                    wall.transform.Rotate(new Vector3(0, 0, -45f));
                } else {
                    wall.transform.Rotate(new Vector3(0, 0, 45f));
                }

                walls.Add(wall.transform);
            }
        }

        return walls;
    }
}
