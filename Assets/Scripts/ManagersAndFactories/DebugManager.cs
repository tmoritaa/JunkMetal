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

    void Awake() {
        instance = this;
    }

    void Update() {
        if (Input.GetMouseButton(0)) {
            GameManager.Instance.AiTank.DestPos = GameManager.Instance.MainCamera.ScreenToWorldPoint(Input.mousePosition);
        }
    }

    void OnDrawGizmos() {
        if (Application.isPlaying) {
            Color origColor = Gizmos.color;

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(GameManager.Instance.AiTank.DestPos, 30);

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

            Gizmos.color = Color.blue;
            
            foreach (Node node in GameManager.Instance.AiTank.Path) {
                Vector2 pos = GameManager.Instance.Map.NodeToPosition(node);
                Gizmos.DrawWireSphere(pos, 15);
            }

            Gizmos.color = origColor;
        }
    }
}
