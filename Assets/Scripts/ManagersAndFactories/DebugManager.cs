using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class DebugManager : MonoBehaviour 
{
    private static DebugManager instance;
    public static DebugManager Instance
    {
        get {
            return instance;
        }
    }

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
    private bool mapDisplayDebugOn = true;

    void Awake() {
        instance = this;
    }

    void Update() {
        if (Input.GetMouseButton(0)) {
            GameManager.Instance.AITankController.DestPos = GameManager.Instance.MainCamera.ScreenToWorldPoint(Input.mousePosition);
        }
    }

    void OnDrawGizmos() {
        if (Application.isPlaying) {
            Color origColor = Gizmos.color;

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(GameManager.Instance.AITankController.DestPos, 30);

            if (mapDisplayDebugOn) {
                // Draw Map
                TileMap map = GameManager.Instance.Map;
                for (int x = 0; x < map.Cols; ++x) {
                    for (int y = 0; y < map.Rows; ++y) {
                        char value = map.GetValueAtIdx(x, y);
                        Vector2 pos = map.IdxToPosition(x, y);

                        Gizmos.color = value == 0 ? Color.green : Color.red;
                        Gizmos.DrawWireCube(pos, new Vector3(map.TileDim, map.TileDim, map.TileDim));
                    }
                }

                if (GameManager.Instance.AITankController.CurGoal.GetType() == typeof(SearchGoal)) {
                    Gizmos.color = Color.blue;

                    SearchGoal goal = (SearchGoal)GameManager.Instance.AITankController.CurGoal;

                    // TODO: a bit hacky right now. Maybe we can clean this up once we have blackboards.
                    List<Node> path = goal.Path;

                    foreach (Node node in path) {
                        Vector2 pos = GameManager.Instance.Map.NodeToPosition(node);
                        Gizmos.DrawWireSphere(pos, 15);
                    }

                    TileMap searchQuads = goal.SearchQuadrants;

                    foreach (Node node in searchQuads.Map) {
                        if (node.value == 0) {
                            Gizmos.color = Color.blue;
                        } else {
                            Gizmos.color = Color.red;
                        }

                        Gizmos.DrawWireCube(searchQuads.NodeToPosition(node), new Vector3(searchQuads.TileDim, searchQuads.TileDim, searchQuads.TileDim));
                    }
                }
            }

            Gizmos.color = origColor;
        }
    }
}
